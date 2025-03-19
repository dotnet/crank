using Moq;
using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GitFile"/> class.
    /// </summary>
    public class GitFileTests
    {
        private readonly GitFile _gitFile;

        public GitFileTests()
        {
            _gitFile = new GitFile("test/path");
        }

        /// <summary>
        /// Tests the <see cref="GitFile.IsDirectory"/> property to ensure it always returns false.
        /// </summary>
        [Fact]
        public void IsDirectory_Always_ReturnsFalse()
        {
            // Act
            var result = _gitFile.IsDirectory;

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests the <see cref="GitFile.Path"/> property to ensure it returns the correct path.
        /// </summary>
        [Fact]
        public void Path_WhenCalled_ReturnsCorrectPath()
        {
            // Act
            var result = _gitFile.Path;

            // Assert
            Assert.Equal("test/path", result);
        }

        /// <summary>
        /// Tests the <see cref="GitFile.ToString"/> method to ensure it returns the correct path.
        /// </summary>
        [Fact]
        public void ToString_WhenCalled_ReturnsCorrectPath()
        {
            // Act
            var result = _gitFile.ToString();

            // Assert
            Assert.Equal("test/path", result);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="GitDirectory"/> class.
    /// </summary>
    public class GitDirectoryTests
    {
        private readonly GitDirectory _gitDirectory;

        public GitDirectoryTests()
        {
            _gitDirectory = new GitDirectory("test/path");
        }

        /// <summary>
        /// Tests the <see cref="GitDirectory.IsDirectory"/> property to ensure it always returns true.
        /// </summary>
        [Fact]
        public void IsDirectory_Always_ReturnsTrue()
        {
            // Act
            var result = _gitDirectory.IsDirectory;

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests the <see cref="GitDirectory.Path"/> property to ensure it returns the correct path.
        /// </summary>
        [Fact]
        public void Path_WhenCalled_ReturnsCorrectPath()
        {
            // Act
            var result = _gitDirectory.Path;

            // Assert
            Assert.Equal("test/path", result);
        }

        /// <summary>
        /// Tests the <see cref="GitDirectory.ToString"/> method to ensure it returns the correct path.
        /// </summary>
        [Fact]
        public void ToString_WhenCalled_ReturnsCorrectPath()
        {
            // Act
            var result = _gitDirectory.ToString();

            // Assert
            Assert.Equal("test/path", result);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="IgnoreRule"/> class.
    /// </summary>
    public class IgnoreRuleTests
    {
        private readonly IgnoreRule _ignoreRule;

        public IgnoreRuleTests()
        {
            _ignoreRule = IgnoreRule.Parse("base/path", "/*.txt");
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match"/> method to ensure it correctly matches files.
        /// </summary>
        [Fact]
        public void Match_FileMatchesPattern_ReturnsTrue()
        {
            // Arrange
            var mockFile = new Mock<IGitFile>();
            mockFile.Setup(f => f.IsDirectory).Returns(false);
            mockFile.Setup(f => f.Path).Returns("base/path/file.txt");

            // Act
            var result = _ignoreRule.Match(mockFile.Object);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match"/> method to ensure it correctly does not match directories when _matchDir is false.
        /// </summary>
        [Fact]
        public void Match_DirectoryWhenMatchDirIsFalse_ReturnsFalse()
        {
            // Arrange
            var ignoreRule = IgnoreRule.Parse("base/path", "/**");
            var mockFile = new Mock<IGitFile>();
            mockFile.Setup(f => f.IsDirectory).Returns(true);
            mockFile.Setup(f => f.Path).Returns("base/path/dir");

            // Act
            var result = ignoreRule.Match(mockFile.Object);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Match"/> method to ensure it correctly does not match files outside the base path.
        /// </summary>
        [Fact]
        public void Match_FileOutsideBasePath_ReturnsFalse()
        {
            // Arrange
            var mockFile = new Mock<IGitFile>();
            mockFile.Setup(f => f.IsDirectory).Returns(false);
            mockFile.Setup(f => f.Path).Returns("other/path/file.txt");

            // Act
            var result = _ignoreRule.Match(mockFile.Object);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse"/> method to ensure it throws an exception for an empty rule.
        /// </summary>
        [Fact]
        public void Parse_EmptyRule_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => IgnoreRule.Parse("base/path", ""));
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.Parse"/> method to ensure it correctly parses a valid rule.
        /// </summary>
        [Fact]
        public void Parse_ValidRule_ReturnsIgnoreRule()
        {
            // Act
            var result = IgnoreRule.Parse("base/path", "/*.txt");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/*.txt", result.Rule);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreRule.ToString"/> method to ensure it returns the correct pattern.
        /// </summary>
        [Fact]
        public void ToString_WhenCalled_ReturnsCorrectPattern()
        {
            // Act
            var result = _ignoreRule.ToString();

            // Assert
            Assert.Equal("^/[^/]*\\.txt(/|$)", result);
        }
    }
}
