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
            // Act
            string result = _scriptFile.ReadFile(null);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReadFile returns null when the filename is an empty string.
        /// </summary>
        [Fact]
        public void ReadFile_EmptyFilename_ReturnsNull()
        {
            // Act
            string result = _scriptFile.ReadFile(string.Empty);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReadFile returns the correct content from a valid file.
        /// </summary>
        [Fact]
        public void ReadFile_ValidFile_ReturnsContent()
        {
            // Arrange
            string expectedContent = "Test content for reading.";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, expectedContent);

                // Act
                string result = _scriptFile.ReadFile(tempFile);

                // Assert
                Assert.Equal(expectedContent, result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests that ReadFile throws an exception when the file does not exist.
        /// </summary>
        [Fact]
        public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _scriptFile.ReadFile(nonExistentFile));
        }

        /// <summary>
        /// Tests that WriteFile does not throw an exception when the filename is null.
        /// </summary>
        [Fact]
        public void WriteFile_NullFilename_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _scriptFile.WriteFile(null, "Data"));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests that WriteFile does not throw an exception when the filename is an empty string.
        /// </summary>
        [Fact]
        public void WriteFile_EmptyFilename_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _scriptFile.WriteFile(string.Empty, "Data"));
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests that WriteFile correctly writes data to a valid file.
        /// </summary>
        [Fact]
        public void WriteFile_ValidFile_WritesCorrectContent()
        {
            // Arrange
            string expectedContent = "Sample data to write.";
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            try
            {
                // Act
                _scriptFile.WriteFile(tempFile, expectedContent);

                // Assert
                string actualContent = File.ReadAllText(tempFile);
                Assert.Equal(expectedContent, actualContent);
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
            // Act
            bool result = _scriptFile.Exists(null);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that Exists returns false when the filename is an empty string.
        /// </summary>
        [Fact]
        public void Exists_EmptyFilename_ReturnsFalse()
        {
            // Act
            bool result = _scriptFile.Exists(string.Empty);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that Exists returns false when the file does not exist.
        /// </summary>
        [Fact]
        public void Exists_FileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

            // Act
            bool result = _scriptFile.Exists(nonExistentFile);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that Exists returns true when the file exists.
        /// </summary>
        [Fact]
        public void Exists_FileExists_ReturnsTrue()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                // Ensure file exists by writing some data
                File.WriteAllText(tempFile, "Data exists.");

                // Act
                bool result = _scriptFile.Exists(tempFile);

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
