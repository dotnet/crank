using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Crank.Wrk;
using Xunit;

namespace Microsoft.Crank.Wrk.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WrkProcess"/> class.
    /// </summary>
    public class WrkProcessTests
    {
        /// <summary>
        /// Tests that MeasureFirstRequest writes the skipping message when no URL is provided.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_NoUrl_WritesSkippingMessage()
        {
            // Arrange
            string[] args = { "notAUrl", "--someflag" };
            using var sw = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.SetOut(sw);
            
            // Act
            await WrkProcess.MeasureFirstRequest(args);
            Console.SetOut(originalOut);
            string output = sw.ToString();
            
            // Assert
            Assert.Contains("URL not found, skipping first request", output);
        }

        /// <summary>
        /// Tests that MeasureFirstRequest handles an HTTP connection exception when an invalid URL is provided.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_InvalidUrl_HandlesConnectionException()
        {
            // Arrange - using a URL that should fail quickly.
            string[] args = { "http://localhost:12345" };
            using var sw = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.SetOut(sw);
            
            // Act
            await WrkProcess.MeasureFirstRequest(args);
            Console.SetOut(originalOut);
            string output = sw.ToString();
            
            // Assert - Expected branch: HttpRequestException causes a connection exception message.
            Assert.Contains("A connection exception occurred while measuring the first request", output);
        }

        /// <summary>
        /// Tests that RunAsync returns -1 when the required duration argument ("-d") is missing.
        /// </summary>
        [Fact]
        public async Task RunAsync_NoDuration_ReturnsMinusOne()
        {
            // Arrange: no "-d" argument included.
            string[] args = { "http://localhost" };
            
            // Act
            int result = await WrkProcess.RunAsync(args);
            
            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Tests that RunAsync returns -1 when an exception occurs during process invocation (simulated by null _wrkFilename).
        /// </summary>
        [Fact]
        public async Task RunAsync_WithDuration_ProcessStartFails_ReturnsMinusOne()
        {
            // Arrange: provide the required "-d" argument and an URL.
            // Since _wrkFilename is computed in RunAsync by the method RunCore and is not set,
            // process execution will fail due to an invalid file name, causing the method to return -1.
            string[] args = { "-d", "10s", "http://localhost" };
            
            // Act
            int result = await WrkProcess.RunAsync(args);
            
            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Tests that RunAsync returns -1 when a script download fails (e.g. due to an invalid script URL).
        /// </summary>
        [Fact]
        public async Task RunAsync_WithInvalidScriptUrl_ReturnsMinusOne()
        {
            // Arrange: provide "-s" with an invalid URL and required "-d" argument.
            string[] args = { "-d", "10s", "-s", "http://nonexistent", "http://localhost" };
            
            // Act
            int result = await WrkProcess.RunAsync(args);
            
            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Tests that DownloadWrkAsync does not attempt to download when the target file already exists.
        /// </summary>
        [Fact]
        public async Task DownloadWrkAsync_FileAlreadyExists_DoesNotDownload()
        {
            // Arrange
            string wrkUrl = RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "https://aspnetbenchmarks.z5.web.core.windows.net/tools/wrk-linux-amd64"
                : "https://aspnetbenchmarks.z5.web.core.windows.net/tools/wrk-linux-arm64";
            string expectedFileName = Path.Combine(Path.GetTempPath(), ".crank", Path.GetFileName(wrkUrl));
            Directory.CreateDirectory(Path.GetDirectoryName(expectedFileName));
            // Pre-create the file to simulate it already exists.
            File.WriteAllText(expectedFileName, "dummy content");

            using var sw = new StringWriter();
            TextWriter originalOut = Console.Out;
            Console.SetOut(sw);

            try
            {
                // Act
                await WrkProcess.DownloadWrkAsync();
                Console.SetOut(originalOut);
                string output = sw.ToString();
                
                // Assert - since file exists, there should be no download message.
                Assert.DoesNotContain("Downloading wrk from", output);
                Assert.True(File.Exists(expectedFileName));
            }
            finally
            {
                // Cleanup: remove the dummy file.
                if (File.Exists(expectedFileName))
                {
                    File.Delete(expectedFileName);
                }
            }
        }
    }
}
