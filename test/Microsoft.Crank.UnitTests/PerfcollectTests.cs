// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class PerfcollectTests
    {
        [Fact]
        public async Task InitializationFailureIncludesStdoutAndStderr()
        {
            var job = new Job { Error = "Existing job error." };
            var startInfo = CreateStartInfo(
                "echo LTTng not installed. & echo Collector error. 1>&2 & exit /b 1",
                "echo 'LTTng not installed.'; echo 'Collector error.' >&2; exit 1");

            var (process, completion) = Startup.StartPerfcollectProcess(job, startInfo);
            using (process)
            {
                Assert.False(await completion.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            Assert.StartsWith("Existing job error.", job.Error);
            Assert.Contains("Perfcollect failed with exit code 1.", job.Error);
            Assert.Contains("LTTng not installed.", job.Error);
            Assert.Contains("Collector error.", job.Error);
        }

        [Fact]
        public async Task FailureDiagnosticsAreBounded()
        {
            var job = new Job();
            var startInfo = CreateStartInfo(
                "(for /L %i in (1,1,1000) do @echo 012345678901234567890123456789) & exit /b 1",
                "i=0; while [ \"$i\" -lt 1000 ]; do echo 012345678901234567890123456789; i=$((i+1)); done; exit 1");

            var (process, completion) = Startup.StartPerfcollectProcess(job, startInfo);
            using (process)
            {
                Assert.False(await completion.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            Assert.StartsWith("Perfcollect failed with exit code 1.", job.Error);
            Assert.Contains("012345678901234567890123456789", job.Error);
            Assert.True(job.Error.Length <= 16 * 1024 + 100);
        }

        [Fact]
        public async Task SuccessfulExitDoesNotSetJobError()
        {
            var job = new Job();
            var (process, completion) = Startup.StartPerfcollectProcess(job,
                CreateStartInfo("echo Collection finished. & exit /b 0", "echo 'Collection finished.'; exit 0"));
            using (process)
            {
                Assert.True(await completion.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            Assert.Null(job.Error);
        }

        [Fact]
        public async Task MissingExecutableSetsJobError()
        {
            var job = new Job();
            var context = new JobContext { Job = job };
            (context.PerfCollectProcess, context.PerfCollectTask) = Startup.StartPerfcollectProcess(job,
                new ProcessStartInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "perfcollect")));

            Assert.Null(context.PerfCollectProcess);
            Assert.False(await Startup.StopPerfcollectAsync(context));

            Assert.Null(context.PerfCollectTask);
            Assert.Contains("Failed to start perfcollect:", job.Error);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingOrEmptyTraceSetsJobError(bool createEmptyFile)
        {
            var tracePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".trace.zip");
            try
            {
                if (createEmptyFile)
                {
                    File.WriteAllBytes(tracePath, Array.Empty<byte>());
                }

                var job = new Job { PerfViewTraceFile = tracePath, Error = "LTTng not installed." };

                Assert.False(Startup.ValidatePerfcollectTrace(job, collectionSucceeded: false));
                Assert.StartsWith("LTTng not installed.", job.Error);
                Assert.Contains("Perfcollect did not produce a non-empty trace file.", job.Error);
            }
            finally
            {
                File.Delete(tracePath);
            }
        }

        [Fact]
        public void TraceThatWasNeverStartedSetsJobError()
        {
            var job = new Job();

            Assert.False(Startup.ValidatePerfcollectTrace(job, collectionSucceeded: false));
            Assert.Contains("Perfcollect did not produce a non-empty trace file.", job.Error);
        }

        [Fact]
        public void NonEmptyTraceDoesNotSetJobError()
        {
            var tracePath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tracePath, "trace");
                var job = new Job { PerfViewTraceFile = tracePath };

                Assert.True(Startup.ValidatePerfcollectTrace(job, collectionSucceeded: true));
                Assert.Null(job.Error);
            }
            finally
            {
                File.Delete(tracePath);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task TraceSuccessRequiresSuccessfulCollectorExit(int exitCode)
        {
            var tracePath = Path.GetTempFileName();
            var context = new JobContext { Job = new Job { PerfViewTraceFile = tracePath, Error = "Existing benchmark error." } };
            try
            {
                File.WriteAllText(tracePath, "partial or complete trace");
                (context.PerfCollectProcess, context.PerfCollectTask) = Startup.StartPerfcollectProcess(context.Job,
                    CreateStartInfo($"exit /b {exitCode}", $"exit {exitCode}"));
                await context.PerfCollectTask.WaitAsync(TimeSpan.FromSeconds(10));

                var collectionSucceeded = await Startup.StopPerfcollectAsync(context);

                Assert.Equal(exitCode == 0, Startup.ValidatePerfcollectTrace(context.Job, collectionSucceeded));
                Assert.Null(context.PerfCollectProcess);
                Assert.Null(context.PerfCollectTask);
                Assert.True(File.Exists(tracePath));
                if (exitCode != 0)
                {
                    Assert.Contains("Perfcollect failed with exit code 1.", context.Job.Error);
                }
                else
                {
                    Assert.Equal("Existing benchmark error.", context.Job.Error);
                }
            }
            finally
            {
                context.PerfCollectProcess?.Dispose();
                File.Delete(tracePath);
            }
        }

        [Fact]
        public async Task StoppingOneJobDoesNotStopOrDisposeAnotherJobsCollector()
        {
            var first = new JobContext { Job = new Job { Service = "first" } };
            var second = new JobContext { Job = new Job { Service = "second" } };
            (first.PerfCollectProcess, first.PerfCollectTask) = Startup.StartPerfcollectProcess(first.Job,
                CreateStartInfo("exit /b 1", "exit 1"));
            (second.PerfCollectProcess, second.PerfCollectTask) = Startup.StartPerfcollectProcess(second.Job,
                CreateStartInfo("ping -n 30 127.0.0.1 >nul & exit /b 0", "sleep 30; exit 0"));
            var secondProcess = second.PerfCollectProcess;
            var secondTask = second.PerfCollectTask;
            try
            {
                await first.PerfCollectTask.WaitAsync(TimeSpan.FromSeconds(10));

                Assert.False(await Startup.StopPerfcollectAsync(first));

                Assert.Null(first.PerfCollectProcess);
                Assert.Null(first.PerfCollectTask);
                Assert.Same(secondProcess, second.PerfCollectProcess);
                Assert.Same(secondTask, second.PerfCollectTask);
                Assert.False(secondProcess.HasExited);
                Assert.False(secondTask.IsCompleted);
                Assert.Null(second.Job.Error);
            }
            finally
            {
                first.PerfCollectProcess?.Dispose();
                if (!secondProcess.HasExited)
                {
                    secondProcess.Kill(entireProcessTree: true);
                }
                await secondTask.WaitAsync(TimeSpan.FromSeconds(10));
                await Startup.StopPerfcollectAsync(second);
            }
        }

        private static ProcessStartInfo CreateStartInfo(string windowsCommand, string linuxCommand)
        {
            var startInfo = new ProcessStartInfo();
            if (System.OperatingSystem.IsWindows())
            {
                startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                startInfo.ArgumentList.Add("/d");
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add(windowsCommand);
            }
            else
            {
                startInfo.FileName = "/bin/sh";
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add(linuxCommand);
            }
            return startInfo;
        }
    }
}
