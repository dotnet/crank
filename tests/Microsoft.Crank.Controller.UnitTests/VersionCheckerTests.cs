using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="VersionChecker"/> class.
    /// </summary>
    [TestClass]
    public class VersionCheckerTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;

        public VersionCheckerTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it correctly checks for updates when the cache is expired.
        /// </summary>
        [TestMethod]
        public async Task CheckUpdateAsync_WhenCacheExpired_ChecksForUpdates()
        {
            // Arrange
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(versionFilename));
            File.WriteAllText(versionFilename, "0.1.0");
            File.SetLastWriteTimeUtc(versionFilename, DateTime.UtcNow - TimeSpan.FromDays(2));

            var latestVersion = "0.2.0";
            var jsonResponse = new JObject
            {
                ["versions"] = new JArray(latestVersion)
            }.ToString();

            _mockHttpClient.Setup(client => client.GetStringAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

            var assemblyMock = new Mock<Assembly>();
            var attribute = new AssemblyInformationalVersionAttribute("0.1.0");
            assemblyMock.Setup(a => a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()).Returns(attribute);

            // Act
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);

            // Assert
            var cachedVersion = File.ReadAllText(versionFilename);
            Assert.AreEqual(latestVersion, cachedVersion, "The cached version should be updated to the latest version.");
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it does not check for updates when the cache is valid.
        /// </summary>
        [TestMethod]
        public async Task CheckUpdateAsync_WhenCacheValid_DoesNotCheckForUpdates()
        {
            // Arrange
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(versionFilename));
            var cachedVersion = "0.1.0";
            File.WriteAllText(versionFilename, cachedVersion);
            File.SetLastWriteTimeUtc(versionFilename, DateTime.UtcNow);

            // Act
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);

            // Assert
            _mockHttpClient.Verify(client => client.GetStringAsync(It.IsAny<string>()), Times.Never);
            var currentCachedVersion = File.ReadAllText(versionFilename);
            Assert.AreEqual(cachedVersion, currentCachedVersion, "The cached version should remain unchanged.");
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it handles exceptions gracefully.
        /// </summary>
        [TestMethod]
        public async Task CheckUpdateAsync_WhenExceptionThrown_HandlesGracefully()
        {
            // Arrange
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(versionFilename));
            File.WriteAllText(versionFilename, "0.1.0");
            File.SetLastWriteTimeUtc(versionFilename, DateTime.UtcNow - TimeSpan.FromDays(2));

            _mockHttpClient.Setup(client => client.GetStringAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException());

            // Act
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);

            // Assert
            // No exception should be thrown and the method should complete gracefully.
        }
    }
}
