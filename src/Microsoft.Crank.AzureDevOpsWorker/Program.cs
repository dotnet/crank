// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using McMaster.Extensions.CommandLineUtils;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public class Program
    {
        private static readonly TimeSpan TaskLogFeedDelay = TimeSpan.FromSeconds(2);
        private static ServiceBusClient? s_client = null!;
        private static ServiceBusProcessor? s_processer = null!;

        public static Task<int> Main(string[] args)
        {
            using var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var connectionStringOption = app.Option("-c|--connection-string <string>", "The Azure Service Bus connection string. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            var queueOption = app.Option("-q|--queue <string>", "The Azure Service Bus queue name. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();

            app.OnExecuteAsync(async cancellationToken =>
            {
                // Substitute with ENV value if it exists
                var connectionString = connectionStringOption.Value();
                _ = connectionString!.TryGetEnvironmentVariableValue(out connectionString);

                // Substitute with ENV value if it exists
                var queue = queueOption.Value();
                _ = queue!.TryGetEnvironmentVariableValue(out queue);
                

                await ProcessAzureQueueAsync(connectionString, queue);
            });

            return app.ExecuteAsync(args);
        }

        private static async Task ProcessAzureQueueAsync(string connectionString, string queue)
        {
            s_client = new ServiceBusClient(connectionString);
            s_processer = s_client.CreateProcessor(queue, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1, // Process one message at a time
                MaxAutoLockRenewalDuration = TimeSpan.FromHours(1) // Maintaining the lock for as much as a job should run 
            });

            // Whenever a message is available on the queue
            s_processer.ProcessMessageAsync += MessageHandler;
            s_processer.ProcessErrorAsync += ErrorHandler;

            await s_processer.StartProcessingAsync();

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            Console.WriteLine($"{LogNow} Processing message '{args.Message}'");

            var message = args.Message;

            DevopsMessage devopsMessage = null!;
            Job driverJob = null!;

            try
            {
                // The DevopsMessage does the communications with AzDo
                devopsMessage = new DevopsMessage(message);

                // The Body contains the parameters for the application to run
                // We can't use message.Body.FromObjectAsJson since the raw json returned by AzDo is not valid
                var jobPayload = JobPayload.Deserialize(message.Body.ToArray());

                // The only way to detect if a Task still needs to be executed is to download all the details of all tasks (there is no API to retrieve the status of a single task.

                var records = await devopsMessage.GetRecordsAsync();

                if (records == null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{LogNow} Could not retrieve records...");
                    Console.ResetColor();

                    return;
                }

                var record = records.Value.FirstOrDefault(x => x.Id == devopsMessage.TaskInstanceId);
                if (record is { State: "completed" })
                {
                    Console.WriteLine($"{LogNow} Job is completed ({record.Result}), skipping...");

                    // Mark the message as completed
                    await args.CompleteMessageAsync(message);
                }
                else 
                {
                    // Inform AzDo that the job is started
                    await devopsMessage.SendTaskStartedEventAsync();

                    var arguments = string.Join(' ', jobPayload.Args);

                    Console.WriteLine($"{LogNow} Invoking crank with arguments: {arguments}");

                    // The DriverJob manages the application's lifetime and standard output
                    driverJob = new Job("crank", arguments)
                    {
                        OnStandardOutput = log => Console.WriteLine(log)
                    };

                    driverJob.Start();

                    // Pump application standard output while it's running
                    while (driverJob.IsRunning)
                    {
                        if ((DateTime.UtcNow - driverJob.StartTimeUtc) > jobPayload.Timeout)
                        {
                            throw new Exception($"{LogNow} Job timed out ({jobPayload.Timeout}). The timeout can be increased in the queued message.");
                        }

                        var logs = driverJob.FlushStandardOutput().ToArray();

                        // Send any page of logs to the AzDo task log feed
                        if (logs.Any())
                        {
                            var success = await devopsMessage.SendTaskLogFeedsAsync(string.Join(Environment.NewLine, logs));

                            if (!success)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"{LogNow} SendTaskLogFeedsAsync failed. If the task was canceled, this jobs should be stopped.");
                                Console.ResetColor();

                                driverJob.Stop();
                            }
                        }

                        // Check if task is still active (not canceled)

                        records = await devopsMessage.GetRecordsAsync();
                        record = records?.Value.FirstOrDefault(x => x.Id == devopsMessage.TaskInstanceId);

                        if (record is { State: "completed" })
                        {
                            Console.WriteLine($"{LogNow} Job is completed ({record.Result}), interrupting...");

                            driverJob.Stop();
                        }

                        await Task.Delay(TaskLogFeedDelay);
                    }

                    // Mark the task as completed
                    await devopsMessage.SendTaskCompletedEventAsync(succeeded: driverJob.WasSuccessful);

                    // Create a task log entry
                    var taskLogObjectString = await devopsMessage.CreateTaskLogAsync();

                    if (string.IsNullOrEmpty(taskLogObjectString))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"{LogNow} CreateTaskLogAsync failed. The job is probably canceled.");
                        Console.ResetColor();
                    }
                    else
                    {
                        var taskLogObject = JsonSerializer.Deserialize<Dictionary<string, object>>(taskLogObjectString);
                        var taskLogId = taskLogObject?["id"].ToString();

                        if (devopsMessage is { } && taskLogId is not null)
                        {
                            await devopsMessage.AppendToTaskLogAsync(
                                taskLogId, driverJob.OutputBuilder?.ToString() ?? "No output message.");

                            // Attach task log to the timeline record
                            await devopsMessage.UpdateTaskTimelineRecordAsync(taskLogObjectString);
                        }
                    }

                    // Mark the message as completed
                    await args.CompleteMessageAsync(message);

                    driverJob.Stop();
                    
                    Console.WriteLine($"{LogNow} Job completed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LogNow} Job failed: {e}");

                Console.WriteLine("Stopping the task and releasing the message...");

                try
                {
                    if (devopsMessage is { })
                        await devopsMessage.SendTaskCompletedEventAsync(succeeded: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{LogNow} Failed to complete the task: {ex}");
                }

                try
                {
                    // TODO: Should the message still be copmleted instead of abandonned?
                    await args.AbandonMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{LogNow} Failed to abandon the message: {ex}");
                }
            }
            finally
            {
                try
                {
                    driverJob?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{LogNow} Failed to dispose the job : {ex}");
                }
            }
        }

        private static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"{LogNow} Process error: {args.Exception}");

            return Task.CompletedTask;
        }

        private static string LogNow => $"[{DateTime.Now:hh:mm:ss.fff}]";
    }
}
