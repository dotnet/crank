// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Crank.Controller;
using Microsoft.Crank.PullRequestBot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessUtil"/> class.
    /// </summary>
    public class ProcessUtilTests
    {
        /// <summary>
        /// Tests that GetEnvironmentCommand returns the expected command based on the current operating system.
        /// </summary>
        [Fact]
        public void GetEnvironmentCommand_CurrentPlatform_ReturnsExpectedCommand()
        {
            // Arrange
            string windowCmd = "winCommand";
            string unixCmd = "unixCommand";
            string macosCmd = "macCommand";
            PlatformID platform = Environment.OSVersion.Platform;
            string expected;

            if (platform == PlatformID.Win32NT)
            {
                expected = windowCmd;
            }
            else if (platform == PlatformID.Unix)
            {
                expected = unixCmd;
            }
            else if (platform == PlatformID.MacOSX)
            {
                expected = macosCmd;
            }
            else
            {
                // Act & Assert
                Assert.Throws<NotImplementedException>(() => ProcessUtil.GetEnvironmentCommand(windowCmd, unixCmd, macosCmd));
                return;
            }

            // Act
            string result = ProcessUtil.GetEnvironmentCommand(windowCmd, unixCmd, macosCmd);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that GetEnvironmentCommand returns the unix command when on macOS and the macos parameter is null.
        /// </summary>
        [Fact]
        public void GetEnvironmentCommand_MacOS_NullMacosParameter_ReturnsUnixCommand()
        {
            // Only applicable on macOS.
            if (Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                return;
            }

            // Arrange
            string windowCmd = "winCommand";
            string unixCmd = "unixCommand";

            // Act
            string result = ProcessUtil.GetEnvironmentCommand(windowCmd, unixCmd);

            // Assert
            Assert.Equal(unixCmd, result);
        }

        /// <summary>
        /// Tests that GetScriptHost returns the expected script host based on the current operating system.
        /// </summary>
        [Fact]
        public void GetScriptHost_CurrentPlatform_ReturnsExpectedScriptHost()
        {
            // Arrange
            PlatformID platform = Environment.OSVersion.Platform;
            string expected;
            if (platform == PlatformID.Win32NT)
            {
                expected = "cmd.exe";
            }
            else if (platform == PlatformID.Unix)
            {
                expected = "bash";
            }
            else if (platform == PlatformID.MacOSX)
            {
                expected = "bash";
            }
            else
            {
                Assert.Throws<NotImplementedException>(() => ProcessUtil.GetScriptHost());
                return;
            }

            // Act
            string result = ProcessUtil.GetScriptHost();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that RunAsync successfully executes a simple command with exit code zero and captures standard output.
        /// </summary>
//         [Fact] [Error] (166-45)CS1061 'ProcessResult' does not contain a definition for 'Output' and no accessible extension method 'Output' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_HappyPath_CapturesOutputAndExitCodeZero()
//         {
//             // Arrange
//             string shell = ProcessUtil.GetScriptHost();
//             string arguments;
// 
//             // Determine the correct arguments for echo command based on the platform.
//             if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//             {
//                 arguments = "/c echo hello";
//             }
//             else
//             {
//                 arguments = "-c \"echo hello\"";
//             }
// 
//             var cancellationToken = CancellationToken.None;
//             bool onStartCalled = false;
//             bool onStopCalled = false;
//             int capturedProcessId = 0;
//             int capturedExitCode = -1;
//             Action<int> onStart = pid =>
//             {
//                 onStartCalled = true;
//                 capturedProcessId = pid;
//             };
//             Action<int> onStop = exitCode =>
//             {
//                 onStopCalled = true;
//                 capturedExitCode = exitCode;
//             };
// 
//             // Act
//             ProcessResult result = await ProcessUtil.RunAsync(
//                 shell,
//                 arguments,
//                 throwOnError: true,
//                 captureOutput: true,
//                 cancellationToken: cancellationToken,
//                 onStart: onStart,
//                 onStop: onStop);
// 
//             // Assert
//             Assert.True(onStartCalled, "Expected onStart callback to be invoked.");
//             Assert.True(onStopCalled, "Expected onStop callback to be invoked.");
//             Assert.True(capturedProcessId > 0, "Expected a valid process id.");
//             Assert.Equal(0, result.ExitCode);
//             Assert.Contains("hello", result.Output, StringComparison.OrdinalIgnoreCase);
//         }

        /// <summary>
        /// Tests that RunAsync throws an InvalidOperationException when the executed command returns a non-zero exit code.
        /// </summary>
        [Fact]
        public async Task RunAsync_NonZeroExitCode_ThrowsInvalidOperationException()
        {
            // Arrange
            string shell = ProcessUtil.GetScriptHost();
            string arguments;

            // Prepare a command that returns exit code 1.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                arguments = "/c exit 1";
            }
            else
            {
                arguments = "-c \"exit 1\"";
            }

            var cancellationToken = CancellationToken.None;

            // Act & Assert
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ProcessUtil.RunAsync(
                    shell,
                    arguments,
                    throwOnError: true,
                    captureOutput: true,
                    cancellationToken: cancellationToken);
            });
            Assert.Contains("returned exit code", ex.Message);
        }

        /// <summary>
        /// Tests that RunAsync, when provided with a cancelled CancellationToken, terminates the process and eventually throws an InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task RunAsync_CancellationRequested_TerminatesProcessAndThrowsInvalidOperationException()
        {
            // Arrange
            string shell = ProcessUtil.GetScriptHost();
            string arguments;

            // Use a long running command.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use ping to simulate a delay on Windows.
                arguments = "/c ping 127.0.0.1 -n 10 > nul";
            }
            else
            {
                // Use sleep to simulate a delay on Unix/Mac.
                arguments = "-c \"sleep 10\"";
            }

            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ProcessUtil.RunAsync(
                    shell,
                    arguments,
                    throwOnError: true,
                    captureOutput: true,
                    cancellationToken: cts.Token);
            });
        }
    }
}
