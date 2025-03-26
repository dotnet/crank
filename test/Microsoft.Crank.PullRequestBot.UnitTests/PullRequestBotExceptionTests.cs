using Microsoft.Crank.PullRequestBot;
using System;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="PullRequestBotException"/> class.
    /// </summary>
    public class PullRequestBotExceptionTests
    {
        /// <summary>
        /// Tests that the constructor of <see cref="PullRequestBotException"/> sets the Message property correctly when provided a valid message.
        /// Arrange: A valid error message string.
        /// Act: Instantiates a new <see cref="PullRequestBotException"/> with the given message.
        /// Assert: The Message property of the exception should exactly match the provided message.
        /// </summary>
        [Fact]
        public void Constructor_WithValidMessage_SetsMessageCorrectly()
        {
            // Arrange
            string expectedMessage = "An error occurred in the pull request bot.";

            // Act
            var exception = new PullRequestBotException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        /// <summary>
        /// Tests that the constructor of <see cref="PullRequestBotException"/> does not throw when provided a null message.
        /// Arrange: A null message.
        /// Act: Instantiates a new <see cref="PullRequestBotException"/> with null as the error message.
        /// Assert: The exception is created successfully and its Message property is not null.
        /// </summary>
        [Fact]
        public void Constructor_WithNullMessage_DoesNotThrowAndReturnsNonNullMessage()
        {
            // Arrange
            string inputMessage = null;

            // Act
            var exception = new PullRequestBotException(inputMessage);

            // Assert
            // Even if null was passed, accessing Message should not result in a null reference.
            Assert.NotNull(exception.Message);
        }
    }
}
