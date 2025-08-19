using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Controller;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WebUtils"/> class.
    /// </summary>
    public class WebUtilsTests
    {
        /// <summary>
        /// Tests that DownloadFileContentAsync returns the expected content when the HttpClient returns a valid stream.
        /// </summary>
        [Fact]
        public async Task DownloadFileContentAsync_ValidUri_ReturnsExpectedContent()
        {
            // Arrange
            var expectedContent = "Hello World";
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StreamContent(responseStream)
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";

            // Act
            var actualContent = await httpClient.DownloadFileContentAsync(testUri);

            // Assert
            Assert.Equal(expectedContent, actualContent);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.AtLeastOnce(),
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Tests that DownloadFileContentAsync propagates exceptions from HttpClient when GetStreamAsync fails.
        /// </summary>
        [Fact]
        public async Task DownloadFileContentAsync_HttpClientThrowsException_PropagatesException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("Test exception"))
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.DownloadFileContentAsync(testUri));
            Assert.Equal("Test exception", exception.Message);
        }

        /// <summary>
        /// Tests that DownloadFileAsync creates a file with the expected content when provided a valid URI.
        /// </summary>
        [Fact]
        public async Task DownloadFileAsync_ValidUri_CreatesFileWithExpectedContent()
        {
            // Arrange
            var expectedContent = "FileContent";
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StreamContent(responseStream)
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";
            var serverJobUri = "http://serverjob.com";
            var destinationFileName = Path.GetTempFileName();

            try
            {
                // Act
                await httpClient.DownloadFileAsync(testUri, serverJobUri, destinationFileName);

                // Assert
                Assert.True(File.Exists(destinationFileName));
                var actualContent = await File.ReadAllTextAsync(destinationFileName);
                Assert.Equal(expectedContent, actualContent);
            }
            finally
            {
                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
            }
        }

        /// <summary>
        /// Tests that DownloadFileAsync propagates exceptions from HttpClient when GetStreamAsync fails.
        /// </summary>
        [Fact]
        public async Task DownloadFileAsync_HttpClientThrowsException_PropagatesException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("Test exception"))
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";
            var serverJobUri = "http://serverjob.com";
            var destinationFileName = Path.GetTempFileName();

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    httpClient.DownloadFileAsync(testUri, serverJobUri, destinationFileName));
                Assert.Equal("Test exception", exception.Message);
            }
            finally
            {
                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
            }
        }

        /// <summary>
        /// Tests that DownloadFileWithProgressAsync creates a file with the expected content and writes progress to the console.
        /// </summary>
        [Fact]
        public async Task DownloadFileWithProgressAsync_ValidUri_CreatesFileAndReportsProgress()
        {
            // Arrange
            var expectedContent = "ProgressContent";
            var contentBytes = Encoding.UTF8.GetBytes(expectedContent);
            var contentLengthHeader = contentBytes.Length.ToString();
            var responseStream = new MemoryStream(contentBytes);
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() =>
              {
                  var response = new HttpResponseMessage
                  {
                      StatusCode = HttpStatusCode.OK,
                      Content = new StreamContent(responseStream)
                  };
                  response.Headers.Add("FileLength", contentLengthHeader);
                  return response;
              })
              .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";
            var serverJobUri = "http://serverjob.com";
            var destinationFileName = Path.GetTempFileName();

            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                await httpClient.DownloadFileWithProgressAsync(testUri, serverJobUri, destinationFileName);

                // Assert
                Assert.True(File.Exists(destinationFileName));
                var actualContent = await File.ReadAllTextAsync(destinationFileName);
                Assert.Equal(expectedContent, actualContent);

                var output = consoleOutput.ToString();
                Assert.Contains("KB", output);
                // Optionally check that progress percentage is printed if file length header was provided.
                Assert.Contains("%", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
            }
        }

        /// <summary>
        /// Tests that DownloadFileWithProgressAsync throws an exception when the HTTP response is not successful.
        /// </summary>
        [Fact]
        public async Task DownloadFileWithProgressAsync_ResponseNotSuccess_ThrowsException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(new HttpResponseMessage
              {
                  StatusCode = HttpStatusCode.NotFound,
                  Content = new StringContent("Not Found")
              })
              .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            var testUri = "http://test.com";
            var serverJobUri = "http://serverjob.com";
            var destinationFileName = Path.GetTempFileName();

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<HttpRequestException>(() =>
                    httpClient.DownloadFileWithProgressAsync(testUri, serverJobUri, destinationFileName));
            }
            finally
            {
                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
            }
        }

        /// <summary>
        /// Tests that CopyToAsync successfully copies all content from the source stream to the destination stream and reports progress.
        /// </summary>
        [Fact]
        public async Task CopyToAsync_ValidSourceAndDestination_CopiesContentAndReportsProgress()
        {
            // Arrange
            byte[] buffer = Encoding.UTF8.GetBytes("Test content for CopyToAsync.");
            using var sourceStream = new MemoryStream(buffer);
            using var destinationStream = new MemoryStream();
            var progressReports = new List<long>();
            IProgress<long> progress = new Progress<long>(value => progressReports.Add(value));

            // Act
            await sourceStream.CopyToAsync(destinationStream, progress, CancellationToken.None, 1024);

            // Assert
            var actualContent = destinationStream.ToArray();
            Assert.Equal(buffer, actualContent);
            Assert.True(progressReports.Count > 0);
            Assert.Equal(buffer.Length, progressReports.Last());
        }

        /// <summary>
        /// Tests that CopyToAsync throws an OperationCanceledException if the cancellation token is cancelled.
        /// </summary>
        [Fact]
        public async Task CopyToAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            byte[] buffer = Encoding.UTF8.GetBytes("Cancellation test content.");
            using var sourceStream = new MemoryStream(buffer);
            using var destinationStream = new MemoryStream();
            IProgress<long> progress = new Progress<long>(value => { });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                sourceStream.CopyToAsync(destinationStream, progress, cts.Token, 1024));
        }

        /// <summary>
        /// Tests that CopyToAsync throws a NullReferenceException when a null progress reporter is provided.
        /// </summary>
        [Fact]
        public async Task CopyToAsync_NullProgress_ThrowsNullReferenceException()
        {
            // Arrange
            byte[] buffer = Encoding.UTF8.GetBytes("Null progress test.");
            using var sourceStream = new MemoryStream(buffer);
            using var destinationStream = new MemoryStream();
            IProgress<long> progress = null;

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                sourceStream.CopyToAsync(destinationStream, progress, CancellationToken.None, 1024));
        }
    }
}
