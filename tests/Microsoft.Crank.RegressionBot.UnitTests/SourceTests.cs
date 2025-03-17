using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Source"/> class.
    /// </summary>
    [TestClass]
    public class SourceTests
    {
        private readonly Source _source;

        public SourceTests()
        {
            _source = new Source();
        }

        /// <summary>
        /// Tests the <see cref="Source.Match(string)"/> method to ensure it correctly matches rules based on the descriptor.
        /// </summary>
        [TestMethod]
        public void Match_WhenCalledWithMatchingDescriptor_ReturnsMatchingRules()
        {
            // Arrange
            var rule1 = new Rule { Include = "test" };
            var rule2 = new Rule { Include = "sample" };
            _source.Rules.Add(rule1);
            _source.Rules.Add(rule2);
            string descriptor = "test";

            // Act
            var result = _source.Match(descriptor);

            // Assert
            CollectionAssert.Contains(new List<Rule>(result), rule1);
            CollectionAssert.DoesNotContain(new List<Rule>(result), rule2);
        }

        /// <summary>
        /// Tests the <see cref="Source.Match(string)"/> method to ensure it returns an empty collection when no rules match.
        /// </summary>
        [TestMethod]
        public void Match_WhenCalledWithNonMatchingDescriptor_ReturnsEmptyCollection()
        {
            // Arrange
            var rule = new Rule { Include = "test" };
            _source.Rules.Add(rule);
            string descriptor = "nomatch";

            // Act
            var result = _source.Match(descriptor);

            // Assert
            Assert.AreEqual(0, new List<Rule>(result).Count);
        }

        /// <summary>
        /// Tests the <see cref="Source.Include(string)"/> method to ensure it correctly includes descriptors based on the rules.
        /// </summary>
        [TestMethod]
        public void Include_WhenCalledWithMatchingIncludeRule_ReturnsTrue()
        {
            // Arrange
            var rule = new Rule { Include = "test" };
            _source.Rules.Add(rule);
            string descriptor = "test";

            // Act
            var result = _source.Include(descriptor);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="Source.Include(string)"/> method to ensure it correctly excludes descriptors based on the rules.
        /// </summary>
        [TestMethod]
        public void Include_WhenCalledWithMatchingExcludeRule_ReturnsFalse()
        {
            // Arrange
            var rule = new Rule { Exclude = "test" };
            _source.Rules.Add(rule);
            string descriptor = "test";

            // Act
            var result = _source.Include(descriptor);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="Source.Include(string)"/> method to ensure it returns false when no rules match.
        /// </summary>
        [TestMethod]
        public void Include_WhenCalledWithNonMatchingDescriptor_ReturnsFalse()
        {
            // Arrange
            var rule = new Rule { Include = "test" };
            _source.Rules.Add(rule);
            string descriptor = "nomatch";

            // Act
            var result = _source.Include(descriptor);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
