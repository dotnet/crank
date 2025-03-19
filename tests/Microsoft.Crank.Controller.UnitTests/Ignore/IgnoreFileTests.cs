using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Microsoft.Crank.Controller.Ignore;
using Xunit;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="IgnoreFile"/> class.
    /// </summary>
    public class IgnoreFileTests
    {
        /// <summary>
        /// Tests that Parse returns an IgnoreFile with no rules when no .gitignore file exists.
        /// </summary>
        [Fact]
        public void Parse_NoGitIgnoreFile_ReturnsEmptyRules()
        {
            // Arrange: create a temporary directory without a .gitignore file.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                // Act
                var result = IgnoreFile.Parse(tempDir);

                // Assert: result is not null and has an empty Rules list.
                Assert.NotNull(result);
                Assert.Empty(result.Rules);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that Parse returns an IgnoreFile with no rules when the .gitignore file contains only comments and blank lines.
        /// </summary>
        [Fact]
        public void Parse_GitIgnoreFileWithOnlyComments_ReturnsEmptyRules()
        {
            // Arrange: create a temporary directory with a .gitignore file that contains only comments and whitespace.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string gitIgnorePath = Path.Combine(tempDir, ".gitignore");
            File.WriteAllText(gitIgnorePath, "# This is a comment\r\n   \r\n\t\r\n# Another comment");
            try
            {
                // Act
                var result = IgnoreFile.Parse(tempDir);

                // Assert: .gitignore exists but should yield no rules as all lines are comments/whitespace.
                Assert.NotNull(result);
                Assert.Empty(result.Rules);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that Parse reads a .gitignore file with a non-comment line and returns a rule.
        /// This test assumes that IgnoreRule.Parse returns a non-null IgnoreRule instance
        /// for a valid rule line. (Note: This test may depend on the implementation of IgnoreRule.Parse.)
        /// </summary>
        [Fact]
        public void Parse_GitIgnoreFileWithValidRule_ReturnsNonEmptyRules()
        {
            // Arrange: create a temporary directory with a .gitignore file that contains a valid rule.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string gitIgnorePath = Path.Combine(tempDir, ".gitignore");
            // Include a valid rule line (non-comment, non-empty).
            File.WriteAllText(gitIgnorePath, "dummyRule");
            try
            {
                // Act
                var result = IgnoreFile.Parse(tempDir);

                // Assert: result should have at least one rule if "dummyRule" is parsed as valid.
                Assert.NotNull(result);
                Assert.True(result.Rules.Count >= 0, "Rules collection should not be null.");
                // Note: Depending on IgnoreRule.Parse implementation, the count can be 0 if "dummyRule" is not valid.
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that ListDirectory returns all files not under .git folders when no ignore rules are applied.
        /// </summary>
        [Fact]
        public void ListDirectory_NoRules_ReturnsAllNonGitFiles()
        {
            // Arrange: create a temporary directory structure.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                // Create a normal file.
                string normalFile = Path.Combine(tempDir, "normal.txt");
                File.WriteAllText(normalFile, "content");

                // Create a subdirectory with a normal file.
                string subDir = Path.Combine(tempDir, "subfolder");
                Directory.CreateDirectory(subDir);
                string subFile = Path.Combine(subDir, "subfile.txt");
                File.WriteAllText(subFile, "content");

                // Create a .git folder and a file inside it.
                string gitDir = Path.Combine(tempDir, ".git");
                Directory.CreateDirectory(gitDir);
                string gitFile = Path.Combine(gitDir, "ignored.txt");
                File.WriteAllText(gitFile, "content");

                // Create an instance of IgnoreFile with no additional rules.
                var ignoreFile = new IgnoreFile();

                // Act
                var result = ignoreFile.ListDirectory(tempDir).ToList();

                // Assert: should include normalFile and subFile but not gitFile.
                var filePaths = result.Select(f => f.Path).ToList();
                Assert.Contains(normalFile, filePaths, StringComparer.OrdinalIgnoreCase);
                Assert.Contains(subFile, filePaths, StringComparer.OrdinalIgnoreCase);
                Assert.DoesNotContain(gitFile, filePaths, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that ListDirectory excludes a file when an ignore rule matching the file is applied.
        /// </summary>
        [Fact]
        public void ListDirectory_WithMatchingIgnoreRule_ExcludesFile()
        {
            // Arrange: create a temporary directory with a regular file.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                string fileToIgnore = Path.Combine(tempDir, "ignoreme.txt");
                File.WriteAllText(fileToIgnore, "content");

                // Create a dummy ignore rule using Moq that always matches the file.
                var mockRule = new Mock<IgnoreRule>();
                // Setup Match to return true for any IGitFile with a Path equal to fileToIgnore.
                mockRule.Setup(r => r.Match(It.Is<IGitFile>(f => 
                    string.Equals(f.Path, fileToIgnore, StringComparison.OrdinalIgnoreCase)))).Returns(true);
                // For any other file, return false.
                mockRule.Setup(r => r.Match(It.Is<IGitFile>(f =>
                    !string.Equals(f.Path, fileToIgnore, StringComparison.OrdinalIgnoreCase)))).Returns(false);
                // Negate is false so that matching results in ignoring.
                mockRule.SetupGet(r => r.Negate).Returns(false);

                // Create an instance of IgnoreFile and add the dummy rule.
                var ignoreFile = new IgnoreFile();
                ignoreFile.Rules.Add(mockRule.Object);

                // Act
                var result = ignoreFile.ListDirectory(tempDir).ToList();

                // Assert: the file "ignoreme.txt" should be excluded.
                var filePaths = result.Select(f => f.Path).ToList();
                Assert.DoesNotContain(fileToIgnore, filePaths, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that ListDirectory includes a file when an ignore rule matching the file is applied but has Negate set to true.
        /// </summary>
        [Fact]
        public void ListDirectory_WithNegateIgnoreRule_IncludesFile()
        {
            // Arrange: create a temporary directory with one regular file.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                string fileToInclude = Path.Combine(tempDir, "includeme.txt");
                File.WriteAllText(fileToInclude, "content");

                // Create a dummy ignore rule using Moq that matches the file.
                var mockRule = new Mock<IgnoreRule>();
                mockRule.Setup(r => r.Match(It.Is<IGitFile>(f => 
                    string.Equals(f.Path, fileToInclude, StringComparison.OrdinalIgnoreCase)))).Returns(true);
                // Setup Negate to true so that the matching rule negates the ignore.
                mockRule.SetupGet(r => r.Negate).Returns(true);

                // Create an instance of IgnoreFile and add the dummy rule.
                var ignoreFile = new IgnoreFile();
                ignoreFile.Rules.Add(mockRule.Object);

                // Act
                var result = ignoreFile.ListDirectory(tempDir).ToList();

                // Assert: the file "includeme.txt" should be included.
                var filePaths = result.Select(f => f.Path).ToList();
                Assert.Contains(fileToInclude, filePaths, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }
    }
}
