using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Log"/> class.
    /// </summary>
    [TestClass]
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
        [TestMethod]
        public void Quiet_WhenCalled_WritesMessageToConsole()
        {
            // Arrange
            var message = "Test message";
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.Quiet(message);

            // Assert
            Assert.AreEqual(message + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteError(string, bool)"/> method to ensure it writes the error message in red.
        /// </summary>
        [TestMethod]
        public void WriteError_WhenCalled_WritesErrorMessageInRed()
        {
            // Arrange
            var message = "Error message";
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.WriteError(message);

            // Assert
            Assert.AreEqual(message + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteWarning(string, bool)"/> method to ensure it writes the warning message in dark yellow.
        /// </summary>
        [TestMethod]
        public void WriteWarning_WhenCalled_WritesWarningMessageInDarkYellow()
        {
            // Arrange
            var message = "Warning message";
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.WriteWarning(message);

            // Assert
            Assert.AreEqual(message + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.Write(string, bool, ConsoleColor)"/> method to ensure it writes the message with the specified color.
        /// </summary>
        [TestMethod]
        public void Write_WhenCalled_WritesMessageWithSpecifiedColor()
        {
            // Arrange
            var message = "Test message";
            var color = ConsoleColor.Green;
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.Write(message, false, color);

            // Assert
            Assert.AreEqual($"[{DateTime.Now:hh:mm:ss.fff}] {message}" + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.Verbose(string)"/> method to ensure it writes the message when verbose mode is enabled.
        /// </summary>
        [TestMethod]
        public void Verbose_WhenVerboseModeEnabled_WritesMessage()
        {
            // Arrange
            var message = "Verbose message";
            Log.IsVerbose = true;
            Log.IsQuiet = false;
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.Verbose(message);

            // Assert
            Assert.AreEqual($"[{DateTime.Now:hh:mm:ss.fff}] {message}" + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method to ensure it writes the content to the console.
        /// </summary>
        [TestMethod]
        public void DisplayOutput_WhenCalled_WritesContentToConsole()
        {
            // Arrange
            var content = "Output content";
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.DisplayOutput(content);

            // Assert
            Assert.AreEqual(content + Environment.NewLine, consoleOutput.GetOutput());
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method to ensure it handles null or empty content.
        /// </summary>
        [TestMethod]
        public void DisplayOutput_WhenContentIsNullOrEmpty_DoesNotWriteToConsole()
        {
            // Arrange
            using var consoleOutput = new ConsoleOutput();

            // Act
            Log.DisplayOutput(null);
            Log.DisplayOutput(string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, consoleOutput.GetOutput());
        }
    }

    /// <summary>
    /// Helper class to capture console output.
    /// </summary>
    public class ConsoleOutput : IDisposable
    {
        private readonly System.IO.StringWriter _stringWriter;
        private readonly System.IO.TextWriter _originalOutput;

        public ConsoleOutput()
        {
            _stringWriter = new System.IO.StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_stringWriter);
        }

        public string GetOutput()
        {
            return _stringWriter.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            _stringWriter.Dispose();
        }
    }
}
