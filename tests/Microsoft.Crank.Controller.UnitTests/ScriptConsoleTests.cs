using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Microsoft.Crank.Controller.UnitTests
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
        /// Tests the <see cref="ScriptConsole.Log(object[])"/> method to ensure it correctly logs a message.
        /// </summary>
        [TestMethod]
        public void Log_WhenCalledWithValidArgs_LogsMessage()
        {
            // Arrange
            var args = new object[] { "Hello", "World" };
            var expectedOutput = "Hello World";

            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Log(args);

                // Assert
                Assert.AreEqual(expectedOutput, consoleOutput.GetOutput().Trim());
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Log(object[])"/> method to ensure it does nothing when called with null or empty args.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow(new object[] { })]
        public void Log_WhenCalledWithNullOrEmptyArgs_DoesNothing(object[] args)
        {
            // Arrange
            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Log(args);

                // Assert
                Assert.AreEqual(string.Empty, consoleOutput.GetOutput());
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Info(object[])"/> method to ensure it correctly logs an info message.
        /// </summary>
        [TestMethod]
        public void Info_WhenCalledWithValidArgs_LogsInfoMessage()
        {
            // Arrange
            var args = new object[] { "Info", "Message" };
            var expectedOutput = "Info Message";

            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Info(args);

                // Assert
                Assert.AreEqual(expectedOutput, consoleOutput.GetOutput().Trim());
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Warn(object[])"/> method to ensure it correctly logs a warning message.
        /// </summary>
        [TestMethod]
        public void Warn_WhenCalledWithValidArgs_LogsWarningMessage()
        {
            // Arrange
            var args = new object[] { "Warning", "Message" };
            var expectedOutput = "Warning Message";

            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Warn(args);

                // Assert
                Assert.AreEqual(expectedOutput, consoleOutput.GetOutput().Trim());
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Error(object[])"/> method to ensure it correctly logs an error message and sets HasErrors to true.
        /// </summary>
        [TestMethod]
        public void Error_WhenCalledWithValidArgs_LogsErrorMessageAndSetsHasErrors()
        {
            // Arrange
            var args = new object[] { "Error", "Message" };
            var expectedOutput = "Error Message";

            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Error(args);

                // Assert
                Assert.AreEqual(expectedOutput, consoleOutput.GetOutput().Trim());
                Assert.IsTrue(_scriptConsole.HasErrors);
            }
        }

        /// <summary>
        /// Tests the <see cref="ScriptConsole.Error(object[])"/> method to ensure it does nothing when called with null or empty args.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow(new object[] { })]
        public void Error_WhenCalledWithNullOrEmptyArgs_DoesNothing(object[] args)
        {
            // Arrange
            using (var consoleOutput = new ConsoleOutput())
            {
                // Act
                _scriptConsole.Error(args);

                // Assert
                Assert.AreEqual(string.Empty, consoleOutput.GetOutput());
                Assert.IsFalse(_scriptConsole.HasErrors);
            }
        }
    }

    /// <summary>
    /// Helper class to capture console output.
    /// </summary>
//     public class ConsoleOutput : IDisposable [Error] (149-16)CS0111 Type 'ConsoleOutput' already defines a member called 'ConsoleOutput' with the same parameter types
//     {
//         private readonly System.IO.StringWriter _stringWriter;
//         private readonly System.IO.TextWriter _originalOutput;
// 
//         public ConsoleOutput()
//         {
//             _stringWriter = new System.IO.StringWriter();
//             _originalOutput = Console.Out;
//             Console.SetOut(_stringWriter);
//         }
// 
//         public string GetOutput()
//         {
//             return _stringWriter.ToString();
//         }
// 
//         public void Dispose()
//         {
//             Console.SetOut(_originalOutput);
//             _stringWriter.Dispose();
//         }
//     }
}
