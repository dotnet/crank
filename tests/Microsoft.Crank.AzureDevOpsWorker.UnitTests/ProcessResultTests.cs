using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    [TestClass]
    public class ProcessResultTests
    {
        private readonly int _exitCode;
        private readonly string _output;
        private readonly string _error;
        private readonly ProcessResult _processResult;

        public ProcessResultTests()
        {
            _exitCode = 0;
            _output = "Process completed successfully.";
            _error = string.Empty;
            _processResult = new ProcessResult(_exitCode, _output, _error);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ProcessResult(int, string, string)"/> constructor to ensure it correctly initializes properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_InitializesPropertiesCorrectly()
        {
            // Assert
            Assert.AreEqual(_exitCode, _processResult.ExitCode, "ExitCode was not initialized correctly.");
            Assert.AreEqual(_output, _processResult.Output, "Output was not initialized correctly.");
            Assert.AreEqual(_error, _processResult.Error, "Error was not initialized correctly.");
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.Error"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void Error_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            var error = _processResult.Error;

            // Assert
            Assert.AreEqual(_error, error, "Error property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ExitCode"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void ExitCode_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            var exitCode = _processResult.ExitCode;

            // Assert
            Assert.AreEqual(_exitCode, exitCode, "ExitCode property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.Output"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void Output_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            var output = _processResult.Output;

            // Assert
            Assert.AreEqual(_output, output, "Output property did not return the expected value.");
        }
    }
}
