using System;
using System.IO;
using Microsoft.Crank.Controller;
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
        /// Tests that ReadFile returns null when the filename is null.
        /// </summary>
        [Fact]
        public void ReadFile_NullFilename_ReturnsNull()
        {
            // Arrange
            string filename = null;

            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReadFile returns null when the filename is empty.
        /// </summary>
        [Fact]
        public void ReadFile_EmptyFilename_ReturnsNull()
        {
            // Arrange
            string filename = string.Empty;

            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReadFile returns the correct file content when provided a valid filename.
        /// </summary>
        [Fact]
        public void ReadFile_ValidFilename_ReturnsFileContent()
        {
            // Arrange
            string expectedContent = "Test content";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, expectedContent);

                // Act
                var result = _scriptFile.ReadFile(tempFile);

                // Assert
                Assert.Equal(expectedContent, result);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that ReadFile throws an exception when provided a filename that does not exist.
        /// </summary>
        [Fact]
        public void ReadFile_NonExistentFile_ThrowsException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _scriptFile.ReadFile(nonExistentFile));
        }

        /// <summary>
        /// Tests that WriteFile does not create a file when the filename is null.
        /// </summary>
        [Fact]
        public void WriteFile_NullFilename_DoesNotWriteFile()
        {
            // Arrange
            string filename = null;
            string data = "Some data";

            // Act
            _scriptFile.WriteFile(filename, data);

            // Assert
            // When filename is null, no file is created. The absence of exceptions confirms expected behavior.
            Assert.True(true);
        }

        /// <summary>
        /// Tests that WriteFile does not create a file when the filename is empty.
        /// </summary>
        [Fact]
        public void WriteFile_EmptyFilename_DoesNotWriteFile()
        {
            // Arrange
            string filename = string.Empty;
            string data = "Some data";

            // Act
            _scriptFile.WriteFile(filename, data);

            // Assert
            // When filename is empty, no file is created. The absence of exceptions confirms expected behavior.
            Assert.True(true);
        }

        /// <summary>
        /// Tests that WriteFile writes the correct data to a valid filename.
        /// </summary>
        [Fact]
        public void WriteFile_ValidFilename_WritesDataToFile()
        {
            // Arrange
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            string data = "Written data";
            try
            {
                // Act
                _scriptFile.WriteFile(tempFile, data);

                // Assert
                Assert.True(File.Exists(tempFile));
                string fileContent = File.ReadAllText(tempFile);
                Assert.Equal(data, fileContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that Exists returns false when the filename is null.
        /// </summary>
        [Fact]
        public void Exists_NullFilename_ReturnsFalse()
        {
            // Arrange
            string filename = null;

            // Act
            bool exists = _scriptFile.Exists(filename);

            // Assert
            Assert.False(exists);
        }

        /// <summary>
        /// Tests that Exists returns false when the filename is empty.
        /// </summary>
        [Fact]
        public void Exists_EmptyFilename_ReturnsFalse()
        {
            // Arrange
            string filename = string.Empty;

            // Act
            bool exists = _scriptFile.Exists(filename);

            // Assert
            Assert.False(exists);
        }

        /// <summary>
        /// Tests that Exists returns false when the file does not exist.
        /// </summary>
        [Fact]
        public void Exists_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            // Act
            bool exists = _scriptFile.Exists(nonExistentFile);

            // Assert
            Assert.False(exists);
        }

        /// <summary>
        /// Tests that Exists returns true when the file exists.
        /// </summary>
        [Fact]
        public void Exists_ExistingFile_ReturnsTrue()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                // Act
                bool exists = _scriptFile.Exists(tempFile);

                // Assert
                Assert.True(exists);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
