// using Microsoft.VisualStudio.TestTools.UnitTesting; [Error] (1-30)CS0234 The type or namespace name 'TestTools' does not exist in the namespace 'Microsoft.VisualStudio' (are you missing an assembly reference?)
using System;
using System.IO;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptConsole"/> class.
    /// </summary>
//     [TestClass] [Error] (10-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (10-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class ScriptConsoleTests
    {
        private readonly ScriptConsole _scriptConsole;

        public ScriptConsoleTests()
        {
            _scriptConsole = new ScriptConsole();
        }

        #region Log Tests

        /// <summary>
        /// Tests the Log method with null arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (25-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (25-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (37-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Log_NullArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Log(null);
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Log should not produce any output when called with null arguments.");
//         }

        /// <summary>
        /// Tests the Log method with empty arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (43-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (43-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (55-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Log_EmptyArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Log();
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Log should not produce any output when called with empty arguments.");
//         }

        /// <summary>
        /// Tests the Log method with valid arguments to ensure the concatenated string is printed.
        /// </summary>
//         [TestMethod] [Error] (61-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (61-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (75-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Log_ValidArgs_PrintsConcatenatedString()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
//             string arg1 = "Hello";
//             string arg2 = "World";
// 
//             // Act
//             _scriptConsole.Log(arg1, arg2);
// 
//             // Assert
//             string expectedOutput = $"{arg1} {arg2}{Environment.NewLine}";
//             Assert.AreEqual(expectedOutput, writer.ToString(), "Log did not print the expected concatenated string.");
//         }

        #endregion

        #region Info Tests

        /// <summary>
        /// Tests the Info method with null arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (85-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (85-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (97-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Info_NullArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Info(null);
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Info should not produce any output when called with null arguments.");
//         }

        /// <summary>
        /// Tests the Info method with empty arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (103-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (103-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (115-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Info_EmptyArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Info();
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Info should not produce any output when called with empty arguments.");
//         }

        /// <summary>
        /// Tests the Info method with valid arguments to ensure the concatenated string is printed 
        /// and console foreground color is properly reset.
        /// </summary>
//         [TestMethod] [Error] (122-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (122-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (137-13)CS0103 The name 'Assert' does not exist in the current context [Error] (138-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Info_ValidArgs_PrintsConcatenatedString()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
//             var originalColor = Console.ForegroundColor;
//             string arg1 = "Green";
//             string arg2 = "Message";
// 
//             // Act
//             _scriptConsole.Info(arg1, arg2);
// 
//             // Assert
//             string expectedOutput = $"{arg1} {arg2}{Environment.NewLine}";
//             Assert.AreEqual(expectedOutput, writer.ToString(), "Info did not print the expected concatenated string.");
//             Assert.AreEqual(originalColor, Console.ForegroundColor, "Console foreground color was not reset after Info.");
//         }

        #endregion

        #region Warn Tests

        /// <summary>
        /// Tests the Warn method with null arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (148-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (148-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (160-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Warn_NullArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Warn(null);
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Warn should not produce any output when called with null arguments.");
//         }

        /// <summary>
        /// Tests the Warn method with empty arguments to ensure no output is produced.
        /// </summary>
//         [TestMethod] [Error] (166-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (166-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (178-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Warn_EmptyArgs_DoesNothing()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Warn();
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Warn should not produce any output when called with empty arguments.");
//         }

        /// <summary>
        /// Tests the Warn method with valid arguments to ensure the concatenated string is printed 
        /// and console foreground color is properly reset.
        /// </summary>
//         [TestMethod] [Error] (185-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (185-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (200-13)CS0103 The name 'Assert' does not exist in the current context [Error] (201-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Warn_ValidArgs_PrintsConcatenatedString()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
//             var originalColor = Console.ForegroundColor;
//             string arg1 = "Warning";
//             string arg2 = "Message";
// 
//             // Act
//             _scriptConsole.Warn(arg1, arg2);
// 
//             // Assert
//             string expectedOutput = $"{arg1} {arg2}{Environment.NewLine}";
//             Assert.AreEqual(expectedOutput, writer.ToString(), "Warn did not print the expected concatenated string.");
//             Assert.AreEqual(originalColor, Console.ForegroundColor, "Console foreground color was not reset after Warn.");
//         }

        #endregion

        #region Error Tests

        /// <summary>
        /// Tests the Error method with null arguments to ensure no output is produced and the error flag is not set.
        /// </summary>
//         [TestMethod] [Error] (211-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (211-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (217-13)CS0103 The name 'Assert' does not exist in the current context [Error] (224-13)CS0103 The name 'Assert' does not exist in the current context [Error] (225-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Error_NullArgs_DoesNothingAndDoesNotSetErrorFlag()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
//             Assert.IsFalse(_scriptConsole.HasErrors, "HasErrors should be false before any error is logged.");
// 
//             // Act
//             _scriptConsole.Error(null);
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Error should not produce any output when called with null arguments.");
//             Assert.IsFalse(_scriptConsole.HasErrors, "HasErrors should remain false when Error is called with null arguments.");
//         }

        /// <summary>
        /// Tests the Error method with empty arguments to ensure no output is produced and the error flag is not set.
        /// </summary>
//         [TestMethod] [Error] (231-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (231-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (243-13)CS0103 The name 'Assert' does not exist in the current context [Error] (244-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Error_EmptyArgs_DoesNothingAndDoesNotSetErrorFlag()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
// 
//             // Act
//             _scriptConsole.Error();
// 
//             // Assert
//             string output = writer.ToString();
//             Assert.AreEqual(string.Empty, output, "Error should not produce any output when called with empty arguments.");
//             Assert.IsFalse(_scriptConsole.HasErrors, "HasErrors should remain false when Error is called with empty arguments.");
//         }

        /// <summary>
        /// Tests the Error method with valid arguments to ensure the concatenated string is printed, 
        /// the error flag is set, and console foreground color is properly reset.
        /// </summary>
//         [TestMethod] [Error] (251-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (251-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (266-13)CS0103 The name 'Assert' does not exist in the current context [Error] (267-13)CS0103 The name 'Assert' does not exist in the current context [Error] (268-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Error_ValidArgs_PrintsConcatenatedStringAndSetsErrorFlag()
//         {
//             // Arrange
//             using var writer = new StringWriter();
//             Console.SetOut(writer);
//             var originalColor = Console.ForegroundColor;
//             string arg1 = "Error";
//             string arg2 = "Occurred";
// 
//             // Act
//             _scriptConsole.Error(arg1, arg2);
// 
//             // Assert
//             string expectedOutput = $"{arg1} {arg2}{Environment.NewLine}";
//             Assert.AreEqual(expectedOutput, writer.ToString(), "Error did not print the expected concatenated string.");
//             Assert.IsTrue(_scriptConsole.HasErrors, "HasErrors should be true after Error is called with valid arguments.");
//             Assert.AreEqual(originalColor, Console.ForegroundColor, "Console foreground color was not reset after Error.");
//         }

        #endregion
    }
}
