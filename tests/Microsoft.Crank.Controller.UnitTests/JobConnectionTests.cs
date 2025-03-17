using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobConnection"/> class.
    /// </summary>
    [TestClass]
    public class JobConnectionTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;
        private readonly Job _job;
        private readonly Uri _serverUri;
        private readonly JobConnection _jobConnection;

        public JobConnectionTests()
        {
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
            _mockHttpClient = new Mock<HttpClient>(_mockHttpClientHandler.Object);
            _job = new Job();
            _serverUri = new Uri("http://localhost");
            _jobConnection = new JobConnection(_job, _serverUri);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.ConfigureRelay(string)"/> method to ensure it correctly adds the ServiceBusAuthorization header.
        /// </summary>
        [TestMethod]
        public void ConfigureRelay_ValidToken_AddsHeader()
        {
            // Arrange
            var token = "test-token";

            // Act
            _jobConnection.ConfigureRelay(token);

            // Assert
            Assert.IsTrue(_mockHttpClient.Object.DefaultRequestHeaders.Contains("ServiceBusAuthorization"));
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.CheckConnectionAsync"/> method to ensure it returns true for a successful connection.
        /// </summary>
        [TestMethod]
        public async Task CheckConnectionAsync_SuccessfulConnection_ReturnsTrue()
        {
            // Arrange
            _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await _jobConnection.CheckConnectionAsync();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.CheckConnectionAsync"/> method to ensure it returns false for a failed connection.
        /// </summary>
        [TestMethod]
        public async Task CheckConnectionAsync_FailedConnection_ReturnsFalse()
        {
            // Arrange
            _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await _jobConnection.CheckConnectionAsync();

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.StartAsync(string)"/> method to ensure it starts a job successfully.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_ValidJobName_StartsJobSuccessfully()
        {
            // Arrange
            var jobName = "test-job";
            var jobContent = JsonConvert.SerializeObject(_job);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"state\":\"Initializing\"}", Encoding.UTF8, "application/json")
            };

            _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(responseMessage);

            _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"state\":\"Running\"}", Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _jobConnection.StartAsync(jobName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_jobConnection.ServerJobUri, result);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.StopAsync(bool)"/> method to ensure it stops a job successfully.
        /// </summary>
        [TestMethod]
        public async Task StopAsync_ValidCall_StopsJobSuccessfully()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(responseMessage);

            // Act
            await _jobConnection.StopAsync();

            // Assert
            _mockHttpClient.Verify(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.DeleteAsync"/> method to ensure it deletes a job successfully.
        /// </summary>
        [TestMethod]
        public async Task DeleteAsync_ValidCall_DeletesJobSuccessfully()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _mockHttpClient.Setup(client => client.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            // Act
            await _jobConnection.DeleteAsync();

            // Assert
            _mockHttpClient.Verify(client => client.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.TryUpdateJobAsync"/> method to ensure it updates the job successfully.
        /// </summary>
        [TestMethod]
        public async Task TryUpdateJobAsync_ValidCall_UpdatesJobSuccessfully()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(_job), Encoding.UTF8, "application/json")
            };
            _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _jobConnection.TryUpdateJobAsync();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="JobConnection.GetStateAsync"/> method to ensure it returns the correct job state.
        /// </summary>
        [TestMethod]
        public async Task GetStateAsync_ValidCall_ReturnsJobState()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"Running\"", Encoding.UTF8, "application/json")
            };
            _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _jobConnection.GetStateAsync();

            // Assert
            Assert.AreEqual(JobState.Running, result);
        }
    }
}
