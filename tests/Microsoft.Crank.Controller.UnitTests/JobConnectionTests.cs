using Microsoft.Crank.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobConnection"/> class.
    /// </summary>
//     public class JobConnectionTests : IDisposable [Error] (32-17)CS0117 'JobConnection' does not contain a definition for 'HttpClient'
//     {
//         private readonly Mock<HttpClientHandler> _mockHttpClientHandler;
//         private readonly Mock<HttpClient> _mockHttpClient;
//         private readonly JobConnection _jobConnection;
//         private readonly Job _job;
//         private readonly Uri _serverUri;
// 
//         public JobConnectionTests()
//         {
//             _mockHttpClientHandler = new Mock<HttpClientHandler>();
//             _mockHttpClient = new Mock<HttpClient>(_mockHttpClientHandler.Object);
//             _job = new Job();
//             _serverUri = new Uri("http://localhost");
//             _jobConnection = new JobConnection(_job, _serverUri)
//             {
//                 HttpClient = _mockHttpClient.Object
//             };
//         }
// 
//         public void Dispose()
//         {
//             _mockHttpClientHandler.VerifyAll();
//             _mockHttpClient.VerifyAll();
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.ConfigureRelay(string)"/> method to ensure it correctly configures the relay.
//         /// </summary>
//         [Fact]
//         public void ConfigureRelay_ValidToken_AddsHeader()
//         {
//             // Arrange
//             var token = "test-token";
// 
//             // Act
//             _jobConnection.ConfigureRelay(token);
// 
//             // Assert
//             _mockHttpClient.Verify(client => client.DefaultRequestHeaders.Add("ServiceBusAuthorization", token), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.CheckConnectionAsync"/> method to ensure it returns true when the connection is successful.
//         /// </summary>
//         [Fact]
//         public async Task CheckConnectionAsync_ConnectionSuccessful_ReturnsTrue()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
// 
//             // Act
//             var result = await _jobConnection.CheckConnectionAsync();
// 
//             // Assert
//             Assert.True(result);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.CheckConnectionAsync"/> method to ensure it returns false when the connection fails.
//         /// </summary>
//         [Fact]
//         public async Task CheckConnectionAsync_ConnectionFails_ReturnsFalse()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                 .ThrowsAsync(new HttpRequestException());
// 
//             // Act
//             var result = await _jobConnection.CheckConnectionAsync();
// 
//             // Assert
//             Assert.False(result);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.StartAsync(string)"/> method to ensure it starts the job correctly.
//         /// </summary>
//         [Fact]
//         public async Task StartAsync_ValidJobName_StartsJob()
//         {
//             // Arrange
//             var jobName = "test-job";
//             var jobUri = new Uri("http://localhost/jobs/1");
//             var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
//             {
//                 Content = new StringContent("{\"state\":\"Initializing\"}"),
//                 Headers = { Location = jobUri }
//             };
// 
//             _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
//                 .ReturnsAsync(responseMessage);
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
//                 {
//                     Content = new StringContent("{\"state\":\"Running\"}")
//                 });
// 
//             // Act
//             var result = await _jobConnection.StartAsync(jobName);
// 
//             // Assert
//             Assert.Equal(jobUri.ToString(), result);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.StopAsync(bool)"/> method to ensure it stops the job correctly.
//         /// </summary>
//         [Fact]
//         public async Task StopAsync_ValidCall_StopsJob()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
//                 {
//                     Content = new StringContent("{\"state\":\"Stopped\"}")
//                 });
// 
//             // Act
//             await _jobConnection.StopAsync();
// 
//             // Assert
//             _mockHttpClient.Verify(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.DeleteAsync"/> method to ensure it deletes the job correctly.
//         /// </summary>
//         [Fact]
//         public async Task DeleteAsync_ValidCall_DeletesJob()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.DeleteAsync(It.IsAny<string>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
// 
//             // Act
//             await _jobConnection.DeleteAsync();
// 
//             // Assert
//             _mockHttpClient.Verify(client => client.DeleteAsync(It.IsAny<string>()), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.TryUpdateJobAsync"/> method to ensure it updates the job correctly.
//         /// </summary>
//         [Fact]
//         public async Task TryUpdateJobAsync_ValidCall_UpdatesJob()
//         {
//             // Arrange
//             var jobContent = "{\"id\":\"1\"}";
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
//                 {
//                     Content = new StringContent(jobContent)
//                 });
// 
//             // Act
//             var result = await _jobConnection.TryUpdateJobAsync();
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal("1", _jobConnection.Job.Id);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.ClearMeasurements"/> method to ensure it clears the measurements correctly.
//         /// </summary>
//         [Fact]
//         public async Task ClearMeasurements_ValidCall_ClearsMeasurements()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
// 
//             // Act
//             await _jobConnection.ClearMeasurements();
// 
//             // Assert
//             _mockHttpClient.Verify(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.FlushMeasurements"/> method to ensure it flushes the measurements correctly.
//         /// </summary>
//         [Fact]
//         public async Task FlushMeasurements_ValidCall_FlushesMeasurements()
//         {
//             // Arrange
//             _mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
// 
//             // Act
//             await _jobConnection.FlushMeasurements();
// 
//             // Assert
//             _mockHttpClient.Verify(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Once);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.GetStateAsync"/> method to ensure it returns the correct state.
//         /// </summary>
//         [Fact]
//         public async Task GetStateAsync_ValidCall_ReturnsState()
//         {
//             // Arrange
//             var stateContent = "\"Running\"";
//             _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>()))
//                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
//                 {
//                     Content = new StringContent(stateContent)
//                 });
// 
//             // Act
//             var result = await _jobConnection.GetStateAsync();
// 
//             // Assert
//             Assert.Equal(JobState.Running, result);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.StartKeepAlive"/> method to ensure it starts the keep-alive process correctly.
//         /// </summary>
//         [Fact]
//         public void StartKeepAlive_ValidCall_StartsKeepAlive()
//         {
//             // Act
//             _jobConnection.StartKeepAlive();
// 
//             // Assert
//             Assert.True(_jobConnection.KeepAlive);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="JobConnection.StopKeepAlive"/> method to ensure it stops the keep-alive process correctly.
//         /// </summary>
//         [Fact]
//         public void StopKeepAlive_ValidCall_StopsKeepAlive()
//         {
//             // Arrange
//             _jobConnection.StartKeepAlive();
// 
//             // Act
//             _jobConnection.StopKeepAlive();
// 
//             // Assert
//             Assert.False(_jobConnection.KeepAlive);
//         }
//     }
}
