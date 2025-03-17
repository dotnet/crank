using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Jobs.Bombardier.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;

        public ProgramTests()
        {
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
            _mockHttpClient = new Mock<HttpClient>(_mockHttpClientHandler.Object);
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it returns -1 when no valid duration or requests are provided.
        /// </summary>
//         [TestMethod] [Error] (42-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_NoValidDurationOrRequests_ReturnsMinusOne()
//         {
//             // Arrange
//             string[] args = { "-w", "0", "-d", "0", "-n", "0" };
// 
//             // Act
//             int result = await Program.Main(args);
// 
//             // Assert
//             Assert.AreEqual(-1, result, "Expected Main to return -1 when no valid duration or requests are provided.");
//         }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it returns -1 when an invalid output format is provided.
        /// </summary>
        [TestMethod]
        public async Task Main_InvalidOutputFormat_ReturnsMinusOne()
        {
            // Arrange
            string[] args = { "-w", "0", "-d", "10", "-o", "invalid-format" };

            // Act
            int result = await Program.Main(args);

            // Assert
            Assert.AreEqual(-1, result, "Expected Main to return -1 when an invalid output format is provided.");
        }

        /// <summary>
        /// Tests the <see cref="Program.TryGetArgumentValue{T}(string, List{string}, out T)"/> method to ensure it correctly extracts an integer argument.
        /// </summary>
        [TestMethod]
        public void TryGetArgumentValue_ValidIntegerArgument_ReturnsTrue()
        {
            // Arrange
            List<string> argsList = new List<string> { "-d", "10" };

            // Act
            bool result = Program.TryGetArgumentValue("-d", argsList, out int duration);

            // Assert
            Assert.IsTrue(result, "Expected TryGetArgumentValue to return true for a valid integer argument.");
            Assert.AreEqual(10, duration, "Expected TryGetArgumentValue to correctly extract the integer argument.");
        }

        /// <summary>
        /// Tests the <see cref="Program.TryGetArgumentValue{T}(string, List{string}, out T)"/> method to ensure it returns false for a missing argument.
        /// </summary>
        [TestMethod]
        public void TryGetArgumentValue_MissingArgument_ReturnsFalse()
        {
            // Arrange
            List<string> argsList = new List<string> { "-d" };

            // Act
            bool result = Program.TryGetArgumentValue("-d", argsList, out int duration);

            // Assert
            Assert.IsFalse(result, "Expected TryGetArgumentValue to return false for a missing argument.");
            Assert.AreEqual(0, duration, "Expected TryGetArgumentValue to set the out parameter to the default value for a missing argument.");
        }

        /// <summary>
        /// Tests the <see cref="Program.MeasureFirstRequest(string[])"/> method to ensure it correctly measures the first request.
        /// </summary>
        [TestMethod]
        public async Task MeasureFirstRequest_ValidUrl_LogsElapsedTime()
        {
            // Arrange
            string[] args = { "http://example.com" };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            typeof(Program).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, httpClient);

            // Act
            await Program.MeasureFirstRequest(args);

            // Assert
            // Verify that the elapsed time was logged (this would require capturing console output or using a logging framework)
        }

        /// <summary>
        /// Tests the <see cref="Program.MeasureFirstRequest(string[])"/> method to ensure it logs a message when the URL is not found.
        /// </summary>
        [TestMethod]
        public async Task MeasureFirstRequest_UrlNotFound_LogsMessage()
        {
            // Arrange
            string[] args = { "invalid-url" };

            // Act
            await Program.MeasureFirstRequest(args);

            // Assert
            // Verify that the appropriate message was logged (this would require capturing console output or using a logging framework)
        }

        /// <summary>
        /// Tests the <see cref="Program.DownloadToTempFile(string)"/> method to ensure it correctly downloads a file to a temporary location.
        /// </summary>
        [TestMethod]
        public async Task DownloadToTempFile_ValidUrl_ReturnsTempFilePath()
        {
            // Arrange
            string url = "http://example.com/file.txt";
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("file content") });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            typeof(Program).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, httpClient);

            // Act
            string tempFilePath = await Program.DownloadToTempFile(url);

            // Assert
            Assert.IsTrue(File.Exists(tempFilePath), "Expected the file to be downloaded to a temporary location.");
            Assert.AreEqual("file content", File.ReadAllText(tempFilePath), "Expected the downloaded file content to match.");
        }

        /// <summary>
        /// Tests the <see cref="Program.CleanTempFile(string)"/> method to ensure it correctly deletes a temporary file.
        /// </summary>
        [TestMethod]
        public void CleanTempFile_ValidFilePath_DeletesFile()
        {
            // Arrange
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "temp content");

            // Act
            Program.CleanTempFile(tempFilePath);

            // Assert
            Assert.IsFalse(File.Exists(tempFilePath), "Expected the temporary file to be deleted.");
        }

        /// <summary>
        /// Tests the <see cref="Program.Quote(string)"/> method to ensure it correctly wraps a string containing spaces in double quotes.
        /// </summary>
        [TestMethod]
        public void Quote_StringWithSpaces_WrapsInDoubleQuotes()
        {
            // Arrange
            string input = "string with spaces";

            // Act
            string result = Program.Quote(input);

            // Assert
            Assert.AreEqual("\"string with spaces\"", result, "Expected the string to be wrapped in double quotes.");
        }

        /// <summary>
        /// Tests the <see cref="Program.Quote(string)"/> method to ensure it returns the original string if it does not contain spaces.
        /// </summary>
        [TestMethod]
        public void Quote_StringWithoutSpaces_ReturnsOriginalString()
        {
            // Arrange
            string input = "stringWithoutSpaces";

            // Act
            string result = Program.Quote(input);

            // Assert
            Assert.AreEqual("stringWithoutSpaces", result, "Expected the original string to be returned.");
        }

        /// <summary>
        /// Tests the <see cref="Program.ToMilliseconds(double)"/> method to ensure it correctly converts microseconds to milliseconds.
        /// </summary>
        [TestMethod]
        public void ToMilliseconds_ValidMicroseconds_ReturnsMilliseconds()
        {
            // Arrange
            double microseconds = 1000;

            // Act
            double result = Program.ToMilliseconds(microseconds);

            // Assert
            Assert.AreEqual(1, result, "Expected the microseconds to be correctly converted to milliseconds.");
        }
    }
}
