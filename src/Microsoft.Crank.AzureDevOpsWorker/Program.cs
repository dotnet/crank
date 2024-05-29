// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Esprima;
using Jint;
using McMaster.Extensions.CommandLineUtils;
using System.Runtime.ConstrainedExecution;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public class Program
    {
        private static TimeSpan TaskLogFeedDelay = TimeSpan.FromSeconds(2);
        private static Engine Engine = new Engine();
        private static bool Verbose = false;

        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var connectionStringOption = app.Option("-c|--connection-string <string>", "The Azure Service Bus connection string. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            var queueOption = app.Option("-q|--queue <string>", "The Azure Service Bus queue name. Can be an environment variable name.", CommandOptionType.SingleValue).IsRequired();
            var certPath = app.Option("--cert-path", "Path to a certificate.", CommandOptionType.SingleValue);
            var certThumbprint = app.Option("--cert-thumbprint", "Thumbprint of the certificate being used.", CommandOptionType.SingleValue);
            var certTenantId = app.Option("--cert-tenant-id", "Tenant id for service principal used as part of cert based auth.", CommandOptionType.SingleValue);
            var certClientId = app.Option("--cert-client-id", "Client id for service principal used as part of cert based auth.", CommandOptionType.SingleValue);
            var verboseOption = app.Option("-v|--verbose", "Display verbose log.", CommandOptionType.NoValue);

            app.OnExecuteAsync(async cancellationToken =>
            {
                var connectionString = connectionStringOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(connectionString)))
                {
                    connectionString = Environment.GetEnvironmentVariable(connectionString);
                }

                Verbose = verboseOption.HasValue();

                var queue = queueOption.Value();

                // Substitute with ENV value if it exists
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(queue)))
                {
                    queue = Environment.GetEnvironmentVariable(queue);
                }

                if ((certClientId.HasValue() || certTenantId.HasValue() || certThumbprint.HasValue() || certPath.HasValue()) && (!(certClientId.HasValue() && certTenantId.HasValue()) && (certThumbprint.HasValue() || certPath.HasValue())))
                {
                    Console.WriteLine("If using cert based auth, must provide client id, tenant id, and either a thumbprint or certificate path.");
                    return;
                }

                CertificateOptions certificateOptions = null;

                if(certClientId.HasValue())
                {
                    certificateOptions = new CertificateOptions(certClientId.Value(), certTenantId.Value(), certThumbprint.Value(), certPath.Value());
                }

                await ProcessAzureQueue(connectionString, queue, certificateOptions);
            });

            return app.Execute(args);
        }

        private static async Task ProcessAzureQueue(string connectionString, string queue, CertificateOptions certificateOptions = null)
        {
            ClientCertificateCredential ccc = null;

            if (certificateOptions != null)
            {
                X509Store store = null;
                if (!String.IsNullOrEmpty(certificateOptions.Path))
                {
                    ccc = new ClientCertificateCredential(certificateOptions.TenantId, certificateOptions.ClientId, certificateOptions.Path);
                }
                else
                {
                    foreach (var storeName in Enum.GetValues<StoreName>())
                    {
                        store = new X509Store(storeName, StoreLocation.LocalMachine);
                        store.Open(OpenFlags.ReadOnly);
                        var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, certificateOptions.Thumbprint, true).First();
                        ccc = new ClientCertificateCredential(certificateOptions.TenantId, certificateOptions.ClientId, certificate);
                    }
                }
            }

            ServiceBusClient client = null;
            if (certificateOptions != null)
            {
                client = new ServiceBusClient(connectionString, ccc);
            }
            else
            {
                client = new ServiceBusClient(connectionString);
            }

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
                var bodyArray = message.Body.ToArray();
                jobPayload = JobPayload.Deserialize(bodyArray);

                if (Verbose)
                {
                    Console.WriteLine($"{LogNow} Payload (Base64): {Convert.ToBase64String(bodyArray)}");
                }

                // The only way to detect if a Task still needs to be executed is to download all the details of all tasks (there is no API to retrieve the status of a single task.

                var records = await devopsMessage.GetRecordsAsync();

                if (records == null)
                {
                    await devopsMessage?.SendTaskCompletedEventAsync(DevopsMessage.ResultTypes.Skipped);

                    Console.WriteLine($"{LogNow} Could not retrieve Records, skipping...");

                    // Release the message for further processing
                    await args.AbandonMessageAsync(message);
                    return;
                }

                var record = records.Value.FirstOrDefault(x => x.Id == devopsMessage.TaskInstanceId);

                if (record != null && record.State == "completed")
                {
                    Console.WriteLine($"{LogNow} Job is completed ({record.Result}), skipping...");

                    // Mark the message as completed
                    await args.CompleteMessageAsync(message);
                }
                else 
                {
                    if (!String.IsNullOrWhiteSpace(jobPayload.Condition))
                    {
                        try
                        {
                            var condition = Engine.Evaluate(jobPayload.Condition).AsBoolean();

                            if (!condition)
                            {
                                await devopsMessage?.SendTaskCompletedEventAsync(DevopsMessage.ResultTypes.Skipped);

                                Console.WriteLine($"{LogNow} Job skipped based on condition [{jobPayload.Condition}]");

                                // Mark the message as completed
                                await args.CompleteMessageAsync(message);
                                return;
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"{LogNow} Could not evaluate condition [{jobPayload.Condition}], ignoring ...");
                        }
                    }

                    // Inform AzDo that the job is started
                    await devopsMessage.SendTaskStartedEventAsync();

                    var arguments = String.Join(' ', jobPayload.Args);

                    Console.WriteLine($"{LogNow} Invoking crank with arguments: {arguments}");
                    
                    if (Verbose)
                    {
                        Console.WriteLine($"{LogNow} Invoking crank with timeout: {jobPayload.Timeout}");
                    }

                    var retries = 0;

                    do
                    {
                        if (retries > 0)
                        {
                            Console.WriteLine($"{LogNow} Job failed, attempt ({retries + 1} out of {jobPayload.Retries + 1}).");
                        }

                        // The DriverJob manages the application's lifetime and standard output
                        driverJob = new Job("crank", arguments);

                        driverJob.OnStandardOutput = log => Console.WriteLine(log);

                        driverJob.Start();

                        // Pump application standard output while it's running
                        while (driverJob.IsRunning)
                        {
                            var logs = driverJob.FlushStandardOutput().ToList();

                            // Has the job run for too long?
                            if ((DateTime.UtcNow - driverJob.StartTimeUtc) > jobPayload.Timeout)
                            {
                                var timeoutMessage = $"{LogNow} Job timed out ({jobPayload.Timeout}). The timeout can be increased in the payload message.";

                                Console.WriteLine(timeoutMessage);
                                logs.Add(timeoutMessage);

                                driverJob.Stop();
                            }

                            // Send any page of logs to the AzDo task log feed
                            if (logs.Any())
                            {
                                var success = await devopsMessage.SendTaskLogFeedsAsync(String.Join("\r\n", logs));

                                if (!success)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($"{LogNow} SendTaskLogFeedsAsync failed. If the task was canceled, this jobs should be stopped.");
                                    Console.ResetColor();
                                }
                            }

                            // Check if task is still active (not canceled)

                            records = await devopsMessage.GetRecordsAsync();

                            // This can return a stale value (see DevopsMessage.RecordsCacheTimeSpan)

                            record = records.Value.FirstOrDefault(x => x.Id == devopsMessage.TaskInstanceId);

                            if (record != null && record?.State == "completed")
                            {
                                Console.WriteLine($"{LogNow} Job is completed ({record.Result}), interrupting...");

                                driverJob.Stop();
                            }
                            else
                            {
                                await Task.Delay(TaskLogFeedDelay);
                            }
                        }

                        retries++;
                    }
                    while (!driverJob.WasSuccessful && jobPayload.Retries >= retries);

                    // Mark the task as completed
                    await devopsMessage.SendTaskCompletedEventAsync(driverJob.WasSuccessful ? DevopsMessage.ResultTypes.Succeeded : DevopsMessage.ResultTypes.Failed);

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
                    
                    Console.WriteLine($"{LogNow} Job completed");
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LogNow} Job failed: {e}");

                Console.WriteLine("Stopping the task and releasing the message...");

                try
                {
                    await devopsMessage?.SendTaskCompletedEventAsync(DevopsMessage.ResultTypes.SucceededWithIssues);
                }
                catch (Exception f)
                {
                    Console.WriteLine($"{LogNow} Failed to complete the task: {f}");
                }

                try
                {
                    // TODO: Should the message still be completed instead of abandoned?
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
