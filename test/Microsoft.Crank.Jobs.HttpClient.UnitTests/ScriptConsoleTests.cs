using System;
using System.IO;
using System.Linq;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptConsole"/> class.
    /// </summary>
    public class ScriptConsoleTests
    {
        private readonly ScriptConsole _scriptConsole;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptConsoleTests"/> class.
        /// </summary>
        public ScriptConsoleTests()
        {
            _scriptConsole = new ScriptConsole();
        }

        /// <summary>
        /// Tests the Log method with multiple arguments.
        /// Expected outcome: The concatenated message is written to the console without altering HasErrors.
        /// </summary>
        [Fact]
        public void Log_WithMultipleArguments_WritesConcatenatedMessage()
        {
            // Arrange
            var args = new object[] { "Hello", "World" };
            string expectedOutput = "Hello World" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Log(args);

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests the Log method with no arguments.
        /// Expected outcome: An empty line is written to the console and HasErrors remains false.
        /// </summary>
        [Fact]
        public void Log_WithNoArguments_WritesEmptyLine()
        {
            // Arrange
            string expectedOutput = "" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Log();

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        
        /// <summary>
        /// Tests the Log method with a null argument.
        /// Expected outcome: A NullReferenceException is thrown.
        /// </summary>
        [Fact]
        public void Log_WithNullArgument_ThrowsNullReferenceException()
        {
            // Arrange
            object[] args = new object[] { null };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _scriptConsole.Log(args));
        }

        /// <summary>
        /// Tests the Info method with multiple arguments.
        /// Expected outcome: The concatenated message is written to the console with a temporary green foreground color and then the console color is reset.
        /// </summary>
        [Fact]
        public void Info_WithMultipleArguments_WritesConcatenatedMessageAndResetsConsoleColor()
        {
            // Arrange
            var args = new object[] { "Information", "Message" };
            string expectedOutput = "Information Message" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            // Set a known default foreground color.
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Info(args);

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests the Info method with no arguments.
        /// Expected outcome: An empty line is written to the console, and the console color is reset afterwards.
        /// </summary>
        [Fact]
        public void Info_WithNoArguments_WritesEmptyLineAndResetsConsoleColor()
        {
            // Arrange
            string expectedOutput = "" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Info();

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        
        /// <summary>
        /// Tests the Info method with a null argument.
        /// Expected outcome: A NullReferenceException is thrown.
        /// </summary>
        [Fact]
        public void Info_WithNullArgument_ThrowsNullReferenceException()
        {
            // Arrange
            object[] args = new object[] { null };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _scriptConsole.Info(args));
        }
        
        /// <summary>
        /// Tests the Warn method with multiple arguments.
        /// Expected outcome: The concatenated message is written to the console with a temporary dark yellow foreground color and then the console color is reset.
        /// </summary>
        [Fact]
        public void Warn_WithMultipleArguments_WritesConcatenatedMessageAndResetsConsoleColor()
        {
            // Arrange
            var args = new object[] { "Warning", "Message" };
            string expectedOutput = "Warning Message" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Warn(args);

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests the Warn method with no arguments.
        /// Expected outcome: An empty line is written to the console and the console color is reset afterwards.
        /// </summary>
        [Fact]
        public void Warn_WithNoArguments_WritesEmptyLineAndResetsConsoleColor()
        {
            // Arrange
            string expectedOutput = "" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Warn();

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        
        /// <summary>
        /// Tests the Warn method with a null argument.
        /// Expected outcome: A NullReferenceException is thrown.
        /// </summary>
        [Fact]
        public void Warn_WithNullArgument_ThrowsNullReferenceException()
        {
            // Arrange
            object[] args = new object[] { null };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _scriptConsole.Warn(args));
        }
        
        /// <summary>
        /// Tests the Error method with multiple arguments.
        /// Expected outcome: The concatenated message is written to the console with a temporary red foreground color,
        /// the console color is reset afterwards, and HasErrors is set to true.
        /// </summary>
        [Fact]
        public void Error_WithMultipleArguments_WritesConcatenatedMessageResetsConsoleColorAndSetsHasErrors()
        {
            // Arrange
            var args = new object[] { "Error", "Occurred" };
            string expectedOutput = "Error Occurred" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Error(args);

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.True(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Tests the Error method with no arguments.
        /// Expected outcome: An empty line is written to the console, the console color is reset afterwards,
        /// and HasErrors is set to true.
        /// </summary>
        [Fact]
        public void Error_WithNoArguments_WritesEmptyLineResetsConsoleColorAndSetsHasErrors()
        {
            // Arrange
            string expectedOutput = "" + Environment.NewLine;
            using var writer = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.ForegroundColor = ConsoleColor.White;
            var defaultColor = Console.ForegroundColor;
            try
            {
                Console.SetOut(writer);

                // Act
                _scriptConsole.Error();

                // Assert
                string actualOutput = writer.ToString();
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.True(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        
        /// <summary>
        /// Tests the Error method with a null argument.
        /// Expected outcome: A NullReferenceException is thrown and HasErrors remains false.
        /// </summary>
        [Fact]
        public void Error_WithNullArgument_ThrowsNullReferenceExceptionAndDoesNotSetHasErrors()
        {
            // Arrange
            // Reset HasErrors
            object[] args = new object[] { null };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _scriptConsole.Error(args));
            Assert.False(_scriptConsole.HasErrors);
        }
    }
}
