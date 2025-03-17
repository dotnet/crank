using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GZipFileResult"/> class.
    /// </summary>
    [TestClass]
    public class GZipFileResultTests
    {
        private readonly string _testFileName;
        private readonly GZipFileResult _gzipFileResult;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockHttpRequest;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly Mock<ActionContext> _mockActionContext;

        public GZipFileResultTests()
        {
            _testFileName = "testfile.txt";
            _gzipFileResult = new GZipFileResult(_testFileName);
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpRequest = new Mock<HttpRequest>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockActionContext = new Mock<ActionContext>();

            _mockHttpContext.SetupGet(c => c.Request).Returns(_mockHttpRequest.Object);
            _mockHttpContext.SetupGet(c => c.Response).Returns(_mockHttpResponse.Object);
            _mockActionContext.SetupGet(a => a.HttpContext).Returns(_mockHttpContext.Object);
        }

        /// <summary>
        /// Tests the <see cref="GZipFileResult.ExecuteResultAsync(ActionContext)"/> method to ensure it correctly sets the headers and writes the file to the response body.
        /// </summary>
        [TestMethod]
        public async Task ExecuteResultAsync_WhenCalled_SetsHeadersAndWritesFileToResponseBody()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            _mockHttpResponse.SetupGet(r => r.Body).Returns(memoryStream);
            _mockHttpRequest.SetupGet(r => r.Headers).Returns(new HeaderDictionary());

            // Act
            await _gzipFileResult.ExecuteResultAsync(_mockActionContext.Object);

            // Assert
            _mockHttpResponse.Verify(r => r.Headers.Append(HeaderNames.Vary, HeaderNames.ContentEncoding), Times.Once);
            Assert.AreEqual(new FileInfo(_testFileName).Length.ToString(CultureInfo.InvariantCulture), _mockHttpResponse.Object.Headers["FileLength"]);
        }

        /// <summary>
        /// Tests the <see cref="GZipFileResult.ExecuteResultAsync(ActionContext)"/> method to ensure it correctly compresses the file when gzip is accepted.
        /// </summary>
        [TestMethod]
        public async Task ExecuteResultAsync_WhenGzipAccepted_CompressesFile()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            _mockHttpResponse.SetupGet(r => r.Body).Returns(memoryStream);
            var headers = new HeaderDictionary { { HeaderNames.AcceptEncoding, "gzip" } };
            _mockHttpRequest.SetupGet(r => r.Headers).Returns(headers);

            // Act
            await _gzipFileResult.ExecuteResultAsync(_mockActionContext.Object);

            // Assert
            _mockHttpResponse.Verify(r => r.Headers.Append(HeaderNames.Vary, HeaderNames.ContentEncoding), Times.Once);
            Assert.AreEqual("gzip", _mockHttpResponse.Object.Headers[HeaderNames.ContentEncoding]);
        }

        /// <summary>
        /// Tests the <see cref="GZipFileResult.ExecuteResultAsync(ActionContext)"/> method to ensure it writes the file without compression when gzip is not accepted.
        /// </summary>
        [TestMethod]
        public async Task ExecuteResultAsync_WhenGzipNotAccepted_WritesFileWithoutCompression()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            _mockHttpResponse.SetupGet(r => r.Body).Returns(memoryStream);
            _mockHttpRequest.SetupGet(r => r.Headers).Returns(new HeaderDictionary());

            // Act
            await _gzipFileResult.ExecuteResultAsync(_mockActionContext.Object);

            // Assert
            _mockHttpResponse.Verify(r => r.Headers.Append(HeaderNames.Vary, HeaderNames.ContentEncoding), Times.Once);
            Assert.IsFalse(_mockHttpResponse.Object.Headers.ContainsKey(HeaderNames.ContentEncoding));
        }
    }
}

