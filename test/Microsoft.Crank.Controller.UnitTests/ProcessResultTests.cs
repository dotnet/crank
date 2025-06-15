using Microsoft.Crank.PullRequestBot;
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
        /// Tests that the constructor correctly assigns the provided valid parameters to the properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidParameters_AssignsPropertiesCorrectly()
        {
            // Arrange
            int expectedExitCode = 0;
            string expectedStandardOutput = "Sample output";
            string expectedStandardError = "Sample error";

            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedStandardOutput, processResult.StandardOutput);
            Assert.Equal(expectedStandardError, processResult.StandardError);
        }

        /// <summary>
        /// Tests that the constructor assigns negative exit code correctly while setting string properties.
        /// </summary>
        [Fact]
        public void Constructor_NegativeExitCode_AssignsPropertiesCorrectly()
        {
            // Arrange
            int expectedExitCode = -1;
            string expectedStandardOutput = "Output for negative exit";
            string expectedStandardError = "Error for negative exit";

            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedStandardOutput, processResult.StandardOutput);
            Assert.Equal(expectedStandardError, processResult.StandardError);
        }

        /// <summary>
        /// Tests that the constructor can handle null values for string parameters.
        /// </summary>
        [Fact]
        public void Constructor_NullStringParameters_AssignsPropertiesCorrectly()
        {
            // Arrange
            int expectedExitCode = 1;
            string expectedStandardOutput = null;
            string expectedStandardError = null;

            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Null(processResult.StandardOutput);
            Assert.Null(processResult.StandardError);
        }
    }
}
