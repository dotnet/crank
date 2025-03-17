using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptConsole"/> class.
    /// </summary>
    [TestClass]
    public class ScriptConsoleTests
    {
        private readonly ScriptConsole _scriptConsole;

        public ScriptConsoleTests()
        {
            _scriptConsole = new ScriptConsole();
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Log(object[])"/> method to ensure it correctly logs the provided arguments.
        /// </summary>
        [TestMethod]
        public void Log_WhenCalledWithArguments_LogsCorrectly()
        {
            // Arrange
            var args = new object[] { "Test", 123, true };

            // Act
            using (var consoleOutput = new ConsoleOutput())
            {
                _scriptConsole.Log(args);
                var output = consoleOutput.GetOutput();

                // Assert
                Assert.AreEqual("Test 123 True\r\n", output);
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Info(object[])"/> method to ensure it correctly logs the provided arguments in green color.
        /// </summary>
        [TestMethod]
        public void Info_WhenCalledWithArguments_LogsCorrectly()
        {
            // Arrange
            var args = new object[] { "Info", 456, false };

            // Act
            using (var consoleOutput = new ConsoleOutput())
            {
                _scriptConsole.Info(args);
                var output = consoleOutput.GetOutput();

                // Assert
                Assert.AreEqual("Info 456 False\r\n", output);
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Warn(object[])"/> method to ensure it correctly logs the provided arguments in dark yellow color.
        /// </summary>
        [TestMethod]
        public void Warn_WhenCalledWithArguments_LogsCorrectly()
        {
            // Arrange
            var args = new object[] { "Warning", 789, null };

            // Act
            using (var consoleOutput = new ConsoleOutput())
            {
                _scriptConsole.Warn(args);
                var output = consoleOutput.GetOutput();

                // Assert
                Assert.AreEqual("Warning 789 \r\n", output);
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Error(object[])"/> method to ensure it correctly logs the provided arguments in red color and sets HasErrors to true.
        /// </summary>
        [TestMethod]
        public void Error_WhenCalledWithArguments_LogsCorrectlyAndSetsHasErrors()
        {
            // Arrange
            var args = new object[] { "Error", 101112, "Critical" };

            // Act
            using (var consoleOutput = new ConsoleOutput())
            {
                _scriptConsole.Error(args);
                var output = consoleOutput.GetOutput();

                // Assert
                Assert.AreEqual("Error 101112 Critical\r\n", output);
                Assert.IsTrue(_scriptConsole.HasErrors);
            }
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
