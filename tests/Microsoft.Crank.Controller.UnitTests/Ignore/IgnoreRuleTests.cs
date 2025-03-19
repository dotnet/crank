// using Microsoft.VisualStudio.TestTools.UnitTesting; [Error] (1-30)CS0234 The type or namespace name 'TestTools' does not exist in the namespace 'Microsoft.VisualStudio' (are you missing an assembly reference?)
using Moq;
using System;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GitFile"/> class.
    /// </summary>
//     [TestClass] [Error] (10-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (10-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class GitFileTests
    {
        private readonly string _samplePathWithBackslash = "folder\\subfolder\\file.txt";
        private readonly string _expectedPathWithSlash = "folder/subfolder/file.txt";

        /// <summary>
        /// Tests that the GitFile constructor converts backslashes to forward slashes.
        /// </summary>
//         [TestMethod] [Error] (19-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (19-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (26-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_Backslashes_ReplacedWithForwardSlashes()
//         {
//             // Arrange & Act
//             var gitFile = new GitFile(_samplePathWithBackslash);
// 
//             // Assert
//             Assert.AreEqual(_expectedPathWithSlash, gitFile.Path, "The backslashes should be replaced with forward slashes in the path.");
//         }

        /// <summary>
        /// Tests that the IsDirectory property returns false.
        /// </summary>
//         [TestMethod] [Error] (32-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (32-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (42-13)CS0103 The name 'Assert' does not exist in the current context
//         public void IsDirectory_Always_ReturnsFalse()
//         {
//             // Arrange
//             var gitFile = new GitFile("some/path.txt");
// 
//             // Act
//             bool isDirectory = gitFile.IsDirectory;
// 
//             // Assert
//             Assert.IsFalse(isDirectory, "GitFile should not be a directory.");
//         }

        /// <summary>
        /// Tests that the ToString method returns the same value as the Path property.
        /// </summary>
//         [TestMethod] [Error] (48-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (48-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (59-13)CS0103 The name 'Assert' does not exist in the current context
//         public void ToString_ReturnsPath()
//         {
//             // Arrange
//             var path = "some/path.txt";
//             var gitFile = new GitFile(path);
// 
//             // Act
//             var toStringResult = gitFile.ToString();
// 
//             // Assert
//             Assert.AreEqual(gitFile.Path, toStringResult, "ToString should return the path.");
//         }
    }

    /// <summary>
    /// Unit tests for the <see cref="GitDirectory"/> class.
    /// </summary>
//     [TestClass] [Error] (66-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (66-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class GitDirectoryTests
    {
        private readonly string _samplePathWithBackslash = "folder\\subfolder\\";
        private readonly string _expectedPathWithSlash = "folder/subfolder/";

        /// <summary>
        /// Tests that the GitDirectory constructor converts backslashes to forward slashes.
        /// </summary>
//         [TestMethod] [Error] (75-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (75-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (82-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Constructor_Backslashes_ReplacedWithForwardSlashes()
//         {
//             // Arrange & Act
//             var gitDirectory = new GitDirectory(_samplePathWithBackslash);
// 
//             // Assert
//             Assert.AreEqual(_expectedPathWithSlash, gitDirectory.Path, "The backslashes should be replaced with forward slashes in the path.");
//         }

        /// <summary>
        /// Tests that the IsDirectory property returns true.
        /// </summary>
//         [TestMethod] [Error] (88-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (88-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (98-13)CS0103 The name 'Assert' does not exist in the current context
//         public void IsDirectory_Always_ReturnsTrue()
//         {
//             // Arrange
//             var gitDirectory = new GitDirectory("some/directory/");
// 
//             // Act
//             bool isDirectory = gitDirectory.IsDirectory;
// 
//             // Assert
//             Assert.IsTrue(isDirectory, "GitDirectory should be a directory.");
//         }

        /// <summary>
        /// Tests that the ToString method returns the same value as the Path property.
        /// </summary>
//         [TestMethod] [Error] (104-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (104-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (115-13)CS0103 The name 'Assert' does not exist in the current context
//         public void ToString_ReturnsPath()
//         {
//             // Arrange
//             var path = "some/directory/";
//             var gitDirectory = new GitDirectory(path);
// 
//             // Act
//             var toStringResult = gitDirectory.ToString();
// 
//             // Assert
//             Assert.AreEqual(gitDirectory.Path, toStringResult, "ToString should return the path.");
//         }
    }

    /// <summary>
    /// Unit tests for the <see cref="IgnoreRule"/> class.
    /// </summary>
//     [TestClass] [Error] (122-6)CS0246 The type or namespace name 'TestClassAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (122-6)CS0246 The type or namespace name 'TestClass' could not be found (are you missing a using directive or an assembly reference?)
    public class IgnoreRuleTests
    {
        private readonly string _basePath = "repo/";

        /// <summary>
        /// Tests that parsing an empty rule throws an ArgumentException.
        /// </summary>
//         [TestMethod] [Error] (130-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (130-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (137-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Parse_EmptyRule_ThrowsArgumentException()
//         {
//             // Arrange
//             string emptyRule = string.Empty;
// 
//             // Act & Assert
//             Assert.ThrowsException<ArgumentException>(() => IgnoreRule.Parse(_basePath, emptyRule),
//                 "Parsing an empty rule should throw an ArgumentException.");
//         }

        /// <summary>
        /// Tests that a rule starting with an unescaped '!' sets the Negate flag.
        /// </summary>
//         [TestMethod] [Error] (144-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (144-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (154-13)CS0103 The name 'Assert' does not exist in the current context [Error] (155-13)CS0103 The name 'Assert' does not exist in the current context [Error] (156-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Parse_RuleStartingWithExclamation_SetsNegateFlag()
//         {
//             // Arrange
//             string rule = "!foo";
//             
//             // Act
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
// 
//             // Assert
//             Assert.IsNotNull(ignoreRule, "IgnoreRule should not be null for a valid rule.");
//             Assert.IsTrue(ignoreRule.Negate, "The Negate flag should be set when the rule starts with '!'.");
//             Assert.AreEqual(rule, ignoreRule.Rule, "The Rule property should retain the original rule string.");
//         }

        /// <summary>
        /// Tests that a rule with an escaped exclamation (preceded by a backslash) does not set the Negate flag.
        /// </summary>
//         [TestMethod] [Error] (162-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (162-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (172-13)CS0103 The name 'Assert' does not exist in the current context [Error] (173-13)CS0103 The name 'Assert' does not exist in the current context [Error] (174-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Parse_RuleWithEscapedExclamation_DoesNotSetNegateFlag()
//         {
//             // Arrange
//             string rule = @"\!foo";
//             
//             // Act
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
// 
//             // Assert
//             Assert.IsNotNull(ignoreRule, "IgnoreRule should not be null for a valid escaped rule.");
//             Assert.IsFalse(ignoreRule.Negate, "The Negate flag should not be set when '!' is escaped.");
//             Assert.AreEqual(rule, ignoreRule.Rule, "The Rule property should retain the original rule string.");
//         }

        /// <summary>
        /// Tests the Match method returns true for a GitFile that matches the ignore pattern.
        /// </summary>
//         [TestMethod] [Error] (180-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (180-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (194-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Match_GitFileMatchesPattern_ReturnsTrue()
//         {
//             // Arrange
//             // Using a rule that matches any .txt file in the root of the basePath.
//             string rule = "/*.txt";
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
//             // Use a GitFile with path that begins with the basePath and matches the pattern.
//             var gitFile = new GitFile("repo/file.txt");
// 
//             // Act
//             bool isMatch = ignoreRule.Match(gitFile);
// 
//             // Assert
//             Assert.IsTrue(isMatch, "The GitFile should match the ignore pattern for .txt files at the root.");
//         }

        /// <summary>
        /// Tests the Match method returns false for a GitFile that does not match the ignore pattern.
        /// </summary>
//         [TestMethod] [Error] (200-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (200-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (213-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Match_GitFileDoesNotMatchPattern_ReturnsFalse()
//         {
//             // Arrange
//             string rule = "/*.txt";
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
//             // GitFile with a non-matching extension.
//             var gitFile = new GitFile("repo/file.cs");
// 
//             // Act
//             bool isMatch = ignoreRule.Match(gitFile);
// 
//             // Assert
//             Assert.IsFalse(isMatch, "The GitFile should not match the ignore pattern when the extension is different.");
//         }

        /// <summary>
        /// Tests the Match method returns false when the file's path does not start with the base path.
        /// </summary>
//         [TestMethod] [Error] (219-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (219-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (232-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Match_FilePathNotStartingWithBasePath_ReturnsFalse()
//         {
//             // Arrange
//             string rule = "/*.txt";
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
//             // GitFile with a path that does not start with the base path.
//             var gitFile = new GitFile("otherRepo/file.txt");
// 
//             // Act
//             bool isMatch = ignoreRule.Match(gitFile);
// 
//             // Assert
//             Assert.IsFalse(isMatch, "The file should not match if its path does not start with the specified base path.");
//         }

        /// <summary>
        /// Tests that the Match method returns false for directories when the ignore rule is set to not match directories.
        /// </summary>
//         [TestMethod] [Error] (238-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (238-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (252-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Match_GitDirectoryWhenRuleNotMatchingDirectories_ReturnsFalse()
//         {
//             // Arrange
//             // Create a rule ending with "/**" which sets _matchDir to false.
//             string rule = "/*.txt/**";
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
//             // GitDirectory instance should return false when _matchDir is false.
//             var gitDirectory = new GitDirectory("repo/file.txt");
// 
//             // Act
//             bool isMatch = ignoreRule.Match(gitDirectory);
// 
//             // Assert
//             Assert.IsFalse(isMatch, "GitDirectory should not match when the rule is not intended for directories.");
//         }

        /// <summary>
        /// Tests that the Match method works correctly for a GitDirectory when the rule is intended to match directories.
        /// </summary>
//         [TestMethod] [Error] (258-10)CS0246 The type or namespace name 'TestMethodAttribute' could not be found (are you missing a using directive or an assembly reference?) [Error] (258-10)CS0246 The type or namespace name 'TestMethod' could not be found (are you missing a using directive or an assembly reference?) [Error] (272-13)CS0103 The name 'Assert' does not exist in the current context
//         public void Match_GitDirectoryWhenRuleMatchesDirectories_ReturnsTrue()
//         {
//             // Arrange
//             // Create a rule that ends with a slash, which should match directories.
//             string rule = "/folder/";
//             var ignoreRule = IgnoreRule.Parse(_basePath, rule);
//             // GitDirectory with a matching path.
//             var gitDirectory = new GitDirectory("repo/folder/");
// 
//             // Act
//             bool isMatch = ignoreRule.Match(gitDirectory);
// 
//             // Assert
//             Assert.IsTrue(isMatch, "GitDirectory should match when the rule is intended for directories.");
//         }
    }
}
