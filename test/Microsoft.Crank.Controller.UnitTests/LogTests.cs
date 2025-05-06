using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Crank.Controller;
using Moq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Log"/> class.
    /// </summary>
    public class LogTests : IDisposable
    {
        private readonly TextWriter _originalOutput;

        /// <summary>
        /// Constructor that sets up initial test conditions.
        /// </summary>
        public LogTests()
        {
            // Store the original Console.Out.
            _originalOutput = Console.Out;
            // Reset static properties to default values.
            Log.IsQuiet = false;
            Log.IsVerbose = false;
        }

        /// <summary>
        /// Disposes resources and resets Console.Out.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(_originalOutput);
        }

        /// <summary>
        /// Tests the <see cref="Log.Quiet(string)"/> method to verify that it writes the provided message.
        /// </summary>
        [Fact]
        public void Quiet_WithValidMessage_WritesMessage()
        {
            // Arrange
            string message = "Test Quiet Message";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Quiet(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.Equal(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteError(string, bool)"/> method with notime set to false to ensure that the output includes a timestamp and the message.
        /// </summary>
        [Fact]
        public void WriteError_WithNotimeFalse_WritesTimestampAndMessage()
        {
            // Arrange
            string message = "Error occurred";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.WriteError(message);
            string output = sw.ToString().Trim();

            // Assert
            // Since notime is false, the output should start with '[' (indicating a timestamp) and contain the message.
            Assert.StartsWith("[", output);
            Assert.Contains(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteError(string, bool)"/> method with notime set to true to ensure that only the message is written.
        /// </summary>
        [Fact]
        public void WriteError_WithNotimeTrue_WritesOnlyMessage()
        {
            // Arrange
            string message = "Error occurred without time";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.WriteError(message, notime: true);
            string output = sw.ToString().Trim();

            // Assert
            Assert.Equal(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteWarning(string, bool)"/> method with notime set to false to ensure that the output includes a timestamp and the message.
        /// </summary>
        [Fact]
        public void WriteWarning_WithNotimeFalse_WritesTimestampAndMessage()
        {
            // Arrange
            string message = "Warning message";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.WriteWarning(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.StartsWith("[", output);
            Assert.Contains(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.WriteWarning(string, bool)"/> method with notime set to true to ensure that only the message is written.
        /// </summary>
        [Fact]
        public void WriteWarning_WithNotimeTrue_WritesOnlyMessage()
        {
            // Arrange
            string message = "Warning message without time";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.WriteWarning(message, notime: true);
            string output = sw.ToString().Trim();

            // Assert
            Assert.Equal(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.Write(string, bool, ConsoleColor)"/> method when Log.IsQuiet is true, ensuring no output is produced.
        /// </summary>
        [Fact]
        public void Write_WhenIsQuietTrue_WritesNoOutput()
        {
            // Arrange
            Log.IsQuiet = true;
            string message = "This message should not be written";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Write(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.True(string.IsNullOrEmpty(output));
        }

        /// <summary>
        /// Tests the <see cref="Log.Write(string, bool, ConsoleColor)"/> method with notime set to false, ensuring that the output includes a timestamp and the message.
        /// </summary>
        [Fact]
        public void Write_WithNotimeFalse_WritesTimestampAndMessage()
        {
            // Arrange
            Log.IsQuiet = false;
            string message = "Output with time";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Write(message, notime: false);
            string output = sw.ToString().Trim();

            // Assert
            Assert.StartsWith("[", output);
            Assert.Contains(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.Write(string, bool, ConsoleColor)"/> method with notime set to true, ensuring that only the message is written.
        /// </summary>
        [Fact]
        public void Write_WithNotimeTrue_WritesOnlyMessage()
        {
            // Arrange
            Log.IsQuiet = false;
            string message = "Output without time";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Write(message, notime: true);
            string output = sw.ToString().Trim();

            // Assert
            Assert.Equal(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.Verbose(string)"/> method when Log.IsVerbose is true and Log.IsQuiet is false, ensuring that verbose output is produced.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsVerboseTrueAndNotQuiet_WritesOutput()
        {
            // Arrange
            Log.IsQuiet = false;
            Log.IsVerbose = true;
            string message = "Verbose output";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Verbose(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.StartsWith("[", output);
            Assert.Contains(message, output);
        }

        /// <summary>
        /// Tests the <see cref="Log.Verbose(string)"/> method when Log.IsVerbose is false, ensuring that no output is produced.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsVerboseFalse_WritesNoOutput()
        {
            // Arrange
            Log.IsQuiet = false;
            Log.IsVerbose = false;
            string message = "Verbose output should not appear";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Verbose(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.True(string.IsNullOrEmpty(output));
        }

        /// <summary>
        /// Tests the <see cref="Log.Verbose(string)"/> method when Log.IsQuiet is true, ensuring that no output is produced even if verbose is enabled.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsQuietTrue_WritesNoOutput()
        {
            // Arrange
            Log.IsQuiet = true;
            Log.IsVerbose = true;
            string message = "Verbose output should not be written when quiet";
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.Verbose(message);
            string output = sw.ToString().Trim();

            // Assert
            Assert.True(string.IsNullOrEmpty(output));
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method with null content, ensuring that no output is produced.
        /// </summary>
        [Fact]
        public void DisplayOutput_WithNullContent_WritesNoOutput()
        {
            // Arrange
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.DisplayOutput(null);
            string output = sw.ToString().Trim();

            // Assert
            Assert.True(string.IsNullOrEmpty(output));
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method with empty string content, ensuring that no output is produced.
        /// </summary>
        [Fact]
        public void DisplayOutput_WithEmptyContent_WritesNoOutput()
        {
            // Arrange
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.DisplayOutput(string.Empty);
            string output = sw.ToString().Trim();

            // Assert
            Assert.True(string.IsNullOrEmpty(output));
        }

        /// <summary>
        /// Tests the <see cref="Log.DisplayOutput(string)"/> method with valid content containing line feed characters,
        /// ensuring that the output is correctly formatted by replacing LF with environment-specific newlines on Windows and trimming the content.
        /// </summary>
        [Fact]
        public void DisplayOutput_WithValidContent_WritesTrimmedContent()
        {
            // Arrange
            string originalContent = "Line1\nLine2\nLine3";
            string expectedContent;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedContent = originalContent.Replace("\n", Environment.NewLine).Trim();
            }
            else
            {
                expectedContent = originalContent.Trim();
            }
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            Log.DisplayOutput(originalContent);
            string output = sw.ToString().Trim();

            // Assert
            Assert.Equal(expectedContent, output);
        }
    }
}
