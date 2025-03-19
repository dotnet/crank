using Moq;
using System;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptConsole"/> class.
    /// </summary>
    public class ScriptConsoleTests
    {
        private readonly ScriptConsole _scriptConsole;

        public ScriptConsoleTests()
        {
            _scriptConsole = new ScriptConsole();
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Log(object[])"/> method to ensure it correctly logs messages to the console.
        /// </summary>
        [Fact]
        public void Log_WhenCalledWithValidArgs_WritesToConsole()
        {
            // Arrange
            var args = new object[] { "Hello", "World" };
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Log(args);
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Contains("Hello World", consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Log(object[])"/> method to ensure it handles null or empty arguments gracefully.
        /// </summary>
        [Fact]
        public void Log_WhenCalledWithNullOrEmptyArgs_DoesNotWriteToConsole()
        {
            // Arrange
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Log(null);
            _scriptConsole.Log(new object[] { });
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Empty(consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Info(object[])"/> method to ensure it correctly logs info messages to the console.
        /// </summary>
        [Fact]
        public void Info_WhenCalledWithValidArgs_WritesToConsoleInGreen()
        {
            // Arrange
            var args = new object[] { "Info", "Message" };
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Info(args);
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Contains("Info Message", consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Info(object[])"/> method to ensure it handles null or empty arguments gracefully.
        /// </summary>
        [Fact]
        public void Info_WhenCalledWithNullOrEmptyArgs_DoesNotWriteToConsole()
        {
            // Arrange
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Info(null);
            _scriptConsole.Info(new object[] { });
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Empty(consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Warn(object[])"/> method to ensure it correctly logs warning messages to the console.
        /// </summary>
        [Fact]
        public void Warn_WhenCalledWithValidArgs_WritesToConsoleInDarkYellow()
        {
            // Arrange
            var args = new object[] { "Warning", "Message" };
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Warn(args);
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Contains("Warning Message", consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Warn(object[])"/> method to ensure it handles null or empty arguments gracefully.
        /// </summary>
        [Fact]
        public void Warn_WhenCalledWithNullOrEmptyArgs_DoesNotWriteToConsole()
        {
            // Arrange
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Warn(null);
            _scriptConsole.Warn(new object[] { });
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Empty(consoleOutput);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Error(object[])"/> method to ensure it correctly logs error messages to the console and sets HasErrors to true.
        /// </summary>
        [Fact]
        public void Error_WhenCalledWithValidArgs_WritesToConsoleInRedAndSetsHasErrors()
        {
            // Arrange
            var args = new object[] { "Error", "Message" };
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Error(args);
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Contains("Error Message", consoleOutput);
            Assert.True(_scriptConsole.HasErrors);
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Error(object[])"/> method to ensure it handles null or empty arguments gracefully.
        /// </summary>
        [Fact]
        public void Error_WhenCalledWithNullOrEmptyArgs_DoesNotWriteToConsoleAndDoesNotSetHasErrors()
        {
            // Arrange
            var consoleOutput = string.Empty;
            Console.SetOut(new System.IO.StringWriter());

            // Act
            _scriptConsole.Error(null);
            _scriptConsole.Error(new object[] { });
            consoleOutput = Console.Out.ToString();

            // Assert
            Assert.Empty(consoleOutput);
            Assert.False(_scriptConsole.HasErrors);
        }
    }
}
