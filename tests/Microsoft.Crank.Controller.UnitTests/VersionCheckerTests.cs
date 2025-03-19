using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="VersionChecker"/> class.
    /// </summary>
    public class VersionCheckerTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;

        public VersionCheckerTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it correctly checks for updates.
        /// </summary>
//         [Fact] [Error] (39-43)CS0122 'HttpMessageHandler.SendAsync(HttpRequestMessage, CancellationToken)' is inaccessible due to its protection level
//         public async Task CheckUpdateAsync_WhenNewVersionAvailable_DisplaysUpdateMessage()
//         {
//             // Arrange
//             var latestVersion = new NuGetVersion("1.2.3");
//             var currentVersion = new NuGetVersion("1.0.0");
//             var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
// 
//             var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
//             mockHttpMessageHandler
//                 .Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new HttpResponseMessage
//                 {
//                     StatusCode = HttpStatusCode.OK,
//                     Content = new StringContent($"{{\"versions\": [\"{latestVersion}\"]}}")
//                 });
// 
//             var client = new HttpClient(mockHttpMessageHandler.Object);
// 
//             var assemblyMock = new Mock<Assembly>();
//             assemblyMock
//                 .Setup(a => a.GetCustomAttribute<AssemblyInformationalVersionAttribute>())
//                 .Returns(new AssemblyInformationalVersionAttribute(currentVersion.ToNormalizedString()));
// 
//             // Act
//             await VersionChecker.CheckUpdateAsync(client);
// 
//             // Assert
//             Assert.True(File.Exists(versionFilename));
//             var fileContent = File.ReadAllText(versionFilename);
//             Assert.Equal(latestVersion.ToNormalizedString(), fileContent);
//         }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it handles no new version available.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_WhenNoNewVersionAvailable_DoesNotDisplayUpdateMessage()
        {
            // Arrange
            var latestVersion = new NuGetVersion("1.0.0");
            var currentVersion = new NuGetVersion("1.0.0");
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"{{\"versions\": [\"{latestVersion}\"]}}")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);

            var assemblyMock = new Mock<Assembly>();
            assemblyMock
                .Setup(a => a.GetCustomAttribute<AssemblyInformationalVersionAttribute>())
                .Returns(new AssemblyInformationalVersionAttribute(currentVersion.ToNormalizedString()));

            // Act
            await VersionChecker.CheckUpdateAsync(client);

            // Assert
            Assert.True(File.Exists(versionFilename));
            var fileContent = File.ReadAllText(versionFilename);
            Assert.Equal(latestVersion.ToNormalizedString(), fileContent);
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it handles exceptions gracefully.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_WhenExceptionThrown_DoesNotThrow()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            var client = new HttpClient(mockHttpMessageHandler.Object);

            // Act & Assert
            await VersionChecker.CheckUpdateAsync(client);
        }
    }
}
