using System;
using System.IO;
using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
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
        /// Tests that Log does not write any output when passed a null argument.
        /// </summary>
        [Fact]
        public void Log_NullArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Log(null);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Log does not write any output when passed an empty arguments array.
        /// </summary>
        [Fact]
        public void Log_EmptyArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Log(new object[0]);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Log writes the expected concatenated string when passed valid arguments.
        /// </summary>
        [Fact]
        public void Log_WithValidArgs_WritesExpectedOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            string[] testArgs = new[] { "Hello", "World", "123" };
            string expectedOutput = "Hello World 123" + Environment.NewLine;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Log(testArgs);

                // Assert
                Assert.Equal(expectedOutput, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Info does not write any output when passed a null argument.
        /// </summary>
        [Fact]
        public void Info_NullArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Info(null);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Info does not write any output when passed an empty arguments array.
        /// </summary>
        [Fact]
        public void Info_EmptyArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Info(new object[0]);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Info writes the expected output, sets the console foreground color to green, and resets the color thereafter.
        /// </summary>
        [Fact]
        public void Info_WithValidArgs_WritesExpectedOutputAndResetsColor()
        {
            // Arrange
            var originalOutput = Console.Out;
            var defaultColor = Console.ForegroundColor;
            string[] testArgs = new[] { "Information", "Message" };
            string expectedOutput = "Information Message" + Environment.NewLine;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Info(testArgs);

                // Assert
                Assert.Equal(expectedOutput, stringWriter.ToString());
                Assert.Equal(defaultColor, Console.ForegroundColor);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Warn does not write any output when passed a null argument.
        /// </summary>
        [Fact]
        public void Warn_NullArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Warn(null);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Warn does not write any output when passed an empty arguments array.
        /// </summary>
        [Fact]
        public void Warn_EmptyArgs_ProducesNoOutput()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Warn(new object[0]);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Warn writes the expected output, sets the console foreground color to dark yellow, and resets the color thereafter.
        /// </summary>
        [Fact]
        public void Warn_WithValidArgs_WritesExpectedOutputAndResetsColor()
        {
            // Arrange
            var originalOutput = Console.Out;
            var defaultColor = Console.ForegroundColor;
            string[] testArgs = new[] { "Warning", "Message" };
            string expectedOutput = "Warning Message" + Environment.NewLine;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Warn(testArgs);

                // Assert
                Assert.Equal(expectedOutput, stringWriter.ToString());
                Assert.Equal(defaultColor, Console.ForegroundColor);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Error does not write any output and does not set HasErrors when passed a null argument.
        /// </summary>
        [Fact]
        public void Error_NullArgs_ProducesNoOutputAndDoesNotSetHasErrors()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Error(null);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Error does not write any output and does not set HasErrors when passed an empty arguments array.
        /// </summary>
        [Fact]
        public void Error_EmptyArgs_ProducesNoOutputAndDoesNotSetHasErrors()
        {
            // Arrange
            var originalOutput = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Error(new object[0]);

                // Assert
                Assert.Equal(string.Empty, stringWriter.ToString());
                Assert.False(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        /// <summary>
        /// Tests that Error writes the expected output, sets HasErrors to true, and resets the console color after writing.
        /// </summary>
        [Fact]
        public void Error_WithValidArgs_WritesExpectedOutputSetsHasErrorsAndResetsColor()
        {
            // Arrange
            var originalOutput = Console.Out;
            var defaultColor = Console.ForegroundColor;
            string[] testArgs = new[] { "Error", "Occurred" };
            string expectedOutput = "Error Occurred" + Environment.NewLine;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                _scriptConsole.Error(testArgs);

                // Assert
                Assert.Equal(expectedOutput, stringWriter.ToString());
                Assert.Equal(defaultColor, Console.ForegroundColor);
                Assert.True(_scriptConsole.HasErrors);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }
}
