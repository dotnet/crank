using System.Globalization;
using Microsoft.Crank.Controller;
using Xunit;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Core;

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
        /// Tests that when a plain implicit scalar node contains a valid decimal value, 
        /// the resolver sets the current type to decimal and returns true.
        /// </summary>
        [Theory]
        [InlineData("123.45")]
        [InlineData("0")]
        [InlineData("-99.99")]
        public void Resolve_WhenScalarPlainImplicitAndValidDecimal_ReturnsTrueAndSetsCurrentTypeToDecimal(string value)
        {
            // Arrange
            // Creating a Scalar node with IsPlainImplicit = true
            var scalar = new Scalar(null, null, value, ScalarStyle.Any, isPlainImplicit: true, isQuotedImplicit: false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(decimal), currentType);
        }

        /// <summary>
        /// Tests that when a plain implicit scalar node contains a valid boolean value 
        /// (and is not a valid decimal), the resolver sets the current type to bool and returns true.
        /// </summary>
        [Theory]
        [InlineData("true")]
        [InlineData("False")]
        public void Resolve_WhenScalarPlainImplicitAndValidBoolean_ReturnsTrueAndSetsCurrentTypeToBool(string value)
        {
            // Arrange
            // Creating a Scalar node with IsPlainImplicit = true.
            // For a value like "true", decimal.TryParse will fail and bool.TryParse should succeed.
            var scalar = new Scalar(null, null, value, ScalarStyle.Any, isPlainImplicit: true, isQuotedImplicit: false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(bool), currentType);
        }

        /// <summary>
        /// Tests that when a scalar node is not plain implicit, 
        /// the resolver does not perform type resolution and returns false without altering currentType.
        /// </summary>
        [Fact]
        public void Resolve_WhenScalarNotPlainImplicit_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            // Creating a Scalar node with IsPlainImplicit = false.
            var scalar = new Scalar(null, null, "123.45", ScalarStyle.Any, isPlainImplicit: false, isQuotedImplicit: false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Null(currentType);
        }

        /// <summary>
        /// Tests that when a plain implicit scalar node contains a non-parsable value,
        /// the resolver returns false and does not change the current type.
        /// </summary>
        [Fact]
        public void Resolve_WhenScalarPlainImplicitWithNonParsableValue_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            // "hello" cannot be parsed as a number or boolean.
            var scalar = new Scalar(null, null, "hello", ScalarStyle.Any, isPlainImplicit: true, isQuotedImplicit: false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Null(currentType);
        }

        /// <summary>
        /// Tests that when the provided NodeEvent is not a Scalar,
        /// the resolver returns false and does not alter the current type.
        /// </summary>
//         [Fact] [Error] (117-40)CS0029 Cannot implicitly convert type 'YamlDotNet.Core.Events.DocumentStart' to 'YamlDotNet.Core.Events.NodeEvent'
//         public void Resolve_WhenNodeEventNotScalar_ReturnsFalseAndDoesNotChangeCurrentType()
//         {
//             // Arrange
//             // Using a DocumentStart event which is not a Scalar.
//             NodeEvent nonScalarEvent = new DocumentStart();
//             Type currentType = null;
// 
//             // Act
//             bool result = _jsonTypeResolver.Resolve(nonScalarEvent, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Null(currentType);
//         }

        /// <summary>
        /// Tests that when the provided NodeEvent is null,
        /// the resolver returns false and does not change the current type.
        /// </summary>
        [Fact]
        public void Resolve_WhenNodeEventIsNull_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            NodeEvent nullEvent = null;
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(nullEvent, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Null(currentType);
        }
    }
}
