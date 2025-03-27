using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

[assembly: AssemblyInformationalVersion("1.0.0")]

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Microsoft.Crank.Controller.VersionChecker"/> class.
    /// </summary>
    public class VersionCheckerTests : IDisposable
    {
        private readonly string _versionFilePath;

        /// <summary>
        /// Constructor to initialize test setup.
        /// </summary>
        public VersionCheckerTests()
        {
            // Use the same path as defined in VersionChecker.
            _versionFilePath = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");
            CleanupFile();
        }

        /// <summary>
        /// Disposes resources after tests.
        /// </summary>
        public void Dispose()
        {
            CleanupFile();
        }

        /// <summary>
        /// Cleans up the version file and its directory if present.
        /// </summary>
        private void CleanupFile()
        {
            try
            {
                if (File.Exists(_versionFilePath))
                {
                    File.Delete(_versionFilePath);
                }
                var directory = Path.GetDirectoryName(_versionFilePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    // Delete the directory if it's empty.
                    if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                    {
                        Directory.Delete(directory, true);
                    }
                }
            }
            catch
            {
                // Swallow any exceptions during cleanup.
            }
        }

        /// <summary>
        /// Sets the last write time of the version file.
        /// </summary>
        /// <param name="utcTime">The desired UTC time for the file's last write time.</param>
        private void SetFileLastWriteTimeUtc(DateTime utcTime)
        {
            if (File.Exists(_versionFilePath))
            {
                File.SetLastWriteTimeUtc(_versionFilePath, utcTime);
            }
        }

        /// <summary>
        /// Tests that when a fresh cache file exists with the current version, no update message is printed.
        /// Functional steps:
        /// 1. Create a version file with content "1.0.0" (the same as the current version set via assembly attribute).
        /// 2. Set its modification time to the current time (fresh cache).
        /// 3. Execute CheckUpdateAsync with a dummy HttpClient.
        /// Expected outcome: No update message should be printed.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_FreshCache_NoUpdateMessage()
        {
            // Arrange
            Directory.CreateDirectory(Path.GetDirectoryName(_versionFilePath));
            File.WriteAllText(_versionFilePath, "1.0.0");
            SetFileLastWriteTimeUtc(DateTime.UtcNow);
            var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
            using var consoleOutput = new ConsoleOutput();

            // Act
            await Microsoft.Crank.Controller.VersionChecker.CheckUpdateAsync(httpClient);

            // Assert
            string output = consoleOutput.GetOuput();
            Assert.DoesNotContain("A new version is available", output);
        }

        /// <summary>
        /// Tests that when a fresh cache file exists with a version higher than the current,
        /// an update message is printed to the console.
        /// Functional steps:
        /// 1. Create a version file with content "1.1.0" (newer than "1.0.0" from assembly attribute).
        /// 2. Ensure its last write time is current (fresh cache).
        /// 3. Execute CheckUpdateAsync.
        /// Expected outcome: An update message mentioning "1.1.0" is printed.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_FreshCache_UpdateMessage()
        {
            // Arrange
            Directory.CreateDirectory(Path.GetDirectoryName(_versionFilePath));
            File.WriteAllText(_versionFilePath, "1.1.0");
            SetFileLastWriteTimeUtc(DateTime.UtcNow);
            var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
            using var consoleOutput = new ConsoleOutput();

            // Act
            await Microsoft.Crank.Controller.VersionChecker.CheckUpdateAsync(httpClient);

            // Assert
            string output = consoleOutput.GetOuput();
            Assert.Contains("A new version is available", output);
            Assert.Contains("1.1.0", output);
        }

        /// <summary>
        /// Tests that when the cache is expired, the method fetches the latest version from the remote service,
        /// updates the cache file, and prints an update message if the fetched version is newer.
        /// Functional steps:
        /// 1. Create an expired cache file with an old version ("0.0.1").
        /// 2. Set its last write time to more than one day ago.
        /// 3. Configure a mock HttpClient to return a JSON containing versions ["1.0.0", "1.1.0", "0.9.0"].
        /// 4. Execute CheckUpdateAsync.
        /// Expected outcome: The update message is printed with version "1.1.0" and the cache file is updated.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_CacheExpired_FetchesFromRemote()
        {
            // Arrange
            Directory.CreateDirectory(Path.GetDirectoryName(_versionFilePath));
            File.WriteAllText(_versionFilePath, "0.0.1");
            SetFileLastWriteTimeUtc(DateTime.UtcNow.AddDays(-2));
            string jsonResponse = "{\"versions\": [\"1.0.0\", \"1.1.0\", \"0.9.0\"]}";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(jsonResponse)
               });
            var httpClient = new HttpClient(handlerMock.Object);
            using var consoleOutput = new ConsoleOutput();

            // Act
            await Microsoft.Crank.Controller.VersionChecker.CheckUpdateAsync(httpClient);

            // Assert
            string output = consoleOutput.GetOuput();
            Assert.Contains("A new version is available", output);
            Assert.Contains("1.1.0", output);

            string cachedVersion = File.ReadAllText(_versionFilePath);
            Assert.Equal("1.1.0", cachedVersion);
        }

        /// <summary>
        /// Tests that when the remote call fails (e.g., due to an HTTP exception),
        /// the method handles the exception gracefully without throwing.
        /// Functional steps:
        /// 1. Create an expired cache file so that a remote call is attempted.
        /// 2. Configure a mock HttpClient to throw an HttpRequestException.
        /// 3. Execute CheckUpdateAsync.
        /// Expected outcome: No exception is thrown and no update message is printed.
        /// </summary>
        [Fact]
        public async Task CheckUpdateAsync_RemoteFailure_DoesNotThrow()
        {
            // Arrange
            Directory.CreateDirectory(Path.GetDirectoryName(_versionFilePath));
            File.WriteAllText(_versionFilePath, "0.0.1");
            SetFileLastWriteTimeUtc(DateTime.UtcNow.AddDays(-2));

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("Simulated HTTP failure"));
            var httpClient = new HttpClient(handlerMock.Object);
            using var consoleOutput = new ConsoleOutput();

            // Act
            var exception = await Record.ExceptionAsync(() => Microsoft.Crank.Controller.VersionChecker.CheckUpdateAsync(httpClient));

            // Assert
            Assert.Null(exception);
            string output = consoleOutput.GetOuput();
            Assert.DoesNotContain("A new version is available", output);
        }
    }

    /// <summary>
    /// Helper class to capture and restore console output.
    /// </summary>
    internal class ConsoleOutput : IDisposable
    {
        private readonly StringWriter _stringWriter;
        private readonly TextWriter _originalOutput;
        private readonly ConsoleColor _originalForeground;
        private readonly ConsoleColor _originalBackground;

        /// <summary>
        /// Initializes a new instance and redirects console output.
        /// </summary>
        public ConsoleOutput()
        {
            _stringWriter = new StringWriter();
            _originalOutput = Console.Out;
            _originalForeground = Console.ForegroundColor;
            _originalBackground = Console.BackgroundColor;
            Console.SetOut(_stringWriter);
        }

        /// <summary>
        /// Retrieves the captured console output.
        /// </summary>
        /// <returns>The captured output as a string.</returns>
        public string GetOuput()
        {
            return _stringWriter.ToString();
        }

        /// <summary>
        /// Disposes resources and restores original console settings.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            Console.ForegroundColor = _originalForeground;
            Console.BackgroundColor = _originalBackground;
            _stringWriter.Dispose();
        }
    }
}
