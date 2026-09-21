// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    public class AzureWorkerLifecycleTests
    {
        [Theory]
        [InlineData("00:34:59", false)]
        [InlineData("00:35:00", true)]
        public void RenewalBudgetIncludesEntireAttempts(string duration, bool sufficient)
        {
            var configuration = WorkerConfiguration.Create(duration, _ => null);
            var payload = new JobPayload { Timeout = TimeSpan.FromMinutes(10), Retries = 2 };
            Assert.Equal(sufficient, configuration.HasSufficientLockRenewalDuration(payload, out var required));
            Assert.Equal(TimeSpan.FromMinutes(35), required);
        }

        [Fact]
        public void InvalidPayloadDoesNotLogItsContents()
        {
            var exception = Assert.Throws<JobPayloadParseException>(() =>
                JobPayload.Deserialize(Encoding.UTF8.GetBytes("""{"args":["secret"],"timeout":"secret"}""")));
            Assert.DoesNotContain("secret", Program.FormatExceptionForLog(exception));
        }

        [Fact]
        public async Task StopTerminatesCommandSubprocesses()
        {
            var directory = Directory.CreateTempSubdirectory("crank-process-tree-").FullName;
            var pidFile = Path.Combine(directory, "child.pid");
            var script = OperatingSystem.IsWindows()
                ? $"$child = Start-Process powershell.exe -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 60' -PassThru; " +
                  $"Set-Content -LiteralPath '{pidFile.Replace("'", "''")}' -Value $child.Id; Start-Sleep -Seconds 60"
                : $"sleep 60 & echo $! > '{pidFile.Replace("'", "'\\''")}'; wait";
            var arguments = OperatingSystem.IsWindows()
                ? "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script))
                : "-c \"" + script.Replace("\"", "\\\"") + "\"";
            using var job = new Job(OperatingSystem.IsWindows() ? "powershell.exe" : "/bin/bash", arguments, directory);
            Process child = null;
            try
            {
                job.Start();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                while (!File.Exists(pidFile) || new FileInfo(pidFile).Length == 0)
                {
                    await Task.Delay(50, timeout.Token);
                }

                child = Process.GetProcessById(int.Parse((await File.ReadAllTextAsync(pidFile)).Trim()));
                Assert.False(child.HasExited);
                job.Stop();
                await child.WaitForExitAsync(timeout.Token);
                Assert.False(job.IsRunning);
                Assert.True(child.HasExited);
            }
            finally
            {
                job.Stop();
                if (child is not null)
                {
                    if (!child.HasExited)
                    {
                        child.Kill(entireProcessTree: true);
                        await child.WaitForExitAsync();
                    }
                    child.Dispose();
                }
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
