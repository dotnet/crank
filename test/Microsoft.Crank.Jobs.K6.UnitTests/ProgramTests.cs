using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Jobs.K6;
using Xunit;

namespace Microsoft.Crank.Jobs.K6.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests the MeasureFirstRequest method when no URL argument is provided.
        /// Expected outcome: The method should print a message indicating the URL was not found and skip the request.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_NoUrl_PrintsSkippingMessage()
        {
            // Arrange
            string[] args = new string[] { "SOMEARG=value" };
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            await Program.MeasureFirstRequest(args);
            string output = sw.ToString();

            // Assert
            Assert.Contains("URL not found, skipping first request", output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests the MeasureFirstRequest method when a URL argument is provided that leads to a connection exception.
        /// Expected outcome: The method should catch the HttpRequestException and print a connection exception message.
        /// </summary>
        [Fact]
        public async Task MeasureFirstRequest_InvalidUrl_PrintsConnectionExceptionMessage()
        {
            // Arrange
            // Using an unlikely valid URL to force a connection exception.
            string[] args = new string[] { "URL=http://nonexistent.invalid" };
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            await Program.MeasureFirstRequest(args);
            string output = sw.ToString();

            // Assert
            Assert.Contains("A connection exception occurred while measuring the first request", output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Integration test for the Main method when warmup is skipped.
        /// Note: This test is marked as skipped due to its integration and side-effect nature.
        /// It requires file system access, network connectivity, and process execution.
        /// </summary>
//         [Fact(Skip = "Integration test not run in unit environment")] [Error] (117-42)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_NoWarmupIntegrationTest()
//         {
//             // Arrange
//             // Create dummy arguments with no warmup parameter so that "Warmup skipped" is expected.
//             string[] args = new string[] { "URL=http://nonexistent.invalid", "--duration", "1" };
// 
//             // To avoid actual downloading and process execution, pre-create the dummy K6 file.
//             string dummyUrl = null;
//             if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
//             {
//                 if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-win-amd64.exe";
//                 }
//             }
//             else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
//             {
//                 if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-linux-amd64";
//                 }
//                 else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-linux-arm64";
//                 }
//             }
//             if (dummyUrl == null)
//             {
//                 // Force unsupported platform outcome.
//                 args = new string[] { };
//             }
//             else
//             {
//                 string tempPath = Path.GetTempPath();
//                 string k6FileName = System.IO.Path.Combine(tempPath, ".crank", System.IO.Path.GetFileName(dummyUrl));
//                 Directory.CreateDirectory(Path.GetDirectoryName(k6FileName));
//                 // Create a dummy executable that exits with 0.
//                 if (!File.Exists(k6FileName))
//                 {
//                     // On Windows, create a batch file; on Unix-like, create a shell script.
//                     if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
//                     {
//                         File.WriteAllText(k6FileName, "@echo off\necho Dummy executable\nexit 0");
//                     }
//                     else
//                     {
//                         File.WriteAllText(k6FileName, "#!/bin/bash\necho Dummy executable\nexit 0");
//                         System.Diagnostics.Process.Start("chmod", $"+x {k6FileName}")?.WaitForExit();
//                     }
//                 }
//             }
// 
//             using var sw = new StringWriter();
//             Console.SetOut(sw);
// 
//             // Act
//             int exitCode = await Program.Main(args);
//             string output = sw.ToString();
// 
//             // Assert
//             // Expecting warmup to be skipped and process to run yielding exit code 0.
//             Assert.Contains("Warmup skipped", output, StringComparison.OrdinalIgnoreCase);
//             Assert.Equal(0, exitCode);
//         }

        /// <summary>
        /// Integration test for the Main method when warmup is performed.
        /// Note: This test is marked as skipped due to its integration and side-effect nature.
        /// It requires file system access, network connectivity, and process execution.
        /// </summary>
//         [Fact(Skip = "Integration test not run in unit environment")] [Error] (187-42)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WarmupIntegrationTest()
//         {
//             // Arrange
//             // Provide warmup and duration parameters.
//             string[] args = new string[] { "URL=http://nonexistent.invalid", "--warmup", "1", "--duration", "1" };
// 
//             // Pre-create dummy K6 file as in the previous integration test.
//             string dummyUrl = null;
//             if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
//             {
//                 if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-win-amd64.exe";
//                 }
//             }
//             else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
//             {
//                 if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-linux-amd64";
//                 }
//                 else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
//                 {
//                     dummyUrl = "https://aspnetbenchmarks.z5.web.core.windows.net/tools/k6-linux-arm64";
//                 }
//             }
//             if (dummyUrl == null)
//             {
//                 // Force unsupported platform outcome.
//                 args = new string[] { };
//             }
//             else
//             {
//                 string tempPath = Path.GetTempPath();
//                 string k6FileName = System.IO.Path.Combine(tempPath, ".crank", System.IO.Path.GetFileName(dummyUrl));
//                 Directory.CreateDirectory(Path.GetDirectoryName(k6FileName));
//                 // Create a dummy executable that exits with 0.
//                 if (!File.Exists(k6FileName))
//                 {
//                     if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
//                     {
//                         File.WriteAllText(k6FileName, "@echo off\necho Dummy executable\nexit 0");
//                     }
//                     else
//                     {
//                         File.WriteAllText(k6FileName, "#!/bin/bash\necho Dummy executable\nexit 0");
//                         System.Diagnostics.Process.Start("chmod", $"+x {k6FileName}")?.WaitForExit();
//                     }
//                 }
//             }
// 
//             using var sw = new StringWriter();
//             Console.SetOut(sw);
// 
//             // Act
//             int exitCode = await Program.Main(args);
//             string output = sw.ToString();
// 
//             // Assert
//             // Expecting the warmup to be executed (so output should contain the k6 command with warmup duration) and final exit code 0.
//             Assert.Contains("--duration 1s", output, StringComparison.OrdinalIgnoreCase);
//             Assert.Equal(0, exitCode);
//         }
    }
}
