using Microsoft.Crank.Agent;
using System;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    public class ProcessResultTests
    {
        /// <summary>
        /// Tests the ProcessResult constructor with valid non-null arguments to ensure all properties are correctly assigned.
        /// </summary>
        [Fact]
        public void Constructor_WithValidArguments_SetsPropertiesCorrectly()
        {
            // Arrange
            int expectedExitCode = 0;
            string expectedStandardOutput = "Standard output sample";
            string expectedStandardError = "Standard error sample";

            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedStandardOutput, expectedStandardError);

            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedStandardOutput, processResult.StandardOutput);
            Assert.Equal(expectedStandardError, processResult.StandardError);
        }

        /// <summary>
        /// Tests the ProcessResult constructor with null string arguments to ensure that properties are set accordingly.
        /// </summary>
        [Fact]
        public void Constructor_WithNullArguments_SetsPropertiesCorrectly()
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

        /// <summary>
        /// Tests the ProcessResult constructor with a negative exit code to verify that negative exit codes are handled correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithNegativeExitCode_SetsPropertiesCorrectly()
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
    }
}
