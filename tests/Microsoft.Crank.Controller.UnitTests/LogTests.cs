using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Log"/> class.
    /// </summary>
    public class LogTests
    {
        /// <summary>
        /// Tests that <see cref="Log.Quiet(string)"/> writes the message directly to the console.
        /// </summary>
        [Fact]
        public void Quiet_WithMessage_WritesMessageToConsole()
        {
            // Arrange
            string expectedMessage = "Test Quiet Message";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Quiet(expectedMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(expectedMessage + Environment.NewLine, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Write(string, bool, ConsoleColor)"/> writes a time prefixed message when notime is false.
        /// </summary>
        [Fact]
        public void Write_WhenNotimeIsFalseAndIsQuietIsFalse_WritesTimePrefixedMessage()
        {
            // Arrange
            Log.IsQuiet = false;
            string message = "Test Write Message";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Write(message, notime: false, color: ConsoleColor.White);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                // Expect output format: [hh:mm:ss.fff] Test Write Message{NewLine}
                Assert.StartsWith("[", output);
                Assert.Contains("] " + message, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Write(string, bool, ConsoleColor)"/> writes a message without time prefix when notime is true.
        /// </summary>
        [Fact]
        public void Write_WhenNotimeIsTrueAndIsQuietIsFalse_WritesMessageWithoutTimePrefix()
        {
            // Arrange
            Log.IsQuiet = false;
            string message = "Test Write Message NoTime";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Write(message, notime: true, color: ConsoleColor.White);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(message + Environment.NewLine, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Write(string, bool, ConsoleColor)"/> writes no message when <see cref="Log.IsQuiet"/> is set to true.
        /// </summary>
        [Fact]
        public void Write_WhenIsQuietIsTrue_WritesNoMessage()
        {
            // Arrange
            Log.IsQuiet = true;
            string message = "Message Should Not Appear";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Write(message, notime: false, color: ConsoleColor.White);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(string.Empty, output);
            }
            finally
            {
                Console.SetOut(originalOut);
                Log.IsQuiet = false; // Reset for other tests
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.WriteError(string, bool)"/> writes an error message using a time prefix by default.
        /// </summary>
        [Fact]
        public void WriteError_Default_WritesTimePrefixedErrorMessage()
        {
            // Arrange
            Log.IsQuiet = false;
            string errorMessage = "Error occurred";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.WriteError(errorMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.StartsWith("[", output);
                Assert.Contains("] " + errorMessage, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.WriteError(string, bool)"/> writes an error message without a time prefix when notime is true.
        /// </summary>
        [Fact]
        public void WriteError_WhenNotimeIsTrue_WritesErrorMessageWithoutTimePrefix()
        {
            // Arrange
            Log.IsQuiet = false;
            string errorMessage = "Error without time";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.WriteError(errorMessage, notime: true);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(errorMessage + Environment.NewLine, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.WriteWarning(string, bool)"/> writes a warning message using a time prefix by default.
        /// </summary>
        [Fact]
        public void WriteWarning_Default_WritesTimePrefixedWarningMessage()
        {
            // Arrange
            Log.IsQuiet = false;
            string warningMessage = "Warning occurred";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.WriteWarning(warningMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.StartsWith("[", output);
                Assert.Contains("] " + warningMessage, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.WriteWarning(string, bool)"/> writes a warning message without a time prefix when notime is true.
        /// </summary>
        [Fact]
        public void WriteWarning_WhenNotimeIsTrue_WritesWarningMessageWithoutTimePrefix()
        {
            // Arrange
            Log.IsQuiet = false;
            string warningMessage = "Warning without time";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.WriteWarning(warningMessage, notime: true);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(warningMessage + Environment.NewLine, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Verbose(string)"/> writes the message when <see cref="Log.IsVerbose"/> is true and <see cref="Log.IsQuiet"/> is false.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsVerboseAndNotQuiet_WritesMessage()
        {
            // Arrange
            Log.IsVerbose = true;
            Log.IsQuiet = false;
            string verboseMessage = "Verbose message";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Verbose(verboseMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                // Since Verbose uses Write which adds a time prefix when notime is false
                Assert.StartsWith("[", output);
                Assert.Contains("] " + verboseMessage, output);
            }
            finally
            {
                Console.SetOut(originalOut);
                Log.IsVerbose = false; // Reset after test
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Verbose(string)"/> does not write the message when <see cref="Log.IsVerbose"/> is false.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsNotVerbose_DoesNotWriteMessage()
        {
            // Arrange
            Log.IsVerbose = false;
            Log.IsQuiet = false;
            string verboseMessage = "This should not appear";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Verbose(verboseMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(string.Empty, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.Verbose(string)"/> does not write the message when <see cref="Log.IsQuiet"/> is true even if <see cref="Log.IsVerbose"/> is true.
        /// </summary>
        [Fact]
        public void Verbose_WhenIsQuiet_DoesNotWriteMessage()
        {
            // Arrange
            Log.IsVerbose = true;
            Log.IsQuiet = true;
            string verboseMessage = "This should not appear";
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.Verbose(verboseMessage);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(string.Empty, output);
            }
            finally
            {
                Console.SetOut(originalOut);
                Log.IsQuiet = false;
                Log.IsVerbose = false;
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.DisplayOutput(string)"/> does nothing when provided with an empty string.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void DisplayOutput_WhenContentIsNullOrEmpty_DoesNotWriteAnything(string content)
        {
            // Arrange
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.DisplayOutput(content);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                Assert.Equal(string.Empty, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests that <see cref="Log.DisplayOutput(string)"/> trims the content and writes the output without a time prefix.
        /// Also verifies newline conversion on Windows.
        /// </summary>
        [Fact]
        public void DisplayOutput_WithContent_WritesTrimmedOutput()
        {
            // Arrange
            Log.IsQuiet = false;
            string inputContent = "  Line1\nLine2  ";
            string expectedContent;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedContent = ("Line1" + Environment.NewLine + "Line2").Trim();
            }
            else
            {
                expectedContent = "Line1\nLine2";
            }
            var originalOut = Console.Out;
            try
            {
                using var writer = new StringWriter();
                Console.SetOut(writer);
                // Act
                Log.DisplayOutput(inputContent);
                writer.Flush();
                string output = writer.ToString();
                // Assert
                // Since DisplayOutput calls Write with notime true, the output should be exactly the trimmed expected content plus a newline.
                Assert.Equal(expectedContent + Environment.NewLine, output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
