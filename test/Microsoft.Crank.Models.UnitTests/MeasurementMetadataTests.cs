using Microsoft.Crank.Models;
using Newtonsoft.Json;
using System;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MeasurementMetadata"/> class.
    /// </summary>
    public class MeasurementMetadataTests
    {
        /// <summary>
        /// Verifies that each property of MeasurementMetadata can be set and retrieved correctly.
        /// This test covers the getters and setters of all public properties.
        /// </summary>
        [Fact]
        public void Properties_GetSetValues_ShouldWork()
        {
            // Arrange
            const string expectedSource = "TestSource";
            const string expectedName = "TestName";
            const string expectedShortDescription = "Test short description";
            const string expectedLongDescription = "Test long description";
            const string expectedFormat = "n2";
            Operation expectedReduce = Operation.Avg;
            Operation expectedAggregate = Operation.Sum;

            // Act
            var metadata = new MeasurementMetadata
            {
                Source = expectedSource,
                Name = expectedName,
                ShortDescription = expectedShortDescription,
                LongDescription = expectedLongDescription,
                Format = expectedFormat,
                Reduce = expectedReduce,
                Aggregate = expectedAggregate
            };

            // Assert
            Assert.Equal(expectedSource, metadata.Source);
            Assert.Equal(expectedName, metadata.Name);
            Assert.Equal(expectedShortDescription, metadata.ShortDescription);
            Assert.Equal(expectedLongDescription, metadata.LongDescription);
            Assert.Equal(expectedFormat, metadata.Format);
            Assert.Equal(expectedReduce, metadata.Reduce);
            Assert.Equal(expectedAggregate, metadata.Aggregate);
        }

        /// <summary>
        /// Verifies that MeasurementMetadata serializes correctly to JSON with enum properties represented as strings.
        /// This test uses JsonConvert to serialize an instance and checks that the enum values appear as their string representations.
        /// </summary>
        [Fact]
        public void Serialization_ToJson_ShouldSerializeEnumAsString()
        {
            // Arrange
            var metadata = new MeasurementMetadata
            {
                Source = "JsonSource",
                Name = "JsonName",
                ShortDescription = "ShortDesc",
                LongDescription = "LongDesc",
                Format = "n0",
                Reduce = Operation.Max,
                Aggregate = Operation.Min
            };

            // Act
            string json = JsonConvert.SerializeObject(metadata);
            
            // Assert
            Assert.Contains("\"Reduce\":\"Max\"", json);
            Assert.Contains("\"Aggregate\":\"Min\"", json);
            Assert.Contains("\"Source\":\"JsonSource\"", json);
            Assert.Contains("\"Name\":\"JsonName\"", json);
            Assert.Contains("\"ShortDescription\":\"ShortDesc\"", json);
            Assert.Contains("\"LongDescription\":\"LongDesc\"", json);
            Assert.Contains("\"Format\":\"n0\"", json);
        }

        /// <summary>
        /// Verifies that JSON is correctly deserialized into a MeasurementMetadata instance with proper enum conversion.
        /// This test uses JsonConvert to deserialize a JSON string and confirms that the enum properties are set as expected.
        /// </summary>
        [Fact]
        public void Deserialization_FromJson_ShouldDeserializeEnumAsString()
        {
            // Arrange
            string json = "{" +
                          "\"Source\":\"DeserializedSource\"," +
                          "\"Name\":\"DeserializedName\"," +
                          "\"ShortDescription\":\"DeserializedShortDesc\"," +
                          "\"LongDescription\":\"DeserializedLongDesc\"," +
                          "\"Format\":\"n1\"," +
                          "\"Reduce\":\"Delta\"," +
                          "\"Aggregate\":\"All\"" +
                          "}";

            // Act
            var metadata = JsonConvert.DeserializeObject<MeasurementMetadata>(json);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("DeserializedSource", metadata.Source);
            Assert.Equal("DeserializedName", metadata.Name);
            Assert.Equal("DeserializedShortDesc", metadata.ShortDescription);
            Assert.Equal("DeserializedLongDesc", metadata.LongDescription);
            Assert.Equal("n1", metadata.Format);
            Assert.Equal(Operation.Delta, metadata.Reduce);
            Assert.Equal(Operation.All, metadata.Aggregate);
        }

        /// <summary>
        /// Verifies that assigning null values to string properties in MeasurementMetadata is handled correctly.
        /// This test sets string properties to null and checks that they return null without any exception.
        /// </summary>
        [Fact]
        public void NullProperties_SetNull_ShouldReturnNull()
        {
            // Arrange
            var metadata = new MeasurementMetadata();

            // Act
            metadata.Source = null;
            metadata.Name = null;
            metadata.ShortDescription = null;
            metadata.LongDescription = null;
            metadata.Format = null;

            // Assert
            Assert.Null(metadata.Source);
            Assert.Null(metadata.Name);
            Assert.Null(metadata.ShortDescription);
            Assert.Null(metadata.LongDescription);
            Assert.Null(metadata.Format);
        }
    }
}
