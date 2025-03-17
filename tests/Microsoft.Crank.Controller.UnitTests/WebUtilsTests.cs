using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WebUtils"/> class.
    /// </summary>
    [TestClass]
    public class WebUtilsTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;

        public WebUtilsTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
        }

        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileContentAsync(HttpClient, string)"/> method to ensure it correctly downloads file content.
        /// </summary>
//         [TestMethod] [Error] (35-43)CS0122 'HttpMessageHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task DownloadFileContentAsync_ValidUri_ReturnsFileContent()
//         {
//             // Arrange
//             var uri = "http://example.com/file.txt";
//             var expectedContent = "File content";
//             var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
//             mockHttpMessageHandler
//                 .Setup(handler => handler.SendAsync(It.Is<HttpRequestMessage>(req => req.RequestUri == new Uri(uri)), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new HttpResponseMessage
//                 {
//                     StatusCode = System.Net.HttpStatusCode.OK,
//                     Content = new StringContent(expectedContent)
//                 });
//             var httpClient = new HttpClient(mockHttpMessageHandler.Object);
// 
//             // Act
//             var actualContent = await httpClient.DownloadFileContentAsync(uri);
// 
//             // Assert
//             Assert.AreEqual(expectedContent, actualContent);
//         }

        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileAsync(HttpClient, string, string, string)"/> method to ensure it correctly downloads a file.
        /// </summary>
        [TestMethod]
        public async Task DownloadFileAsync_ValidUri_DownloadsFile()
        {
            // Arrange
            var uri = "http://example.com/file.txt";
            var destinationFileName = "downloadedFile.txt";
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.Is<HttpRequestMessage>(req => req.RequestUri == new Uri(uri)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StreamContent(new MemoryStream())
                });
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);

            // Act
            await httpClient.DownloadFileAsync(uri, null, destinationFileName);

            // Assert
            Assert.IsTrue(File.Exists(destinationFileName));
            File.Delete(destinationFileName);
        }

        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileWithProgressAsync(HttpClient, string, string, string)"/> method to ensure it correctly downloads a file with progress.
        /// </summary>
        [TestMethod]
        public async Task DownloadFileWithProgressAsync_ValidUri_DownloadsFileWithProgress()
        {
            // Arrange
            var uri = "http://example.com/file.txt";
            var destinationFileName = "downloadedFile.txt";
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.Is<HttpRequestMessage>(req => req.RequestUri == new Uri(uri)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StreamContent(new MemoryStream())
                });
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);

            // Act
            await httpClient.DownloadFileWithProgressAsync(uri, null, destinationFileName);

            // Assert
            Assert.IsTrue(File.Exists(destinationFileName));
            File.Delete(destinationFileName);
        }

        /// <summary>
        /// Tests the <see cref="WebUtils.CopyToAsync(Stream, Stream, IProgress{long}, CancellationToken, int)"/> method to ensure it correctly copies data between streams.
        /// </summary>
        [TestMethod]
        public async Task CopyToAsync_ValidStreams_CopiesData()
        {
            // Arrange
            var sourceStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var destinationStream = new MemoryStream();
            var progress = new Mock<IProgress<long>>();

            // Act
            await sourceStream.CopyToAsync(destinationStream, progress.Object);

            // Assert
            Assert.AreEqual(sourceStream.Length, destinationStream.Length);
        }
    }
}
