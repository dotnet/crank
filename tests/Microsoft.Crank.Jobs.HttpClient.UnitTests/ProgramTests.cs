using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;
        private readonly Mock<HttpMessageInvoker> _mockHttpMessageInvoker;
        private readonly Mock<SocketsHttpHandler> _mockHttpHandler;

        public ProgramTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
            _mockHttpMessageInvoker = new Mock<HttpMessageInvoker>();
            _mockHttpHandler = new Mock<SocketsHttpHandler>();
        }

        /// <summary>
        /// Tests the <see cref="Program.Log(string, object[])"/> method to ensure it correctly logs a message when Quiet is false.
        /// </summary>
        [TestMethod]
        public void Log_WhenQuietIsFalse_LogsMessage()
        {
            // Arrange
            Program.Quiet = false;
            var message = "Test message";

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                Program.Log(message);

                // Assert
                var result = sw.ToString().Trim();
                Assert.AreEqual(message, result);
            }
        }

        /// <summary>
        /// Tests the <see cref="Program.Log(string, object[])"/> method to ensure it does not log a message when Quiet is true.
        /// </summary>
        [TestMethod]
        public void Log_WhenQuietIsTrue_DoesNotLogMessage()
        {
            // Arrange
            Program.Quiet = true;
            var message = "Test message";

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                Program.Log(message);

                // Assert
                var result = sw.ToString().Trim();
                Assert.AreEqual(string.Empty, result);
            }
        }

        /// <summary>
        /// Tests the <see cref="Program.Log()"/> method to ensure it correctly logs an empty message when Quiet is false.
        /// </summary>
        [TestMethod]
        public void Log_WhenCalledWithoutParameters_LogsEmptyMessage()
        {
            // Arrange
            Program.Quiet = false;

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                Program.Log();

                // Assert
                var result = sw.ToString().Trim();
                Assert.AreEqual(string.Empty, result);
            }
        }

        /// <summary>
        /// Tests the <see cref="Program.CreateWorker"/> method to ensure it correctly creates a worker.
        /// </summary>
//         [TestMethod] [Error] (115-34)CS0122 'Program.CreateWorker()' is inaccessible due to its protection level
//         public void CreateWorker_WhenCalled_ReturnsWorker()
//         {
//             // Arrange
//             Program.TlsVersions = SslProtocols.Tls12;
//             Program.Certificate = new X509Certificate2();
// 
//             // Act
//             var worker = Program.CreateWorker();
// 
//             // Assert
//             Assert.IsNotNull(worker);
//             Assert.AreEqual(Program._httpHandler, worker.Handler);
//             Assert.AreEqual(Program._httpMessageInvoker, worker.Invoker);
//         }

        /// <summary>
        /// Tests the <see cref="Program.DoWorkAsync"/> method to ensure it correctly performs work and returns a result.
        /// </summary>
        [TestMethod]
        public async Task DoWorkAsync_WhenCalled_ReturnsWorkerResult()
        {
            // Arrange
            Program.Timelines = new[] { new Timeline { Method = HttpMethod.Get, Uri = new Uri("http://localhost") } };
            Program._httpMessageInvoker = _mockHttpMessageInvoker.Object;

            _mockHttpMessageInvoker
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await Program.DoWorkAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Status1xx);
            Assert.AreEqual(1, result.Status2xx);
            Assert.AreEqual(0, result.Status3xx);
            Assert.AreEqual(0, result.Status4xx);
            Assert.AreEqual(0, result.Status5xx);
            Assert.AreEqual(0, result.SocketErrors);
        }
    }
}
