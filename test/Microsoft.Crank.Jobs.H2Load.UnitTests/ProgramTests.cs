using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using H2LoadClient;
using Xunit;

namespace H2LoadClient.UnitTests
{
    /// <summary>
    /// Contains unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Resets the static properties of the <see cref="Program"/> class to default values.
        /// This is necessary to avoid test interference because the properties are static.
        /// </summary>
        private void ResetProgramStaticProperties()
        {
            // Reset string properties to null.
            SetStaticProperty("ServerUrl", (string)null);
            SetStaticProperty("Protocol", (string)null);
            SetStaticProperty("RequestBodyFile", (string)null);
            SetStaticProperty("Output", (string)null);
            SetStaticProperty("Error", (string)null);
            
            // Reset int properties to 0.
            SetStaticProperty("Requests", 0);
            SetStaticProperty("Connections", 0);
            SetStaticProperty("Threads", 0);
            SetStaticProperty("Streams", 0);
            SetStaticProperty("Timeout", 0);
            SetStaticProperty("Warmup", 0);
            SetStaticProperty("Duration", 0);
            
            // Reset Headers dictionary to null.
            SetStaticProperty("Headers", (Dictionary<string, string>)null);
        }

        /// <summary>
        /// Uses reflection to set a static property on the Program class.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">Value to set.</param>
        private void SetStaticProperty<T>(string propertyName, T value)
        {
            PropertyInfo property = typeof(Program).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
            if (property == null)
            {
                throw new InvalidOperationException($"Property {propertyName} not found on Program.");
            }
            MethodInfo setMethod = property.GetSetMethod(true);
            if (setMethod == null)
            {
                throw new InvalidOperationException($"No setter found for property {propertyName}.");
            }
            setMethod.Invoke(null, new object[] { value });
        }

        /// <summary>
        /// Tests that when the help option is provided, the Program.Main method does not execute the main processing logic, 
        /// and static properties remain at their default values.
        /// </summary>
//         [Fact] [Error] (75-27)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WithHelpArgument_ShouldNotChangeStaticProperties()
//         {
//             // Arrange
//             ResetProgramStaticProperties();
//             string[] args = new string[] { "--help" };
// 
//             // Act
//             await Program.Main(args);
// 
//             // Assert: Since help was requested, the OnExecuteAsync delegate should not have been executed.
//             Assert.Null(Program.ServerUrl);
//             Assert.Null(Program.Protocol);
//             Assert.Null(Program.RequestBodyFile);
//             Assert.Null(Program.Output);
//             Assert.Null(Program.Error);
//             Assert.Equal(0, Program.Requests);
//             Assert.Equal(0, Program.Connections);
//             Assert.Equal(0, Program.Threads);
//             Assert.Equal(0, Program.Streams);
//             Assert.Equal(0, Program.Timeout);
//             Assert.Equal(0, Program.Warmup);
//             Assert.Equal(0, Program.Duration);
//             Assert.Null(Program.Headers);
//         }

        /// <summary>
        /// Tests that when an unknown protocol is provided to Program.Main, it throws an InvalidOperationException.
        /// </summary>
//         [Fact] [Error] (116-107)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WithInvalidProtocol_ShouldThrowInvalidOperationException()
//         {
//             // Arrange
//             ResetProgramStaticProperties();
//             // Providing minimal required arguments and an invalid protocol.
//             string[] args = new string[]
//             {
//                 "-u", "http://example.com",
//                 "-c", "10",
//                 "-t", "2",
//                 "-m", "5",
//                 "-n", "100",
//                 "-T", "5",
//                 "-w", "5",
//                 "-d", "10",
//                 "-p", "invalidprotocol"
//             };
// 
//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Program.Main(args));
//             Assert.Equal("Unknown protocol: invalidprotocol", exception.Message);
//         }
    }
}
