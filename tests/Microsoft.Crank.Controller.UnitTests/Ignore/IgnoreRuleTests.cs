using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="IgnoreRule"/> class.
    /// </summary>
    [TestClass]
    public class IgnoreRuleTests
    {
        private const string BasePath = "/base/path";
        private readonly Mock<IGitFile> _mockGitFile;

        public IgnoreRuleTests()
        {
            _mockGitFile = new Mock<IGitFile>();
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match(IGitFile)"/> method to ensure it correctly matches a file path.
        /// </summary>
        [TestMethod]
        public void Match_FilePathMatches_ReturnsTrue()
        {
            // Arrange
            var rule = IgnoreRule.Parse(BasePath, "*.txt");
            _mockGitFile.Setup(f => f.Path).Returns("/base/path/file.txt");
            _mockGitFile.Setup(f => f.IsDirectory).Returns(false);

            // Act
            var result = rule.Match(_mockGitFile.Object);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match(IGitFile)"/> method to ensure it does not match a directory when _matchDir is false.
        /// </summary>
        [TestMethod]
        public void Match_DirectoryWhenMatchDirIsFalse_ReturnsFalse()
        {
            // Arrange
            var rule = IgnoreRule.Parse(BasePath, "*.txt");
            _mockGitFile.Setup(f => f.Path).Returns("/base/path/directory");
            _mockGitFile.Setup(f => f.IsDirectory).Returns(true);

            // Act
            var result = rule.Match(_mockGitFile.Object);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match(IGitFile)"/> method to ensure it does not match a file path outside the base path.
        /// </summary>
        [TestMethod]
        public void Match_FilePathOutsideBasePath_ReturnsFalse()
        {
            // Arrange
            var rule = IgnoreRule.Parse(BasePath, "*.txt");
            _mockGitFile.Setup(f => f.Path).Returns("/other/path/file.txt");
            _mockGitFile.Setup(f => f.IsDirectory).Returns(false);

            // Act
            var result = rule.Match(_mockGitFile.Object);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse(string, string)"/> method to ensure it throws an exception for an empty rule.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Parse_EmptyRule_ThrowsArgumentException()
        {
            // Act
            IgnoreRule.Parse(BasePath, string.Empty);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse(string, string)"/> method to ensure it correctly parses a negated rule.
        /// </summary>
        [TestMethod]
        public void Parse_NegatedRule_SetsNegateToTrue()
        {
            // Act
            var rule = IgnoreRule.Parse(BasePath, "!*.txt");

            // Assert
            Assert.IsTrue(rule.Negate);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse(string, string)"/> method to ensure it correctly parses a rule with a leading slash.
        /// </summary>
        [TestMethod]
        public void Parse_RuleWithLeadingSlash_CorrectlyParsesPattern()
        {
            // Act
            var rule = IgnoreRule.Parse(BasePath, "/file.txt");

            // Assert
            Assert.AreEqual("^file\\.txt(/|$)", rule.ToString());
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse(string, string)"/> method to ensure it correctly parses a rule with double asterisks.
        /// </summary>
        [TestMethod]
        public void Parse_RuleWithDoubleAsterisks_CorrectlyParsesPattern()
        {
            // Act
            var rule = IgnoreRule.Parse(BasePath, "**/file.txt");

            // Assert
            Assert.AreEqual("(/|^)\\DOT_STAR\\/file\\.txt(/|$)", rule.ToString());
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.ToString()"/> method to ensure it returns the correct pattern.
        /// </summary>
        [TestMethod]
        public void ToString_ReturnsCorrectPattern()
        {
            // Arrange
            var rule = IgnoreRule.Parse(BasePath, "*.txt");

            // Act
            var pattern = rule.ToString();

            // Assert
            Assert.AreEqual("[^/]*\\.txt(/|$)", pattern);
        }
    }
}
