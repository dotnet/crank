using Microsoft.Crank.Models;
using System;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Attachment"/> class.
    /// </summary>
    public class AttachmentTests
    {
        private readonly Attachment _attachment;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentTests"/> class.
        /// </summary>
        public AttachmentTests()
        {
            _attachment = new Attachment();
        }

        /// <summary>
        /// Tests that a new instance of Attachment has null default values for the properties.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_DefaultPropertiesAreNull()
        {
            // Arrange
            var attachment = new Attachment();

            // Act & Assert
            Assert.Null(attachment.Filename);
            Assert.Null(attachment.TempFilename);
        }

        /// <summary>
        /// Tests that setting the Filename property stores and returns the same value.
        /// </summary>
        [Fact]
        public void Filename_SetValue_ReturnsSameValue()
        {
            // Arrange
            const string expectedFilename = "file.txt";

            // Act
            _attachment.Filename = expectedFilename;
            string actualFilename = _attachment.Filename;

            // Assert
            Assert.Equal(expectedFilename, actualFilename);
        }

        /// <summary>
        /// Tests that setting the TempFilename property stores and returns the same value.
        /// </summary>
        [Fact]
        public void TempFilename_SetValue_ReturnsSameValue()
        {
            // Arrange
            const string expectedTempFilename = "tempfile.txt";

            // Act
            _attachment.TempFilename = expectedTempFilename;
            string actualTempFilename = _attachment.TempFilename;

            // Assert
            Assert.Equal(expectedTempFilename, actualTempFilename);
        }

        /// <summary>
        /// Tests that setting properties to null results in null values without exceptions.
        /// </summary>
        [Fact]
        public void Properties_SetToNull_ShouldReturnNull()
        {
            // Act
            _attachment.Filename = null;
            _attachment.TempFilename = null;

            // Assert
            Assert.Null(_attachment.Filename);
            Assert.Null(_attachment.TempFilename);
        }
    }
}
