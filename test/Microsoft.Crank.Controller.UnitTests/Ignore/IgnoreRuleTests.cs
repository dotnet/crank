using Microsoft.Crank.Controller.Ignore;
using System;
using Xunit;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GitFile"/> class.
    /// </summary>
    public class GitFileTests
    {
        /// <summary>
        /// Tests that the GitFile constructor converts backslashes to forward slashes and sets properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithBackslashes_ReplacesWithForwardSlashes()
        {
            // Arrange
            string inputPath = "folder\\subfolder\\file.txt";
            string expectedPath = "folder/subfolder/file.txt";

            // Act
            var gitFile = new GitFile(inputPath);

            // Assert
            Assert.Equal(expectedPath, gitFile.Path);
            Assert.False(gitFile.IsDirectory);
            Assert.Equal(expectedPath, gitFile.ToString());
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="GitDirectory"/> class.
    /// </summary>
    public class GitDirectoryTests
    {
        /// <summary>
        /// Tests that the GitDirectory constructor converts backslashes to forward slashes and sets properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithBackslashes_ReplacesWithForwardSlashes()
        {
            // Arrange
            string inputPath = "folder\\subfolder";
            string expectedPath = "folder/subfolder";

            // Act
            var gitDirectory = new GitDirectory(inputPath);

            // Assert
            Assert.Equal(expectedPath, gitDirectory.Path);
            Assert.True(gitDirectory.IsDirectory);
            Assert.Equal(expectedPath, gitDirectory.ToString());
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="IgnoreRule"/> class.
    /// </summary>
    public class IgnoreRuleTests
    {
        private readonly string _basePath;

        public IgnoreRuleTests()
        {
            _basePath = "repo/";
        }

        /// <summary>
        /// Tests that calling Parse with an empty rule string throws an ArgumentException.
        /// </summary>
        [Fact]
        public void Parse_EmptyRule_ThrowsArgumentException()
        {
            // Arrange
            string rule = string.Empty;

            // Act & Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() => IgnoreRule.Parse(_basePath, rule));
            Assert.Equal("Invalid empty rule", ex.Message);
        }

        /// <summary>
        /// Tests that calling Parse with a rule that is a single backslash returns null.
        /// </summary>
        [Fact]
        public void Parse_SingleBackslashRule_ReturnsNull()
        {
            // Arrange
            string rule = "\\";

            // Act
            var result = IgnoreRule.Parse(_basePath, rule);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that calling Parse with a rule starting with an unescaped exclamation mark sets Negate to true.
        /// </summary>
        [Fact]
        public void Parse_RuleStartingWithExclamation_SetsNegateTrue()
        {
            // Arrange
            string rule = "!foo";

            // Act
            var parsedRule = IgnoreRule.Parse(_basePath, rule);

            // Assert
            Assert.NotNull(parsedRule);
            Assert.True(parsedRule.Negate);
        }

        /// <summary>
        /// Tests that calling Parse with a rule starting with an escaped exclamation mark does not set Negate.
        /// </summary>
        [Fact]
        public void Parse_RuleStartingWithEscapedExclamation_DoesNotSetNegate()
        {
            // Arrange
            string rule = @"\!foo";

            // Act
            var parsedRule = IgnoreRule.Parse(_basePath, rule);

            // Assert
            Assert.NotNull(parsedRule);
            Assert.False(parsedRule.Negate);
        }

        /// <summary>
        /// Tests that Match returns true when the file path matches the parsed rule pattern.
        /// </summary>
        [Fact]
        public void Match_FileMatchesPattern_ReturnsTrue()
        {
            // Arrange
            string rule = "/foo";
            var ignoreRule = IgnoreRule.Parse(_basePath, rule);
            var file = new GitFile("repo/foo");

            // Act
            bool isMatch = ignoreRule.Match(file);

            // Assert
            Assert.True(isMatch);
        }

        /// <summary>
        /// Tests that Match returns false when the file path does not start with the base path.
        /// </summary>
        [Fact]
        public void Match_FilePathNotStartingWithBasePath_ReturnsFalse()
        {
            // Arrange
            string rule = "/foo";
            var ignoreRule = IgnoreRule.Parse(_basePath, rule);
            var file = new GitFile("other/foo");

            // Act
            bool isMatch = ignoreRule.Match(file);

            // Assert
            Assert.False(isMatch);
        }

        /// <summary>
        /// Tests that Match returns false for a directory when the parsed rule is intended for files only.
        /// </summary>
        [Fact]
        public void Match_DirectoryWhenRuleForFilesOnly_ReturnsFalse()
        {
            // Arrange
            // A rule ending with "/**" sets _matchDir to false.
            string rule = "/folder/**";
            var ignoreRule = IgnoreRule.Parse(_basePath, rule);
            var directory = new GitDirectory("repo/folder");

            // Act
            bool isMatch = ignoreRule.Match(directory);

            // Assert
            Assert.False(isMatch);
        }

        /// <summary>
        /// Tests that Match returns true for a file matching the exact pattern.
        /// </summary>
        [Fact]
        public void Match_FileMatchesExactPattern_ReturnsTrue()
        {
            // Arrange
            string rule = "/folder";
            var ignoreRule = IgnoreRule.Parse(_basePath, rule);
            var file = new GitFile("repo/folder");

            // Act
            bool isMatch = ignoreRule.Match(file);

            // Assert
            Assert.True(isMatch);
        }

        /// <summary>
        /// Tests that the ToString method returns the generated regex pattern.
        /// </summary>
        [Fact]
        public void ToString_ReturnsGeneratedRegexPattern()
        {
            // Arrange
            string rule = "/foo*bar?baz";
            var ignoreRule = IgnoreRule.Parse(_basePath, rule);

            // Act
            var pattern = ignoreRule.ToString();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(pattern));
        }
    }
}
