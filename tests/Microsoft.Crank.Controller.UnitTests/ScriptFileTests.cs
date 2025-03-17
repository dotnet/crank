// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Crank.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ScriptFile"/> class.
    /// </summary>
    [TestClass]
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
        [TestMethod]
        public void ReadFile_ValidFilename_ReturnsFileContent()
        {
            // Arrange
            var filename = "test.txt";
            var expectedContent = "Hello, World!";
            File.WriteAllText(filename, expectedContent);

            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.AreEqual(expectedContent, result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.ReadFile(string)"/> method to ensure it returns null when the filename is null or empty.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void ReadFile_NullOrEmptyFilename_ReturnsNull(string filename)
        {
            // Act
            var result = _scriptFile.ReadFile(filename);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.WriteFile(string, string)"/> method to ensure it writes the data to the file when a valid filename is provided.
        /// </summary>
        [TestMethod]
        public void WriteFile_ValidFilename_WritesDataToFile()
        {
            // Arrange
            var filename = "test.txt";
            var data = "Hello, World!";

            // Act
            _scriptFile.WriteFile(filename, data);

            // Assert
            var result = File.ReadAllText(filename);
            Assert.AreEqual(data, result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.WriteFile(string, string)"/> method to ensure it does nothing when the filename is null or empty.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void WriteFile_NullOrEmptyFilename_DoesNothing(string filename)
        {
            // Arrange
            var data = "Hello, World!";

            // Act
            _scriptFile.WriteFile(filename, data);

            // Assert
            Assert.IsFalse(File.Exists(filename));
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns true when the file exists.
        /// </summary>
        [TestMethod]
        public void Exists_FileExists_ReturnsTrue()
        {
            // Arrange
            var filename = "test.txt";
            File.WriteAllText(filename, "Hello, World!");

            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.IsTrue(result);

            // Cleanup
            File.Delete(filename);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns false when the file does not exist.
        /// </summary>
        [TestMethod]
        public void Exists_FileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var filename = "nonexistent.txt";

            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="ScriptFile.Exists(string)"/> method to ensure it returns false when the filename is null or empty.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Exists_NullOrEmptyFilename_ReturnsFalse(string filename)
        {
            // Act
            var result = _scriptFile.Exists(filename);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
