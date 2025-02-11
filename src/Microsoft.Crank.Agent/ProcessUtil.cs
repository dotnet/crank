// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static Process StreamOutput(
                    string filename,
                    string arguments,
                    Action<string> outputDataReceivedCallback,
                    Action<string> errorDataReceivedCallback,
                    string workingDirectory = null,
                    IDictionary<string, string> environmentVariables = null)
        {
            var process = new Process()
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

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    outputDataReceivedCallback(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    errorDataReceivedCallback(e.Data);
                }
            };

            process.Exited += (_, e) =>
            {
                // Even though the Exited event has been raised, WaitForExit() must still be called to ensure the output buffers
                // have been flushed before the process is considered completely done.
                // However, lets not give it infinite time to exit: 1 second should do it
                process.WaitForExit(TimeSpan.FromSeconds(1));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Log.Info($"Started process '{process.ProcessName}#{process.Id}' for streaming");

            return process;
        }

        // Strings in arguments collection don't need to be previously escaped
        public static Task<ProcessResult> RunAsync(
            string filename,
            IEnumerable<string> arguments,
            TimeSpan? timeout = null,
            string workingDirectory = null,
            bool throwOnError = true,
            IDictionary<string, string> environmentVariables = null,
            Action<string> outputDataReceived = null,
            bool log = false,
            Action<int> onStart = null,
            Action<int> onStop = null,
            bool captureOutput = false,
            bool captureError = false,
            bool runAsRoot = false,
            CancellationToken cancellationToken = default
        )
        {
            var startInfo = new ProcessStartInfo(filename, arguments);
            return RunAsync(startInfo, timeout, workingDirectory, throwOnError, environmentVariables, outputDataReceived, log, onStart, onStop, captureOutput, captureError, runAsRoot, cancellationToken);
        }

        public static Task<ProcessResult> RunAsync(
            string filename,
            string encodedArguments,
            TimeSpan? timeout = null,
            string workingDirectory = null,
            bool throwOnError = true,
            IDictionary<string, string> environmentVariables = null,
            Action<string> outputDataReceived = null,
            bool log = false,
            Action<int> onStart = null,
            Action<int> onStop = null,
            bool captureOutput = false,
            bool captureError = false,
            bool runAsRoot = false,
            CancellationToken cancellationToken = default
        )
        {
            var startInfo = new ProcessStartInfo(filename, encodedArguments);
            return RunAsync(startInfo, timeout, workingDirectory, throwOnError, environmentVariables, outputDataReceived, log, onStart, onStop, captureOutput, captureError, runAsRoot, cancellationToken);
        }

        private static async Task<ProcessResult> RunAsync(
            ProcessStartInfo startInfo,
            TimeSpan? timeout = null,
            string workingDirectory = null,
            bool throwOnError = true,
            IDictionary<string, string> environmentVariables = null,
            Action<string> outputDataReceived = null,
            bool log = false,
            Action<int> onStart = null,
            Action<int> onStop = null,
            bool captureOutput = false,
            bool captureError = false,
            bool runAsRoot = false,
            CancellationToken cancellationToken = default
        )
        {
            var logWorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();

            var arguments = string.IsNullOrWhiteSpace(startInfo.Arguments) ? string.Join(" ", startInfo.ArgumentList) : startInfo.Arguments;

            if (log)
            {
                Log.Info($"[{logWorkingDirectory}] {startInfo.FileName} {arguments}");
            }

            using var process = new Process();
            process.StartInfo = startInfo;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;

            if (runAsRoot)
            {
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
            }
            else
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
            }

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
            if (process.StartInfo.RedirectStandardOutput)
            {
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
                            Log.Info(e.Data);
                        }
                    }
                };
            }

            var errorBuilder = new StringBuilder();
            if (process.StartInfo.RedirectStandardError)
            {
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

                        Log.Info("[STDERR] " + e.Data);
                    }
                };
            }

            var processLifetimeTask = new TaskCompletionSource<ProcessResult>();

            process.Exited += (_, e) =>
            {
                // Even though the Exited event has been raised, WaitForExit() must still be called to ensure the output buffers
                // have been flushed before the process is considered completely done.
                process.WaitForExit();

                if (throwOnError && process.ExitCode != 0)
                {
                    processLifetimeTask.TrySetException(new InvalidOperationException($"Command {startInfo.FileName} {arguments} returned exit code {process.ExitCode}"));
                }
                else
                {
                    processLifetimeTask.TrySetResult(new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString()));
                }
            };

            process.Start();

            onStart?.Invoke(process.Id);

            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }

            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            var cancelledTcs = new TaskCompletionSource<object>();
            await using var _ = cancellationToken.Register(() => cancelledTcs.TrySetResult(null));

            var result = await Task.WhenAny(processLifetimeTask.Task, cancelledTcs.Task, Task.Delay(timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1, cancellationToken));

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

            var processResult = await processLifetimeTask.Task;
            onStop?.Invoke(processResult.ExitCode);
            return processResult;
        }

        public static async Task<T> RetryOnExceptionAsync<T>(int retries, Func<Task<T>> operation, CancellationToken cancellationToken = default)
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
                    if (attempts == retries + 1 || cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    Log.Info($"Attempt {attempts} failed: {e.Message}");
                }
            } while (true);
        }

        public static Task RetryOnExceptionAsync(int retries, Func<Task> operation, CancellationToken cancellationToken = default)
        {
            return RetryOnExceptionAsync(retries, async () => 
            {
                await operation();
                return 0;
            }, cancellationToken);
        }
    }
}
