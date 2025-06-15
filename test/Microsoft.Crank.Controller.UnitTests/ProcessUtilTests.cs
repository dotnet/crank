using Microsoft.Crank.Controller;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref = "ProcessUtil"/> class.
    /// </summary>
    public class ProcessUtilTests
    {
        private readonly string _scriptHost;
        private readonly PlatformID _currentPlatform;
        public ProcessUtilTests()
        {
            _scriptHost = ProcessUtil.GetScriptHost();
            _currentPlatform = Environment.OSVersion.Platform;
        }

        /// <summary>
        /// Tests the GetEnvironmentCommand method to ensure it returns the platform-specific command
        /// when all parameters are provided.
        /// </summary>
        [Fact]
        public void GetEnvironmentCommand_AllParametersProvided_ReturnsCorrectCommandForCurrentPlatform()
        {
            // Arrange
            string winCommand = "win";
            string unixCommand = "unix";
            string macCommand = "mac";
            // Act
            string result = ProcessUtil.GetEnvironmentCommand(winCommand, unixCommand, macCommand);
            // Assert
            string expected = _currentPlatform switch
            {
                PlatformID.Win32NT => winCommand,
                PlatformID.Unix => unixCommand,
                PlatformID.MacOSX => macCommand,
                _ => throw new NotImplementedException()};
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the GetEnvironmentCommand method to ensure that when the macOS parameter is omitted,
        /// on macOS it defaults to the Unix command.
        /// </summary>
        [Fact]
        public void GetEnvironmentCommand_MacParameterOmitted_ReturnsUnixCommandOnMacOS()
        {
            // Arrange
            string winCommand = "win";
            string unixCommand = "unix";
            // Act
            string result = ProcessUtil.GetEnvironmentCommand(winCommand, unixCommand);
            // Assert
            if (_currentPlatform == PlatformID.MacOSX)
            {
                Assert.Equal(unixCommand, result);
            }
            else
            {
                // For non-macOS platforms, result should match the expected based on the platform.
                string expected = _currentPlatform switch
                {
                    PlatformID.Win32NT => winCommand,
                    PlatformID.Unix => unixCommand,
                    _ => throw new NotImplementedException()};
                Assert.Equal(expected, result);
            }
        }

        /// <summary>
        /// Tests the GetScriptHost method to ensure it returns the correct script host for the current platform.
        /// </summary>
        [Fact]
        public void GetScriptHost_ReturnsCorrectScriptHostForCurrentPlatform()
        {
            // Arrange
            string expected = _currentPlatform switch
            {
                PlatformID.Win32NT => "cmd.exe",
                PlatformID.Unix => "bash",
                PlatformID.MacOSX => "bash",
                _ => throw new NotImplementedException()};
            // Act
            string result = ProcessUtil.GetScriptHost();
            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the RunAsync method for a successful execution that produces expected output.
        /// </summary>
//         [Fact] [Error] (123-45)CS1061 'ProcessResult' does not contain a definition for 'Output' and no accessible extension method 'Output' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_SuccessfulExecution_ReturnsExpectedOutput()
//         {
//             // Arrange
//             string filename;
//             string arguments;
//             if (_currentPlatform == PlatformID.Win32NT)
//             {
//                 filename = "cmd.exe";
//                 arguments = "/c echo hello";
//             }
//             else
//             {
//                 filename = "bash";
//                 // The -c argument takes the command to run.
//                 arguments = "-c \"echo hello\"";
//             }
// 
//             // Using captureOutput to capture the standard output.
//             bool captureOutput = true;
//             // Act
//             var result = await ProcessUtil.RunAsync(filename: filename, arguments: arguments, timeout: TimeSpan.FromSeconds(10), workingDirectory: null, throwOnError: true, environmentVariables: null, outputDataReceived: null, log: false, onStart: null, onStop: null, captureOutput: captureOutput, captureError: false, cancellationToken: CancellationToken.None);
//             // Assert
//             Assert.Equal(0, result.ExitCode);
//             Assert.Contains("hello", result.Output);
//         }

        /// <summary>
        /// Tests the RunAsync method to ensure that when the executed process returns a non-zero exit code,
        /// an InvalidOperationException is thrown if throwOnError is set to true.
        /// </summary>
        [Fact]
        public async Task RunAsync_NonZeroExitCodeWithThrowOnError_ThrowsInvalidOperationException()
        {
            // Arrange
            string filename;
            string arguments;
            if (_currentPlatform == PlatformID.Win32NT)
            {
                filename = "cmd.exe";
                arguments = "/c exit 1";
            }
            else
            {
                filename = "bash";
                arguments = "-c \"exit 1\"";
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ProcessUtil.RunAsync(filename: filename, arguments: arguments, timeout: TimeSpan.FromSeconds(10), workingDirectory: null, throwOnError: true, environmentVariables: null, outputDataReceived: null, log: false, onStart: null, onStop: null, captureOutput: true, captureError: true, cancellationToken: CancellationToken.None);
            });
            Assert.Contains("returned exit code", exception.Message);
        }

        /// <summary>
        /// Tests the RunAsync method with a cancellation token that is cancelled immediately,
        /// expecting the process to be terminated and an exception thrown if throwOnError is true.
        /// </summary>
        [Fact]
        public async Task RunAsync_CancellationTokenCancelled_ThrowsInvalidOperationException()
        {
            // Arrange
            string filename;
            string arguments;
            if (_currentPlatform == PlatformID.Win32NT)
            {
                filename = "cmd.exe";
                // Using a command that will wait for a while.
                arguments = "/c ping 127.0.0.1 -n 100 > nul";
            }
            else
            {
                filename = "bash";
                arguments = "-c \"sleep 10\"";
            }

            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ProcessUtil.RunAsync(filename: filename, arguments: arguments, timeout: TimeSpan.FromSeconds(10), workingDirectory: null, throwOnError: true, environmentVariables: null, outputDataReceived: null, log: false, onStart: null, onStop: null, captureOutput: true, captureError: true, cancellationToken: cts.Token);
            });
        }

        /// <summary>
        /// Tests the RunAsync method to ensure that when captureOutput and captureError are enabled,
        /// the process output and error streams are captured correctly.
        /// </summary>
//         [Fact] [Error] (212-43)CS1061 'ProcessResult' does not contain a definition for 'Output' and no accessible extension method 'Output' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?) [Error] (213-43)CS1061 'ProcessResult' does not contain a definition for 'Error' and no accessible extension method 'Error' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_CaptureOutputAndError_ReturnsCapturedData()
//         {
//             // Arrange
//             string filename;
//             string arguments;
//             if (_currentPlatform == PlatformID.Win32NT)
//             {
//                 filename = "cmd.exe";
//                 // This command writes to both stdout and stderr.
//                 arguments = "/c (echo out & echo err 1>&2)";
//             }
//             else
//             {
//                 filename = "bash";
//                 arguments = "-c \"echo out; echo err 1>&2\"";
//             }
// 
//             // Act
//             var result = await ProcessUtil.RunAsync(filename: filename, arguments: arguments, timeout: TimeSpan.FromSeconds(10), workingDirectory: null, throwOnError: false, environmentVariables: new Dictionary<string, string>(), outputDataReceived: null, log: false, onStart: null, onStop: null, captureOutput: true, captureError: true, cancellationToken: CancellationToken.None);
//             // Assert
//             Assert.Equal(0, result.ExitCode);
//             Assert.Contains("out", result.Output);
//             Assert.Contains("err", result.Error);
//         }

        /// <summary>
        /// Tests the RunAsync method to verify that the onStart and onStop callbacks are invoked with the expected values.
        /// </summary>
        [Fact]
        public async Task RunAsync_OnStartAndOnStopCallbacks_AreInvokedWithExpectedValues()
        {
            // Arrange
            string filename;
            string arguments;
            if (_currentPlatform == PlatformID.Win32NT)
            {
                filename = "cmd.exe";
                arguments = "/c echo callback";
            }
            else
            {
                filename = "bash";
                arguments = "-c \"echo callback\"";
            }

            int startedProcessId = 0;
            int? exitCodeFromCallback = null;
            void OnStart(int pid)
            {
                startedProcessId = pid;
            }

            void OnStop(int exitCode)
            {
                exitCodeFromCallback = exitCode;
            }

            // Act
            var result = await ProcessUtil.RunAsync(filename: filename, arguments: arguments, timeout: TimeSpan.FromSeconds(10), workingDirectory: null, throwOnError: true, environmentVariables: null, outputDataReceived: null, log: false, onStart: OnStart, onStop: OnStop, captureOutput: true, captureError: false, cancellationToken: CancellationToken.None);
            // Assert
            Assert.True(startedProcessId > 0, "Expected onStart callback to be invoked with a valid process Id.");
            Assert.True(exitCodeFromCallback.HasValue, "Expected onStop callback to be invoked.");
            Assert.Equal(result.ExitCode, exitCodeFromCallback.Value);
        }
    }
}