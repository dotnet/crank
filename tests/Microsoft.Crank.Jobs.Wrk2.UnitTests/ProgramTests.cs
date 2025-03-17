using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Jobs.Wrk2.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public ProgramTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it returns -1 for unsupported platforms.
        /// </summary>
//         [TestMethod] [Error] (38-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_UnsupportedPlatform_ReturnsMinusOne()
//         {
//             // Arrange
//             var args = new string[] { };
// 
//             // Act
//             var result = await Program.Main(args);
// 
//             // Assert
//             Assert.AreEqual(-1, result);
//         }

        /// <summary>
        /// Tests the <see cref="Program.MeasureFirstRequest(string[])"/> method to ensure it handles missing URL.
        /// </summary>
        [TestMethod]
        public async Task MeasureFirstRequest_NoUrl_SkipsFirstRequest()
        {
            // Arrange
            var args = new string[] { };

            // Act
            await Program.MeasureFirstRequest(args);

            // Assert
            // No exception means the test passed
        }

        /// <summary>
        /// Tests the <see cref="Program.MeasureFirstRequest(string[])"/> method to ensure it handles a valid URL.
        /// </summary>
        [TestMethod]
        public async Task MeasureFirstRequest_ValidUrl_MeasuresFirstRequest()
        {
            // Arrange
            var args = new string[] { "http://example.com" };
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            _httpMessageHandlerMock
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMessage);

            // Act
            await Program.MeasureFirstRequest(args);

            // Assert
            // No exception means the test passed
        }

        /// <summary>
        /// Tests the <see cref="Program.DownloadWrk2Async"/> method to ensure it downloads the file correctly.
        /// </summary>
        [TestMethod]
        public async Task DownloadWrk2Async_DownloadsFile_ReturnsFilename()
        {
            // Act
            var result = await Program.DownloadWrk2Async();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadDuration(Match)"/> method to ensure it parses duration correctly.
        /// </summary>
        [TestMethod]
        public void ReadDuration_ValidMatch_ReturnsTimeSpan()
        {
            // Arrange
            var match = Regex.Match("10s", @"([\d\.]+)([a-z]+)");

            // Act
            var result = Program.ReadDuration(match);

            // Assert
            Assert.AreEqual(TimeSpan.FromSeconds(10), result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadRequests(Match)"/> method to ensure it parses requests correctly.
        /// </summary>
        [TestMethod]
        public void ReadRequests_ValidMatch_ReturnsRequestCount()
        {
            // Arrange
            var match = Regex.Match("100 requests in 10s", @"([\d\.]+) requests in ([\d\.]+)([a-z]+)");

            // Act
            var result = Program.ReadRequests(match);

            // Assert
            Assert.AreEqual(100, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadBadReponses(Match)"/> method to ensure it parses bad responses correctly.
        /// </summary>
        [TestMethod]
        public void ReadBadReponses_ValidMatch_ReturnsBadResponseCount()
        {
            // Arrange
            var match = Regex.Match("Non-2xx or 3xx responses: 5", @"Non-2xx or 3xx responses: ([\d\.]+)");

            // Act
            var result = Program.ReadBadReponses(match);

            // Assert
            Assert.AreEqual(5, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.CountSocketErrors(Match)"/> method to ensure it parses socket errors correctly.
        /// </summary>
        [TestMethod]
        public void CountSocketErrors_ValidMatch_ReturnsSocketErrorCount()
        {
            // Arrange
            var match = Regex.Match("Socket errors: connect 1, read 2, write 3, timeout 4", @"Socket errors: connect ([\d\.]+), read ([\d\.]+), write ([\d\.]+), timeout ([\d\.]+)");

            // Act
            var result = Program.CountSocketErrors(match);

            // Assert
            Assert.AreEqual(10, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadLatency(Match)"/> method to ensure it parses latency correctly.
        /// </summary>
        [TestMethod]
        public void ReadLatency_ValidMatch_ReturnsLatency()
        {
            // Arrange
            var match = Regex.Match("Latency 10ms", @"Latency ([\d\.]+)([a-z]+)");

            // Act
            var result = Program.ReadLatency(match);

            // Assert
            Assert.AreEqual(10, result);
        }
    }
}
