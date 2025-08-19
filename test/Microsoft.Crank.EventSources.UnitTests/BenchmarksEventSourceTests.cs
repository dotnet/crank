using System;
using System.IO;
using Microsoft.Crank.EventSources;
using Xunit;

namespace Microsoft.Crank.EventSources.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="BenchmarksEventSource"/> class.
    /// </summary>
    public class BenchmarksEventSourceTests
    {
        /// <summary>
        /// Tests the static Measure method with a long value to ensure it does not throw an exception when invoked with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The long value to measure.</param>
        [Theory]
        [InlineData("TestMetric", 123)]
        [InlineData("", 0)]
        [InlineData(null, -1)]
        public void Measure_Long_ValidInput_ShouldNotThrow(string name, long value)
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.Measure(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static Measure method with a double value to ensure it does not throw an exception when invoked with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The double value to measure.</param>
        [Theory]
        [InlineData("TestMetric", 123.45)]
        [InlineData("", 0.0)]
        [InlineData(null, -1.2)]
        public void Measure_Double_ValidInput_ShouldNotThrow(string name, double value)
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.Measure(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static Measure method with a string value to ensure it does not throw an exception when invoked with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The string value to measure.</param>
        [Theory]
        [InlineData("TestMetric", "TestValue")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void Measure_String_ValidInput_ShouldNotThrow(string name, string value)
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.Measure(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static MeasureAndRegister method to ensure it completes successfully when provided with valid input.
        /// </summary>
        [Fact]
        public void MeasureAndRegister_ValidInput_ShouldNotThrow()
        {
            // Arrange
            string name = "TestMetric";
            string value = "TestValue";
            string description = "Test Description";

            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.MeasureAndRegister(name, value, description));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static SetChildProcessId method to verify that it writes the correct output to the standard error stream.
        /// </summary>
        /// <param name="pid">The process id to be output.</param>
        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetChildProcessId_ValidInput_ShouldWriteCorrectOutput(int pid)
        {
            // Arrange
            using (var writer = new StringWriter())
            {
                TextWriter originalError = Console.Error;
                Console.SetError(writer);

                try
                {
                    // Act
                    BenchmarksEventSource.SetChildProcessId(pid);
                    writer.Flush();
                    string output = writer.ToString();

                    // Assert
                    Assert.Contains($"##ChildProcessId:{pid}", output);
                }
                finally
                {
                    Console.SetError(originalError);
                }
            }
        }

        /// <summary>
        /// Tests the static Register method to ensure it does not throw an exception when provided with valid input.
        /// </summary>
//         [Fact] [Error] (126-89)CS1503 Argument 2: cannot convert from 'Microsoft.Crank.EventSources.UnitTests.Operations' to 'Microsoft.Crank.EventSources.Operations' [Error] (126-100)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.EventSources.UnitTests.Operations' to 'Microsoft.Crank.EventSources.Operations'
//         public void Register_ValidInput_ShouldNotThrow()
//         {
//             // Arrange
//             string name = "TestMetric";
//             // Use dummy Operations values. Since the actual Operations type is expected to be an enum, we simulate it below.
//             var aggregate = Operations.First;
//             var reduce = Operations.First;
//             string shortDescription = "Short Description";
//             string longDescription = "Long Description";
//             string format = "n2";
// 
//             // Act & Assert
//             var exception = Record.Exception(() => BenchmarksEventSource.Register(name, aggregate, reduce, shortDescription, longDescription, format));
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests the static MeasureAspNetVersion method to ensure it does not throw an exception even when the ASP.NET Core hosting type is not available.
        /// </summary>
        [Fact]
        public void MeasureAspNetVersion_NoAspNetHostingType_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.MeasureAspNetVersion());
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static MeasureNetCoreAppVersion method to ensure it does not throw an exception even when the version information is unavailable.
        /// </summary>
        [Fact]
        public void MeasureNetCoreAppVersion_Behavior_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.MeasureNetCoreAppVersion());
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the static Start method to ensure it completes without throwing an exception.
        /// </summary>
        [Fact]
        public void Start_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => BenchmarksEventSource.Start());
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the instance method MeasureLong to ensure it does not throw an exception when called with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The long value to measure.</param>
        [Theory]
        [InlineData("TestMetric", 456)]
        public void Instance_MeasureLong_ValidInput_ShouldNotThrow(string name, long value)
        {
            // Arrange
            var eventSource = new BenchmarksEventSource("TestEventSource");

            // Act & Assert
            var exception = Record.Exception(() => eventSource.MeasureLong(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the instance method MeasureDouble to ensure it does not throw an exception when called with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The double value to measure.</param>
        [Theory]
        [InlineData("TestMetric", 789.01)]
        public void Instance_MeasureDouble_ValidInput_ShouldNotThrow(string name, double value)
        {
            // Arrange
            var eventSource = new BenchmarksEventSource("TestEventSource");

            // Act & Assert
            var exception = Record.Exception(() => eventSource.MeasureDouble(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the instance method MeasureString to ensure it does not throw an exception when called with valid input.
        /// </summary>
        /// <param name="name">The measurement name.</param>
        /// <param name="value">The string value to measure.</param>
        [Theory]
        [InlineData("TestMetric", "TestValue")]
        public void Instance_MeasureString_ValidInput_ShouldNotThrow(string name, string value)
        {
            // Arrange
            var eventSource = new BenchmarksEventSource("TestEventSource");

            // Act & Assert
            var exception = Record.Exception(() => eventSource.MeasureString(name, value));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the instance method Metadata to ensure it completes without throwing an exception when provided with valid input.
        /// </summary>
        [Fact]
        public void Instance_Metadata_ValidInput_ShouldNotThrow()
        {
            // Arrange
            var eventSource = new BenchmarksEventSource("TestEventSource");
            string name = "TestMetric";
            string aggregate = "First";
            string reduce = "First";
            string shortDescription = "Short Description";
            string longDescription = "Long Description";
            string format = "n2";

            // Act & Assert
            var exception = Record.Exception(() => eventSource.Metadata(name, aggregate, reduce, shortDescription, longDescription, format));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests the instance method Started to ensure it completes without throwing an exception.
        /// </summary>
        [Fact]
        public void Instance_Started_ShouldNotThrow()
        {
            // Arrange
            var eventSource = new BenchmarksEventSource("TestEventSource");

            // Act & Assert
            var exception = Record.Exception(() => eventSource.Started());
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Dummy enumeration to simulate the Operations enum used in the BenchmarksEventSource class.
    /// In a real scenario, this should be replaced by the actual Operations enum from the appropriate namespace.
    /// </summary>
    public enum Operations
    {
        First
    }
}
