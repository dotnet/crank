// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    public class AgentFixture : IAsyncLifetime
    {
        private StringBuilder _output = new StringBuilder();
        private string _crankAgentDirectory;
        private CancellationTokenSource _stopAgentCts;
        private Task<ProcessResult> _agent;

        public AgentFixture()
        {
            _crankAgentDirectory = Path.GetDirectoryName(typeof(CommonTests).Assembly.Location).Replace("Microsoft.Crank.IntegrationTests", "Microsoft.Crank.Agent");
        }

        public async Task InitializeAsync()
        {
            _output.AppendLine($"[AGT] Starting agent");

            var agentReadyTcs = new TaskCompletionSource<bool>();
            _stopAgentCts = new CancellationTokenSource();

            var reusableSdksPath = Path.Combine(_crankAgentDirectory, ".dotnet");

            // Start the agent
            _agent = ProcessUtil.RunAsync(
                "dotnet", 
                $"exec crank-agent.dll --dotnethome {reusableSdksPath}", 
                workingDirectory: _crankAgentDirectory,
                captureOutput: true,
                throwOnError: false,
                cancellationToken: _stopAgentCts.Token,
                outputDataReceived: t => 
                { 
                    _output.AppendLine($"[AGT] {t}");

                    if (t.Contains("Agent ready"))
                    {
                        agentReadyTcs.SetResult(true);
                    }
                } 
            );
            
            // Wait either for the message of the agent to stop
            await Task.WhenAny(agentReadyTcs.Task, _agent);

            if (_agent.IsCompleted)
            {
                _output.AppendLine($"[AGT] Agent exited with exit code {_agent.Result.ExitCode}");
            }
            else
            {
                _output.AppendLine($"[AGT] Started agent");
            }

        }

        public bool IsReady() => _agent != null && !_agent.IsCompleted;

        public async Task DisposeAsync()
        {
            _output.AppendLine($"[AGT] Releasing agent");

            _stopAgentCts.Cancel();

            var cancel = new CancellationTokenSource();

            // Give 10 seconds to the agent to stop
            await Task.WhenAny(_agent, Task.Delay(TimeSpan.FromSeconds(10), cancel.Token));

            cancel.Cancel();

            _output.AppendLine($"[AGT] Released agent");
        }

        public string FlushOutput()
        {
            var localOutput = _output;
            _output = new StringBuilder();
            return localOutput.ToString();
        }
    }
}
