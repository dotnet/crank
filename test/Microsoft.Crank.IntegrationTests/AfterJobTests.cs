// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Jint;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Xunit;
using ControllerProgram = Microsoft.Crank.Controller.Program;

namespace Microsoft.Crank.IntegrationTests
{
    public class AfterJobTests
    {
        [Theory]
        [InlineData(0, true, "export")]
        [InlineData(1, true, "skip")]
        [InlineData(0, false, "skip")]
        public async Task ResultConditionsSelectExportWithoutSuppressingCleanup(int returnCode, bool hasResults, string expected)
        {
            var directory = Directory.CreateTempSubdirectory("crank-afterjob-").FullName;
            try
            {
                var marker = Path.Combine(directory, "commands.txt");
                var configuration = new Configuration();
                var job = new Job { Commands = new() };
                configuration.Jobs["application"] = job;
                job.AfterJob.AddRange(["export", "cleanup"]);
                job.Commands["export"] =
                [
                    Command("export", marker, "result.returnCode == 0 && result.jobResults.jobs.Count > 0"),
                    Command("skip", marker)
                ];
                job.Commands["cleanup"] = [Command("cleanup", marker)];
                var result = new ExecutionResult { ReturnCode = returnCode };
                if (hasResults)
                {
                    result.JobResults.Jobs["application"] = new JobResult();
                    result.JobResults.Jobs["load"] = new JobResult();
                }

                await ControllerProgram.RunAfterJobsAsync(configuration, ["application"], new Engine(), result);

                Assert.Equal([expected, "cleanup"], File.ReadAllLines(marker));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task CommandFailureFailsTheController()
        {
            var configuration = new Configuration();
            var job = new Job { Commands = new() };
            configuration.Jobs["application"] = job;
            job.AfterJob.Add("export");
            job.Commands["export"] =
            [
                new CommandDefinition
                {
                    ScriptType = System.OperatingSystem.IsWindows() ? ScriptType.Powershell : ScriptType.Bash,
                    Script = "exit 7"
                }
            ];

            await Assert.ThrowsAsync<ControllerException>(() =>
                ControllerProgram.RunAfterJobsAsync(configuration, ["application"], new Engine(), new ExecutionResult()));
        }

        private static CommandDefinition Command(string text, string path, string condition = "true") => new()
        {
            Condition = condition,
            ScriptType = System.OperatingSystem.IsWindows() ? ScriptType.Powershell : ScriptType.Bash,
            Script = System.OperatingSystem.IsWindows()
                ? $"Add-Content -LiteralPath '{path.Replace("'", "''")}' -Value '{text}'"
                : $"printf '%s\\n' '{text}' >> '{path.Replace("'", "'\\''")}'"
        };
    }
}
