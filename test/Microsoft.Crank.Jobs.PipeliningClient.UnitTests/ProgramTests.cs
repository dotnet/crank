using Microsoft.Crank.Jobs.PipeliningClient;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalOutput;

        public ProgramTests()
        {
            // Redirect console output to capture it.
            _originalOutput = Console.Out;
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);
        }

        /// <summary>
        /// Restores the original console output.
        /// </summary>
        ~ProgramTests()
        {
            Console.SetOut(_originalOutput);
        }

        /// <summary>
        /// Tests the RunAsync method when there are zero connections.
        /// This verifies that RunAsync completes successfully and prints the expected metrics.
        /// </summary>
        [Fact]
        public async Task RunAsync_WithZeroConnections_ShouldPrintMetrics()
        {
            // Arrange
            Program.ServerUrl = "http://dummy";
            Program.PipelineDepth = 1;
            Program.WarmupTimeSeconds = 0;
            Program.ExecutionTimeSeconds = 0;
            Program.Connections = 0;
            Program.Headers = new List<string>();

            // Act
            await Program.RunAsync();
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("Stopped...", output);
            Assert.Contains("Average RPS:", output);
        }

        /// <summary>
        /// Tests the DoWorkAsync method when the running flag is false.
        /// This verifies that the method returns a WorkerResult with all counters set to zero.
        /// </summary>
        [Fact]
        public async Task DoWorkAsync_WhenNotRunning_ReturnsZeroResult()
        {
            // Arrange
            // Use reflection to set the private static _running field to false.
            FieldInfo runningField = typeof(Program).GetField("_running", BindingFlags.NonPublic | BindingFlags.Static);
            runningField.SetValue(null, false);

            // Reset _connectionCount to a known value via reflection.
            FieldInfo connectionCountField = typeof(Program).GetField("_connectionCount", BindingFlags.NonPublic | BindingFlags.Static);
            connectionCountField.SetValue(null, 0);

            // Act
            var result = await Program.DoWorkAsync();
            string output = _consoleOutput.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Status1xx);
            Assert.Equal(0, result.Status2xx);
            Assert.Equal(0, result.Status3xx);
            Assert.Equal(0, result.Status4xx);
            Assert.Equal(0, result.Status5xx);
            Assert.Equal(0, result.SocketErrors);
            Assert.Contains("Connection closed", output);
        }

        /// <summary>
        /// Tests the Main method by supplying valid command-line arguments.
        /// This verifies that the application executes successfully and outputs expected text.
        /// </summary>
//         [Fact] [Error] (103-27)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WithValidArgs_ShouldExecuteSuccessfully()
//         {
//             // Arrange
//             // Supply minimal valid arguments: URL, setting connections, duration, and warmup to zero to avoid delays.
//             string[] args = new string[] { "-u", "http://dummy", "-c", "0", "-d", "0", "-w", "0" };
// 
//             // Act
//             await Program.Main(args);
//             string output = _consoleOutput.ToString();
// 
//             // Assert
//             Assert.Contains("Pipelining Client", output);
//             Assert.Contains("Stopped...", output);
//         }
    }
}
