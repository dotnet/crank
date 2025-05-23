using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Jobs.Bombardier;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Crank.Jobs.Bombardier.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly StringWriter _consoleOutput;
        private readonly object _httpClientLock = new object();
        private readonly HttpClient _originalHttpClient;
        private readonly FieldInfo _httpClientField;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramTests"/> class.
        /// Captures the original console output and the original _httpClient.
        /// </summary>
        public ProgramTests()
        {
            _originalOut = Console.Out;
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);

            _httpClientField = typeof(Program).GetField("_httpClient", BindingFlags.Static | BindingFlags.NonPublic);
            if (_httpClientField == null)
            {
                throw new Exception("Could not find field _httpClient on Program class.");
            }
            // Store original _httpClient to restore it later.
            _originalHttpClient = (HttpClient)_httpClientField.GetValue(null);
        }

        /// <summary>
        /// Restores original console output and _httpClient.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(_originalOut);
            // Restore the original _httpClient.
            lock (_httpClientLock)
            {
                _httpClientField.SetValue(null, _originalHttpClient);
            }
            _consoleOutput.Dispose();
        }

        /// <summary>
        /// Tests the Main method when called without valid duration and request arguments.
        /// Expected outcome: returns -1.
        /// </summary>
//         [Fact] [Error] (70-42)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_NoDurationAndNoRequests_ReturnsMinusOne()
//         {
//             // Arrange
//             string[] args = new string[0];
// 
//             // Act
//             int exitCode = await Program.Main(args);
// 
//             // Assert
//             Assert.Equal(-1, exitCode);
//         }

        /// <summary>
        /// Tests the MeasureFirstRequest method when no URL is provided.
        /// Expected outcome: outputs a message indicating that the URL was not found.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_NoUrl_PrintsSkippedMessage()
        {
            // Arrange
            string[] args = new string[] { "-d", "10" };

            // Act
            await Program.MeasureFirstRequest(args);
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("URL not found, skipping first request", output);
        }

        /// <summary>
        /// Tests the MeasureFirstRequest method when a successful HTTP response is returned.
        /// Expected outcome: prints elapsed milliseconds ending with "ms".
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_WithValidUrl_ReturnsElapsedTime()
        {
            // Arrange
            var testResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(testResponse)
               .Verifiable();

            var fakeHttpClient = new HttpClient(handlerMock.Object);
            lock (_httpClientLock)
            {
                _httpClientField.SetValue(null, fakeHttpClient);
            }
            string[] args = new string[] { "http://example.com", "-d", "10" };

            // Act
            await Program.MeasureFirstRequest(args);
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("ms", output);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == "http://example.com"),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests the MeasureFirstRequest method when an OperationCanceledException occurs.
        /// Expected outcome: prints a timeout message.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_OperationCanceledException_PrintsTimeoutMessage()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new OperationCanceledException())
               .Verifiable();

            var fakeHttpClient = new HttpClient(handlerMock.Object);
            lock (_httpClientLock)
            {
                _httpClientField.SetValue(null, fakeHttpClient);
            }
            string[] args = new string[] { "http://example.com", "-d", "10" };

            // Act
            await Program.MeasureFirstRequest(args);
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("A timeout occurred while measuring the first request", output);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests the MeasureFirstRequest method when an HttpRequestException occurs.
        /// Expected outcome: prints a connection exception message.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_HttpRequestException_PrintsConnectionExceptionMessage()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("Connection failed"))
               .Verifiable();

            var fakeHttpClient = new HttpClient(handlerMock.Object);
            lock (_httpClientLock)
            {
                _httpClientField.SetValue(null, fakeHttpClient);
            }
            string[] args = new string[] { "http://example.com", "-d", "10" };

            // Act
            await Program.MeasureFirstRequest(args);
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("A connection exception occurred while measuring the first request", output);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests the MeasureFirstRequest method when a generic exception occurs.
        /// Expected outcome: prints an unexpected exception message.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_GenericException_PrintsUnexpectedExceptionMessage()
        {
            // Arrange
            var ex = new Exception("Test exception");
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(ex)
               .Verifiable();

            var fakeHttpClient = new HttpClient(handlerMock.Object);
            lock (_httpClientLock)
            {
                _httpClientField.SetValue(null, fakeHttpClient);
            }
            string[] args = new string[] { "http://example.com", "-d", "10" };

            // Act
            await Program.MeasureFirstRequest(args);
            string output = _consoleOutput.ToString();

            // Assert
            Assert.Contains("An unexpected exception occurred while measuring the first request", output);
            Assert.Contains("Test exception", output);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
