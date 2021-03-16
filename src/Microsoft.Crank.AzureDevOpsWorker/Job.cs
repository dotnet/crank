// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public sealed class Job : IDisposable
    {
        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int sys_kill(int pid, int sig);

        private Process? _process;

        private ConcurrentQueue<string>? _standardOutput = new();

        private ConcurrentQueue<string>? _standardError = new();

        public StringBuilder? OutputBuilder { get; private set; } = new();

        public StringBuilder? ErrorBuilder { get; private set; } = new();

        public Action<string>? OnStandardOutput { get; set; }

        public Action<string>? OnStandardError { get; set; }

        public DateTime StartTimeUtc { get; private set; }

#pragma warning disable CS8618
        // Non-nullable field must contain a non-null value when exiting constructor.
        // _process.OnStandardOutput and _process.OnStandardError events are not assigned.
        // But these events are registered later in Job.Start()
        public Job(string applicationPath, string arguments, string? workingDirectory = null) =>
#pragma warning restore CS8618
            _process = new Process()
            {
                StartInfo =
                {
                    FileName = applicationPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(applicationPath)!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

        public void Start()
        {
            if (_process == null)
            {
                throw new Exception("Can't reuse disposed job");
            }

            _process.OutputDataReceived += (_, e) =>
            {
                // e.Data is null to signal end of stream
                if (e.Data != null)
                {
                    _standardOutput?.Enqueue(e.Data);
                    OnStandardOutput?.Invoke(e.Data);
                    OutputBuilder?.AppendLine(e.Data);
                }
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                // e.Data is null to signal end of stream
                if (e.Data != null)
                {
                    _standardError?.Enqueue(e.Data);
                    OnStandardError?.Invoke(e.Data);
                    ErrorBuilder?.AppendLine(e.Data);
                }
            };

            StartTimeUtc = DateTime.UtcNow;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Stop()
        {
            try
            {
                if (_process != null)
                {
                    Console.WriteLine($"Stopping process id: {_process.Id}");

                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        sys_kill(_process.Id, sig: 2); // SIGINT

                        Thread.Sleep(2000);
                    }

                    TryActionThenSleep(_process, p => p.Close());
                    TryActionThenSleep(_process, p => p.CloseMainWindow());
                    TryActionThenSleep(_process, p => p.Kill());

                    static void TryActionThenSleep(
                        Process process, Action<Process> action, int millisecondsTimeout = 2_000)
                    {
                        if (!process.HasExited)
                        {
                            try
                            {
                                action(process);
                                Thread.Sleep(millisecondsTimeout);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore if the application is already stopped:
                //Process error: System.InvalidOperationException: No process is associated with this object.
                //   at System.Diagnostics.Process.EnsureState(State state)
                //   at System.Diagnostics.Process.get_HasExited()
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public IEnumerable<string> FlushStandardOutput() => Flush(_standardOutput);

        public IEnumerable<string> FlushStandardError() => Flush(_standardError);

        private static IEnumerable<string> Flush(ConcurrentQueue<string>? queue)
        {
            while (queue is { }
                && queue.TryDequeue(out var result))
            {
                yield return result;
            }
        }

        public bool WasSuccessful => _process is { ExitCode: 0 };

        public bool IsRunning => _process is not { HasExited: true };

        public void Dispose()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                Stop();
            }
            catch
            {
            }

            OnStandardOutput = null;
            OnStandardError = null;
            _standardError = null;
            _standardOutput = null;
            OutputBuilder = null;
            ErrorBuilder = null;
        }
    }
}
