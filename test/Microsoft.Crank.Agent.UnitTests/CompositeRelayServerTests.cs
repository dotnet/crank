// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Crank.Agent;
using Moq;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CompositeServer"/> class.
    /// </summary>
    public class CompositeServerTests
    {
        /// <summary>
        /// A dummy implementation of <see cref="IHttpApplication{TContext}"/>
        /// for testing the StartAsync method of <see cref="CompositeServer"/>.
        /// </summary>
//         private class DummyHttpApplication : IHttpApplication<string> [Error] (27-46)CS0535 'CompositeServerTests.DummyHttpApplication' does not implement interface member 'IHttpApplication<string>.CreateContext(IFeatureCollection)'
//         {
//             /// <summary>
//             /// Creates a dummy context.
//             /// </summary>
//             public string CreateContext(Microsoft.AspNetCore.Http.HttpContext context) => "dummy";
// 
//             /// <summary>
//             /// Processes the request asynchronously (dummy implementation).
//             /// </summary>
//             public Task ProcessRequestAsync(string context) => Task.CompletedTask;
// 
//             /// <summary>
//             /// Disposes of the context (dummy implementation).
//             /// </summary>
//             public void DisposeContext(string context, Exception exception) { }
//         }

        /// <summary>
        /// Tests that the constructor throws an ArgumentNullException when the servers parameter is null.
        /// </summary>
        [Fact]
        public void Constructor_NullServers_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<IServer> servers = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new CompositeServer(servers));
            Assert.Equal("servers", exception.ParamName);
        }

        /// <summary>
        /// Tests that the constructor throws an ArgumentException when the servers collection contains fewer than 2 servers.
        /// </summary>
        [Fact]
        public void Constructor_LessThanTwoServers_ThrowsArgumentException()
        {
            // Arrange
            var serverMock = new Mock<IServer>();
            IEnumerable<IServer> servers = new List<IServer> { serverMock.Object };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new CompositeServer(servers));
            Assert.Equal("servers", exception.ParamName);
            Assert.Contains("Expected at least 2 servers.", exception.Message);
        }

        /// <summary>
        /// Tests that the Features property returns the Features collection from the first server.
        /// </summary>
        [Fact]
        public void Features_WhenCalled_ReturnsFirstServerFeatures()
        {
            // Arrange
            var expectedFeatures = new FeatureCollection();
            var serverMock1 = new Mock<IServer>();
            serverMock1.Setup(s => s.Features).Returns(expectedFeatures);
            var serverMock2 = new Mock<IServer>();
            serverMock2.Setup(s => s.Features).Returns(new FeatureCollection());
            IEnumerable<IServer> servers = new List<IServer> { serverMock1.Object, serverMock2.Object };
            var compositeServer = new CompositeServer(servers);

            // Act
            var actualFeatures = compositeServer.Features;

            // Assert
            Assert.Same(expectedFeatures, actualFeatures);
        }

        /// <summary>
        /// Tests that the Dispose method calls Dispose on all inner servers.
        /// </summary>
        [Fact]
        public void Dispose_WhenCalled_DisposesAllInnerServers()
        {
            // Arrange
            var serverMock1 = new Mock<IServer>();
            var serverMock2 = new Mock<IServer>();
            IEnumerable<IServer> servers = new List<IServer> { serverMock1.Object, serverMock2.Object };
            var compositeServer = new CompositeServer(servers);

            // Act
            compositeServer.Dispose();

            // Assert
            serverMock1.Verify(s => s.Dispose(), Times.Once);
            serverMock2.Verify(s => s.Dispose(), Times.Once);
        }

        /// <summary>
        /// Tests that the StartAsync method invokes StartAsync on all inner servers.
        /// </summary>
//         [Fact] [Error] (144-54)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.Agent.UnitTests.CompositeServerTests.DummyHttpApplication' to 'Microsoft.AspNetCore.Hosting.Server.IHttpApplication<object>'
//         public async Task StartAsync_WhenCalled_StartsAllInnerServers()
//         {
//             // Arrange
//             var cancellationToken = CancellationToken.None;
//             var dummyApplication = new DummyHttpApplication();
//             var serverMock1 = new Mock<IServer>();
//             var serverMock2 = new Mock<IServer>();
// 
//             // Setup StartAsync for both servers to return a completed task.
//             serverMock1
//                 .Setup(s => s.StartAsync<object>(It.IsAny<IHttpApplication<object>>(), cancellationToken))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();
//             serverMock2
//                 .Setup(s => s.StartAsync<object>(It.IsAny<IHttpApplication<object>>(), cancellationToken))
//                 .Returns(Task.CompletedTask)
//                 .Verifiable();
// 
//             // Use object as TContext for testing
//             IEnumerable<IServer> servers = new List<IServer> { serverMock1.Object, serverMock2.Object };
//             var compositeServer = new CompositeServer(servers);
// 
//             // Act
//             await compositeServer.StartAsync<object>(dummyApplication, cancellationToken);
// 
//             // Assert
//             serverMock1.Verify(s => s.StartAsync<object>(It.IsAny<IHttpApplication<object>>(), cancellationToken), Times.Once);
//             serverMock2.Verify(s => s.StartAsync<object>(It.IsAny<IHttpApplication<object>>(), cancellationToken), Times.Once);
//         }

        /// <summary>
        /// Tests that the StopAsync method invokes StopAsync on all inner servers.
        /// </summary>
        [Fact]
        public async Task StopAsync_WhenCalled_StopsAllInnerServers()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var serverMock1 = new Mock<IServer>();
            var serverMock2 = new Mock<IServer>();

            // Setup StopAsync for both servers to return a completed task.
            serverMock1.Setup(s => s.StopAsync(cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();
            serverMock2.Setup(s => s.StopAsync(cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            IEnumerable<IServer> servers = new List<IServer> { serverMock1.Object, serverMock2.Object };
            var compositeServer = new CompositeServer(servers);

            // Act
            await compositeServer.StopAsync(cancellationToken);

            // Assert
            serverMock1.Verify(s => s.StopAsync(cancellationToken), Times.Once);
            serverMock2.Verify(s => s.StopAsync(cancellationToken), Times.Once);
        }
    }
}
