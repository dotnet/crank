using Microsoft.Crank.AzureDevOpsWorker;
using System;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessResult"/> class.
    /// </summary>
    public class ProcessResultTests
    {
        /// <summary>
        /// Tests that the constructor of <see cref="ProcessResult"/> properly sets the properties for valid, non-null parameters.
        /// </summary>
        [Fact]
        public void Constructor_ValidParameters_PropertiesAreSetCorrectly()
        {
            // Arrange
            int expectedExitCode = 0;
            string expectedOutput = "Sample output";
            string expectedError = "Sample error";
            
            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedOutput, expectedError);
            
            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedOutput, processResult.Output);
            Assert.Equal(expectedError, processResult.Error);
        }
        
        /// <summary>
        /// Tests that the constructor of <see cref="ProcessResult"/> properly sets the properties even when output is null.
        /// </summary>
        [Fact]
        public void Constructor_NullOutput_PropertiesAreSetCorrectly()
        {
            // Arrange
            int expectedExitCode = 1;
            string expectedOutput = null;
            string expectedError = "Error occurred";
            
            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedOutput, expectedError);
            
            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Null(processResult.Output);
            Assert.Equal(expectedError, processResult.Error);
        }
        
        /// <summary>
        /// Tests that the constructor of <see cref="ProcessResult"/> properly sets the properties even when error is null.
        /// </summary>
        [Fact]
        public void Constructor_NullError_PropertiesAreSetCorrectly()
        {
            // Arrange
            int expectedExitCode = 2;
            string expectedOutput = "Operation succeeded";
            string expectedError = null;
            
            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedOutput, expectedError);
            
            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedOutput, processResult.Output);
            Assert.Null(processResult.Error);
        }
        
        /// <summary>
        /// Tests that the constructor of <see cref="ProcessResult"/> properly sets the properties for an edge case where exit code is negative.
        /// </summary>
        [Fact]
        public void Constructor_NegativeExitCode_PropertiesAreSetCorrectly()
        {
            // Arrange
            int expectedExitCode = -1;
            string expectedOutput = "Negative exit code test";
            string expectedError = "No error message";
            
            // Act
            var processResult = new ProcessResult(expectedExitCode, expectedOutput, expectedError);
            
            // Assert
            Assert.Equal(expectedExitCode, processResult.ExitCode);
            Assert.Equal(expectedOutput, processResult.Output);
            Assert.Equal(expectedError, processResult.Error);
        }
    }
}
