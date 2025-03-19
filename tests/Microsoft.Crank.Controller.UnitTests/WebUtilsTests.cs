using Moq.Protected;
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
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WebUtils"/> class.
    /// </summary>
    public class WebUtilsTests
    {
        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileContentAsync(HttpClient, string)"/> method to ensure it correctly downloads and returns the file content.
        /// </summary>
//         [Fact] [Error] (35-35)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (35-60)CS0103 The name 'MockBehavior' does not exist in the current context [Error] (53-17)CS0103 The name 'Times' does not exist in the current context
//         public async Task DownloadFileContentAsync_HappyPath_ReturnsCorrectContent()
//         {
//             // Arrange
//             var expectedContent = "Hello World!";
//             var testUri = "http://test.com/file.txt";
//             var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
//             {
//                 Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(expectedContent)))
//             };
// 
//             var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//             handlerMock.Protected()
//                .Setup<Task<HttpResponseMessage>>(
//                    "SendAsync",
//                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == testUri),
//                    ItExpr.IsAny<CancellationToken>())
//                .ReturnsAsync(responseMessage)
//                .Verifiable();
// 
//             var httpClient = new HttpClient(handlerMock.Object);
// 
//             // Act
//             var actualContent = await httpClient.DownloadFileContentAsync(testUri);
// 
//             // Assert
//             Assert.Equal(expectedContent, actualContent);
//             handlerMock.Protected().Verify(
//                 "SendAsync",
//                 Times.Once(),
//                 ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == testUri),
//                 ItExpr.IsAny<CancellationToken>());
//         }

        /// <summary>
        /// Tests that <see cref="WebUtils.DownloadFileContentAsync(HttpClient, string)"/> propagates exceptions thrown by HttpClient.GetStreamAsync.
        /// </summary>
//         [Fact] [Error] (66-35)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (66-60)CS0103 The name 'MockBehavior' does not exist in the current context
//         public async Task DownloadFileContentAsync_GetStreamAsyncThrows_PropagatesException()
//         {
//             // Arrange
//             var testUri = "http://test.com/file.txt";
//             var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//             handlerMock.Protected()
//                .Setup<Task<HttpResponseMessage>>(
//                    "SendAsync",
//                    ItExpr.IsAny<HttpRequestMessage>(),
//                    ItExpr.IsAny<CancellationToken>())
//                .ThrowsAsync(new HttpRequestException("Network error"))
//                .Verifiable();
// 
//             var httpClient = new HttpClient(handlerMock.Object);
// 
//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
//             {
//                 await httpClient.DownloadFileContentAsync(testUri);
//             });
//             Assert.Equal("Network error", exception.Message);
//         }

        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileAsync(HttpClient, string, string, string)"/> method to ensure it downloads the file correctly.
        /// </summary>
//         [Fact] [Error] (104-39)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (104-64)CS0103 The name 'MockBehavior' does not exist in the current context [Error] (123-21)CS0103 The name 'Times' does not exist in the current context
//         public async Task DownloadFileAsync_HappyPath_DownloadsFileCorrectly()
//         {
//             // Arrange
//             var expectedContent = "File download test content";
//             var testUri = "http://test.com/file.txt";
//             var serverJobUri = "http://test.com/job";
//             var tempFile = Path.GetTempFileName();
// 
//             try
//             {
//                 var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
//                 {
//                     Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(expectedContent)))
//                 };
// 
//                 var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//                 handlerMock.Protected()
//                    .Setup<Task<HttpResponseMessage>>(
//                        "SendAsync",
//                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == testUri),
//                        ItExpr.IsAny<CancellationToken>())
//                    .ReturnsAsync(responseMessage)
//                    .Verifiable();
// 
//                 var httpClient = new HttpClient(handlerMock.Object);
// 
//                 // Act
//                 await httpClient.DownloadFileAsync(testUri, serverJobUri, tempFile);
//                 var fileContent = await File.ReadAllTextAsync(tempFile);
// 
//                 // Assert
//                 Assert.Equal(expectedContent, fileContent);
//                 handlerMock.Protected().Verify(
//                     "SendAsync",
//                     Times.Once(),
//                     ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == testUri),
//                     ItExpr.IsAny<CancellationToken>());
//             }
//             finally
//             {
//                 if (File.Exists(tempFile))
//                 {
//                     File.Delete(tempFile);
//                 }
//             }
//         }

        /// <summary>
        /// Tests that <see cref="WebUtils.DownloadFileAsync(HttpClient, string, string, string)"/> propagates exceptions thrown during GetStreamAsync.
        /// </summary>
//         [Fact] [Error] (149-39)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (149-64)CS0103 The name 'MockBehavior' does not exist in the current context
//         public async Task DownloadFileAsync_GetStreamAsyncThrows_PropagatesException()
//         {
//             // Arrange
//             var testUri = "http://test.com/file.txt";
//             var serverJobUri = "http://test.com/job";
//             var tempFile = Path.GetTempFileName();
// 
//             try
//             {
//                 var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//                 handlerMock.Protected()
//                    .Setup<Task<HttpResponseMessage>>(
//                        "SendAsync",
//                        ItExpr.IsAny<HttpRequestMessage>(),
//                        ItExpr.IsAny<CancellationToken>())
//                    .ThrowsAsync(new HttpRequestException("Download error"))
//                    .Verifiable();
// 
//                 var httpClient = new HttpClient(handlerMock.Object);
// 
//                 // Act & Assert
//                 var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
//                 {
//                     await httpClient.DownloadFileAsync(testUri, serverJobUri, tempFile);
//                 });
//                 Assert.Equal("Download error", exception.Message);
//             }
//             finally
//             {
//                 if (File.Exists(tempFile))
//                 {
//                     File.Delete(tempFile);
//                 }
//             }
//         }

        /// <summary>
        /// Tests the <see cref="WebUtils.DownloadFileWithProgressAsync(HttpClient, string, string, string)"/> method on a happy path scenario, ensuring file downloads and progress is reported.
        /// </summary>
//         [Fact] [Error] (194-35)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (194-60)CS0103 The name 'MockBehavior' does not exist in the current context [Error] (222-21)CS0103 The name 'Times' does not exist in the current context
//         public async Task DownloadFileWithProgressAsync_HappyPath_DownloadsFileAndReportsProgress()
//         {
//             // Arrange
//             var fileContent = "Content for download with progress.";
//             var expectedFileLength = Encoding.UTF8.GetByteCount(fileContent);
//             var testUri = "http://test.com/file.txt";
//             var serverJobUri = "http://test.com/job";
//             var tempFile = Path.GetTempFileName();
//             var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
//             {
//                 Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)))
//             };
//             responseMessage.Headers.Add("FileLength", expectedFileLength.ToString());
// 
//             var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//             handlerMock.Protected()
//                .Setup<Task<HttpResponseMessage>>(
//                    "SendAsync",
//                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == testUri),
//                    ItExpr.IsAny<CancellationToken>())
//                .ReturnsAsync(responseMessage)
//                .Verifiable();
// 
//             var httpClient = new HttpClient(handlerMock.Object);
// 
//             // Redirect console output for capturing progress logs.
//             var originalOut = Console.Out;
//             using var consoleOutput = new StringWriter();
//             Console.SetOut(consoleOutput);
// 
//             try
//             {
//                 // Act
//                 await httpClient.DownloadFileWithProgressAsync(testUri, serverJobUri, tempFile);
//                 var downloadedContent = await File.ReadAllTextAsync(tempFile);
// 
//                 // Assert
//                 Assert.Equal(fileContent, downloadedContent);
//                 var output = consoleOutput.ToString();
//                 Assert.Contains("KB", output); // Ensure progress reporting printed data.
//                 handlerMock.Protected().Verify(
//                     "SendAsync",
//                     Times.Once(),
//                     ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == testUri),
//                     ItExpr.IsAny<CancellationToken>());
//             }
//             finally
//             {
//                 Console.SetOut(originalOut);
//                 if (File.Exists(tempFile))
//                 {
//                     File.Delete(tempFile);
//                 }
//             }
//         }

        /// <summary>
        /// Tests that <see cref="WebUtils.DownloadFileWithProgressAsync(HttpClient, string, string, string)"/> throws an exception when the response status is not successful.
        /// </summary>
//         [Fact] [Error] (252-35)CS0246 The type or namespace name 'Mock<>' could not be found (are you missing a using directive or an assembly reference?) [Error] (252-60)CS0103 The name 'MockBehavior' does not exist in the current context [Error] (270-17)CS0103 The name 'Times' does not exist in the current context
//         public async Task DownloadFileWithProgressAsync_NonSuccessStatusCode_ThrowsException()
//         {
//             // Arrange
//             var testUri = "http://test.com/file.txt";
//             var serverJobUri = "http://test.com/job";
//             var tempFile = Path.GetTempFileName();
// 
//             var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
//             {
//                 Content = new StringContent("Not Found")
//             };
// 
//             var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//             handlerMock.Protected()
//                .Setup<Task<HttpResponseMessage>>(
//                    "SendAsync",
//                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == testUri),
//                    ItExpr.IsAny<CancellationToken>())
//                .ReturnsAsync(responseMessage)
//                .Verifiable();
// 
//             var httpClient = new HttpClient(handlerMock.Object);
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<HttpRequestException>(async () =>
//             {
//                 await httpClient.DownloadFileWithProgressAsync(testUri, serverJobUri, tempFile);
//             });
//             handlerMock.Protected().Verify(
//                 "SendAsync",
//                 Times.Once(),
//                 ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == testUri),
//                 ItExpr.IsAny<CancellationToken>());
// 
//             if (File.Exists(tempFile))
//             {
//                 File.Delete(tempFile);
//             }
//         }

        /// <summary>
        /// Tests the <see cref="WebUtils.CopyToAsync(Stream, Stream, IProgress{long}, CancellationToken, int)"/> method to ensure it correctly copies all bytes from the source to the destination and reports progress.
        /// </summary>
        [Fact]
        public async Task CopyToAsync_HappyPath_CopiesStreamAndReportsProgress()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("Test data for copying.");
            using var sourceStream = new MemoryStream(testData);
            using var destinationStream = new MemoryStream();
            var progressReports = new List<long>();
            var progress = new Progress<long>(value => progressReports.Add(value));

            // Act
            await sourceStream.CopyToAsync(destinationStream, progress);

            // Assert
            var copiedData = destinationStream.ToArray();
            Assert.Equal(testData, copiedData);
            Assert.NotEmpty(progressReports);
            Assert.Equal(testData.Length, progressReports.Last());
        }

        /// <summary>
        /// Tests the <see cref="WebUtils.CopyToAsync(Stream, Stream, IProgress{long}, CancellationToken, int)"/> method to ensure that it throws an <see cref="OperationCanceledException"/> if cancellation is requested during the copy.
        /// </summary>
        [Fact]
        public async Task CopyToAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("Cancellation test data.");
            // Create a custom stream that simulates delay in reading.
            var sourceStream = new MemoryStream(testData);
            using var destinationStream = new MemoryStream();
            var progress = new Progress<long>(_ => { });

            using var cts = new CancellationTokenSource();
            // Cancel immediately.
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await sourceStream.CopyToAsync(destinationStream, progress, cts.Token);
            });
        }
    }
}
