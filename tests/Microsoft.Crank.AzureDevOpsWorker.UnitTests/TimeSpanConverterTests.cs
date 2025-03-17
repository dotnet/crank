// See the LICENSE file in the project root for more information.

using Microsoft.Crank.AzureDevOpsWorker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TimeSpanConverter"/> class.
    /// </summary>
    [TestClass]
    public class TimeSpanConverterTests
    {
        private readonly TimeSpanConverter _timeSpanConverter;

        public TimeSpanConverterTests()
        {
            _timeSpanConverter = new TimeSpanConverter();
        }

        /// <summary>
        /// Tests the <see cref="TimeSpanConverter.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/> method to ensure it correctly parses a valid TimeSpan string.
        /// </summary>
        [TestMethod]
        public void Read_ValidTimeSpanString_ReturnsCorrectTimeSpan()
        {
            // Arrange
            string json = "\"02:00:00\"";
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            TimeSpan result = _timeSpanConverter.Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions());

            // Assert
            Assert.AreEqual(TimeSpan.FromHours(2), result);
        }

        /// <summary>
        /// Tests the <see cref="TimeSpanConverter.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/> method to ensure it throws a FormatException for an invalid TimeSpan string.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Read_InvalidTimeSpanString_ThrowsFormatException()
        {
            // Arrange
            string json = "\"invalid\"";
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
            reader.Read();

            // Act
            _timeSpanConverter.Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions());

            // Assert is handled by ExpectedException
        }

        /// <summary>
        /// Tests the <see cref="TimeSpanConverter.Write(Utf8JsonWriter, TimeSpan, JsonSerializerOptions)"/> method to ensure it correctly writes a TimeSpan value as a string.
        /// </summary>
        [TestMethod]
        public void Write_ValidTimeSpan_WritesCorrectString()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(buffer);
            TimeSpan timeSpan = TimeSpan.FromHours(2);

            // Act
            _timeSpanConverter.Write(writer, timeSpan, options);
            writer.Flush();
            string json = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

            // Assert
            Assert.AreEqual("\"02:00:00\"", json);
        }
    }
}

