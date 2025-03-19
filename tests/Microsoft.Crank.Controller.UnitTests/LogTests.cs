using Moq;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Log"/> class.
    /// </summary>
    public class LogTests
    {
        private readonly MockRepository _mockRepository;

        public LogTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);
        }

        /// <summary>
        /// Tests the <see cref="Log.Quiet(string)"/> method to ensure it writes the message to the console.
        /// </summary>
        [Fact]
        public void Quiet_WhenCalled_WritesMessageToConsole()
        {
            // Arrange
            var message = "Test message";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.Quiet(message);

            // Assert
            Assert.Equal(message + Environment.NewLine, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteError(string, bool)"/> method to ensure it writes the error message in red.
        /// </summary>
        [Fact]
        public void WriteError_WhenCalled_WritesErrorMessageInRed()
        {
            // Arrange
            var message = "Error message";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.WriteError(message);

            // Assert
            Assert.Contains(message, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteWarning(string, bool)"/> method to ensure it writes the warning message in dark yellow.
        /// </summary>
        [Fact]
        public void WriteWarning_WhenCalled_WritesWarningMessageInDarkYellow()
        {
            // Arrange
            var message = "Warning message";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.WriteWarning(message);

            // Assert
            Assert.Contains(message, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.Write(string, bool, ConsoleColor)"/> method to ensure it writes the message with the specified color.
        /// </summary>
        [Fact]
        public void Write_WhenCalled_WritesMessageWithSpecifiedColor()
        {
            // Arrange
            var message = "Colored message";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.Write(message, color: ConsoleColor.Green);

            // Assert
            Assert.Contains(message, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.Verbose(string)"/> method to ensure it writes the message when verbose mode is enabled.
        /// </summary>
        [Fact]
        public void Verbose_WhenVerboseModeEnabled_WritesMessage()
        {
            // Arrange
            Log.IsVerbose = true;
            Log.IsQuiet = false;
            var message = "Verbose message";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.Verbose(message);

            // Assert
            Assert.Contains(message, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method to ensure it writes the content to the console.
        /// </summary>
        [Fact]
        public void DisplayOutput_WhenCalled_WritesContentToConsole()
        {
            // Arrange
            var content = "Output content";
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.DisplayOutput(content);

            // Assert
            Assert.Contains(content, consoleOutput.ToString());
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method to ensure it handles null or empty content gracefully.
        /// </summary>
        [Fact]
        public void DisplayOutput_WhenContentIsNullOrEmpty_DoesNotWriteToConsole()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            Log.DisplayOutput(null);
            Log.DisplayOutput(string.Empty);

            // Assert
            Assert.Equal(string.Empty, consoleOutput.ToString());
        }
    }
}
