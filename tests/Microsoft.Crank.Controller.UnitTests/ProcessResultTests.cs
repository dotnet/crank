using System;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    public class ProcessResultTests
    {
        /// <summary>
        /// Tests that the ProcessResult constructor correctly assigns provided values for exit code, standard output, and standard error.
        /// </summary>
        [Fact]
        public void Constructor_WithValidValues_PropertiesAreAssignedCorrectly()
        {
            // Arrange
            int expectedExitCode = 0;
            string expectedStandardOutput = "Output message";
            string expectedStandardError = "Error message";

            // Act
            var result = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, result.ExitCode);
            Assert.Equal(expectedStandardOutput, result.StandardOutput);
            Assert.Equal(expectedStandardError, result.StandardError);
        }

        /// <summary>
        /// Tests that the ProcessResult constructor assigns a null StandardOutput when it is passed in as null.
        /// </summary>
        [Fact]
        public void Constructor_WithNullStandardOutput_PropertiesAreAssignedCorrectly()
        {
            // Arrange
            int expectedExitCode = 1;
            string? expectedStandardOutput = null;
            string expectedStandardError = "Error message";

            // Act
            var result = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, result.ExitCode);
            Assert.Null(result.StandardOutput);
            Assert.Equal(expectedStandardError, result.StandardError);
        }

        /// <summary>
        /// Tests that the ProcessResult constructor assigns a null StandardError when it is passed in as null.
        /// </summary>
        [Fact]
        public void Constructor_WithNullStandardError_PropertiesAreAssignedCorrectly()
        {
            // Arrange
            int expectedExitCode = 2;
            string expectedStandardOutput = "Output message";
            string? expectedStandardError = null;

            // Act
            var result = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, result.ExitCode);
            Assert.Equal(expectedStandardOutput, result.StandardOutput);
            Assert.Null(result.StandardError);
        }

        /// <summary>
        /// Tests that the ProcessResult constructor correctly assigns negative exit code and empty string values.
        /// </summary>
        [Fact]
        public void Constructor_WithNegativeExitCodeAndEmptyStrings_PropertiesAreAssignedCorrectly()
        {
            // Arrange
            int expectedExitCode = -1;
            string expectedStandardOutput = string.Empty;
            string expectedStandardError = string.Empty;

            // Act
            var result = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, result.ExitCode);
            Assert.Equal(expectedStandardOutput, result.StandardOutput);
            Assert.Equal(expectedStandardError, result.StandardError);
        }
    }
}
