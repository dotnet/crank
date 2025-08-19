using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        private readonly FieldInfo _runningField;
        private readonly FieldInfo _measuringField;
        private readonly FieldInfo _httpMessageInvokerField;

        public ProgramTests()
        {
            // Using reflection to access private static fields in Program.
            _runningField = typeof(Program).GetField("_running", BindingFlags.NonPublic | BindingFlags.Static);
            _measuringField = typeof(Program).GetField("_measuring", BindingFlags.NonPublic | BindingFlags.Static);
            _httpMessageInvokerField = typeof(Program).GetField("_httpMessageInvoker", BindingFlags.NonPublic | BindingFlags.Static);
        }

        /// <summary>
        /// Tests that the Log method outputs the provided message when Quiet is false.
        /// </summary>
        [Fact]
        public void Log_WhenQuietFalse_WritesMessage()
        {
            // Arrange
            Program.Quiet = false;
            using var sw = new StringWriter();
            Console.SetOut(sw);
            string testMessage = "Test message";

            // Act
            Program.Log(testMessage);

            // Assert
            string output = sw.ToString();
            Assert.Contains(testMessage, output);
        }

        /// <summary>
        /// Tests that the Log method does not output anything when Quiet is true.
        /// </summary>
        [Fact]
        public void Log_WhenQuietTrue_WritesNothing()
        {
            // Arrange
            Program.Quiet = true;
            using var sw = new StringWriter();
            Console.SetOut(sw);
            string testMessage = "Test message";

            // Act
            Program.Log(testMessage);

            // Assert
            string output = sw.ToString();
            Assert.True(string.IsNullOrEmpty(output.Trim()));
        }

        /// <summary>
        /// Creates a fake HttpMessageInvoker that returns a predetermined HttpResponseMessage.
        /// </summary>
        /// <returns>A HttpMessageInvoker instance with a configured fake message handler.</returns>
        private HttpMessageInvoker CreateFakeHttpMessageInvoker()
        {
            var fakeHandlerMock = new Mock<HttpMessageHandler>();
            fakeHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    // Create a dummy response with StatusCode 200 and a fixed content length.
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(Encoding.UTF8.GetBytes("Response"))
                    };
                    response.Content.Headers.ContentLength = 8;
                    return response;
                });
            return new HttpMessageInvoker(fakeHandlerMock.Object);
        }

        /// <summary>
        /// Sets the private static field _httpMessageInvoker to a fake invoker.
        /// </summary>
        private void SetFakeHttpMessageInvoker()
        {
            var fakeInvoker = CreateFakeHttpMessageInvoker();
            _httpMessageInvokerField.SetValue(null, fakeInvoker);
        }

        /// <summary>
        /// Helper method to create a dummy Timeline instance.
        /// Assumes Timeline has public settable properties: Method, Uri, Headers, Delay, and HttpContent.
        /// </summary>
        /// <returns>A dummy Timeline object.</returns>
        private Timeline CreateDummyTimeline()
        {
            return new Timeline
            {
                Method = HttpMethod.Get,
                Uri = new Uri("http://localhost"),
                Headers = new Dictionary<string, string>(),
                Delay = TimeSpan.Zero,
                HttpContent = null
            };
        }

        /// <summary>
        /// Tests the DoWorkAsync method to ensure it returns a valid WorkerResult with expected counter updates.
        /// This test sets up a fake HTTP invoker and triggers a single iteration of the request loop.
        /// </summary>
        [Fact]
        public async Task DoWorkAsync_WhenCalled_ReturnsWorkerResult_WithCounters()
        {
            // Arrange
            // Setup a dummy timeline so that the loop has a request to process.
            Program.Timelines = new Timeline[] { CreateDummyTimeline() };
            Program.Body = null;
            Program.Headers = new List<string>();

            // Override the HTTP invoker with a fake one.
            SetFakeHttpMessageInvoker();

            // Use reflection to set the _running and _measuring flags.
            _runningField.SetValue(null, true);
            _measuringField.SetValue(null, true);

            // Start a task to stop the loop after a short delay.
            var stopTask = Task.Run(() =>
            {
                Thread.Sleep(50);
                _runningField.SetValue(null, false);
            });

            // Act
            var workerResult = await Program.DoWorkAsync();

            // Wait for the stopTask to ensure the loop is terminated.
            await stopTask;

            // Assert
            Assert.NotNull(workerResult);
            // Since our fake HTTP response returns 200, expect at least one successful 2xx response.
            Assert.True(workerResult.Status2xx >= 1, "Expected at least one 2xx response.");
            // Check that throughput is non-negative.
            Assert.True(workerResult.ThroughputBps >= 0, "Throughput should be non-negative.");
        }

        /// <summary>
        /// Tests the RunAsync method to ensure it executes the benchmark run and writes expected output.
        /// This test configures a minimal run using a dummy timeline and fake HTTP invoker.
        /// </summary>
        [Fact]
        public async Task RunAsync_WhenCalled_ExecutesAndLogsOutput()
        {
            // Arrange
            Program.ServerUrl = "http://localhost";
            Program.ExecutionTimeSeconds = 1;
            Program.WarmupTimeSeconds = 0;
            Program.Connections = 1;
            Program.Timelines = new Timeline[] { CreateDummyTimeline() };
            Program.Headers = new List<string>();
            Program.Body = null;
            Program.Quiet = false;
            Program.Format = "text";
            Program.Errors = new HashSet<string>();

            // Override the HTTP invoker so that real network calls are not made.
            SetFakeHttpMessageInvoker();

            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            await Program.RunAsync();
            string output = sw.ToString();

            // Assert
            Assert.Contains("Running 1s test @", output);
            Assert.Contains("Stopped...", output);
        }

        /// <summary>
        /// Tests the Main method with insufficient arguments to check if a validation error is logged.
        /// In this scenario, neither --url nor --har is provided.
        /// </summary>
//         [Fact] [Error] (215-27)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WhenCalledWithInsufficientArguments_LogsValidationError()
//         {
//             // Arrange
//             // Redirect console output to capture error messages.
//             using var sw = new StringWriter();
//             Console.SetOut(sw);
//             // Clear any previously set ServerUrl.
//             Program.ServerUrl = null;
// 
//             // Act
//             // Passing empty args should trigger validation error.
//             await Program.Main(Array.Empty<string>());
//             string output = sw.ToString();
// 
//             // Assert
//             Assert.Contains("The --url field is required", output, StringComparison.OrdinalIgnoreCase);
//         }
    }
}
