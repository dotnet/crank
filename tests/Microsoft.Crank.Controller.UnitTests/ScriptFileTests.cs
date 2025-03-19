using Moq;
using System;
using System.IO;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptFile"/> class.
    /// </summary>
    public class ScriptFileTests
    {
        private readonly ScriptFile _scriptFile;

        public ScriptFileTests()
        {
            _scriptFile = new ScriptFile();
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.ReadFile(string)"/> method to ensure it returns the file content when a valid filename is provided.
        /// </summary>
        [Fact]
        public void ReadFile_ValidFilename_ReturnsFileContent()
        {
            // Arrange
            var filename = "test.txt";
            var expectedContent = "file content";
            File.WriteAllText(filename, expectedContent);

            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.Equal(expectedContent, result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.ReadFile(string)"/> method to ensure it returns null when the filename is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ReadFile_NullOrEmptyFilename_ReturnsNull(string filename)
        {
            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.WriteFile(string, string)"/> method to ensure it writes the data to the file when a valid filename is provided.
        /// </summary>
        [Fact]
        public void WriteFile_ValidFilename_WritesDataToFile()
        {
            // Arrange
            var filename = "test.txt";
            var data = "file content";

            // Act
            _scriptFile.WriteFile(filename, data);

            // Assert
            var result = File.ReadAllText(filename);
            Assert.Equal(data, result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.WriteFile(string, string)"/> method to ensure it does nothing when the filename is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WriteFile_NullOrEmptyFilename_DoesNothing(string filename)
        {
            // Act
            _scriptFile.WriteFile(filename, "data");

            // Assert
            // No exception should be thrown and no file should be created
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns true when the file exists.
        /// </summary>
        [Fact]
        public void Exists_FileExists_ReturnsTrue()
        {
            // Arrange
            var filename = "test.txt";
            File.WriteAllText(filename, "content");

            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.True(result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns false when the file does not exist.
        /// </summary>
        [Fact]
        public void Exists_FileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var filename = "nonexistent.txt";

            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns false when the filename is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Exists_NullOrEmptyFilename_ReturnsFalse(string filename)
        {
            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.False(result);
        }
    }
}
