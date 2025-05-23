using Microsoft.Crank.Jobs.PipeliningClient;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HttpConnection"/> class.
    /// </summary>
    public class HttpConnectionTests
    {
        private const string ValidResponse = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n";
        private const string InvalidStatusLineResponse = "BAD/1.1 200 OK\r\nContent-Length: 0\r\n\r\n";
        private const string InvalidHeaderResponse = "HTTP/1.1 200 OK\r\nInvalidHeader\r\n\r\n";

        /// <summary>
        /// Tests that ConnectAsync successfully establishes a connection to a valid endpoint.
        /// The test sets up a TCP listener that accepts the connection and then immediately closes.
        /// Expected outcome: ConnectAsync completes without exceptions.
        /// </summary>
        [Fact]
        public async Task ConnectAsync_ValidEndpoint_ConnectsSuccessfully()
        {
            // Arrange
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{port}/";
            var headers = new List<string>();
            int pipelineDepth = 1;
            using var connection = new HttpConnection(url, pipelineDepth, headers);

            // Accept the connection on the server side.
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

            // Act
            Task connectTask = connection.ConnectAsync();
            TcpClient serverClient = await acceptTask.ConfigureAwait(false);
            // Immediately close the server client to complete the FillPipeAsync loop.
            serverClient.Close();
            await connectTask.ConfigureAwait(false);

            // Assert
            // If no exception is thrown, the connection is considered successful.
            listener.Stop();
        }

        /// <summary>
        /// Tests that SendRequestsAsync processes multiple valid HTTP responses correctly.
        /// The test sets up a TCP listener that returns multiple HTTP responses with Content-Length: 0.
        /// Expected outcome: All responses are marked as Completed with a 200 status code.
        /// </summary>
        [Fact]
        public async Task SendRequestsAsync_HappyPath_MultipleResponsesCompleted()
        {
            // Arrange
            int pipelineDepth = 2;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{port}/";
            var headers = new List<string> { "Custom-Header: test" };
            using var connection = new HttpConnection(url, pipelineDepth, headers);

            // Start accepting connection on the server side.
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            Task connectTask = connection.ConnectAsync();
            TcpClient serverClient = await acceptTask.ConfigureAwait(false);
            await connectTask.ConfigureAwait(false);

            // In parallel, read the request from the client and then send concatenated responses.
            _ = Task.Run(async () =>
            {
                try
                {
                    using (serverClient)
                    using (NetworkStream networkStream = serverClient.GetStream())
                    {
                        // Read incoming request bytes (ignore the content).
                        byte[] buffer = new byte[1024];
                        int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        // Send multiple valid responses back.
                        // Concatenate responses for each pipelined request.
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < pipelineDepth; i++)
                        {
                            sb.Append(ValidResponse);
                        }
                        byte[] responseBytes = Encoding.UTF8.GetBytes(sb.ToString());
                        await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Server encountered an exception: " + ex);
                }
            });

            // Act
            var responses = await connection.SendRequestsAsync().ConfigureAwait(false);

            // Assert
            Assert.Equal(pipelineDepth, responses.Length);
            foreach (var response in responses)
            {
                // Assuming that a valid response will set status code to 200 and state to Completed.
                Assert.Equal(200, response.StatusCode);
                Assert.Equal(HttpResponseState.Completed, response.State);
            }

            listener.Stop();
        }

        /// <summary>
        /// Tests that SendRequestsAsync returns an error state when the HTTP status line is invalid.
        /// The server sends a response with an invalid status line.
        /// Expected outcome: The response state is marked as Error.
        /// </summary>
        [Fact]
        public async Task SendRequestsAsync_InvalidStatusLine_ResponseError()
        {
            // Arrange
            int pipelineDepth = 1;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{port}/";
            var headers = new List<string>();
            using var connection = new HttpConnection(url, pipelineDepth, headers);

            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            Task connectTask = connection.ConnectAsync();
            TcpClient serverClient = await acceptTask.ConfigureAwait(false);
            await connectTask.ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                try
                {
                    using (serverClient)
                    using (NetworkStream networkStream = serverClient.GetStream())
                    {
                        // Read the request from the client.
                        byte[] buffer = new byte[1024];
                        await networkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        // Send an invalid status line response.
                        byte[] responseBytes = Encoding.UTF8.GetBytes(InvalidStatusLineResponse);
                        await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Server encountered an exception: " + ex);
                }
            });

            // Act
            var responses = await connection.SendRequestsAsync().ConfigureAwait(false);

            // Assert
            Assert.Single(responses);
            Assert.Equal(HttpResponseState.Error, responses[0].State);

            listener.Stop();
        }

        /// <summary>
        /// Tests that SendRequestsAsync returns an error state when the HTTP header format is invalid.
        /// The server sends a response with a header missing the colon separator.
        /// Expected outcome: The response state is marked as Error.
        /// </summary>
        [Fact]
        public async Task SendRequestsAsync_InvalidHeaderFormat_ResponseError()
        {
            // Arrange
            int pipelineDepth = 1;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{port}/";
            var headers = new List<string>();
            using var connection = new HttpConnection(url, pipelineDepth, headers);

            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            Task connectTask = connection.ConnectAsync();
            TcpClient serverClient = await acceptTask.ConfigureAwait(false);
            await connectTask.ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                try
                {
                    using (serverClient)
                    using (NetworkStream networkStream = serverClient.GetStream())
                    {
                        // Read the request.
                        byte[] buffer = new byte[1024];
                        await networkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        // Send a response with an invalid header.
                        byte[] responseBytes = Encoding.UTF8.GetBytes(InvalidHeaderResponse);
                        await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Server encountered an exception: " + ex);
                }
            });

            // Act
            var responses = await connection.SendRequestsAsync().ConfigureAwait(false);

            // Assert
            Assert.Single(responses);
            Assert.Equal(HttpResponseState.Error, responses[0].State);

            listener.Stop();
        }

        /// <summary>
        /// Tests that after Dispose is called, subsequent send operations throw an exception.
        /// Expected outcome: Calling SendRequestsAsync on a disposed connection results in a SocketException or ObjectDisposedException.
        /// </summary>
        [Fact]
        public async Task Dispose_ConnectionClosed_SendingThrowsException()
        {
            // Arrange
            int pipelineDepth = 1;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{port}/";
            var headers = new List<string>();
            var connection = new HttpConnection(url, pipelineDepth, headers);

            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            Task connectTask = connection.ConnectAsync();
            TcpClient serverClient = await acceptTask.ConfigureAwait(false);
            await connectTask.ConfigureAwait(false);

            // Dispose the connection.
            connection.Dispose();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                // This should throw because the underlying socket is closed.
                await connection.SendRequestsAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            // Cleanup
            serverClient.Close();
            listener.Stop();
        }
    }
}
