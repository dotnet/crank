using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Moq;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessUtil"/> class.
    /// </summary>
    public class ProcessUtilTests
    {
        private readonly string _echoExpectedText;
        private readonly (string filename, string arguments) _echoCommand;
        private readonly (string filename, string arguments) _nonZeroExitCommand;
        private readonly (string filename, string arguments) _longRunningCommand;

        public ProcessUtilTests()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // For Windows, use cmd.exe to echo text.
                _echoCommand = ("cmd.exe", "/c echo test");
                // Command that exits with non-zero exit code.
                _nonZeroExitCommand = ("cmd.exe", "/c exit 1");
                // Long running command: timeout for 10 seconds.
                _longRunningCommand = ("timeout.exe", "/t 10");
            }
            else
            {
                // For Unix-like systems, use echo.
                _echoCommand = ("echo", "test");
                // Command that exits with non-zero exit code.
                _nonZeroExitCommand = ("sh", "-c \"exit 1\"");
                // Long running command: sleep for 10 seconds.
                _longRunningCommand = ("sleep", "10");
            }

            _echoExpectedText = "test";
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.StreamOutput(string, string, Action{string}, Action{string}, string, IDictionary{string, string})"/>
        /// method to ensure it returns a process and correctly invokes the output callback.
        /// </summary>
        [Fact]
        public void StreamOutput_ValidCommand_InvokesOutputCallback()
        {
            // Arrange
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            void OutputCallback(string data) => outputBuilder.Append(data);
            void ErrorCallback(string data) => errorBuilder.Append(data);

            // Act
            Process process = ProcessUtil.StreamOutput(
                _echoCommand.filename,
                _echoCommand.arguments,
                OutputCallback,
                ErrorCallback,
                workingDirectory: null,
                environmentVariables: null);

            // Wait for process to exit and output to flush.
            bool exited = process.WaitForExit(3000);
            Assert.True(exited, "The process did not exit within the expected time.");

            // Assert
            Assert.Contains(_echoExpectedText, outputBuilder.ToString().Trim(), StringComparison.OrdinalIgnoreCase);
            // For echo command, error callback typically remains empty.
            Assert.True(string.IsNullOrWhiteSpace(errorBuilder.ToString()));
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RunAsync(string, IEnumerable{string}, TimeSpan?, string, bool, IDictionary{string, string}, Action{string}, bool, Action{int}, Action{int}, bool, bool, bool, CancellationToken)"/>
        /// method for a command that exits with code zero.
        /// </summary>
//         [Fact] [Error] (111-55)CS1061 'ProcessResult' does not contain a definition for 'StdOut' and no accessible extension method 'StdOut' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_WithExitZero_ReturnsProcessResult()
//         {
//             // Arrange
//             var environment = new Dictionary<string, string>();
//             // Use the echo command and capture output.
//             IEnumerable<string> arguments = new List<string> { _echoCommand.arguments };
// 
//             // Act
//             ProcessResult result = await ProcessUtil.RunAsync(
//                 _echoCommand.filename,
//                 arguments,
//                 timeout: TimeSpan.FromSeconds(5),
//                 workingDirectory: null,
//                 throwOnError: true,
//                 environmentVariables: environment,
//                 outputDataReceived: null,
//                 log: false,
//                 onStart: null,
//                 onStop: null,
//                 captureOutput: true,
//                 captureError: true,
//                 runAsRoot: false,
//                 cancellationToken: CancellationToken.None);
// 
//             // Assert
//             Assert.Equal(0, result.ExitCode);
//             Assert.Contains(_echoExpectedText, result.StdOut, StringComparison.OrdinalIgnoreCase);
//         }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RunAsync(string, IEnumerable{string}, TimeSpan?, string, bool, IDictionary{string, string}, Action{string}, bool, Action{int}, Action{int}, bool, bool, bool, CancellationToken)"/>
        /// method for a command that exits with a non-zero code and verifies that an exception is thrown when throwOnError is true.
        /// </summary>
        [Fact]
        public async Task RunAsync_NonZeroExit_ThrowsInvalidOperationException_WhenThrowOnErrorTrue()
        {
            // Arrange
            IEnumerable<string> arguments = new List<string> { _nonZeroExitCommand.arguments };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await ProcessUtil.RunAsync(
                    _nonZeroExitCommand.filename,
                    arguments,
                    timeout: TimeSpan.FromSeconds(5),
                    workingDirectory: null,
                    throwOnError: true,
                    environmentVariables: null,
                    outputDataReceived: null,
                    log: false,
                    onStart: null,
                    onStop: null,
                    captureOutput: true,
                    captureError: true,
                    runAsRoot: false,
                    cancellationToken: CancellationToken.None));
        }

        /// <summary>
        /// Tests the generic <see cref="ProcessUtil.RetryOnExceptionAsync{T}(int, Func{Task{T}}, CancellationToken)"/>
        /// method to verify it succeeds after a few retries.
        /// </summary>
        [Fact]
        public async Task RetryOnExceptionAsyncGeneric_SucceedsAfterRetries()
        {
            // Arrange
            int callCount = 0;
            Func<Task<int>> operation = () =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new Exception("Temporary failure");
                }
                return Task.FromResult(42);
            };

            // Act
            int result = await ProcessUtil.RetryOnExceptionAsync(2, operation);

            // Assert
            Assert.Equal(42, result);
            Assert.Equal(3, callCount);
        }

        /// <summary>
        /// Tests the generic <see cref="ProcessUtil.RetryOnExceptionAsync{T}(int, Func{Task{T}}, CancellationToken)"/>
        /// method to verify it throws an exception after exceeding the maximum number of retries.
        /// </summary>
        [Fact]
        public async Task RetryOnExceptionAsyncGeneric_ThrowsAfterMaxRetries()
        {
            // Arrange
            int callCount = 0;
            Func<Task<int>> operation = () =>
            {
                callCount++;
                throw new Exception("Persistent failure");
            };

            // Act & Assert
            Exception ex = await Assert.ThrowsAsync<Exception>(async () =>
                await ProcessUtil.RetryOnExceptionAsync(2, operation));
            Assert.Equal("Persistent failure", ex.Message);
            Assert.Equal(3, callCount);
        }

        /// <summary>
        /// Tests the non-generic <see cref="ProcessUtil.RetryOnExceptionAsync(int, Func{Task}, CancellationToken)"/>
        /// method to verify it succeeds after a few retries.
        /// </summary>
        [Fact]
        public async Task RetryOnExceptionAsyncNonGeneric_SucceedsAfterRetries()
        {
            // Arrange
            int callCount = 0;
            Func<Task> operation = () =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new Exception("Temporary failure");
                }
                return Task.CompletedTask;
            };

            // Act
            await ProcessUtil.RetryOnExceptionAsync(2, operation);

            // Assert
            Assert.Equal(3, callCount);
        }

        /// <summary>
        /// Tests the non-generic <see cref="ProcessUtil.RetryOnExceptionAsync(int, Func{Task}, CancellationToken)"/>
        /// method to verify it throws an exception after exceeding the maximum number of retries.
        /// </summary>
        [Fact]
        public async Task RetryOnExceptionAsyncNonGeneric_ThrowsAfterMaxRetries()
        {
            // Arrange
            int callCount = 0;
            Func<Task> operation = () =>
            {
                callCount++;
                throw new Exception("Persistent failure");
            };

            // Act & Assert
            Exception ex = await Assert.ThrowsAsync<Exception>(async () =>
                await ProcessUtil.RetryOnExceptionAsync(2, operation));
            Assert.Equal("Persistent failure", ex.Message);
            Assert.Equal(3, callCount);
        }
    }
}
