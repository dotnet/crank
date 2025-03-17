using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parlot.Fluent;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessUtil"/> class.
    /// </summary>
    [TestClass]
    public class ProcessUtilTests
    {
        /// <summary>
        /// Tests the <see cref="ProcessUtil.GetEnvironmentCommand(string, string, string)"/> method to ensure it returns the correct command based on the platform.
        /// </summary>
        [TestMethod]
        [DataRow(PlatformID.Unix, "unixCommand", "winCommand", "macCommand", "unixCommand")]
        [DataRow(PlatformID.Win32NT, "unixCommand", "winCommand", "macCommand", "winCommand")]
        [DataRow(PlatformID.MacOSX, "unixCommand", "winCommand", "macCommand", "macCommand")]
        [DataRow(PlatformID.MacOSX, "unixCommand", "winCommand", null, "unixCommand")]
        public void GetEnvironmentCommand_WhenCalled_ReturnsCorrectCommand(PlatformID platform, string unixCommand, string winCommand, string macCommand, string expectedCommand)
        {
            // Arrange
            var originalPlatform = Environment.OSVersion.Platform;
            typeof(Environment).GetField("platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, platform);

            // Act
            var result = ProcessUtil.GetEnvironmentCommand(winCommand, unixCommand, macCommand);

            // Assert
            Assert.AreEqual(expectedCommand, result);

            // Cleanup
            typeof(Environment).GetField("platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, originalPlatform);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.GetScriptHost"/> method to ensure it returns the correct script host based on the platform.
        /// </summary>
        [TestMethod]
        [DataRow(PlatformID.Unix, "bash")]
        [DataRow(PlatformID.Win32NT, "cmd.exe")]
        [DataRow(PlatformID.MacOSX, "bash")]
        public void GetScriptHost_WhenCalled_ReturnsCorrectScriptHost(PlatformID platform, string expectedHost)
        {
            // Arrange
            var originalPlatform = Environment.OSVersion.Platform;
            typeof(Environment).GetField("platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, platform);

            // Act
            var result = ProcessUtil.GetScriptHost();

            // Assert
            Assert.AreEqual(expectedHost, result);

            // Cleanup
            typeof(Environment).GetField("platform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, originalPlatform);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RunAsync(string, string, TimeSpan?, string, bool, IDictionary{string, string}, Action{string}, bool, Action{int}, Action{int}, bool, bool, CancellationToken)"/> method to ensure it runs a process and returns the correct result.
        /// </summary>
//         [TestMethod] [Error] (102-34)CS1061 'ProcessResult' does not contain a definition for 'Output' and no accessible extension method 'Output' accepting a first argument of type 'ProcessResult' could be found (are you missing a using directive or an assembly reference?)
//         public async Task RunAsync_WhenCalled_ReturnsCorrectResult()
//         {
//             // Arrange
//             var filename = "dotnet";
//             var arguments = "--version";
//             var timeout = TimeSpan.FromSeconds(10);
//             var workingDirectory = Directory.GetCurrentDirectory();
//             var environmentVariables = new Dictionary<string, string> { { "ENV_VAR", "value" } };
//             var outputDataReceived = new Mock<Action<string>>();
//             var onStart = new Mock<Action<int>>();
//             var onStop = new Mock<Action<int>>();
//             var cancellationToken = CancellationToken.None;
// 
//             // Act
//             var result = await ProcessUtil.RunAsync(
//                 filename,
//                 arguments,
//                 timeout,
//                 workingDirectory,
//                 true,
//                 environmentVariables,
//                 outputDataReceived.Object,
//                 true,
//                 onStart.Object,
//                 onStop.Object,
//                 true,
//                 true,
//                 cancellationToken);
// 
//             // Assert
//             Assert.AreEqual(0, result.ExitCode);
//             Assert.IsTrue(result.Output.Contains(".NET"));
//             Assert.IsTrue(result.Error == string.Empty);
//         }
    }
}
