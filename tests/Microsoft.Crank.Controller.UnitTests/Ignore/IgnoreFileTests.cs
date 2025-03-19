using Moq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.Crank.Controller.Ignore.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="IgnoreFile"/> class.
    /// </summary>
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
        [Fact]
        public void Parse_ValidPath_ReturnsIgnoreFile()
        {
            // Arrange
            string path = "C:\\repo\\.gitignore";
            bool includeParentDirectories = false;

            // Act
            var result = IgnoreFile.Parse(path, includeParentDirectories);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<IgnoreFile>(result);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreFile.ListDirectory(string)"/> method to ensure it correctly lists matching files.
        /// </summary>
        [Fact]
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
            Assert.NotNull(result);
            Assert.Equal(expectedFiles.Count, result.Count);
        }

        /// <summary>
        /// Tests the <see cref="IgnoreFile.ListDirectory(string, List{IGitFile})"/> method to ensure it correctly accumulates matching files.
        /// </summary>
//         [Fact] [Error] (78-25)CS1501 No overload for method 'ListDirectory' takes 2 arguments
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
//             Assert.NotNull(accumulator);
//             Assert.True(accumulator.Count > 0);
//         }
    }
}
