using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace H2LoadClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;

        public ProgramTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it correctly initializes and executes the application.
        /// </summary>
//         [TestMethod] [Error] (36-27)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_ValidArguments_ExecutesSuccessfully()
//         {
//             // Arrange
//             string[] args = new string[] { "--url", "http://example.com", "--connections", "10", "--threads", "2", "--streams", "5", "--requests", "100", "--timeout", "10", "--warmup", "5", "--duration", "60", "--protocol", "h2", "--body", "SGVsbG8gd29ybGQ=", "--header", "Content-Type: application/json" };
// 
//             // Act
//             await Program.Main(args);
// 
//             // Assert
//             Assert.AreEqual("http://example.com", Program.ServerUrl);
//             Assert.AreEqual(10, Program.Connections);
//             Assert.AreEqual(2, Program.Threads);
//             Assert.AreEqual(5, Program.Streams);
//             Assert.AreEqual(100, Program.Requests);
//             Assert.AreEqual(10, Program.Timeout);
//             Assert.AreEqual(5, Program.Warmup);
//             Assert.AreEqual(60, Program.Duration);
//             Assert.AreEqual("h2", Program.Protocol);
//             Assert.IsTrue(Program.Headers.ContainsKey("Content-Type"));
//             Assert.AreEqual("application/json", Program.Headers["Content-Type"]);
//         }

        /// <summary>
        /// Tests the <see cref="Program.ReadRequests(Match)"/> method to ensure it correctly parses the number of requests.
        /// </summary>
        [TestMethod]
        public void ReadRequests_ValidMatch_ReturnsCorrectRequestCount()
        {
            // Arrange
            var match = Regex.Match("requests: 100 total", @"requests: ([\d\.]+) total");

            // Act
            int result = Program.ReadRequests(match);

            // Assert
            Assert.AreEqual(100, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadRequests(Match)"/> method to ensure it returns -1 for an invalid match.
        /// </summary>
        [TestMethod]
        public void ReadRequests_InvalidMatch_ReturnsMinusOne()
        {
            // Arrange
            var match = Regex.Match("invalid data", @"requests: ([\d\.]+) total");

            // Act
            int result = Program.ReadRequests(match);

            // Assert
            Assert.AreEqual(-1, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadBadResponses(Match)"/> method to ensure it correctly parses the number of bad responses.
        /// </summary>
        [TestMethod]
        public void ReadBadResponses_ValidMatch_ReturnsCorrectBadResponseCount()
        {
            // Arrange
            var match = Regex.Match("status codes: 90 2xx, 5 3xx, 3 4xx, 2 5xx", @"status codes: ([\d\.]+) 2xx, ([\d\.]+) 3xx, ([\d\.]+) 4xx, ([\d\.]+) 5xx");

            // Act
            int result = Program.ReadBadResponses(match);

            // Assert
            Assert.AreEqual(5, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadBadResponses(Match)"/> method to ensure it returns 0 for an invalid match.
        /// </summary>
        [TestMethod]
        public void ReadBadResponses_InvalidMatch_ReturnsZero()
        {
            // Arrange
            var match = Regex.Match("invalid data", @"status codes: ([\d\.]+) 2xx, ([\d\.]+) 3xx, ([\d\.]+) 4xx, ([\d\.]+) 5xx");

            // Act
            int result = Program.ReadBadResponses(match);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.CountSocketErrors(Match)"/> method to ensure it correctly parses the number of socket errors.
        /// </summary>
        [TestMethod]
        public void CountSocketErrors_ValidMatch_ReturnsCorrectSocketErrorCount()
        {
            // Arrange
            var match = Regex.Match("10 failed, 5 errored, 2 timeout", @"([\d\.]+) failed, ([\d\.]+) errored, ([\d\.]+) timeout");

            // Act
            int result = Program.CountSocketErrors(match);

            // Assert
            Assert.AreEqual(17, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.CountSocketErrors(Match)"/> method to ensure it returns 0 for an invalid match.
        /// </summary>
        [TestMethod]
        public void CountSocketErrors_InvalidMatch_ReturnsZero()
        {
            // Arrange
            var match = Regex.Match("invalid data", @"([\d\.]+) failed, ([\d\.]+) errored, ([\d\.]+) timeout");

            // Act
            int result = Program.CountSocketErrors(match);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadLatency(Match)"/> method to ensure it correctly parses the latency.
        /// </summary>
        [TestMethod]
        public void ReadLatency_ValidMatch_ReturnsCorrectLatency()
        {
            // Arrange
            var match = Regex.Match("time for request: 1.23s", @"time for request: \s+[\d\.]+\w+\s+([\d\.]+)(\w+)");

            // Act
            double result = Program.ReadLatency(match);

            // Assert
            Assert.AreEqual(1230, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ReadLatency(Match)"/> method to ensure it returns -1 for an invalid match.
        /// </summary>
        [TestMethod]
        public void ReadLatency_InvalidMatch_ReturnsMinusOne()
        {
            // Arrange
            var match = Regex.Match("invalid data", @"time for request: \s+[\d\.]+\w+\s+([\d\.]+)(\w+)");

            // Act
            double result = Program.ReadLatency(match);

            // Assert
            Assert.AreEqual(-1, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.StartProcess()"/> method to ensure it starts the process correctly.
        /// </summary>
        [TestMethod]
        public void StartProcess_ValidConfiguration_StartsProcess()
        {
            // Arrange
            Program.ServerUrl = "http://example.com";
            Program.Connections = 10;
            Program.Threads = 2;
            Program.Streams = 5;
            Program.Requests = 100;
            Program.Timeout = 10;
            Program.Warmup = 5;
            Program.Duration = 60;
            Program.Protocol = "h2";
            Program.Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };

            // Act
            var process = Program.StartProcess();

            // Assert
            Assert.IsNotNull(process);
            Assert.AreEqual("stdbuf", process.StartInfo.FileName);
            Assert.IsTrue(process.StartInfo.Arguments.Contains("http://example.com"));
        }
    }
}
