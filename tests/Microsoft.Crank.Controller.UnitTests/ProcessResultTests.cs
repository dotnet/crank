using Moq;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    public class ProcessResultTests
    {
        private readonly int _exitCode;
        private readonly string _standardOutput;
        private readonly string _standardError;
        private readonly ProcessResult _processResult;

        public ProcessResultTests()
        {
            _exitCode = 0;
            _standardOutput = "Success";
            _standardError = string.Empty;
            _processResult = new ProcessResult(_exitCode, _standardOutput, _standardError);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ProcessResult(int, string, string)"/> constructor to ensure it correctly initializes properties.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalledWithValidParameters_InitializesPropertiesCorrectly()
        {
            // Assert
            Assert.Equal(_exitCode, _processResult.ExitCode);
            Assert.Equal(_standardOutput, _processResult.StandardOutput);
            Assert.Equal(_standardError, _processResult.StandardError);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.ExitCode"/> property to ensure it returns the correct exit code.
        /// </summary>
        [Fact]
        public void ExitCode_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            int exitCode = _processResult.ExitCode;

            // Assert
            Assert.Equal(_exitCode, exitCode);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.StandardOutput"/> property to ensure it returns the correct standard output.
        /// </summary>
        [Fact]
        public void StandardOutput_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            string standardOutput = _processResult.StandardOutput;

            // Assert
            Assert.Equal(_standardOutput, standardOutput);
        }

        /// <summary>
        /// Tests the <see cref="ProcessResult.StandardError"/> property to ensure it returns the correct standard error.
        /// </summary>
        [Fact]
        public void StandardError_WhenAccessed_ReturnsCorrectValue()
        {
            // Act
            string standardError = _processResult.StandardError;

            // Assert
            Assert.Equal(_standardError, standardError);
        }
    }
}
