// using System;
// using System.IO;
// using Moq;
// using Serilog;
// using Xunit;
// using Microsoft.Crank.Agent;
// 
// namespace Microsoft.Crank.Agent.UnitTests
// {
//     /// <summary>
//     /// Unit tests for the <see cref="Log"/> class.
//     /// </summary>
//     public class LogTests : IDisposable
//     {
//         private readonly TextWriter _originalConsoleOut;
// 
//         /// <summary>
//         /// Initializes a new instance of the <see cref="LogTests"/> class and saves the original Console output.
//         /// </summary>
//         public LogTests()
//         {
//             _originalConsoleOut = Console.Out;
//         }
// 
//         /// <summary>
//         /// Disposes resources and resets Console output and Startup.Logger after each test.
//         /// </summary>
// //         public void Dispose() [Error] (31-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         {
// //             Console.SetOut(_originalConsoleOut);
// //             Startup.Logger = null;
// //         }
// 
//         /// <summary>
//         /// Tests that Info method writes a timestamped message to the console when Startup.Logger is null and timestamp is true.
//         /// Expected outcome is that the output starts with a '[' indicating the presence of a timestamp,
//         /// and the message is appended after the timestamp.
//         /// </summary>
// //         [Fact] [Error] (43-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Info_WhenLoggerIsNullAndTimestampTrue_WritesTimestampedMessageToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var testMessage = "Test info message with timestamp";
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Info(testMessage, true);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.StartsWith("[", output);
// //             Assert.Contains("] " + testMessage, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Info method writes the message without a timestamp to the console when Startup.Logger is null and timestamp is false.
//         /// Expected outcome is that the output exactly equals the provided message.
//         /// </summary>
// //         [Fact] [Error] (65-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Info_WhenLoggerIsNullAndTimestampFalse_WritesMessageWithoutTimestampToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var testMessage = "Test info message without timestamp";
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Info(testMessage, false);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.Equal(testMessage, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Info method calls Logger.Information when Startup.Logger is not null and timestamp is true.
//         /// Expected outcome is that the Logger.Information method is invoked exactly once with the provided message.
//         /// </summary>
// //         [Fact] [Error] (88-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Info_WhenLoggerIsNotNullAndTimestampTrue_CallsLoggerInformation()
// //         {
// //             // Arrange
// //             var testMessage = "Test info message with logger";
// //             var mockLogger = new Mock<ILogger>();
// //             Startup.Logger = mockLogger.Object;
// // 
// //             // Act
// //             Log.Info(testMessage, true);
// // 
// //             // Assert
// //             mockLogger.Verify(logger => logger.Information(testMessage), Times.Once);
// //         }
// 
//         /// <summary>
//         /// Tests that Info method calls Logger.Information when Startup.Logger is not null and timestamp is false.
//         /// Expected outcome is that the Logger.Information method is invoked exactly once with the provided message.
//         /// </summary>
// //         [Fact] [Error] (107-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Info_WhenLoggerIsNotNullAndTimestampFalse_CallsLoggerInformation()
// //         {
// //             // Arrange
// //             var testMessage = "Test info message with logger no timestamp";
// //             var mockLogger = new Mock<ILogger>();
// //             Startup.Logger = mockLogger.Object;
// // 
// //             // Act
// //             Log.Info(testMessage, false);
// // 
// //             // Assert
// //             mockLogger.Verify(logger => logger.Information(testMessage), Times.Once);
// //         }
// 
//         /// <summary>
//         /// Tests that Error(Exception, string) writes a timestamped error message to the console when Startup.Logger is null and no additional message is provided.
//         /// Expected outcome is that the console output starts with a timestamp and contains the exception message.
//         /// </summary>
// //         [Fact] [Error] (124-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void ErrorException_WhenLoggerIsNullAndNoMessage_WritesTimestampedExceptionMessageToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var exception = new Exception("Error occurred");
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Error(exception);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.StartsWith("[", output);
// //             Assert.Contains("] " + exception.Message, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Error(Exception, string) writes a timestamped combined message to the console when Startup.Logger is null and an additional message is provided.
//         /// Expected outcome is that the console output starts with a timestamp and contains both the additional message and the exception message.
//         /// </summary>
// //         [Fact] [Error] (146-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void ErrorException_WhenLoggerIsNullAndAdditionalMessageProvided_WritesTimestampedCombinedMessageToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var exception = new Exception("Error occurred");
// //             var additionalMessage = "Additional context";
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Error(exception, additionalMessage);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.StartsWith("[", output);
// //             Assert.Contains("] " + additionalMessage + " " + exception.Message, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Error(Exception, string) calls Logger.Error when Startup.Logger is not null.
//         /// Expected outcome is that the Logger.Error method is invoked exactly once with the exception and the additional message.
//         /// </summary>
// //         [Fact] [Error] (172-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void ErrorException_WhenLoggerIsNotNull_CallsLoggerErrorWithExceptionAndMessage()
// //         {
// //             // Arrange
// //             var exception = new Exception("Logger error occurred");
// //             var additionalMessage = "Logger context";
// //             var mockLogger = new Mock<ILogger>();
// //             Startup.Logger = mockLogger.Object;
// // 
// //             // Act
// //             Log.Error(exception, additionalMessage);
// // 
// //             // Assert
// //             mockLogger.Verify(logger => logger.Error(exception, additionalMessage), Times.Once);
// //         }
// 
//         /// <summary>
//         /// Tests that Error(string) writes a timestamped error message to the console when Startup.Logger is null.
//         /// Expected outcome is that the console output starts with a timestamp and contains the error message.
//         /// </summary>
// //         [Fact] [Error] (189-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void ErrorString_WhenLoggerIsNull_WritesTimestampedErrorMessageToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var testMessage = "Error string message";
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Error(testMessage);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.StartsWith("[", output);
// //             Assert.Contains("] " + testMessage, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Error(string) calls Logger.Error when Startup.Logger is not null.
//         /// Expected outcome is that the Logger.Error method is invoked exactly once with the error message.
//         /// </summary>
// //         [Fact] [Error] (213-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void ErrorString_WhenLoggerIsNotNull_CallsLoggerErrorWithMessage()
// //         {
// //             // Arrange
// //             var testMessage = "Error string message with logger";
// //             var mockLogger = new Mock<ILogger>();
// //             Startup.Logger = mockLogger.Object;
// // 
// //             // Act
// //             Log.Error(testMessage);
// // 
// //             // Assert
// //             mockLogger.Verify(logger => logger.Error(testMessage), Times.Once);
// //         }
// 
//         /// <summary>
//         /// Tests that Warning method writes a timestamped warning message to the console when Startup.Logger is null.
//         /// Expected outcome is that the console output starts with a timestamp and contains the warning message.
//         /// </summary>
// //         [Fact] [Error] (230-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Warning_WhenLoggerIsNull_WritesTimestampedWarningMessageToConsole()
// //         {
// //             // Arrange
// //             Startup.Logger = null;
// //             var testMessage = "Warning message";
// //             using var writer = new StringWriter();
// //             Console.SetOut(writer);
// // 
// //             // Act
// //             Log.Warning(testMessage);
// // 
// //             // Assert
// //             var output = writer.ToString().Trim();
// //             Assert.StartsWith("[", output);
// //             Assert.Contains("] " + testMessage, output);
// //         }
// 
//         /// <summary>
//         /// Tests that Warning method calls Logger.Warning when Startup.Logger is not null.
//         /// Expected outcome is that the Logger.Warning method is invoked exactly once with the warning message.
//         /// </summary>
// //         [Fact] [Error] (254-13)CS0272 The property or indexer 'Startup.Logger' cannot be used in this context because the set accessor is inaccessible
// //         public void Warning_WhenLoggerIsNotNull_CallsLoggerWarningWithMessage()
// //         {
// //             // Arrange
// //             var testMessage = "Warning message with logger";
// //             var mockLogger = new Mock<ILogger>();
// //             Startup.Logger = mockLogger.Object;
// // 
// //             // Act
// //             Log.Warning(testMessage);
// // 
// //             // Assert
// //             mockLogger.Verify(logger => logger.Warning(testMessage), Times.Once);
// //         }
//     }
// }
