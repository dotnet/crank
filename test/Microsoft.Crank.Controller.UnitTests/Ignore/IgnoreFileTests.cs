using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crank.Controller.Ignore;
using Moq;
using Xunit;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Contains unit tests for the <see cref="IgnoreFile"/> class.
    /// </summary>
    public class IgnoreFileTests : IDisposable
    {
        // Holds paths for temporary directories created during tests.
        private readonly List<string> _tempDirectories;

        public IgnoreFileTests()
        {
            _tempDirectories = new List<string>();
        }

        /// <summary>
        /// Creates a unique temporary directory for testing.
        /// </summary>
        /// <returns>The path to the created temporary directory.</returns>
        private string CreateTempDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        /// <summary>
        /// Cleans up any temporary directories created during the tests.
        /// </summary>
        public void Dispose()
        {
            foreach (var dir in _tempDirectories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch
                {
                    // If cleanup fails, ignore the exception.
                }
            }
        }

        /// <summary>
        /// Tests the Parse method when no .gitignore file exists.
        /// Expected outcome: The Rules list is empty.
        /// </summary>
        [Fact]
        public void Parse_NoGitignoreExists_ReturnsEmptyRules()
        {
            // Arrange
            string tempDir = CreateTempDirectory();

            // Act
            var result = IgnoreFile.Parse(tempDir, includeParentDirectories: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Rules);
        }

        /// <summary>
        /// Tests the Parse method when the .gitignore file contains only comments and blank lines.
        /// Expected outcome: The Rules list remains empty.
        /// </summary>
        [Fact]
        public void Parse_GitignoreContainsOnlyCommentsAndBlanks_ReturnsEmptyRules()
        {
            // Arrange
            string tempDir = CreateTempDirectory();
            string gitignorePath = Path.Combine(tempDir, ".gitignore");
            File.WriteAllText(gitignorePath, "   \n# This is a comment\n\n   ");
            
            // Act
            var result = IgnoreFile.Parse(Path.Combine(tempDir, "somefile.txt"), includeParentDirectories: false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Rules);
        }

        /// <summary>
        /// Tests the Parse method when the .gitignore file contains a valid rule.
        /// Expected outcome: The Rules list contains the parsed rule.
        /// Note: This test assumes that a non-comment, non-blank line in .gitignore results in a non-null rule from IgnoreRule.Parse.
        /// </summary>
        [Fact]
        public void Parse_GitignoreWithValidRule_ReturnsNonEmptyRules()
        {
            // Arrange
            string tempDir = CreateTempDirectory();
            string gitignorePath = Path.Combine(tempDir, ".gitignore");
            // "dummy_rule" is assumed to be interpreted as a valid rule.
            File.WriteAllText(gitignorePath, "dummy_rule");

            // Act
            var result = IgnoreFile.Parse(tempDir, includeParentDirectories: false);

            // Assert
            Assert.NotNull(result);
            // If the rule line is valid, then Rules count should be greater than 0.
            Assert.True(result.Rules.Count > 0, "Expected that a valid rule line produces at least one rule.");
        }

        /// <summary>
        /// Tests the Parse method with includeParentDirectories flag set to true.
        /// Expected outcome: Both child and parent .gitignore files are processed and their rules are included.
        /// </summary>
        [Fact]
        public void Parse_WithParentDirectories_IncludesParentGitignore()
        {
            // Arrange
            // Create parent temporary directory.
            string parentDir = CreateTempDirectory();
            // Create child directory under parent.
            string childDir = Path.Combine(parentDir, "child");
            Directory.CreateDirectory(childDir);
            _tempDirectories.Add(childDir);

            // Create .gitignore in parent directory.
            string parentGitignore = Path.Combine(parentDir, ".gitignore");
            File.WriteAllText(parentGitignore, "parent_rule");

            // Create .gitignore in child directory.
            string childGitignore = Path.Combine(childDir, ".gitignore");
            File.WriteAllText(childGitignore, "child_rule");

            // Act
            var result = IgnoreFile.Parse(childDir, includeParentDirectories: true);

            // Assert
            Assert.NotNull(result);
            // Assuming that both rule lines are valid, the total count should be 2.
            Assert.Equal(2, result.Rules.Count);
        }

        /// <summary>
        /// Tests the ListDirectory method to ensure that files within .git directories are ignored.
        /// Expected outcome: Files in .git folders are not listed.
        /// </summary>
        [Fact]
        public void ListDirectory_IgnoresGitFolderContent()
        {
            // Arrange
            string tempDir = CreateTempDirectory();

            // Create a .git folder and add a file inside it.
            string gitFolder = Path.Combine(tempDir, ".git");
            Directory.CreateDirectory(gitFolder);
            string gitFilePath = Path.Combine(gitFolder, "ignored.txt");
            File.WriteAllText(gitFilePath, "ignored content");

            // Create a normal file in the root directory.
            string normalFilePath = Path.Combine(tempDir, "included.txt");
            File.WriteAllText(normalFilePath, "included content");

            // Instantiate IgnoreFile with no extra rules.
            var ignoreFile = new IgnoreFile();

            // Act
            var listedFiles = ignoreFile.ListDirectory(tempDir);

            // Assert
            Assert.NotNull(listedFiles);
            // Expect only the normal file to be listed.
            Assert.Single(listedFiles);
            Assert.Contains(listedFiles, f => f.Path.EndsWith("included.txt", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tests the ListDirectory method with a custom rule that matches specific files.
        /// Expected outcome: Files matching the custom rule are ignored, while others are included.
        /// </summary>
        [Fact]
        public void ListDirectory_AppliesCustomRules()
        {
            // Arrange
            string tempDir = CreateTempDirectory();

            // Create two files: one that should be ignored and one that should be included.
            string fileToIgnore = Path.Combine(tempDir, "ignore.txt");
            string fileToInclude = Path.Combine(tempDir, "file.txt");
            File.WriteAllText(fileToIgnore, "content");
            File.WriteAllText(fileToInclude, "content");

            // Create an instance of IgnoreFile.
            var ignoreFile = new IgnoreFile();

            // Use Moq to create a fake rule that matches files containing "ignore.txt".
            var fakeRuleMock = new Mock<IgnoreRule>();
            // Setup the Match method for files that contain "ignore.txt" in their path.
            fakeRuleMock.Setup(r => r.Match(It.Is<IGitFile>(f => f.Path.Contains("ignore.txt", StringComparison.OrdinalIgnoreCase))))
                        .Returns(true);
            // For other files, return false.
            fakeRuleMock.Setup(r => r.Match(It.Is<IGitFile>(f => !f.Path.Contains("ignore.txt", StringComparison.OrdinalIgnoreCase))))
                        .Returns(false);
            fakeRuleMock.SetupGet(r => r.Negate).Returns(false);

            // Add the fake rule to the IgnoreFile rules.
            ignoreFile.Rules.Add(fakeRuleMock.Object);

            // Act
            var listedFiles = ignoreFile.ListDirectory(tempDir);

            // Assert
            Assert.NotNull(listedFiles);
            // Expect only the file that does not match the fake rule to be included.
            Assert.Single(listedFiles);
            Assert.Contains(listedFiles, f => f.Path.EndsWith("file.txt", StringComparison.OrdinalIgnoreCase));
        }
    }
}
