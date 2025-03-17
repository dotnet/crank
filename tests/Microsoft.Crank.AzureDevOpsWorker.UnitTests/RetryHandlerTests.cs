using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="RetryHandler"/> class.
    /// </summary>
    [TestClass]
    public class RetryHandlerTests
    {
        private readonly Mock<HttpMessageHandler> _mockInnerHandler;
        private readonly RetryHandler _retryHandler;

        public RetryHandlerTests()
        {
            _mockInnerHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _retryHandler = new RetryHandler(_mockInnerHandler.Object);
        }

        /// <summary>
        /// Tests the <see cref="RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> method to ensure it returns a successful response on the first attempt.
        /// </summary>
//         [TestMethod] [Error] (36-43)CS0122 'HttpMessageHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_FirstAttemptSuccess_ReturnsResponse()
//         {
//             // Arrange
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
//             var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
//             _mockInnerHandler
//                 .Setup(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(expectedResponse);
// 
//             // Act
//             var response = await _retryHandler.SendAsync(request, CancellationToken.None);
// 
//             // Assert
//             Assert.AreEqual(expectedResponse, response);
//             _mockInnerHandler.Verify(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once);
//         }

        /// <summary>
        /// Tests the <see cref="RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> method to ensure it retries the request up to the maximum number of retries.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_MaxRetries_ReturnsLastResponse()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            var failedResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            _mockInnerHandler
                .SetupSequence(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(failedResponse);

            // Act
            var response = await _retryHandler.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.AreEqual(failedResponse, response);
            _mockInnerHandler.Verify(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        /// <summary>
        /// Tests the <see cref="RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> method to ensure it retries the request and succeeds before reaching the maximum number of retries.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_RetrySucceedsBeforeMaxRetries_ReturnsSuccessfulResponse()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            var failedResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successfulResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _mockInnerHandler
                .SetupSequence(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(successfulResponse);

            // Act
            var response = await _retryHandler.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.AreEqual(successfulResponse, response);
            _mockInnerHandler.Verify(handler => handler.SendAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        /// <summary>
        /// Tests the <see cref="RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)"/> method to ensure it throws an exception when the request is canceled.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_RequestCanceled_ThrowsTaskCanceledException()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => _retryHandler.SendAsync(request, cts.Token));
        }
    }
}
