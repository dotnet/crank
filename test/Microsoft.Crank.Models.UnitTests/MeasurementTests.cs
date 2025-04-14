using System;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Measurement"/> class.
    /// </summary>
    public class MeasurementTests
    {
        /// <summary>
        /// Tests that the Delimiter constant has the expected value.
        /// </summary>
        [Fact]
        public void DelimiterConstant_Value_IsExpected()
        {
            // Arrange
            string expectedDelimiter = "$$Delimiter$$";
            
            // Act
            string actualDelimiter = Measurement.Delimiter;
            
            // Assert
            Assert.Equal(expectedDelimiter, actualDelimiter);
        }

        /// <summary>
        /// Tests that the IsDelimiter property returns true when the Name exactly matches the delimiter ignoring case,
        /// and returns false otherwise.
        /// </summary>
        /// <param name="name">The input name value for the Measurement instance.</param>
        /// <param name="expectedResult">The expected outcome for the IsDelimiter property.</param>
        [Theory]
        [InlineData("$$Delimiter$$", true)]
        [InlineData("$$delimiter$$", true)]
        [InlineData("$$DeLiMiTeR$$", true)]
        [InlineData("Other", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsDelimiter_Property_NameComparison_ReturnsExpected(string name, bool expectedResult)
        {
            // Arrange
            Measurement measurement = new Measurement
            {
                Name = name
            };

            // Act
            bool result = measurement.IsDelimiter;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests that the Timestamp property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Timestamp_Property_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            Measurement measurement = new Measurement();
            DateTime expectedTimestamp = DateTime.UtcNow;

            // Act
            measurement.Timestamp = expectedTimestamp;
            DateTime actualTimestamp = measurement.Timestamp;

            // Assert
            Assert.Equal(expectedTimestamp, actualTimestamp);
        }

        /// <summary>
        /// Tests that the Name property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Name_Property_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            Measurement measurement = new Measurement();
            string expectedName = "TestName";

            // Act
            measurement.Name = expectedName;
            string actualName = measurement.Name;

            // Assert
            Assert.Equal(expectedName, actualName);
        }

        /// <summary>
        /// Tests that the Value property can be set and retrieved correctly, including scenarios with null and different types.
        /// </summary>
        [Fact]
        public void Value_Property_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            Measurement measurement = new Measurement();
            object expectedStringValue = "TestValue";
            object expectedIntValue = 123;
            object expectedNullValue = null;

            // Act & Assert for string value
            measurement.Value = expectedStringValue;
            Assert.Equal(expectedStringValue, measurement.Value);

            // Act & Assert for integer value
            measurement.Value = expectedIntValue;
            Assert.Equal(expectedIntValue, measurement.Value);

            // Act & Assert for null value
            measurement.Value = expectedNullValue;
            Assert.Null(measurement.Value);
        }
    }
}
