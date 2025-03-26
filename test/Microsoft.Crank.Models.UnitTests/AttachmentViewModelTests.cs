using Microsoft.AspNetCore.Http;
using Microsoft.Crank.Models;
using Moq;
using System;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="AttachmentViewModel"/> class.
    /// </summary>
    public class AttachmentViewModelTests
    {
        private readonly AttachmentViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentViewModelTests"/> class.
        /// </summary>
        public AttachmentViewModelTests()
        {
            _viewModel = new AttachmentViewModel();
        }

        /// <summary>
        /// Tests that the default instance of <see cref="AttachmentViewModel"/> returns the expected default values.
        /// Expected outcome: Id equals 0, DestinationFilename and Content are null.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_ReturnsExpectedDefaults()
        {
            // Assert
            Assert.Equal(0, _viewModel.Id);
            Assert.Null(_viewModel.DestinationFilename);
            Assert.Null(_viewModel.Content);
        }

        /// <summary>
        /// Tests the Id property to ensure that setting a value returns the same value.
        /// This test covers a range of integer inputs including zero, positive, and negative numbers.
        /// </summary>
        /// <param name="value">The integer value to set and verify.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-5)]
        public void Id_SetValue_ReturnsSameValue(int value)
        {
            // Arrange & Act
            _viewModel.Id = value;

            // Assert
            Assert.Equal(value, _viewModel.Id);
        }

        /// <summary>
        /// Tests the DestinationFilename property to ensure that setting a non-null or null string returns the same value.
        /// This test considers valid file names, empty strings, and null.
        /// </summary>
        /// <param name="input">The filename string to set and verify.</param>
        [Theory]
        [InlineData("test.txt")]
        [InlineData("")]
        [InlineData(null)]
        public void DestinationFilename_SetValue_ReturnsSameValue(string input)
        {
            // Arrange & Act
            _viewModel.DestinationFilename = input;

            // Assert
            Assert.Equal(input, _viewModel.DestinationFilename);
        }

        /// <summary>
        /// Tests the Content property to ensure that setting an IFormFile instance returns the same instance.
        /// Uses Moq to create a mock of the IFormFile.
        /// </summary>
        [Fact]
        public void Content_SetValue_ReturnsSameValue()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>().Object;

            // Act
            _viewModel.Content = mockFormFile;

            // Assert
            Assert.Same(mockFormFile, _viewModel.Content);
        }
    }
}
