using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="VariableParser"/> class.
    /// </summary>
    [TestClass]
    public class VariableParserTests
    {
        private readonly VariableParser _variableParser;

        public VariableParserTests()
        {
            _variableParser = new VariableParser();
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it returns the default value when the input value is null or whitespace.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Parse_WhenValueIsNullOrWhitespace_ReturnsDefaultValue(string value)
        {
            // Arrange
            string argName = "testArg";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _variableParser.Parse(argName, value, culture);

            // Assert
            Assert.AreEqual(default(ValueTuple<string, JToken>), result);
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it correctly parses a valid JSON value.
        /// </summary>
        [TestMethod]
        public void Parse_WhenValueIsValidJson_ReturnsParsedValue()
        {
            // Arrange
            string argName = "testArg";
            string value = "key={\"name\":\"value\"}";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _variableParser.Parse(argName, value, culture);

            // Assert
            var expected = ("key", JToken.Parse("{\"name\":\"value\"}"));
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Tests the <see cref="VariableParser.Parse(string, string, CultureInfo)"/> method to ensure it throws a <see cref="FormatException"/> when the value is not a valid JSON.
        /// </summary>
        [TestMethod]
        public void Parse_WhenValueIsInvalidJson_ThrowsFormatException()
        {
            // Arrange
            string argName = "testArg";
            string value = "key=invalidJson";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act & Assert
            var exception = Assert.ThrowsException<FormatException>(() => _variableParser.Parse(argName, value, culture));
            Assert.AreEqual($"Invalid {argName} argument: 'key' is not a valid JSON value.", exception.Message);
        }
    }
}
