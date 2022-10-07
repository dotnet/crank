// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Crank.IntegrationTests
{
    public class CommonTests : IClassFixture<AgentFixture>, IDisposable
    {
        private static readonly TimeSpan DefaultTimeOut = TimeSpan.FromMinutes(10);

        private readonly ITestOutputHelper _output;
        private readonly AgentFixture _agent;
        private string _crankDirectory;
        private string _crankTestsDirectory;

        public CommonTests(ITestOutputHelper output, AgentFixture fixture)
        {
            _output = output;
            _agent = fixture;
            _crankDirectory = Path.GetDirectoryName(typeof(CommonTests).Assembly.Location).Replace("Microsoft.Crank.IntegrationTests", "Microsoft.Crank.Controller");
            _crankTestsDirectory = Path.GetDirectoryName(typeof(CommonTests).Assembly.Location);
            _output.WriteLine($"[TEST] Running tests in {_crankDirectory}");

            // Check if the agent is started
            if (!_agent.IsReady())
            {
                // Dispose() will not be called, flush the agent output now
                _output.WriteLine(_agent.FlushOutput());
                Assert.True(false, "Agent failed to start");
            }
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
        public async Task BenchmarkHello()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);
                
            Assert.Contains("Requests/sec", result.StandardOutput);
            Assert.Contains(".NET Core SDK Version", result.StandardOutput);
            Assert.Contains(".NET Runtime Version", result.StandardOutput);
            Assert.Contains("ASP.NET Core Version", result.StandardOutput);
        }

        [Fact]
        public async Task ExecutesScripts()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local --config ./assets/scripts.benchmarks.yml --script add_current_time --json results.json --property a=b --command-line-property", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Custom result (s)", result.StandardOutput);
            Assert.Contains("123.00", result.StandardOutput);

            var results = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(_crankTestsDirectory, "results.json")));
            
            Assert.Contains("a default script", result.StandardOutput);
            Assert.NotEmpty(results.RootElement.GetProperty("jobResults").GetProperty("properties").GetProperty("time").GetString());
            Assert.Equal(123.0, results.RootElement.GetProperty("jobResults").GetProperty("jobs").GetProperty("application").GetProperty("results").GetProperty("my/result").GetDouble());
            Assert.Equal("b", results.RootElement.GetProperty("jobResults").GetProperty("properties").GetProperty("a").GetString());

            var commandLineArguments = results.RootElement.GetProperty("jobResults").GetProperty("properties").GetProperty("command-line").GetString();

            Assert.Contains("--config ./assets/hello.benchmarks.yml", commandLineArguments);
            Assert.Contains("--scenario hello", commandLineArguments);
            Assert.Contains("--profile local", commandLineArguments);
            Assert.DoesNotContain("--json results.json", commandLineArguments);
        }

        [Fact]
        public async Task DotnetCounters()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local --application.options.counterProviders System.Runtime", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);

            Assert.Contains("Lock Contention", result.StandardOutput);
        }

        [Fact]
        public async Task UploadFile()
        {
            _output.WriteLine($"[TEST] Starting controller");

            // Create a local folder to download file into
            var outputFileDirectory = Path.Combine(_crankTestsDirectory, "outputfiles");
            Directory.CreateDirectory(outputFileDirectory);

            var expectedOutputFilename = Path.Combine(outputFileDirectory, "hello.benchmarks.yml");
            var expectedOutputFileContent = File.ReadAllText(Path.Combine(_crankTestsDirectory, "assets", "hello.benchmarks.yml"));

            var result = await ProcessUtil.RunAsync(
                "dotnet",
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local --application.options.outputFiles ./assets/hello.benchmarks.yml  --application.options.downloadFiles hello.benchmarks.yml --application.options.downloadFilesOutput {outputFileDirectory}",
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); }
            );

            Assert.Equal(0, result.ExitCode);

            _output.WriteLine(_agent.FlushOutput());

            Assert.Contains("Uploading", result.StandardOutput);
            Assert.True(File.Exists(expectedOutputFilename));
            Assert.Equal(expectedOutputFileContent, File.ReadAllText(expectedOutputFilename));
        }

        [SkipOnMacOs]
        public async Task CollectDump()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet",
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local --application.options.dumpType mini",
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: TimeSpan.FromMinutes(10),
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); }
            );

            Assert.Equal(0, result.ExitCode);

            Assert.Contains("Downloading dump file", result.StandardOutput);
            Assert.Contains("(100%)", result.StandardOutput);
        }

        [Fact]
        public async Task Iterations()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/hello.benchmarks.yml --scenario hello --profile local --exclude 1 --exclude-order load:http/rps/mean --iterations 3", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);
                
            Assert.Contains("Iteration 1 of 3", result.StandardOutput);
            Assert.Contains("Iteration 2 of 3", result.StandardOutput);
            Assert.Contains("Iteration 3 of 3", result.StandardOutput);
            Assert.Contains("Values of load->http/rps/mean:", result.StandardOutput);
        }
        
        [Fact]
        public async Task MultiClients()
        {
            _output.WriteLine($"[TEST] Starting controller");

            var result = await ProcessUtil.RunAsync(
                "dotnet", 
                $"exec {Path.Combine(_crankDirectory, "crank.dll")} --config ./assets/multiclient.benchmarks.yml --scenario hello --profile local", 
                workingDirectory: _crankTestsDirectory,
                captureOutput: true,
                timeout: DefaultTimeOut,
                throwOnError: false,
                outputDataReceived: t => { _output.WriteLine($"[CTL] {t}"); } 
            );

            Assert.Equal(0, result.ExitCode);
                
            // Two load jobs are started
            var firstLoad = result.StandardOutput.IndexOf("'load' is now building");
            var secondLoad = result.StandardOutput.IndexOf("'load' is now building", firstLoad + 1);

            Assert.NotEqual(-1, firstLoad);
            Assert.NotEqual(-1, secondLoad);          

            // The results are computed
            Assert.Contains("Requests/sec", result.StandardOutput);
        }

        public void Dispose()
        {
            _output.WriteLine(_agent.FlushOutput());
        }
    }
}
