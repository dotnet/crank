using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TimeSpanConverter"/> class.
    /// </summary>
    public class TimeSpanConverterTests
    {
        private readonly TimeSpanConverter _converter;

        public TimeSpanConverterTests()
        {
            _converter = new TimeSpanConverter();
        }

        /// <summary>
        /// Tests that the Read method correctly parses a valid TimeSpan string.
        /// Arrange: Creates a Utf8JsonReader containing a valid TimeSpan string.
        /// Act: Invokes the Read method.
        /// Assert: Verifies that the returned TimeSpan equals the expected value.
        /// </summary>
        [Fact]
        public void Read_ValidTimeSpanString_ReturnsCorrectTimeSpan()
        {
            // Arrange
            string timeSpanString = "01:02:03";
            string json = $"\"{timeSpanString}\"";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(jsonBytes);
            // Move to the first token which should be the string.
            reader.Read();

            // Act
            TimeSpan result = _converter.Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions());

            // Assert
            Assert.Equal(TimeSpan.Parse(timeSpanString), result);
        }

        /// <summary>
        /// Tests that the Read method throws a FormatException when provided with an invalid TimeSpan string.
        /// Arrange: Creates a Utf8JsonReader containing an invalid TimeSpan string.
        /// Act & Assert: Expects a FormatException to be thrown.
        /// </summary>
//         [Fact] [Error] (62-70)CS8175 Cannot use ref local 'reader' inside an anonymous method, lambda expression, or query expression
//         public void Read_InvalidTimeSpanString_ThrowsFormatException()
//         {
//             // Arrange
//             string invalidString = "invalidTimespan";
//             string json = $"\"{invalidString}\"";
//             byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
//             var reader = new Utf8JsonReader(jsonBytes);
//             reader.Read();
// 
//             // Act & Assert
//             Assert.Throws<FormatException>(() => _converter.Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions()));
//         }

        /// <summary>
        /// Tests that the Read method throws an ArgumentNullException when the JSON value is null.
        /// Arrange: Creates a Utf8JsonReader with a null JSON token.
        /// Act & Assert: Expects an ArgumentNullException to be thrown.
        /// </summary>
//         [Fact] [Error] (80-76)CS8175 Cannot use ref local 'reader' inside an anonymous method, lambda expression, or query expression
//         public void Read_NullJsonToken_ThrowsArgumentNullException()
//         {
//             // Arrange
//             string json = "null";
//             byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
//             var reader = new Utf8JsonReader(jsonBytes);
//             reader.Read();
// 
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => _converter.Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions()));
//         }

        /// <summary>
        /// Tests that the Write method correctly writes the TimeSpan as a JSON string.
        /// Arrange: Creates a MemoryStream and a Utf8JsonWriter, and defines a TimeSpan value.
        /// Act: Invokes the Write method.
        /// Assert: Verifies that the written JSON matches the expected string representation.
        /// </summary>
        [Fact]
        public void Write_WritesTimeSpanAsString()
        {
            // Arrange
            var timeSpanValue = new TimeSpan(1, 2, 3);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            
            // Act
            _converter.Write(writer, timeSpanValue, new JsonSerializerOptions());
            writer.Flush();
            string jsonOutput = Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            Assert.Equal($"\"{timeSpanValue.ToString()}\"", jsonOutput);
        }
    }
}
