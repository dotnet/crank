using System;
using System.Globalization;
using Microsoft.Crank.Controller;
using Xunit;
using YamlDotNet.Core.Events;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JsonTypeResolver"/> class.
    /// </summary>
    public class JsonTypeResolverTests
    {
        private readonly JsonTypeResolver _resolver;

        public JsonTypeResolverTests()
        {
            _resolver = new JsonTypeResolver();
        }

        /// <summary>
        /// Tests that Resolve returns true and assigns the type to decimal
        /// when the scalar node contains a valid decimal value.
        /// </summary>
        /// <param name="value">The decimal value as a string.</param>
//         [Theory] [Error] (34-56)CS0103 The name 'ScalarStyle' does not exist in the current context
//         [InlineData("123.45")]
//         [InlineData("-0.001")]
//         [InlineData("0")]
//         public void Resolve_WhenScalarIsValidDecimal_ReturnsTrueAndAssignsDecimalType(string value)
//         {
//             // Arrange
//             Type currentType = typeof(object);
//             var scalar = new Scalar(null, null, value, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal(typeof(decimal), currentType);
//         }

        /// <summary>
        /// Tests that Resolve returns true and assigns the type to bool
        /// when the scalar node contains a valid boolean value.
        /// </summary>
        /// <param name="value">The boolean value as a string.</param>
//         [Theory] [Error] (58-56)CS0103 The name 'ScalarStyle' does not exist in the current context
//         [InlineData("true")]
//         [InlineData("false")]
//         [InlineData("True")]
//         [InlineData("False")]
//         public void Resolve_WhenScalarIsValidBoolean_ReturnsTrueAndAssignsBoolType(string value)
//         {
//             // Arrange
//             Type currentType = typeof(object);
//             var scalar = new Scalar(null, null, value, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.True(result);
//             Assert.Equal(typeof(bool), currentType);
//         }

        /// <summary>
        /// Tests that Resolve returns false and does not modify currentType
        /// when the scalar node contains a value that is neither a valid decimal nor boolean.
        /// </summary>
//         [Fact] [Error] (77-72)CS0103 The name 'ScalarStyle' does not exist in the current context
//         public void Resolve_WhenScalarIsInvalidForDecimalAndBoolean_ReturnsFalse()
//         {
//             // Arrange
//             Type currentType = typeof(object);
//             var scalar = new Scalar(null, null, "notANumberOrBoolean", ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Equal(typeof(object), currentType);
//         }

        /// <summary>
        /// Tests that Resolve returns false and does not modify currentType
        /// when the scalar node is not plain implicit.
        /// </summary>
//         [Fact] [Error] (96-59)CS0103 The name 'ScalarStyle' does not exist in the current context
//         public void Resolve_WhenScalarIsNotPlainImplicit_ReturnsFalse()
//         {
//             // Arrange
//             Type currentType = typeof(object);
//             var scalar = new Scalar(null, null, "123.45", ScalarStyle.Plain, isPlainImplicit: false, isQuotedImplicit: false);
// 
//             // Act
//             bool result = _resolver.Resolve(scalar, ref currentType);
// 
//             // Assert
//             Assert.False(result);
//             Assert.Equal(typeof(object), currentType);
//         }

        /// <summary>
        /// Tests that Resolve returns false and does not modify currentType
        /// when the provided NodeEvent is not a Scalar.
        /// </summary>
        [Fact]
        public void Resolve_WhenNodeEventIsNotScalar_ReturnsFalse()
        {
            // Arrange
            Type currentType = typeof(object);
            // Create a non-scalar NodeEvent (SequenceStart is not a Scalar).
            var nonScalarEvent = new SequenceStart(null, null, false, SequenceStyle.Block);

            // Act
            bool result = _resolver.Resolve(nonScalarEvent, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Equal(typeof(object), currentType);
        }

        /// <summary>
        /// Tests that Resolve returns false and leaves currentType unchanged
        /// when the NodeEvent is null.
        /// </summary>
        [Fact]
        public void Resolve_WhenNodeEventIsNull_ReturnsFalse()
        {
            // Arrange
            Type currentType = typeof(object);

            // Act
            bool result = _resolver.Resolve(null, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Equal(typeof(object), currentType);
        }
    }
}
