using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Crank.Jobs.Wrk2;
using Xunit;

namespace Microsoft.Crank.Jobs.Wrk2.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests that Main returns -1 when the duration argument (-d) is missing.
        /// </summary>
//         [Fact] [Error] (27-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_MissingDurationArgument_ReturnsMinusOne()
//         {
//             // Arrange
//             // Provide -w argument but omit the required -d argument to ensure early termination.
//             string[] args = new string[] { "-w", "5s", "http://example.com" };
// 
//             // Act
//             int result = await Program.Main(args);
// 
//             // Assert: Expect -1 due to missing duration argument.
//             Assert.Equal(-1, result);
//         }
    
        /// <summary>
        /// Tests that Main returns -1 on unsupported platforms.
        /// This test assumes that the test environment does not meet the Unix and X64 conditions.
        /// </summary>
//         [Fact] [Error] (45-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_UnsupportedPlatform_ReturnsMinusOne()
//         {
//             // Arrange
//             // Provide required -d argument but expect platform check to fail in non-Unix or non-X64 environments.
//             string[] args = new string[] { "-d", "10s", "http://example.com" };
// 
//             // Act
//             int result = await Program.Main(args);
// 
//             // Assert: Expect -1 due to unsupported platform.
//             Assert.Equal(-1, result);
//         }
    
        /// <summary>
        /// Tests DownloadWrk2Async when a cache file already exists.
        /// Verifies that the cached file is copied to the current directory and the returned filename is as expected.
        /// </summary>
        [Fact]
        public async Task DownloadWrk2Async_CacheExists_ReturnsFileNameAndCopiesCacheContent()
        {
            // Arrange
            string fileName = Path.GetFileName("https://aspnetbenchmarks.z5.web.core.windows.net/tools/wrk2");
            string cacheFolder = Path.Combine(Path.GetTempPath(), ".benchmarks");
            string cacheFilePath = Path.Combine(cacheFolder, fileName);
            string currentFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            try
            {
                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }
                
                // Create a dummy cache file with known content.
                File.WriteAllText(cacheFilePath, "dummy content");
                
                // Ensure destination file does not exist.
                if (File.Exists(currentFilePath))
                {
                    File.Delete(currentFilePath);
                }

                // Act
                string result = await Program.DownloadWrk2Async();

                // Assert
                Assert.Equal(fileName, result);
                Assert.True(File.Exists(currentFilePath), "The file should be copied to the current directory.");
                string copiedContent = File.ReadAllText(currentFilePath);
                Assert.Equal("dummy content", copiedContent);
            }
            finally
            {
                // Cleanup created files.
                if (File.Exists(currentFilePath))
                {
                    File.Delete(currentFilePath);
                }
                if (File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                }
            }
        }
    
        /// <summary>
        /// Tests MeasureFirstRequest with no URL provided in the arguments.
        /// Verifies that the method outputs a message indicating skipping of the first request.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_NoUrl_SkipsRequest()
        {
            // Arrange
            string[] args = Array.Empty<string>();
            using (var sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);

                try
                {
                    // Act
                    await Program.MeasureFirstRequest(args);
                    string output = sw.ToString();

                    // Assert
                    Assert.Contains("URL not found, skipping first request", output);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
        }
    
        /// <summary>
        /// Tests MeasureFirstRequest with an invalid URL.
        /// Verifies that the method handles connection exceptions gracefully.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_WithInvalidUrl_HandlesException()
        {
            // Arrange
            string[] args = new string[] { "http://nonexistent.invalid" };
            using (var sw = new StringWriter())
            {
                TextWriter originalOut = Console.Out;
                Console.SetOut(sw);

                try
                {
                    // Act
                    await Program.MeasureFirstRequest(args);
                    string output = sw.ToString();

                    // Assert: Expect output indicating a connection exception, timeout, or an unexpected exception.
                    bool containsConnectionError = output.Contains("A connection exception occurred while measuring the first request");
                    bool containsTimeout = output.Contains("A timeout occurred while measuring the first request");
                    bool containsUnexpected = output.Contains("An unexpected exception occurred while measuring the first request");
                    Assert.True(containsConnectionError || containsTimeout || containsUnexpected, "Expected an exception message due to invalid URL.");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
        }
    }
}
