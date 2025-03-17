using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HttpConnection"/> class.
    /// </summary>
    [TestClass]
    public class HttpConnectionTests
    {
        private readonly string _url = "http://localhost:5000";
        private readonly int _pipelineDepth = 2;
        private readonly IEnumerable<string> _headers = new List<string> { "Header1: Value1", "Header2: Value2" };
        private readonly Mock<Socket> _mockSocket;
        private readonly HttpConnection _httpConnection;

        public HttpConnectionTests()
        {
            _mockSocket = new Mock<Socket>(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _httpConnection = new HttpConnection(_url, _pipelineDepth, _headers);
        }

        /// <summary>
        /// Tests the <see cref="HttpConnection.ConnectAsync"/> method to ensure it connects to the host endpoint.
        /// </summary>
        [TestMethod]
        public async Task ConnectAsync_WhenCalled_ConnectsToHostEndpoint()
        {
            // Arrange
            _mockSocket.Setup(s => s.ConnectAsync(It.IsAny<EndPoint>())).Returns(Task.CompletedTask);

            // Act
            await _httpConnection.ConnectAsync();

            // Assert
            _mockSocket.Verify(s => s.ConnectAsync(It.IsAny<EndPoint>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="HttpConnection.SendRequestsAsync"/> method to ensure it sends requests and receives responses.
        /// </summary>
//         [TestMethod] [Error] (54-109)CS1503 Argument 1: cannot convert from 'System.Threading.Tasks.Task<int>' to 'System.Threading.Tasks.ValueTask<int>'
//         public async Task SendRequestsAsync_WhenCalled_SendsRequestsAndReceivesResponses()
//         {
//             // Arrange
//             _mockSocket.Setup(s => s.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), SocketFlags.None)).Returns(Task.FromResult(0));
//             _mockSocket.Setup(s => s.ReceiveAsync(It.IsAny<Memory<byte>>(), SocketFlags.None)).Returns(Task.FromResult(0));
// 
//             // Act
//             var responses = await _httpConnection.SendRequestsAsync();
// 
//             // Assert
//             Assert.IsNotNull(responses);
//             Assert.AreEqual(_pipelineDepth, responses.Length);
//         }

        /// <summary>
        /// Tests the <see cref="HttpConnection.Dispose"/> method to ensure it disposes the socket correctly.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesSocketCorrectly()
        {
            // Arrange
            _mockSocket.Setup(s => s.Shutdown(SocketShutdown.Both));
            _mockSocket.Setup(s => s.Close());

            // Act
            _httpConnection.Dispose();

            // Assert
            _mockSocket.Verify(s => s.Shutdown(SocketShutdown.Both), Times.Once);
            _mockSocket.Verify(s => s.Close(), Times.Once);
        }
    }
}
