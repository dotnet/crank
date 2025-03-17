using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="DevopsMessage"/> class.
    /// </summary>
    [TestClass]
    public class DevopsMessageTests
    {
        private readonly Mock<ServiceBusReceivedMessage> _mockMessage;
        private readonly DevopsMessage _devopsMessage;

        public DevopsMessageTests()
        {
            _mockMessage = new Mock<ServiceBusReceivedMessage>();
            _mockMessage.Setup(m => m.ApplicationProperties["PlanUrl"]).Returns("http://example.com");
            _mockMessage.Setup(m => m.ApplicationProperties["ProjectId"]).Returns("projectId");
            _mockMessage.Setup(m => m.ApplicationProperties["HubName"]).Returns("hubName");
            _mockMessage.Setup(m => m.ApplicationProperties["PlanId"]).Returns("planId");
            _mockMessage.Setup(m => m.ApplicationProperties["JobId"]).Returns("jobId");
            _mockMessage.Setup(m => m.ApplicationProperties["TimelineId"]).Returns("timelineId");
            _mockMessage.Setup(m => m.ApplicationProperties["TaskInstanceName"]).Returns("taskInstanceName");
            _mockMessage.Setup(m => m.ApplicationProperties["TaskInstanceId"]).Returns("taskInstanceId");
            _mockMessage.Setup(m => m.ApplicationProperties["AuthToken"]).Returns("authToken");

            _devopsMessage = new DevopsMessage(_mockMessage.Object);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.SendTaskStartedEventAsync"/> method to ensure it correctly sends a task started event.
        /// </summary>
        [TestMethod]
        public async Task SendTaskStartedEventAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _devopsMessage.SendTaskStartedEventAsync();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.SendTaskCompletedEventAsync"/> method to ensure it correctly sends a task completed event.
        /// </summary>
        [TestMethod]
        public async Task SendTaskCompletedEventAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _devopsMessage.SendTaskCompletedEventAsync(DevopsMessage.ResultTypes.Succeeded);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.SendTaskLogFeedsAsync"/> method to ensure it correctly sends task log feeds.
        /// </summary>
        [TestMethod]
        public async Task SendTaskLogFeedsAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _devopsMessage.SendTaskLogFeedsAsync("log message");

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.CreateTaskLogAsync"/> method to ensure it correctly creates a task log.
        /// </summary>
        [TestMethod]
        public async Task CreateTaskLogAsync_ValidRequest_ReturnsLogId()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("logId")
                });

            // Act
            var result = await _devopsMessage.CreateTaskLogAsync();

            // Assert
            Assert.AreEqual("logId", result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.AppendToTaskLogAsync"/> method to ensure it correctly appends to a task log.
        /// </summary>
        [TestMethod]
        public async Task AppendToTaskLogAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _devopsMessage.AppendToTaskLogAsync("logId", "log message");

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.UpdateTaskTimelineRecordAsync"/> method to ensure it correctly updates a task timeline record.
        /// </summary>
        [TestMethod]
        public async Task UpdateTaskTimelineRecordAsync_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            httpClientMock.Setup(client => client.PatchAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

            // Act
            var result = await _devopsMessage.UpdateTaskTimelineRecordAsync("taskLogObject");

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="DevopsMessage.GetRecordsAsync"/> method to ensure it correctly retrieves records.
        /// </summary>
        [TestMethod]
        public async Task GetRecordsAsync_ValidRequest_ReturnsRecords()
        {
            // Arrange
            var httpClientMock = new Mock<HttpClient>();
            var records = new Records();
            var jsonRecords = JsonSerializer.Serialize(records);
            httpClientMock.Setup(client => client.GetAsync(It.IsAny<Uri>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonRecords)
                });

            // Act
            var result = await _devopsMessage.GetRecordsAsync();

            // Assert
            Assert.IsNotNull(result);
        }
    }
}

