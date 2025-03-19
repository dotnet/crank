using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
        /// Tests the <see cref="ProcessUtil.GetEnvironmentCommand(string, string, string)"/> method to ensure it returns the correct command based on the platform.
        /// </summary>
        [Theory]
        [InlineData(PlatformID.Unix, "unixCommand", "winCommand", "macCommand", "unixCommand")]
        [InlineData(PlatformID.Win32NT, "unixCommand", "winCommand", "macCommand", "winCommand")]
        [InlineData(PlatformID.MacOSX, "unixCommand", "winCommand", "macCommand", "macCommand")]
        [InlineData(PlatformID.MacOSX, "unixCommand", "winCommand", null, "unixCommand")]
        public void GetEnvironmentCommand_WhenCalled_ReturnsCorrectCommand(PlatformID platform, string unixCommand, string winCommand, string macCommand, string expectedCommand)
        {
            // Arrange
            var originalPlatform = Environment.OSVersion.Platform;
            typeof(Environment).GetField("s_platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, platform);

            // Act
            var result = ProcessUtil.GetEnvironmentCommand(winCommand, unixCommand, macCommand);

            // Assert
            Assert.Equal(expectedCommand, result);

            // Cleanup
            typeof(Environment).GetField("s_platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, originalPlatform);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.GetScriptHost"/> method to ensure it returns the correct script host based on the platform.
        /// </summary>
        [Theory]
        [InlineData(PlatformID.Unix, "bash")]
        [InlineData(PlatformID.Win32NT, "cmd.exe")]
        [InlineData(PlatformID.MacOSX, "bash")]
        public void GetScriptHost_WhenCalled_ReturnsCorrectScriptHost(PlatformID platform, string expectedHost)
        {
            // Arrange
            var originalPlatform = Environment.OSVersion.Platform;
            typeof(Environment).GetField("s_platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, platform);

            // Act
            var result = ProcessUtil.GetScriptHost();

            // Assert
            Assert.Equal(expectedHost, result);

            // Cleanup
            typeof(Environment).GetField("s_platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, originalPlatform);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RunAsync(string, string, TimeSpan?, string, bool, IDictionary{string, string}, Action{string}, bool, Action{int}, Action{int}, bool, bool, CancellationToken)"/> method to ensure it runs a process and returns the correct result.
        /// </summary>
        [Fact]
        public async Task RunAsync_WhenCalled_ReturnsCorrectResult()
        {
            // Arrange
            var filename = "dotnet";
            var arguments = "--version";
            var timeout = TimeSpan.FromSeconds(10);
            var workingDirectory = Directory.GetCurrentDirectory();
            var environmentVariables = new Dictionary<string, string> { { "ENV_VAR", "value" } };
            var outputDataReceived = new Action<string>(output => { });
            var onStart = new Action<int>(pid => { });
            var onStop = new Action<int>(exitCode => { });
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await ProcessUtil.RunAsync(filename, arguments, timeout, workingDirectory, true, environmentVariables, outputDataReceived, true, onStart, onStop, true, true, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ExitCode);
        }
    }
}
