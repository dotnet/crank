using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="IgnoreFile"/> class.
    /// </summary>
    [TestClass]
    public class IgnoreFileTests
    {
        private readonly Mock<DirectoryInfo> _mockDirectoryInfo;
        private readonly Mock<FileInfo> _mockFileInfo;
        private readonly Mock<StreamReader> _mockStreamReader;
        private readonly IgnoreFile _ignoreFile;

        public IgnoreFileTests()
        {
            _mockDirectoryInfo = new Mock<DirectoryInfo>();
            _mockFileInfo = new Mock<FileInfo>();
            _mockStreamReader = new Mock<StreamReader>();
            _ignoreFile = new IgnoreFile();
        }

        /// <summary>
        /// Tests the <see cref="IgnoreFile.Parse(string, bool)"/> method to ensure it correctly parses a .gitignore file.
        /// </summary>
        [TestMethod]
        public void Parse_ValidPath_ReturnsIgnoreFile()
        {
            // Arrange
            string path = "C:\\repo\\.gitignore";
            bool includeParentDirectories = false;

            // Act
            var result = IgnoreFile.Parse(path, includeParentDirectories);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IgnoreFile));
        }

        /// <summary>
        /// Tests the <see cref="IgnoreFile.ListDirectory(string)"/> method to ensure it correctly lists all matching files.
        /// </summary>
        [TestMethod]
        public void ListDirectory_ValidPath_ReturnsMatchingFiles()
        {
            // Arrange
            string path = "C:\\repo";
            var expectedFiles = new List<IGitFile>
            {
                new GitFile("C:\\repo\\file1.txt"),
                new GitFile("C:\\repo\\file2.txt")
            };

            // Act
            var result = _ignoreFile.ListDirectory(path);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedFiles.Count, result.Count);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreFile.ListDirectory(string, List{IGitFile})"/> method to ensure it correctly accumulates matching files.
        /// </summary>
//         [TestMethod] [Error] (79-25)CS1501 No overload for method 'ListDirectory' takes 2 arguments
//         public void ListDirectory_ValidPath_AccumulatesMatchingFiles()
//         {
//             // Arrange
//             string path = "C:\\repo";
//             var accumulator = new List<IGitFile>();
// 
//             // Act
//             _ignoreFile.ListDirectory(path, accumulator);
// 
//             // Assert
//             Assert.IsNotNull(accumulator);
//             Assert.IsTrue(accumulator.Count > 0);
//         }
    }
}
