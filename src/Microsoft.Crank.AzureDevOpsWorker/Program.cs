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

        private static string ConnectionString { get; set; }
        private static string Queue { get; set; }

        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var connectionStringOption = app.Option("-c|--connection-string <string>", "The Azure Service Bus connection string. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            var queueOption = app.Option("-q|--queue <string>", "The Azure Service Bus queue name. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            
            app.OnExecuteAsync(async cancellationToken =>
            {
                ConnectionString = connectionStringOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ConnectionString)))
                {
                    ConnectionString = Environment.GetEnvironmentVariable(ConnectionString);
                }

                Queue = queueOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(Queue)))
                {
                    Queue = Environment.GetEnvironmentVariable(Queue);
                }

                var client = new ServiceBusClient(ConnectionString);
                var processor = client.CreateProcessor(Queue, new ServiceBusProcessorOptions
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 1, // Process one message at a time
                });

                // Whenever a message is available on the queue
                processor.ProcessMessageAsync += async args =>
                {
                    Console.WriteLine("Processing message '{0}'", args.Message.ToString());

                    var message = args.Message;

                    JobPayload jobPayload;
                    DevopsMessage devopsMessage = null;
                    Job driverJob = null;

                    try
                    {
                        // The DevopsMessage does the communications with AzDo
                        devopsMessage = new DevopsMessage(message);

                        // The Body contains the parameters for the application to run
                        jobPayload = JobPayload.Deserialize(message.Body.ToBytes().ToArray());

                        await devopsMessage.SendTaskStartedEventAsync();

                        var arguments = String.Join(' ', jobPayload.Args);

                        Console.WriteLine("Invoking application with arguments: " + arguments);

                        // The DriverJob manages the application's lifetime and standard output
                        driverJob = new Job("crank.exe", arguments);

                        driverJob.OnStandardOutput = log => Console.WriteLine(log);

                        Console.WriteLine("Processing...");

                        driverJob.Start();

                        // Pump application standard output while it's running
                        while (driverJob.IsRunning)
                        {
                            if ((DateTime.UtcNow - driverJob.StartTimeUtc) > jobPayload.Timeout)
                            {
                                throw new Exception("Job timed out");
                            }

                            var logs = driverJob.FlushStandardOutput().ToArray();

                            // Send any page of logs to the AzDo task log feed
                            if (logs.Any())
                            {
                                await devopsMessage.SendTaskLogFeedsAsync(String.Join("\r\n", logs));
                            }

                            await Task.Delay(TaskLogFeedDelay);
                        }

                        // Mark the task as completed
                        await devopsMessage.SendTaskCompletedEventAsync(succeeded: driverJob.WasSuccessful);

                        // Create a task log entry
                        var taskLogObjectString = await devopsMessage?.CreateTaskLogAsync();

                        var taskLogObject = JsonSerializer.Deserialize<Dictionary<string, object>>(taskLogObjectString);

                        var taskLogId = taskLogObject["id"].ToString();

                        await devopsMessage?.AppendToTaskLogAsync(taskLogId, driverJob.OutputBuilder.ToString());

                        // Attach task log to the timeline record
                        await devopsMessage?.UpdateTaskTimelineRecordAsync(taskLogObjectString);

                        // Mark the message as completed
                        await args.CompleteMessageAsync(message);

                        driverJob.Stop();

                        Console.WriteLine("Job completed");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Job failed: " + e.Message);

                        try
                        {
                            await devopsMessage?.SendTaskCompletedEventAsync(succeeded: false);
                            await args.AbandonMessageAsync(message);
                        }
                        catch (Exception f)
                        {
                            Console.WriteLine("Failed to abandon task: " + f.Message);
                        }
                    }
                    finally
                    {
                        driverJob?.Dispose();
                    }
                };

                processor.ProcessErrorAsync += args =>
                {
                    Console.WriteLine("Process error: " + args.Exception.ToString());

                    return Task.CompletedTask;
                };

                await processor.StartProcessingAsync();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            });

            return app.Execute(args);
        }
    }
}
