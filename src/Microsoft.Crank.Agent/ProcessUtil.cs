// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent
{
    public static class ProcessUtil
    {
        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int sys_kill(int pid, int sig);

        public static async Task<ProcessResult> RunAsync(
            string filename, 
            string arguments, 
            TimeSpan? timeout = null, 
            string workingDirectory = null,
            bool throwOnError = true, 
            IDictionary<string, string> environmentVariables = null, 
            Action<string> outputDataReceived = null,
            bool log = false,
            Action<int> onStart = null,
            CancellationToken cancellationToken = default(CancellationToken),
            bool captureOutput = false,
            bool captureError = false
        )
        {
            var logWorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();

            if (log)
            {
                Log.WriteLine($"[{logWorkingDirectory}] {filename} {arguments}");
            }

            using var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    process.StartInfo.Environment.Add(kvp);
                }
            }

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (captureOutput)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }

                    if (outputDataReceived != null)
                    {
                        outputDataReceived.Invoke(e.Data);
                    }

                    if (log)
                    {
                        Log.WriteLine(e.Data);
                    }
                }
            };

            var errorBuilder = new StringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (captureError)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }

                    if (outputDataReceived != null)
                    {
                        outputDataReceived.Invoke(e.Data);
                    }

                    Log.WriteLine("[STDERR] " + e.Data);
                }
            };

            var processLifetimeTask = new TaskCompletionSource<ProcessResult>();

            process.Exited += (_, e) =>
            {
                if (throwOnError && process.ExitCode != 0)
                {
                    processLifetimeTask.TrySetException(new InvalidOperationException($"Command {filename} {arguments} returned exit code {process.ExitCode}"));
                }
                else
                {
                    processLifetimeTask.TrySetResult(new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString()));
                }
            };

            onStart?.Invoke(process.Id);
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var cancelledTcs = new TaskCompletionSource<object>();
            await using var _ = cancellationToken.Register(() => cancelledTcs.TrySetResult(null));

            var result = await Task.WhenAny(processLifetimeTask.Task, cancelledTcs.Task, Task.Delay(timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1));

            if (result != processLifetimeTask.Task)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    sys_kill(process.Id, sig: 2); // SIGINT

                    var cancel = new CancellationTokenSource();

                    await Task.WhenAny(processLifetimeTask.Task, Task.Delay(TimeSpan.FromSeconds(5), cancel.Token));

                    cancel.Cancel();
                }

                if (!process.HasExited)
                {
                    process.CloseMainWindow();

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }

            return await processLifetimeTask.Task;
        }

        public static async Task<T> RetryOnExceptionAsync<T>(int retries, Func<Task<T>> operation)
        {
            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    return await operation();
                }
                catch (Exception e)
                {
                    if (attempts == retries + 1)
                    {
                        throw;
                    }

                    Log.WriteLine($"Attempt {attempts} failed: {e.Message}");
                }
            } while (true);
        }
    }
}
