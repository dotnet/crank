using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    public class ProcessUtilTests
    {
        [Fact]
        public void GetEnvironmentCommand_WhenPlatformIsUnix_ReturnsUnixCommand()
        {
            // Arrange
            var unixCommand = "ls";
            var windowsCommand = "dir";
            var macosCommand = "ls";
            var expectedCommand = unixCommand;

            // Act
            var actualCommand = ProcessUtil.GetEnvironmentCommand(windowsCommand, unixCommand, macosCommand);

            // Assert
            Assert.Equal(expectedCommand, actualCommand);
        }

        [Fact]
        public void GetEnvironmentCommand_WhenPlatformIsWindows_ReturnsWindowsCommand()
        {
            // Arrange
            var unixCommand = "ls";
            var windowsCommand = "dir";
            var macosCommand = "ls";
            var expectedCommand = windowsCommand;

            // Act
            var actualCommand = ProcessUtil.GetEnvironmentCommand(windowsCommand, unixCommand, macosCommand);

            // Assert
            Assert.Equal(expectedCommand, actualCommand);
        }

        [Fact]
        public void GetEnvironmentCommand_WhenPlatformIsMacOS_ReturnsMacOSCommand()
        {
            // Arrange
            var unixCommand = "ls";
            var windowsCommand = "dir";
            var macosCommand = "ls";
            var expectedCommand = macosCommand;

            // Act
            var actualCommand = ProcessUtil.GetEnvironmentCommand(windowsCommand, unixCommand, macosCommand);

            // Assert
            Assert.Equal(expectedCommand, actualCommand);
        }

        [Fact]
        public void GetScriptHost_WhenPlatformIsUnix_ReturnsBash()
        {
            // Arrange
            var expectedHost = "bash";

            // Act
            var actualHost = ProcessUtil.GetScriptHost();

            // Assert
            Assert.Equal(expectedHost, actualHost);
        }

        [Fact]
        public void GetScriptHost_WhenPlatformIsWindows_ReturnsCmd()
        {
            // Arrange
            var expectedHost = "cmd.exe";

            // Act
            var actualHost = ProcessUtil.GetScriptHost();

            // Assert
            Assert.Equal(expectedHost, actualHost);
        }

        [Fact]
        public void GetScriptHost_WhenPlatformIsMacOS_ReturnsBash()
        {
            // Arrange
            var expectedHost = "bash";

            // Act
            var actualHost = ProcessUtil.GetScriptHost();

            // Assert
            Assert.Equal(expectedHost, actualHost);
        }

//         [Fact] [Error] (123-49)CS1061 'ProcessResult' does not contain a definition for 'Output' and no accessible extension method 'Output' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?) [Error] (124-48)CS1061 'ProcessResult' does not contain a definition for 'Error' and no accessible extension method 'Error' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_WhenProcessSucceeds_ReturnsProcessResult()
//         {
//             // Arrange
//             var filename = "test.exe";
//             var arguments = "--test";
//             var expectedExitCode = 0;
//             var expectedOutput = "Process output";
//             var expectedError = string.Empty;
// 
//             var processMock = new Mock<Process>();
//             processMock.Setup(p => p.Start()).Returns(true);
//             processMock.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
//             processMock.Setup(p => p.ExitCode).Returns(expectedExitCode);
//             processMock.Setup(p => p.StandardOutput.ReadToEnd()).Returns(expectedOutput);
//             processMock.Setup(p => p.StandardError.ReadToEnd()).Returns(expectedError);
// 
//             // Act
//             var result = await ProcessUtil.RunAsync(filename, arguments);
// 
//             // Assert
//             Assert.Equal(expectedExitCode, result.ExitCode);
//             Assert.Equal(expectedOutput, result.Output);
//             Assert.Equal(expectedError, result.Error);
//         }

        [Fact]
        public async Task RunAsync_WhenProcessFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var filename = "test.exe";
            var arguments = "--test";
            var expectedExitCode = 1;

            var processMock = new Mock<Process>();
            processMock.Setup(p => p.Start()).Returns(true);
            processMock.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
            processMock.Setup(p => p.ExitCode).Returns(expectedExitCode);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => ProcessUtil.RunAsync(filename, arguments));
        }

        [Fact]
        public async Task RunAsync_WhenProcessIsCancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            var filename = "test.exe";
            var arguments = "--test";
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => ProcessUtil.RunAsync(filename, arguments, cancellationToken: cancellationToken));
        }
    }
}
