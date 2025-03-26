using System;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Source"/> class.
    /// </summary>
    public class SourceTests
    {
        /// <summary>
        /// Tests that the GetSourceKeyData method returns a SourceKeyData object with properties matching the assigned values.
        /// </summary>
        [Fact]
        public void GetSourceKeyData_AssignedProperties_ReturnsMatchingSourceKeyData()
        {
            // Arrange
            const string expectedBranchOrCommit = "commitHash";
            const string expectedRepository = "https://github.com/user/repo.git";
            const string expectedLocalFolder = "C:\\SourceFolder";
            var source = new Source
            {
                BranchOrCommit = expectedBranchOrCommit,
                Repository = expectedRepository,
                InitSubmodules = true,
                LocalFolder = expectedLocalFolder
            };

            // Act
            SourceKeyData result = source.GetSourceKeyData();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedBranchOrCommit, result.BranchOrCommit);
            Assert.Equal(expectedRepository, result.Repository);
            Assert.True(result.InitSubmodules);
            Assert.Equal(expectedLocalFolder, result.LocalFolder);
        }

        /// <summary>
        /// Tests that the GetSourceKeyData method returns a SourceKeyData object with default property values when none are explicitly set.
        /// </summary>
        [Fact]
        public void GetSourceKeyData_DefaultValues_ReturnsDefaultSourceKeyData()
        {
            // Arrange
            // Create a new Source instance without setting optional properties.
            var source = new Source();

            // Act
            SourceKeyData result = source.GetSourceKeyData();

            // Assert
            Assert.NotNull(result);
            // BranchOrCommit has a default value of "" per the property initializer.
            Assert.Equal(string.Empty, result.BranchOrCommit);
            // Repository and LocalFolder are not initialized and should be null.
            Assert.Null(result.Repository);
            Assert.False(result.InitSubmodules);
            Assert.Null(result.LocalFolder);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="SourceKeyData"/> class.
    /// </summary>
    public class SourceKeyDataTests
    {
        /// <summary>
        /// Tests that properties of SourceKeyData can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Properties_SetValues_ReturnsExpectedValues()
        {
            // Arrange
            const string expectedBranchOrCommit = "branch";
            const string expectedRepository = "https://github.com/user/repo.git";
            const string expectedLocalFolder = "/var/source";
            const bool expectedInitSubmodules = true;
            var sourceKeyData = new SourceKeyData
            {
                BranchOrCommit = expectedBranchOrCommit,
                Repository = expectedRepository,
                LocalFolder = expectedLocalFolder,
                InitSubmodules = expectedInitSubmodules
            };

            // Act & Assert
            Assert.Equal(expectedBranchOrCommit, sourceKeyData.BranchOrCommit);
            Assert.Equal(expectedRepository, sourceKeyData.Repository);
            Assert.Equal(expectedLocalFolder, sourceKeyData.LocalFolder);
            Assert.Equal(expectedInitSubmodules, sourceKeyData.InitSubmodules);
        }
    }
}
