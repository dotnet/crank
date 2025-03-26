using System;
using System.Globalization;
using Microsoft.Crank.Controller;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="VariableParser"/> class.
    /// </summary>
    public class VariableParserTests
    {
        private readonly VariableParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableParserTests"/> class.
        /// </summary>
        public VariableParserTests()
        {
            _parser = new VariableParser();
        }

        /// <summary>
        /// Tests that the TargetType property returns the expected type of (string, JToken).
        /// </summary>
        [Fact]
        public void TargetType_ShouldReturnExpectedType()
        {
            // Act
            var actualType = _parser.TargetType;

            // Assert
            Assert.Equal(typeof((string, JToken)), actualType);
        }

        /// <summary>
        /// Tests that the Parse method returns the default tuple when provided a null value.
        /// </summary>
        [Fact]
        public void Parse_WithNullValue_ReturnsDefaultTuple()
        {
            // Arrange
            string argName = "variable";
            string value = null;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            Assert.Equal(default((string, JToken)), (ValueTuple<string, JToken>)result);
        }

        /// <summary>
        /// Tests that the Parse method returns the default tuple when provided an empty string.
        /// </summary>
        [Fact]
        public void Parse_WithEmptyString_ReturnsDefaultTuple()
        {
            // Arrange
            string argName = "variable";
            string value = "";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            Assert.Equal(default((string, JToken)), (ValueTuple<string, JToken>)result);
        }

        /// <summary>
        /// Tests that the Parse method returns the default tuple when provided a whitespace string.
        /// </summary>
        [Fact]
        public void Parse_WithWhitespaceString_ReturnsDefaultTuple()
        {
            // Arrange
            string argName = "variable";
            string value = "   ";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            Assert.Equal(default((string, JToken)), (ValueTuple<string, JToken>)result);
        }

        /// <summary>
        /// Tests that the Parse method correctly parses a valid key and JSON value pair.
        /// </summary>
        [Fact]
        public void Parse_WithValidJson_ReturnsParsedTuple()
        {
            // Arrange
            string argName = "variable";
            string key = "myKey";
            string jsonValue = "{\"number\":123}";
            string input = $"{key}={jsonValue}";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, input, culture);
            var parsedResult = ((string, JToken))result;

            // Assert
            Assert.Equal(key, parsedResult.Item1);
            Assert.Equal(123, parsedResult.Item2["number"].Value<int>());
        }

        /// <summary>
        /// Tests that the Parse method throws a FormatException with the proper message when JSON parsing fails.
        /// </summary>
        [Fact]
        public void Parse_WithInvalidJson_ThrowsFormatException()
        {
            // Arrange
            string argName = "variable";
            string key = "myKey";
            string invalidJson = "notAJson";
            string input = $"{key}={invalidJson}";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => _parser.Parse(argName, input, culture));
            Assert.Contains($"Invalid {argName} argument: '{key}' is not a valid JSON value.", exception.Message);
            Assert.NotNull(exception.InnerException);
        }

        /// <summary>
        /// Tests that the Parse method throws an IndexOutOfRangeException when the input string does not contain the '=' separator.
        /// </summary>
        [Fact]
        public void Parse_WithoutEqualSeparator_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            string argName = "variable";
            string input = "missingEqualsSign";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => _parser.Parse(argName, input, culture));
        }
    }
}
