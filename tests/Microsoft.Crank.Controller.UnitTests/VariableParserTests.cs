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
        /// Tests that the TargetType property returns the expected type.
        /// </summary>
        [Fact]
        public void TargetType_WhenCalled_ReturnsValueTupleStringJTokenType()
        {
            // Act
            var targetType = _parser.TargetType;

            // Assert
            Assert.Equal(typeof(ValueTuple<string, JToken>), targetType);
        }

        /// <summary>
        /// Tests that Parse returns the default tuple when provided with a null value.
        /// </summary>
        [Fact]
        public void Parse_NullValue_ReturnsDefaultTuple()
        {
            // Arrange
            string argName = "variable";
            string value = null;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            Assert.Equal(default(ValueTuple<string, JToken>), result);
        }

        /// <summary>
        /// Tests that Parse returns the default tuple when provided with a whitespace string.
        /// </summary>
        [Fact]
        public void Parse_WhitespaceValue_ReturnsDefaultTuple()
        {
            // Arrange
            string argName = "variable";
            string value = "   ";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            Assert.Equal(default(ValueTuple<string, JToken>), result);
        }

        /// <summary>
        /// Tests that Parse correctly parses a valid input containing a JSON number.
        /// </summary>
        [Fact]
        public void Parse_ValidInputWithNumber_ReturnsExpectedTuple()
        {
            // Arrange
            string argName = "variable";
            string value = "key=123";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            var tupleResult = ((string, JToken))result;
            Assert.Equal("key", tupleResult.Item1);
            Assert.Equal(JTokenType.Integer, tupleResult.Item2.Type);
            Assert.Equal(123, tupleResult.Item2.Value<int>());
        }

        /// <summary>
        /// Tests that Parse correctly parses a valid input containing a JSON object.
        /// </summary>
        [Fact]
        public void Parse_ValidInputWithObject_ReturnsExpectedTuple()
        {
            // Arrange
            string argName = "variable";
            string value = "config = { \"setting\": true }";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            var tupleResult = ((string, JToken))result;
            Assert.Equal("config", tupleResult.Item1);
            Assert.Equal(JTokenType.Object, tupleResult.Item2.Type);
            Assert.True(tupleResult.Item2["setting"].Value<bool>());
        }

        /// <summary>
        /// Tests that Parse throws a FormatException when the input does not contain an '=' character.
        /// </summary>
        [Fact]
        public void Parse_InputMissingEqualSign_ThrowsFormatException()
        {
            // Arrange
            string argName = "variable";
            string value = "invalidInput";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            FormatException exception = Assert.Throws<FormatException>(() => _parser.Parse(argName, value, culture));
            Assert.Contains($"Invalid {argName} argument: 'invalidInput'", exception.Message);
        }

        /// <summary>
        /// Tests that Parse throws a FormatException when the JSON value part is invalid.
        /// </summary>
        [Fact]
        public void Parse_InvalidJsonValue_ThrowsFormatException()
        {
            // Arrange
            string argName = "variable";
            string value = "key=notjson";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            FormatException exception = Assert.Throws<FormatException>(() => _parser.Parse(argName, value, culture));
            Assert.Contains($"Invalid {argName} argument: 'key'", exception.Message);
        }

        /// <summary>
        /// Tests that Parse throws a FormatException when the JSON value is empty after the '=' sign.
        /// </summary>
        [Fact]
        public void Parse_EmptyJsonValueAfterEqual_ThrowsFormatException()
        {
            // Arrange
            string argName = "variable";
            string value = "key=";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            FormatException exception = Assert.Throws<FormatException>(() => _parser.Parse(argName, value, culture));
            Assert.Contains($"Invalid {argName} argument: 'key'", exception.Message);
        }

        /// <summary>
        /// Tests that Parse correctly handles a JSON string that itself contains an '=' character.
        /// </summary>
        [Fact]
        public void Parse_ValidInputWithJsonStringContainingEqual_ReturnsExpectedTuple()
        {
            // Arrange
            string argName = "variable";
            // The JSON string needs to be properly quoted.
            string value = "key=\"value=more\"";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _parser.Parse(argName, value, culture);

            // Assert
            var tupleResult = ((string, JToken))result;
            Assert.Equal("key", tupleResult.Item1);
            Assert.Equal(JTokenType.String, tupleResult.Item2.Type);
            Assert.Equal("value=more", tupleResult.Item2.Value<string>());
        }
    }
}
