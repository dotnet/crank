using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    [TestClass]
    public class ProcessResultTests
    {
        private readonly ProcessResult _processResult;

        public ProcessResultTests()
        {
            _processResult = new ProcessResult(0, "output", "error");
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ProcessResult(int, string, string)"/> constructor to ensure it correctly initializes properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_InitializesProperties()
        {
            // Arrange
            int expectedExitCode = 0;
            string expectedStandardOutput = "output";
            string expectedStandardError = "error";

            // Act
            var result = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.AreEqual(expectedExitCode, result.ExitCode);
            Assert.AreEqual(expectedStandardOutput, result.StandardOutput);
            Assert.AreEqual(expectedStandardError, result.StandardError);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ExitCode"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void ExitCode_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            int expectedExitCode = 0;

            // Act
            int actualExitCode = _processResult.ExitCode;

            // Assert
            Assert.AreEqual(expectedExitCode, actualExitCode);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.StandardOutput"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void StandardOutput_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            string expectedStandardOutput = "output";

            // Act
            string actualStandardOutput = _processResult.StandardOutput;

            // Assert
            Assert.AreEqual(expectedStandardOutput, actualStandardOutput);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.StandardError"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void StandardError_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            string expectedStandardError = "error";

            // Act
            string actualStandardError = _processResult.StandardError;

            // Assert
            Assert.AreEqual(expectedStandardError, actualStandardError);
        }
    }
}
