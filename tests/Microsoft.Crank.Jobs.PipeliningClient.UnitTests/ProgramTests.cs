using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpConnection> _mockHttpConnection;

        public ProgramTests()
        {
            _mockHttpConnection = new Mock<HttpConnection>("http://localhost", 1, new List<string>());
        }

        /// <summary>
        /// Tests the <see cref="Program.RunAsync"/> method to ensure it runs without exceptions.
        /// </summary>
        [TestMethod]
        public async Task RunAsync_WhenCalled_CompletesSuccessfully()
        {
            // Arrange
            Program.ServerUrl = "http://localhost";
            Program.ExecutionTimeSeconds = 1;
            Program.WarmupTimeSeconds = 0;
            Program.Connections = 1;
            Program.Headers = new List<string>();

            // Act
            await Program.RunAsync();

            // Assert
            Assert.IsTrue(true); // If no exception is thrown, the test passes
        }

        /// <summary>
        /// Tests the <see cref="Program.DoWorkAsync"/> method to ensure it returns a valid <see cref="WorkerResult"/>.
        /// </summary>
        [TestMethod]
        public async Task DoWorkAsync_WhenCalled_ReturnsValidWorkerResult()
        {
            // Arrange
            Program.ServerUrl = "http://localhost";
            Program.PipelineDepth = 1;
            Program.Headers = new List<string>();

            // Act
            var result = await Program.DoWorkAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(WorkerResult));
        }

        /// <summary>
        /// Tests the <see cref="Program.DoWorkAsync"/> method to ensure it handles socket errors correctly.
        /// </summary>
        [TestMethod]
        public async Task DoWorkAsync_WhenSocketErrorOccurs_IncrementsSocketErrors()
        {
            // Arrange
            Program.ServerUrl = "http://localhost";
            Program.PipelineDepth = 1;
            Program.Headers = new List<string>();

            _mockHttpConnection.Setup(x => x.SendRequestsAsync()).ThrowsAsync(new Exception());

            // Act
            var result = await Program.DoWorkAsync();

            // Assert
            Assert.IsTrue(result.SocketErrors > 0);
        }
    }
}
