using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="RetryHandler"/> class.
    /// </summary>
    public class RetryHandlerTests
    {
        private const int MaxRetries = 3;

        /// <summary>
        /// Tests that SendAsync returns a successful response on the first try without any retry.
        /// </summary>
//         [Fact] [Error] (35-63)CS0122 'RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_SuccessfulResponse_FirstTry_ReturnsResponse()
//         {
//             // Arrange
//             int callCount = 0;
//             var fakeHandler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
//             {
//                 callCount++;
//                 return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
//             });
//             var retryHandler = new RetryHandler(fakeHandler);
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
// 
//             // Act
//             HttpResponseMessage response = await retryHandler.SendAsync(request, CancellationToken.None);
// 
//             // Assert
//             Assert.True(response.IsSuccessStatusCode);
//             Assert.Equal(1, callCount);
//         }

        /// <summary>
        /// Tests that SendAsync retries until a successful response is received.
        /// It simulates failure responses for the first two attempts and a success on the third attempt.
        /// </summary>
//         [Fact] [Error] (68-63)CS0122 'RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_RetriesUntilSuccess_ReturnsSuccessfulResponse()
//         {
//             // Arrange
//             int callCount = 0;
//             var fakeHandler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
//             {
//                 callCount++;
//                 // First two calls return failure; third call returns success.
//                 if (callCount < 3)
//                 {
//                     return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
//                 }
//                 else
//                 {
//                     return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
//                 }
//             });
//             var retryHandler = new RetryHandler(fakeHandler);
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
// 
//             // Act
//             HttpResponseMessage response = await retryHandler.SendAsync(request, CancellationToken.None);
// 
//             // Assert
//             Assert.True(response.IsSuccessStatusCode);
//             Assert.Equal(3, callCount);
//         }

        /// <summary>
        /// Tests that SendAsync, after exhausting all retries, returns the last failure response.
        /// </summary>
//         [Fact] [Error] (92-63)CS0122 'RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_AllFailures_ReturnsLastFailure()
//         {
//             // Arrange
//             int callCount = 0;
//             var fakeHandler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
//             {
//                 callCount++;
//                 return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
//             });
//             var retryHandler = new RetryHandler(fakeHandler);
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
// 
//             // Act
//             HttpResponseMessage response = await retryHandler.SendAsync(request, CancellationToken.None);
// 
//             // Assert
//             Assert.False(response.IsSuccessStatusCode);
//             Assert.Equal(MaxRetries, callCount);
//         }

        /// <summary>
        /// Tests that SendAsync propagates exceptions thrown by the inner handler.
        /// </summary>
//         [Fact] [Error] (114-79)CS0122 'RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_BaseHandlerThrowsException_PropagatesException()
//         {
//             // Arrange
//             var fakeHandler = new FakeHttpMessageHandler((request, cancellationToken) =>
//             {
//                 throw new HttpRequestException("Simulated exception from inner handler.");
//             });
//             var retryHandler = new RetryHandler(fakeHandler);
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<HttpRequestException>(() => retryHandler.SendAsync(request, CancellationToken.None));
//         }

        /// <summary>
        /// Tests that SendAsync throws a TaskCanceledException when the provided CancellationToken is canceled.
        /// This simulates cancellation during the delay between retries.
        /// </summary>
//         [Fact] [Error] (145-80)CS0122 'RetryHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task SendAsync_RequestCancelled_ThrowsTaskCanceledException()
//         {
//             // Arrange
//             int callCount = 0;
//             var fakeHandler = new FakeHttpMessageHandler(async (request, cancellationToken) =>
//             {
//                 callCount++;
//                 return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestTimeout));
//             });
//             var retryHandler = new RetryHandler(fakeHandler);
//             var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
//             using var cts = new CancellationTokenSource();
//             
//             // Cancel the token after the first call to simulate cancellation during delay.
//             // We start a task that cancels the token after a short delay.
//             _ = Task.Run(async () =>
//             {
//                 // Wait a short moment to allow the first iteration to complete.
//                 await Task.Delay(100);
//                 cts.Cancel();
//             });
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<TaskCanceledException>(() => retryHandler.SendAsync(request, cts.Token));
//             // The callCount might be 1 or 2 depending on timing, so we assert at least one attempt was made.
//             Assert.True(callCount >= 1);
//         }

        /// <summary>
        /// A fake implementation of HttpMessageHandler to simulate controlled responses.
        /// </summary>
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
            {
                _handlerFunc = handlerFunc;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handlerFunc(request, cancellationToken);
            }
        }
    }
}
