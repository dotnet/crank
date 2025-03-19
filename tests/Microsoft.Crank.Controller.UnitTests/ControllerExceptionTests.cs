using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ControllerException"/> class.
    /// </summary>
    public class ControllerExceptionTests
    {
        /// <summary>
        /// Tests that the ControllerException constructor correctly sets the Message property when a valid message is provided.
        /// </summary>
        [Fact]
        public void ControllerException_Constructor_WithValidMessage_SetsMessage()
        {
            // Arrange
            string expectedMessage = "Test exception message.";

            // Act
            var exception = new ControllerException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Null(exception.InnerException);
        }
        
        /// <summary>
        /// Tests that the ControllerException constructor correctly handles a null message.
        /// </summary>
        [Fact]
        public void ControllerException_Constructor_WithNullMessage_SetsMessageToNull()
        {
            // Arrange
            string expectedMessage = null;

            // Act
            var exception = new ControllerException(expectedMessage);

            // Assert
            Assert.Null(exception.Message);
            Assert.Null(exception.InnerException);
        }
    }
}
