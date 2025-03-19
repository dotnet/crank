using Moq;
using NuGet.Versioning;
using System;
using System.IO;
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
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it correctly checks for updates when the cache is expired.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_CacheExpired_ChecksForUpdates()
        {
            // Arrange
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            if (File.Exists(versionFilename))
            {
                File.SetLastWriteTimeUtc(versionFilename, DateTime.UtcNow - TimeSpan.FromDays(2));
            }
            var expectedVersion = new NuGetVersion("1.0.0");
            var jsonResponse = $"{{\"versions\": [\"{expectedVersion}\"]}}";
            _mockHttpClient.Setup(client => client.GetStringAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

            // Act
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);

            // Assert
            var actualVersion = NuGetVersion.Parse(File.ReadAllText(versionFilename));
            Assert.Equal(expectedVersion, actualVersion);
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it correctly uses the cached version when the cache is not expired.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_CacheNotExpired_UsesCachedVersion()
        {
            // Arrange
            var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            var expectedVersion = new NuGetVersion("1.0.0");
            File.WriteAllText(versionFilename, expectedVersion.ToNormalizedString());
            File.SetLastWriteTimeUtc(versionFilename, DateTime.UtcNow);

            // Act
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);

            // Assert
            var actualVersion = NuGetVersion.Parse(File.ReadAllText(versionFilename));
            Assert.Equal(expectedVersion, actualVersion);
        }

        /// <summary>
        /// Tests the <see cref="VersionChecker.CheckUpdateAsync(HttpClient)"/> method to ensure it correctly handles exceptions.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_ExceptionThrown_DoesNotThrow()
        {
            // Arrange
            _mockHttpClient.Setup(client => client.GetStringAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException());

            // Act & Assert
            await VersionChecker.CheckUpdateAsync(_mockHttpClient.Object);
        }
    }
}
