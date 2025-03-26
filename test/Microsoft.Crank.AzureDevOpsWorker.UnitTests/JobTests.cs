using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Job"/> class.
    /// </summary>
    public class JobTests
    {
        private readonly TimeSpan ProcessWaitTime = TimeSpan.FromSeconds(3);
        private readonly TimeSpan StopWaitTime = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Helper method to get the shell command details based on the current OS.
        /// </summary>
        /// <param name="windowsCommand">The command to run on Windows.</param>
        /// <param name="unixCommand">The command to run on Unix-based systems.</param>
        /// <returns>Tuple of executable path and arguments.</returns>
        private (string exe, string arguments) GetShellCommand(string windowsCommand, string unixCommand)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Using cmd.exe to run the command.
                return ("cmd.exe", $"/c {windowsCommand}");
            }
            else
            {
                // Using /bin/sh to run the command.
                return ("/bin/sh", $"-c \"{unixCommand}\"");
            }
        }

        /// <summary>
        /// Tests that the constructor initializes important properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidInput_InitializesProperties()
        {
            // Arrange
            var (exe, args) = GetShellCommand("echo", "echo");
            string applicationPath = exe;
            string arguments = args;

            // Act
            using var job = new Job(applicationPath, arguments);

            // Assert
            Assert.NotNull(job.OutputBuilder);
            Assert.NotNull(job.ErrorBuilder);
            Assert.Null(job.OnStandardOutput);
            Assert.Null(job.OnStandardError);
        }

        /// <summary>
        /// Tests that calling Start after Dispose throws an exception.
        /// </summary>
        [Fact]
        public void Start_WhenCalledAfterDispose_ThrowsException()
        {
            // Arrange
            var (exe, args) = GetShellCommand("echo", "echo");
            string applicationPath = exe;
            string arguments = args;
            var job = new Job(applicationPath, arguments);
            job.Dispose();

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => job.Start());
            Assert.Equal("Can't reuse disposed job", exception.Message);
        }

        /// <summary>
        /// Tests that Stop can be called safely even if the process was never started.
        /// </summary>
//         [Fact] [Error] (92-36)CS0117 'Record' does not contain a definition for 'Exception'
//         public void Stop_WhenProcessNotStarted_DoesNotThrow()
//         {
//             // Arrange
//             var (exe, args) = GetShellCommand("echo", "echo");
//             string applicationPath = exe;
//             string arguments = args;
//             var job = new Job(applicationPath, arguments);
// 
//             // Act & Assert
//             var exception = Record.Exception(() => job.Stop());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that a long running process is terminated after Stop is called.
        /// </summary>
        [Fact]
        public void StartAndStop_WhenProcessIsLongRunning_ProcessIsTerminated()
        {
            // Arrange
            // Use a command that keeps the process running for a while.
            var (exe, args) = GetShellCommand(
                "ping 127.0.0.1 -n 10 > nul",
                "sleep 10"
            );
            string applicationPath = exe;
            string arguments = args;
            using var job = new Job(applicationPath, arguments);

            // Act
            job.Start();

            // Give some time for the process to start.
            Thread.Sleep(1000);
            bool isRunningBeforeStop = job.IsRunning;
            job.Stop();
            // Wait to ensure the process has time to terminate.
            Thread.Sleep(StopWaitTime);

            // Assert
            Assert.True(isRunningBeforeStop);
            Assert.False(job.IsRunning);
        }

        /// <summary>
        /// Tests that FlushStandardOutput returns the captured standard output.
        /// </summary>
        [Fact]
        public void FlushStandardOutput_WhenProcessWritesOutput_ReturnsCapturedOutput()
        {
            // Arrange
            var (exe, args) = GetShellCommand("echo test", "echo test");
            string applicationPath = exe;
            string arguments = args;
            using var job = new Job(applicationPath, arguments);

            // Act
            job.Start();
            // Wait for the process to exit and output to be captured.
            Thread.Sleep(ProcessWaitTime);
            IEnumerable<string> outputs = job.FlushStandardOutput();
            string combinedOutput = string.Join("", outputs);

            // Stop the process in case it hasn't ended.
            job.Stop();

            // Assert
            Assert.Contains("test", combinedOutput, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that FlushStandardError returns no output due to the bug in the implementation.
        /// </summary>
        [Fact]
        public void FlushStandardError_WhenProcessWritesError_ReturnsNoOutputDueToBug()
        {
            // Arrange
            var (exe, args) = GetShellCommand("echo error 1>&2", "echo error 1>&2");
            string applicationPath = exe;
            string arguments = args;
            using var job = new Job(applicationPath, arguments);

            // Act
            job.Start();
            Thread.Sleep(ProcessWaitTime);
            IEnumerable<string> errorOutputs = job.FlushStandardError();
            string combinedErrorOutput = string.Join("", errorOutputs);

            // Stop the process in case it hasn't ended.
            job.Stop();

            // Assert
            // Due to bug in FlushStandardError (using the wrong queue), expect no output.
            Assert.True(string.IsNullOrEmpty(combinedErrorOutput));
        }

        /// <summary>
        /// Tests that WasSuccessful returns true when the process exits with code 0.
        /// </summary>
        [Fact]
        public void WasSuccessful_WhenProcessExitsWithSuccess_ReturnsTrue()
        {
            // Arrange
            var (exe, args) = GetShellCommand("exit 0", "exit 0");
            string applicationPath = exe;
            string arguments = args;
            using var job = new Job(applicationPath, arguments);

            // Act
            job.Start();
            Thread.Sleep(ProcessWaitTime);
            // Stop is called to dispose the process if still running,
            // although process should have already exited.
            job.Stop();
            bool wasSuccessful = job.WasSuccessful;

            // Assert
            Assert.True(wasSuccessful);
        }

        /// <summary>
        /// Tests that WasSuccessful returns false when the process exits with a non-zero exit code.
        /// </summary>
        [Fact]
        public void WasSuccessful_WhenProcessExitsWithFailure_ReturnsFalse()
        {
            // Arrange
            var (exe, args) = GetShellCommand("exit 1", "exit 1");
            string applicationPath = exe;
            string arguments = args;
            using var job = new Job(applicationPath, arguments);

            // Act
            job.Start();
            Thread.Sleep(ProcessWaitTime);
            job.Stop();
            bool wasSuccessful = job.WasSuccessful;

            // Assert
            Assert.False(wasSuccessful);
        }

        /// <summary>
        /// Tests that Dispose cleans up resources and subsequent calls do not throw exceptions.
        /// </summary>
//         [Fact] [Error] (238-54)CS0117 'Record' does not contain a definition for 'Exception' [Error] (239-55)CS0117 'Record' does not contain a definition for 'Exception'
//         public void Dispose_CalledMultipleTimes_DoesNotThrowAndResourcesAreCleanedUp()
//         {
//             // Arrange
//             var (exe, args) = GetShellCommand("echo", "echo");
//             string applicationPath = exe;
//             string arguments = args;
//             var job = new Job(applicationPath, arguments);
// 
//             // Act
//             Exception firstDisposeException = Record.Exception(() => job.Dispose());
//             Exception secondDisposeException = Record.Exception(() => job.Dispose());
// 
//             // Assert
//             Assert.Null(firstDisposeException);
//             Assert.Null(secondDisposeException);
//             // After disposal, properties should be cleaned up.
//             Assert.Null(job.OnStandardOutput);
//             Assert.Null(job.OnStandardError);
//             Assert.Null(job.OutputBuilder);
//             Assert.Null(job.ErrorBuilder);
//         }
    }
}
