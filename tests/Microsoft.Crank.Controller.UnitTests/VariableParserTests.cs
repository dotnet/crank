using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="VariableParser"/> class.
    /// </summary>
    public class VariableParserTests
    {
        private readonly VariableParser _variableParser;

        public VariableParserTests()
        {
            _variableParser = new VariableParser();
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.TargetType"/> property to ensure it returns the correct type.
        /// </summary>
        [Fact]
        public void TargetType_ReturnsCorrectType()
        {
            // Act
            var targetType = _variableParser.TargetType;

            // Assert
            Assert.Equal(typeof(ValueTuple<string, JToken>), targetType);
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it returns the default value when the input value is null or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_ValueIsNullOrWhitespace_ReturnsDefault(string value)
        {
            // Act
            var result = _variableParser.Parse("argName", value, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(default(ValueTuple<string, JToken>), result);
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it correctly parses a valid input string.
        /// </summary>
        [Fact]
        public void Parse_ValidInput_ReturnsParsedValue()
        {
            // Arrange
            var input = "key={\"name\":\"value\"}";
            var expectedKey = "key";
            var expectedValue = JToken.Parse("{\"name\":\"value\"}");

            // Act
            var result = _variableParser.Parse("argName", input, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal((expectedKey, expectedValue), result);
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it throws a <see cref="FormatException"/> when the input string is not a valid JSON.
        /// </summary>
        [Fact]
        public void Parse_InvalidJson_ThrowsFormatException()
        {
            // Arrange
            var input = "key=invalidJson";

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => _variableParser.Parse("argName", input, CultureInfo.InvariantCulture));
            Assert.Contains("Invalid argName argument: 'key' is not a valid JSON value.", exception.Message);
        }
    }
}
