// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Crank.RegressionBot;
using System;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Microsoft.Crank.RegressionBot.UnitTests
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
        /// Tests that the Resolve method returns true and sets currentType to decimal when provided a plain scalar with a valid decimal value.
        /// </summary>
        [Fact]
        public void Resolve_WhenScalarIsPlainAndDecimalConvertible_ReturnsTrueAndSetsCurrentTypeToDecimal()
        {
            // Arrange
            var scalarValue = "123.45";
            var scalar = new Scalar(string.Empty, string.Empty, scalarValue, ScalarStyle.Plain, true, false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(decimal), currentType);
        }

        /// <summary>
        /// Tests that the Resolve method returns true and sets currentType to bool when provided a plain scalar with a valid boolean value.
        /// </summary>
        /// <param name="value">The string representation of the boolean value.</param>
        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("True")]
        [InlineData("False")]
        public void Resolve_WhenScalarIsPlainAndBooleanConvertible_ReturnsTrueAndSetsCurrentTypeToBoolean(string value)
        {
            // Arrange
            var scalar = new Scalar(string.Empty, string.Empty, value, ScalarStyle.Plain, true, false);
            Type currentType = null;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.True(result);
            Assert.Equal(typeof(bool), currentType);
        }

        /// <summary>
        /// Tests that the Resolve method returns false and does not change currentType when the scalar's plain implicit flag is false.
        /// </summary>
        [Fact]
        public void Resolve_WhenScalarIsNotPlainImplicit_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            var scalar = new Scalar(string.Empty, string.Empty, "123.45", ScalarStyle.Plain, false, false);
            Type initialType = typeof(string);
            Type currentType = initialType;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Equal(initialType, currentType);
        }

        /// <summary>
        /// Tests that the Resolve method returns false and does not change currentType when the scalar value cannot be converted to either decimal or bool.
        /// </summary>
        [Fact]
        public void Resolve_WhenScalarIsPlainAndValueNotConvertible_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            var scalar = new Scalar(string.Empty, string.Empty, "notanumber", ScalarStyle.Plain, true, false);
            Type initialType = null;
            Type currentType = initialType;

            // Act
            bool result = _jsonTypeResolver.Resolve(scalar, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Equal(initialType, currentType);
        }

        /// <summary>
        /// Tests that the Resolve method returns false and does not change currentType when the node event is not a scalar.
        /// </summary>
        [Fact]
        public void Resolve_WhenNodeEventIsNotAScalar_ReturnsFalseAndDoesNotChangeCurrentType()
        {
            // Arrange
            // SequenceStart is used as an example of a non-scalar NodeEvent.
            var nonScalarEvent = new SequenceStart(string.Empty, string.Empty, false, SequenceStyle.Block);
            Type initialType = null;
            Type currentType = initialType;

            // Act
            bool result = _jsonTypeResolver.Resolve(nonScalarEvent, ref currentType);

            // Assert
            Assert.False(result);
            Assert.Equal(initialType, currentType);
        }
    }
}
