// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Jint;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Crank.Models.Security;

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
            var certClientId = app.Option("--cert-client-id", "Client id for service principal used as part of cert based auth.", CommandOptionType.SingleValue);
            var certTenantId = app.Option("--cert-tenant-id", "Tenant id for service principal used as part of cert based auth.", CommandOptionType.SingleValue);
            var certThumbprint = app.Option("--cert-thumbprint", "Thumbprint of the certificate being used.", CommandOptionType.SingleValue);
            var certPath = app.Option("--cert-path", "Path to a certificate.", CommandOptionType.SingleValue);
            var certPassword = app.Option("--cert-pwd", "Password of the certificate to be used for auth.", CommandOptionType.SingleValue);
            var certSniAuth = app.Option("--cert-sni", "Enable subject name / issuer based authentication (SNI).", CommandOptionType.NoValue);
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

                CertificateOptions certificateOptions = null;

                if (certThumbprint.HasValue() || certPath.HasValue())
                {
                    if (!certClientId.HasValue() || !certTenantId.HasValue())
                    {
                        Console.WriteLine("If using cert based auth, must provide client id, tenant id, and either a thumbprint or certificate path.");
                    }

                    certificateOptions = new CertificateOptions(certClientId.Value(), certTenantId.Value(), certThumbprint.Value(), certPath.Value(), certPassword.Value(), certSniAuth.HasValue());
                }

                await ProcessAzureQueue(connectionString, queue, certificateOptions);
            });

            return app.Execute(args);
        }

        private static async Task ProcessAzureQueue(string connectionString, string queue, CertificateOptions certificateOptions)
        {
            ServiceBusClient client;

            if (certificateOptions != null)
            {
                var clientCertificateCredentials = certificateOptions.GetClientCertificateCredential();

                if (clientCertificateCredentials == null)
                {
                    throw new ApplicationException($"The requested certificate could not be found: {certificateOptions.Path ?? certificateOptions.Thumbprint}");
                }

                client = new ServiceBusClient(connectionString, clientCertificateCredentials);
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
                    // Truncate verbose payload logging to avoid huge console output
                    var payloadB64 = Convert.ToBase64String(bodyArray);
                    var max = 2048;
                    Console.WriteLine($"{LogNow} Payload (Base64, len={payloadB64.Length}): {payloadB64.Substring(0, Math.Min(payloadB64.Length, max))}{(payloadB64.Length > max ? "...(truncated)" : "")}");
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

                        // Create a per-attempt temp working directory
                        var workingDirectory = CreateTempWorkingDirectory();
                        Console.WriteLine($"{LogNow} Created temp working directory: {workingDirectory}");

                        // Save the job payload files to disk (extracted step)
                        MaterializeFiles(jobPayload, workingDirectory);

                        // The DriverJob manages the application's lifetime and standard output
                        driverJob = new Job("crank", arguments, workingDirectory);

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

                        // Attempt-specific cleanup of the temp working directory
                        TryDeleteDirectory(workingDirectory);

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

        internal static void MaterializeFiles(JobPayload jobPayload, string workingDirectory)
        {
            if (jobPayload?.Files == null || jobPayload.Files.Count == 0)
            {
                return;
            }

            var baseDir = Path.GetFullPath(string.IsNullOrEmpty(workingDirectory)
                ? Directory.GetCurrentDirectory()
                : workingDirectory);

            // Ensure base working directory exists
            Directory.CreateDirectory(baseDir);

            foreach (var file in jobPayload.Files)
            {
                var relativePath = file.Key?.Replace('\\', '/');
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    Console.WriteLine($"{LogNow} Skipping empty file path entry.");
                    continue;
                }

                var targetPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

                // Ensure targetPath stays within baseDir to prevent path traversal
                var normalizedBaseDir = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Allow baseDir itself (e.g., "./") and any child path
                var baseWithSep = normalizedBaseDir + Path.DirectorySeparatorChar;
                if (!(normalizedTarget.Equals(normalizedBaseDir, StringComparison.OrdinalIgnoreCase) ||
                      normalizedTarget.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"{LogNow} Skipping unsafe path: {relativePath}");
                    continue;
                }

                var parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                try
                {
                    var fileContent = Convert.FromBase64String(file.Value ?? string.Empty);
                    File.WriteAllBytes(targetPath, fileContent);
                }
                catch (FormatException)
                {
                    Console.WriteLine($"{LogNow} Invalid base64 content for file: {relativePath}. Skipping. Content: {file.Value} \n\n");
                }
            }
        }

        // Create a unique temp working directory
        private static string CreateTempWorkingDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "crank-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        // Best-effort deletion of the temp directory
        private static void TryDeleteDirectory(string dir)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LogNow} Failed to delete temp directory '{dir}': {ex.Message}");
            }
        }
    }
}
