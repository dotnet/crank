using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Rule"/> class.
    /// </summary>
    [TestClass]
    public class RuleTests
    {
        private readonly Rule _rule;

        public RuleTests()
        {
            _rule = new Rule();
        }

        /// <summary>
        /// Tests the <see cref="Rule.Include"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void Include_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            string expectedValue = "includePattern";

            // Act
            _rule.Include = expectedValue;
            string actualValue = _rule.Include;

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The Include property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.Exclude"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void Exclude_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            string expectedValue = "excludePattern";

            // Act
            _rule.Exclude = expectedValue;
            string actualValue = _rule.Exclude;

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The Exclude property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.Labels"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void Labels_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            var expectedValue = new List<string> { "label1", "label2" };

            // Act
            _rule.Labels = expectedValue;
            var actualValue = _rule.Labels;

            // Assert
            CollectionAssert.AreEqual(expectedValue, actualValue, "The Labels property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.Owners"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void Owners_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            var expectedValue = new List<string> { "owner1", "owner2" };

            // Act
            _rule.Owners = expectedValue;
            var actualValue = _rule.Owners;

            // Assert
            CollectionAssert.AreEqual(expectedValue, actualValue, "The Owners property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.IgnoreRegressions"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void IgnoreRegressions_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            bool? expectedValue = true;

            // Act
            _rule.IgnoreRegressions = expectedValue;
            bool? actualValue = _rule.IgnoreRegressions;

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The IgnoreRegressions property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.IgnoreErrors"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void IgnoreErrors_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            bool? expectedValue = true;

            // Act
            _rule.IgnoreErrors = expectedValue;
            bool? actualValue = _rule.IgnoreErrors;

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The IgnoreErrors property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Rule.IgnoreFailures"/> property to ensure it correctly sets and gets the value.
        /// </summary>
        [TestMethod]
        public void IgnoreFailures_SetAndGetValue_ReturnsCorrectValue()
        {
            // Arrange
            bool? expectedValue = true;

            // Act
            _rule.IgnoreFailures = expectedValue;
            bool? actualValue = _rule.IgnoreFailures;

            // Assert
            Assert.AreEqual(expectedValue, actualValue, "The IgnoreFailures property did not return the expected value.");
        }
    }
}
