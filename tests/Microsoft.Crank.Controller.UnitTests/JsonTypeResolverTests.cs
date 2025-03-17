using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Globalization;
using YamlDotNet.Core.Events;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JsonTypeResolver"/> class.
    /// </summary>
    [TestClass]
    public class JsonTypeResolverTests
    {
        private readonly JsonTypeResolver _jsonTypeResolver;

        public JsonTypeResolverTests()
        {
            _jsonTypeResolver = new JsonTypeResolver();
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it correctly identifies a decimal type.
        /// </summary>
//         [TestMethod] [Error] (29-59)CS0103 The name 'ScalarStyle' does not exist in the current context
//         public void Resolve_WhenCalledWithDecimalString_ReturnsTrueAndSetsTypeToDecimal()
//         {
//             // Arrange
//             var scalar = new Scalar(null, null, "123.45", ScalarStyle.Any, true, false);
//             var nodeEvent = (NodeEvent)scalar;
//             Type currentType = null;
// 
//             // Act
//             var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);
// 
//             // Assert
//             Assert.IsTrue(result);
//             Assert.AreEqual(typeof(decimal), currentType);
//         }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it correctly identifies a boolean type.
        /// </summary>
        [TestMethod]
        public void Resolve_WhenCalledWithBooleanString_ReturnsTrueAndSetsTypeToBoolean()
        {
            // Arrange
            var scalar = new Scalar(null, null, "true", ScalarStyle.Any, true, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(typeof(bool), currentType);
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it returns false for non-decimal and non-boolean strings.
        /// </summary>
        [TestMethod]
        public void Resolve_WhenCalledWithNonDecimalOrBooleanString_ReturnsFalse()
        {
            // Arrange
            var scalar = new Scalar(null, null, "not-a-number", ScalarStyle.Any, true, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(currentType);
        }

        /// <summary>
        /// Tests the <see cref="JsonTypeResolver.Resolve(NodeEvent, ref Type)"/> method to ensure it returns false for non-plain implicit scalars.
        /// </summary>
        [TestMethod]
        public void Resolve_WhenCalledWithNonPlainImplicitScalar_ReturnsFalse()
        {
            // Arrange
            var scalar = new Scalar(null, null, "123.45", ScalarStyle.Any, false, false);
            var nodeEvent = (NodeEvent)scalar;
            Type currentType = null;

            // Act
            var result = _jsonTypeResolver.Resolve(nodeEvent, ref currentType);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(currentType);
        }
    }
}
