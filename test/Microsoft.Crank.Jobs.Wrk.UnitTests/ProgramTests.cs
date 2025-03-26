using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Wrk;
using Xunit;

namespace Microsoft.Crank.Wrk.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests the Main method when not running on Unix. Expects the method to print a platform not supported message and return -1.
        /// </summary>
//         [Fact] [Error] (33-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WhenNonUnixPlatform_ReturnsMinusOne()
//         {
//             // Arrange: Only run this test if the current OS platform is not Unix.
//             if (Environment.OSVersion.Platform == PlatformID.Unix)
//             {
//                 // Skip test on Unix platforms.
//                 return;
//             }
// 
//             // Capture console output.
//             var originalOut = Console.Out;
//             using var sw = new StringWriter();
//             Console.SetOut(sw);
// 
//             // Act
//             int result = await Program.Main(Array.Empty<string>());
//             string output = sw.ToString();
// 
//             // Restore console output.
//             Console.SetOut(originalOut);
// 
//             // Assert: Check that the unsupported message is printed and result equals -1.
//             Assert.Contains("Platform not supported", output);
//             Assert.Equal(-1, result);
//         }

        /// <summary>
        /// Tests the Main method when running on Unix. Expects the method to execute the workflow (printing client messages and processing args).
        /// Because external static dependencies are invoked, this test either validates output and return value if implemented 
        /// or catches a NotImplementedException from the unimplemented WrkProcess methods.
        /// </summary>
//         [Fact] [Error] (69-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WhenUnixPlatform_ExecutesWorkflow()
//         {
//             // Arrange: Only run this test if the current OS platform is Unix.
//             if (Environment.OSVersion.Platform != PlatformID.Unix)
//             {
//                 // Skip test on non-Unix platforms.
//                 return;
//             }
// 
//             // Capture console output.
//             var originalOut = Console.Out;
//             using var sw = new StringWriter();
//             Console.SetOut(sw);
// 
//             Exception caughtException = null;
//             int result = 0;
//             try
//             {
//                 // Act: Call Main with sample arguments.
//                 result = await Program.Main(new string[] { "arg1", "arg2" });
//             }
//             catch (Exception ex)
//             {
//                 caughtException = ex;
//             }
//             finally
//             {
//                 // Restore console output.
//                 Console.SetOut(originalOut);
//             }
// 
//             // Assert:
//             if (caughtException != null)
//             {
//                 // If external dependencies (WrkProcess methods) are not implemented, 
//                 // we expect a NotImplementedException or similar exception.
//                 Assert.IsType<NotImplementedException>(caughtException);
//             }
//             else
//             {
//                 string output = sw.ToString();
//                 // Validate that expected client messages and arguments are printed.
//                 Assert.Contains("WRK Client", output);
//                 Assert.Contains("args: arg1 arg2", output);
//                 // Validate that the result from RunAsync is non-negative.
//                 Assert.True(result >= 0, "Expected a non-negative result from RunAsync.");
//             }
//         }
    }
}
