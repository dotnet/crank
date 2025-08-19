using Microsoft.Crank.Controller;
using Moq;
using System;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ControllerException"/> class.
    /// </summary>
    public class ControllerExceptionTests
    {
        /// <summary>
        /// Tests that when a non-empty message is provided to the ControllerException constructor,
        /// the Message property is set to the provided message.
        /// </summary>
        [Fact]
        public void Constructor_WithNonEmptyMessage_SetsMessageProperty()
        {
            // Arrange
            string expectedMessage = "Test error message";

            // Act
            var exception = new ControllerException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that when an empty string is provided to the ControllerException constructor,
        /// the Message property is set to the empty string.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyMessage_SetsMessageProperty()
        {
            // Arrange
            string expectedMessage = string.Empty;

            // Act
            var exception = new ControllerException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that when a null message is provided to the ControllerException constructor,
        /// the Message property returns a non-null value (the default system-supplied message).
        /// </summary>
        [Fact]
        public void Constructor_WithNullMessage_ReturnsDefaultMessage()
        {
            // Arrange
            string nullMessage = null;

            // Act
            var exception = new ControllerException(nullMessage);

            // Assert
            Assert.False(string.IsNullOrEmpty(exception.Message), "Expected the default exception message to be non-null and non-empty when a null message is provided.");
        }
    }
}
