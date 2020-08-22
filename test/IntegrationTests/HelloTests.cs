// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Crank.Agent;

namespace Microsoft.Crank.IntegrationTests
{
    public class HelloTests
    {
        private readonly ITestOutputHelper _output;
        private string _crankDirectory;
        private string _crankAgentDirectory;
        private string _crankTestsDirectory;

        public HelloTests(ITestOutputHelper output)
        {
            _output = output;
            _crankDirectory = Path.GetDirectoryName(typeof(HelloTests).Assembly.Location).Replace("Microsoft.Crank.IntegrationTests", "Microsoft.Crank.Controller");
            _crankAgentDirectory = Path.GetDirectoryName(typeof(HelloTests).Assembly.Location).Replace("Microsoft.Crank.IntegrationTests", "Microsoft.Crank.Agent");
            _crankTestsDirectory = Path.GetDirectoryName(typeof(HelloTests).Assembly.Location);
            _output.WriteLine($"Running tests in {_crankDirectory}");
        }

        [Fact]
        public async Task ControllerDisplaysErrorForMissingScenario()
        {
            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                "exec crank.dll", 
                workingDirectory: _crankDirectory,
                captureOutput: true,
                throwOnError: false
            );

            Assert.Equal(1, result.ExitCode);
            // Assert.Equal("No jobs were found. Are you missing the --scenario argument?", result.StandardOutput);
        }

        [Fact]
        public async Task AgentDisplaysReadyMessage()
        {
            var agentReadyTcs = new TaskCompletionSource<bool>();
            var stopAgentCts = new CancellationTokenSource();

            var agent = ProcessUtil.RunAsync(
                "dotnet", 
                "exec crank-agent.dll", 
                workingDirectory: _crankAgentDirectory,
                captureOutput: true,
                timeout: TimeSpan.FromSeconds(10),
                throwOnError: false,
                cancellationToken: stopAgentCts.Token,
                outputDataReceived: t => 
                { 
                    _output.WriteLine($"[AGENT] {t}"); 

                    if (t.Contains("Agent ready"))
                    {
                        agentReadyTcs.SetResult(true);
                    }
                } 
            );
            
            // Wait either for the message of the agent to stop
            await Task.WhenAny(agentReadyTcs.Task, agent);

            Assert.True(agentReadyTcs.Task.IsCompleted);
                
            stopAgentCts.Cancel();
            
            // Give 5 seconds to the agent to stop
            await Task.WhenAny(agent, Task.Delay(TimeSpan.FromSeconds(5)));
            
            Assert.True(agent.IsCompleted);
        }

        [Fact]
        public async Task BenchmarkHello()
        {
            var agentReadyTcs = new TaskCompletionSource<bool>();
            var stopAgentCts = new CancellationTokenSource();

            var agent = ProcessUtil.RunAsync(
                "dotnet", 
                "exec crank-agent.dll", 
                workingDirectory: _crankAgentDirectory,
                captureOutput: true,
                throwOnError: false,
                timeout: TimeSpan.FromMinutes(5),
                cancellationToken: stopAgentCts.Token,
                outputDataReceived: t => 
                { 
                    _output.WriteLine($"[AGT] {t}");

                    if (t.Contains("Agent ready"))
                    {
                        agentReadyTcs.SetResult(true);
                    }
                } 
            );
            
            // Wait either for the message of the agent to stop
            await Task.WhenAny(agentReadyTcs.Task, agent);

            _output.WriteLine($"Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: TimeSpan.FromMinutes(5),
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);
                
            stopAgentCts.Cancel();

            var cancel = new CancellationTokenSource();

            // Give 5 seconds to the agent to stop
            await Task.WhenAny(agent, Task.Delay(TimeSpan.FromSeconds(5), cancel.Token));

            cancel.Cancel();

            Assert.Contains("Requests/sec", result.StandardOutput);
        }
    }
}
