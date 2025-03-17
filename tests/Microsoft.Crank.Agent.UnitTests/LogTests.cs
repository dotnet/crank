using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Log"/> class.
    /// </summary>
//     [TestClass] [Error] (19-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
//     public class LogTests
//     {
//         private readonly Mock<ILogger> _mockLogger;
// 
//         public LogTests()
//         {
//             _mockLogger = new Mock<ILogger>();
//             Startup.Logger = _mockLogger.Object;
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Info(string, bool)"/> method to ensure it logs information with a timestamp.
//         /// </summary>
//         [TestMethod]
//         public void Info_WithTimestamp_LogsInformationWithTimestamp()
//         {
//             // Arrange
//             string message = "Test message";
// 
//             // Act
//             Log.Info(message, true);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Information(It.Is<string>(msg => msg == message)), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Info(string, bool)"/> method to ensure it logs information without a timestamp.
//         /// </summary>
//         [TestMethod]
//         public void Info_WithoutTimestamp_LogsInformationWithoutTimestamp()
//         {
//             // Arrange
//             string message = "Test message";
// 
//             // Act
//             Log.Info(message, false);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Information(It.Is<string>(msg => msg == message)), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Error(Exception, string)"/> method to ensure it logs an error with an exception and a message.
//         /// </summary>
//         [TestMethod]
//         public void Error_WithExceptionAndMessage_LogsErrorWithExceptionAndMessage()
//         {
//             // Arrange
//             string message = "Test error message";
//             Exception exception = new Exception("Test exception");
// 
//             // Act
//             Log.Error(exception, message);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Error(exception, message), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Error(Exception, string)"/> method to ensure it logs an error with an exception and no message.
//         /// </summary>
//         [TestMethod]
//         public void Error_WithExceptionAndNoMessage_LogsErrorWithExceptionAndNoMessage()
//         {
//             // Arrange
//             Exception exception = new Exception("Test exception");
// 
//             // Act
//             Log.Error(exception);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Error(exception, null), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Error(string)"/> method to ensure it logs an error with a message.
//         /// </summary>
//         [TestMethod]
//         public void Error_WithMessage_LogsErrorWithMessage()
//         {
//             // Arrange
//             string message = "Test error message";
// 
//             // Act
//             Log.Error(message);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Error(It.Is<string>(msg => msg == message)), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="Log.Warning(string)"/> method to ensure it logs a warning with a message.
//         /// </summary>
//         [TestMethod]
//         public void Warning_WithMessage_LogsWarningWithMessage()
//         {
//             // Arrange
//             string message = "Test warning message";
// 
//             // Act
//             Log.Warning(message);
// 
//             // Assert
//             _mockLogger.Verify(logger => logger.Warning(It.Is<string>(msg => msg == message)), Times.Once);
//         }
//     }
}
