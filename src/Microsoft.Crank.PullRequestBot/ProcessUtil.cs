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

namespace Microsoft.Crank.PullRequestBot
{
    public static class ProcessUtil
    {
        public static string GetEnvironmentCommand(string window, string unix, string macos = null)
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => unix,
                PlatformID.Win32NT => window,
                PlatformID.MacOSX => macos ?? unix,
                _ => throw new NotImplementedException()
            };
        }

        public static string GetScriptHost()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => "bash",
                PlatformID.Win32NT => "cmd.exe",
                PlatformID.MacOSX => "bash",
                _ => throw new NotImplementedException()
            };
        }

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
            Action<int> onStop = null,
            bool captureOutput = false,
            bool captureError = false,
            CancellationToken cancellationToken = default
        )
        {
            var logWorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();

            if (log)
            {
                Console.WriteLine($"[{logWorkingDirectory}] {filename} {arguments}");
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
                        Console.WriteLine(e.Data);
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

                    Console.WriteLine("[STDERR] " + e.Data);
                }
            };

            var processLifetimeTask = new TaskCompletionSource<ProcessResult>();

            process.Exited += (_, e) =>
            {
                // Even though the Exited event has been raised, WaitForExit() must still be called to ensure the output buffers
                // have been flushed before the process is considered completely done.
                process.WaitForExit();

                if (throwOnError && process.ExitCode != 0)
                {
                    processLifetimeTask.TrySetException(new InvalidOperationException($"Command {filename} {arguments} returned exit code {process.ExitCode}"));
                }
                else
                {
                    processLifetimeTask.TrySetResult(new ProcessResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString()));
                }
            };

            process.Start();

            onStart?.Invoke(process.Id);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

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
    }
}
