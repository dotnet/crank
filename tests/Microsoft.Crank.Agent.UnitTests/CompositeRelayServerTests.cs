using Microsoft.Crank.Agent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CompositeServer"/> class.
    /// </summary>
    [TestClass]
    public class CompositeServerTests
    {
        private readonly Mock<IServer> _mockServer1;
        private readonly Mock<IServer> _mockServer2;
        private readonly CompositeServer _compositeServer;

        public CompositeServerTests()
        {
            _mockServer1 = new Mock<IServer>();
            _mockServer2 = new Mock<IServer>();
            _compositeServer = new CompositeServer(new List<IServer> { _mockServer1.Object, _mockServer2.Object });
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.CompositeServer(IEnumerable{IServer})"/> constructor to ensure it throws an <see cref="ArgumentNullException"/> when the servers parameter is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenServersIsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new CompositeServer(null));
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.CompositeServer(IEnumerable{IServer})"/> constructor to ensure it throws an <see cref="ArgumentException"/> when the servers parameter contains less than 2 servers.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenServersCountIsLessThanTwo_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new CompositeServer(new List<IServer> { _mockServer1.Object }));
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.Features"/> property to ensure it returns the features of the first server.
        /// </summary>
        [TestMethod]
        public void Features_WhenCalled_ReturnsFirstServerFeatures()
        {
            // Arrange
            var mockFeatureCollection = new Mock<IFeatureCollection>();
            _mockServer1.Setup(s => s.Features).Returns(mockFeatureCollection.Object);

            // Act
            var features = _compositeServer.Features;

            // Assert
            Assert.AreEqual(mockFeatureCollection.Object, features);
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.Dispose"/> method to ensure it disposes all servers.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesAllServers()
        {
            // Act
            _compositeServer.Dispose();

            // Assert
            _mockServer1.Verify(s => s.Dispose(), Times.Once);
            _mockServer2.Verify(s => s.Dispose(), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.StartAsync{TContext}(IHttpApplication{TContext}, CancellationToken)"/> method to ensure it starts all servers.
        /// </summary>
        [TestMethod]
        public async Task StartAsync_WhenCalled_StartsAllServers()
        {
            // Arrange
            var mockApplication = new Mock<IHttpApplication<object>>();
            var cancellationToken = new CancellationToken();

            // Act
            await _compositeServer.StartAsync(mockApplication.Object, cancellationToken);

            // Assert
            _mockServer1.Verify(s => s.StartAsync(mockApplication.Object, cancellationToken), Times.Once);
            _mockServer2.Verify(s => s.StartAsync(mockApplication.Object, cancellationToken), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CompositeServer.StopAsync(CancellationToken)"/> method to ensure it stops all servers.
        /// </summary>
        [TestMethod]
        public async Task StopAsync_WhenCalled_StopsAllServers()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            // Act
            await _compositeServer.StopAsync(cancellationToken);

            // Assert
            _mockServer1.Verify(s => s.StopAsync(cancellationToken), Times.Once);
            _mockServer2.Verify(s => s.StopAsync(cancellationToken), Times.Once);
        }
    }
}

