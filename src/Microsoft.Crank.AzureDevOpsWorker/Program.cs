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
        private static TimeSpan TaskLogFeedDelay = TimeSpan.FromSeconds(2);

        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var connectionStringOption = app.Option("-c|--connection-string <string>", "The Azure Service Bus connection string. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            var queueOption = app.Option("-q|--queue <string>", "The Azure Service Bus queue name. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();

            app.OnExecuteAsync(async cancellationToken =>
            {
                var connectionString = connectionStringOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(connectionString)))
                {
                    connectionString = Environment.GetEnvironmentVariable(connectionString);
                }

                var queue = queueOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(queue)))
                {
                    queue = Environment.GetEnvironmentVariable(queue);
                }

                await ProcessAzureQueue(connectionString, queue);
            });

            return app.Execute(args);
        }

        private static async Task ProcessAzureQueue(string connectionString, string queue)
        {
            var client = new ServiceBusClient(connectionString);

            var processor = client.CreateProcessor(queue, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1, // Process one message at a time
                MaxAutoLockRenewalDuration = TimeSpan.FromHours(1) // Maintaining the lock for as much as a job should run 
            });

            // Whenever a message is available on the queue
            processor.ProcessMessageAsync += MessageHandler;

            processor.ProcessErrorAsync += ErrorHandler;

            await processor.StartProcessingAsync();

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            Console.WriteLine($"{LogNow} Processing message '{args.Message}'");

            var message = args.Message;

            JobPayload jobPayload;
            DevopsMessage devopsMessage = null;
            Job driverJob = null;

            try
            {
                // The DevopsMessage does the communications with AzDo
                devopsMessage = new DevopsMessage(message);

                // The Body contains the parameters for the application to run
                // We can't use message.Body.FromObjectAsJson since the raw json returned by AzDo is not valid
                jobPayload = JobPayload.Deserialize(message.Body.ToArray());

                // The SendTaskStartedEventAsync message seems to always succeed even if the job on AzDo is canceled (the message in the queue is old)
                // Hence we use SendTaskLogFeedsAsync as it will fail correctly if there is no need to run crank 
                var success = await devopsMessage.SendTaskLogFeedsAsync("Job starting ...");

                if (!success)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{LogNow} SendTaskLogFeedsAsync failed. The corresponding job is probably canceled on AzDo, skipping ...");
                    Console.ResetColor();
                }
                else
                {
                    // Inform AzDo that the job is started
                    await devopsMessage.SendTaskStartedEventAsync();

                    var arguments = String.Join(' ', jobPayload.Args);

                    Console.WriteLine($"{LogNow} Invoking crank with arguments: {arguments}");

                    // The DriverJob manages the application's lifetime and standard output
                    driverJob = new Job("crank", arguments);

                    driverJob.OnStandardOutput = log => Console.WriteLine(log);

                    driverJob.Start();

                    var azDoFailures = 0;

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
                            success = await devopsMessage.SendTaskLogFeedsAsync(String.Join("\r\n", logs));

                            if (!success)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"{LogNow} SendTaskLogFeedsAsync failed. If the task was canceled, this jobs should be stopped.");
                                Console.ResetColor();

                                azDoFailures++;

                                if (azDoFailures > 1)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine($"{LogNow} Halting job");
                                    Console.ResetColor();

                                    driverJob.Stop();
                                }
                            }
                        }

                        await Task.Delay(TaskLogFeedDelay);
                    }

                    // Mark the task as completed
                    await devopsMessage.SendTaskCompletedEventAsync(succeeded: driverJob.WasSuccessful);

                    // Create a task log entry
                    var taskLogObjectString = await devopsMessage?.CreateTaskLogAsync();

                    if (String.IsNullOrEmpty(taskLogObjectString))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"{LogNow} CreateTaskLogAsync failed. The job is probably canceled.");
                        Console.ResetColor();
                    }
                    else
                    {
                        var taskLogObject = JsonSerializer.Deserialize<Dictionary<string, object>>(taskLogObjectString);

                        var taskLogId = taskLogObject["id"].ToString();

                        await devopsMessage?.AppendToTaskLogAsync(taskLogId, driverJob.OutputBuilder.ToString());

                        // Attach task log to the timeline record
                        await devopsMessage?.UpdateTaskTimelineRecordAsync(taskLogObjectString);
                    }

                    // Mark the message as completed
                    await args.CompleteMessageAsync(message);

                    driverJob.Stop();
                }

                Console.WriteLine($"{LogNow} Job completed");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LogNow} Job failed: {e}");

                Console.WriteLine("Stopping the task and releasing the message...");

                try
                {
                    await devopsMessage?.SendTaskCompletedEventAsync(succeeded: false);
                }
                catch (Exception f)
                {
                    Console.WriteLine($"{LogNow} Failed to complete the task: {f}");
                }

                try
                {
                    // TODO: Should the message still be copmleted instead of abandonned?
                    await args.AbandonMessageAsync(message);
                }
                catch (Exception f)
                {
                    Console.WriteLine($"{LogNow} Failed to abandon the message: {f}");
                }
            }
            finally
            {
                try
                {
                    driverJob?.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{LogNow} Failed to dispose the job : {e}");
                }
            }
        }

        private static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"{LogNow} Process error: {args.Exception}");

            return Task.CompletedTask;
        }

        private static string LogNow => $"[{DateTime.Now.ToString("hh:mm:ss.fff")}]";
    }
}
