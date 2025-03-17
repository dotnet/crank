using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessUtil"/> class.
    /// </summary>
    [TestClass]
    public class ProcessUtilTests
    {
        private readonly Mock<Action<string>> _mockOutputCallback;
        private readonly Mock<Action<string>> _mockErrorCallback;
        private readonly Mock<Action<int>> _mockOnStartCallback;
        private readonly Mock<Action<int>> _mockOnStopCallback;

        public ProcessUtilTests()
        {
            _mockOutputCallback = new Mock<Action<string>>();
            _mockErrorCallback = new Mock<Action<string>>();
            _mockOnStartCallback = new Mock<Action<int>>();
            _mockOnStopCallback = new Mock<Action<int>>();
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.StreamOutput"/> method to ensure it correctly starts a process and streams output.
        /// </summary>
        [TestMethod]
        public void StreamOutput_ValidParameters_ProcessStartedAndOutputStreamed()
        {
            // Arrange
            string filename = "cmd.exe";
            string arguments = "/c echo Hello World";
            string workingDirectory = null;
            IDictionary<string, string> environmentVariables = null;

            // Act
            var process = ProcessUtil.StreamOutput(
                filename,
                arguments,
                _mockOutputCallback.Object,
                _mockErrorCallback.Object,
                workingDirectory,
                environmentVariables);

            // Assert
            Assert.IsNotNull(process);
            Assert.IsFalse(process.HasExited);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RunAsync(string, IEnumerable{string}, TimeSpan?, string, bool, IDictionary{string, string}, Action{string}, bool, Action{int}, Action{int}, bool, bool, bool, CancellationToken)"/> method to ensure it correctly runs a process asynchronously.
        /// </summary>
        [TestMethod]
        public async Task RunAsync_ValidParameters_ProcessRunsSuccessfully()
        {
            // Arrange
            string filename = "cmd.exe";
            IEnumerable<string> arguments = new List<string> { "/c", "echo Hello World" };
            TimeSpan? timeout = TimeSpan.FromSeconds(10);
            string workingDirectory = null;
            bool throwOnError = true;
            IDictionary<string, string> environmentVariables = null;
            bool log = false;
            bool captureOutput = true;
            bool captureError = true;
            bool runAsRoot = false;
            CancellationToken cancellationToken = default;

            // Act
            var result = await ProcessUtil.RunAsync(
                filename,
                arguments,
                timeout,
                workingDirectory,
                throwOnError,
                environmentVariables,
                _mockOutputCallback.Object,
                log,
                _mockOnStartCallback.Object,
                _mockOnStopCallback.Object,
                captureOutput,
                captureError,
                runAsRoot,
                cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.ExitCode);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RetryOnExceptionAsync{T}(int, Func{Task{T}}, CancellationToken)"/> method to ensure it retries the operation on exception.
        /// </summary>
        [TestMethod]
        public async Task RetryOnExceptionAsync_OperationFailsInitially_RetriesAndSucceeds()
        {
            // Arrange
            int retries = 3;
            int attempt = 0;
            Func<Task<int>> operation = async () =>
            {
                attempt++;
                if (attempt < 3)
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                return await Task.FromResult(42);
            };

            // Act
            var result = await ProcessUtil.RetryOnExceptionAsync(retries, operation);

            // Assert
            Assert.AreEqual(42, result);
        }

        /// <summary>
        /// Tests the <see cref="ProcessUtil.RetryOnExceptionAsync(int, Func{Task}, CancellationToken)"/> method to ensure it retries the operation on exception.
        /// </summary>
        [TestMethod]
        public async Task RetryOnExceptionAsync_VoidOperationFailsInitially_RetriesAndSucceeds()
        {
            // Arrange
            int retries = 3;
            int attempt = 0;
            Func<Task> operation = async () =>
            {
                attempt++;
                if (attempt < 3)
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                await Task.CompletedTask;
            };

            // Act
            await ProcessUtil.RetryOnExceptionAsync(retries, operation);

            // Assert
            Assert.AreEqual(3, attempt);
        }
    }
}
