using System;
using Microsoft.Crank.RegressionBot;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="RegressionBotException"/> class.
    /// </summary>
    public class RegressionBotExceptionTests
    {
        /// <summary>
        /// Tests that the constructor of RegressionBotException correctly assigns the provided non-null message
        /// to the Message property.
        /// </summary>
        [Fact]
        public void RegressionBotExceptionConstructor_ValidMessage_SetsMessageProperty()
        {
            // Arrange
            string expectedMessage = "This is a test exception message";

            // Act
            RegressionBotException exception = new RegressionBotException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that the constructor of RegressionBotException correctly assigns a null message to the Message property.
        /// </summary>
        [Fact]
        public void RegressionBotExceptionConstructor_NullMessage_SetsMessagePropertyToNull()
        {
            // Arrange
            string expectedMessage = null;

            // Act
            RegressionBotException exception = new RegressionBotException(expectedMessage);

            // Assert
            Assert.Null(exception.Message);
        }
    }
}
