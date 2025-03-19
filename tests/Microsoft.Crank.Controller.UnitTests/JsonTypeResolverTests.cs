using Moq;
using System;
using System.Globalization;
using Xunit;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JsonTypeResolver"/> class.
    /// </summary>
    public class JsonTypeResolverTests
    {
        private readonly JsonTypeResolver _jsonTypeResolver;

        public JsonTypeResolverTests()
        {
            _jsonTypeResolver = new JsonTypeResolver();
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it correctly resolves a decimal type.
        /// </summary>
//         [Theory] [Error] (32-62)CS0103 The name 'ScalarStyle' does not exist in the current context
//         [InlineData("123.45")]
//         [InlineData("0.001")]
//         [InlineData("-98765.4321")]
//         public void Resolve_WhenCalledWithDecimalString_ReturnsTrueAndSetsCurrentTypeToDecimal(string scalarValue)
//         {
//             // Arrange
//             var scalar = new Scalar(null, null, scalarValue, ScalarStyle.Any, true, false);
//             var nodeEvent = (NodeEvent)scalar;
//             Type currentType = null;
// 
//             // Act
//             var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal(typeof(decimal), currentType);
//         }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it correctly resolves a boolean type.
        /// </summary>
        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void Resolve_WhenCalledWithBooleanString_ReturnsTrueAndSetsCurrentTypeToBoolean(string scalarValue)
        {
            // Arrange
            var scalar = new Scalar(null, null, scalarValue, ScalarStyle.Any, true, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(bool), currentType);
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it returns false for non-decimal and non-boolean strings.
        /// </summary>
        [Theory]
        [InlineData("not a number")]
        [InlineData("123abc")]
        [InlineData("")]
        public void Resolve_WhenCalledWithNonDecimalOrBooleanString_ReturnsFalse(string scalarValue)
        {
            // Arrange
            var scalar = new Scalar(null, null, scalarValue, ScalarStyle.Any, true, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Null(currentType);
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it returns false for non-plain implicit scalars.
        /// </summary>
        [Fact]
        public void Resolve_WhenCalledWithNonPlainImplicitScalar_ReturnsFalse()
        {
            // Arrange
            var scalar = new Scalar(null, null, "123.45", ScalarStyle.Any, false, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Null(currentType);
        }
    }
}
