using System;
using Microsoft.Crank.RegressionBot.Models;
using Xunit;

namespace Microsoft.Crank.RegressionBot.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="DependencyChange"/> class.
    /// </summary>
    public class DependencyChangeTests
    {
        private readonly DependencyChange _dependencyChange;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyChangeTests"/> class.
        /// </summary>
        public DependencyChangeTests()
        {
            _dependencyChange = new DependencyChange();
        }

        /// <summary>
        /// Tests that a newly created DependencyChange object has default null values for reference types
        /// and the default enum value for ChangeType.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_AreNullOrDefault()
        {
            // Arrange & Act
            var dependencyChange = new DependencyChange();

            // Assert
            Assert.Null(dependencyChange.Job);
            Assert.Null(dependencyChange.Id);
            Assert.Null(dependencyChange.Names);
            Assert.Null(dependencyChange.RepositoryUrl);
            Assert.Null(dependencyChange.PreviousVersion);
            Assert.Null(dependencyChange.CurrentVersion);
            Assert.Null(dependencyChange.PreviousCommitHash);
            Assert.Null(dependencyChange.CurrentCommitHash);
            Assert.Equal(ChangeTypes.Diff, dependencyChange.ChangeType);
        }

        /// <summary>
        /// Tests setting and getting the Job property.
        /// </summary>
        [Fact]
        public void Job_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedJob = "application";

            // Act
            _dependencyChange.Job = expectedJob;
            var actualJob = _dependencyChange.Job;

            // Assert
            Assert.Equal(expectedJob, actualJob);
        }

        /// <summary>
        /// Tests setting and getting the Id property.
        /// </summary>
        [Fact]
        public void Id_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedId = "+kL3IPaqvdVHIVR8mUBvrw==";

            // Act
            _dependencyChange.Id = expectedId;
            var actualId = _dependencyChange.Id;

            // Assert
            Assert.Equal(expectedId, actualId);
        }

        /// <summary>
        /// Tests setting and getting the Names property with an empty array.
        /// </summary>
        [Fact]
        public void Names_SetEmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var expectedNames = new string[0];

            // Act
            _dependencyChange.Names = expectedNames;
            var actualNames = _dependencyChange.Names;

            // Assert
            Assert.NotNull(actualNames);
            Assert.Empty(actualNames);
        }

        /// <summary>
        /// Tests setting and getting the Names property with valid string array values.
        /// </summary>
        [Fact]
        public void Names_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            var expectedNames = new[] { "Microsoft.AspNetCore.App", "AnotherName" };

            // Act
            _dependencyChange.Names = expectedNames;
            var actualNames = _dependencyChange.Names;

            // Assert
            Assert.Equal(expectedNames, actualNames);
        }

        /// <summary>
        /// Tests setting and getting the RepositoryUrl property.
        /// </summary>
        [Fact]
        public void RepositoryUrl_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedUrl = "https://github.com/dotnet/runtime";

            // Act
            _dependencyChange.RepositoryUrl = expectedUrl;
            var actualUrl = _dependencyChange.RepositoryUrl;

            // Assert
            Assert.Equal(expectedUrl, actualUrl);
        }

        /// <summary>
        /// Tests setting and getting the PreviousVersion property.
        /// </summary>
        [Fact]
        public void PreviousVersion_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedVersion = "6.0.0-preview.5.21228.5";

            // Act
            _dependencyChange.PreviousVersion = expectedVersion;
            var actualVersion = _dependencyChange.PreviousVersion;

            // Assert
            Assert.Equal(expectedVersion, actualVersion);
        }

        /// <summary>
        /// Tests setting and getting the CurrentVersion property.
        /// </summary>
        [Fact]
        public void CurrentVersion_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedVersion = "6.0.0-preview.5.21228.5";

            // Act
            _dependencyChange.CurrentVersion = expectedVersion;
            var actualVersion = _dependencyChange.CurrentVersion;

            // Assert
            Assert.Equal(expectedVersion, actualVersion);
        }

        /// <summary>
        /// Tests setting and getting the PreviousCommitHash property.
        /// </summary>
        [Fact]
        public void PreviousCommitHash_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedHash = "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60";

            // Act
            _dependencyChange.PreviousCommitHash = expectedHash;
            var actualHash = _dependencyChange.PreviousCommitHash;

            // Assert
            Assert.Equal(expectedHash, actualHash);
        }

        /// <summary>
        /// Tests setting and getting the CurrentCommitHash property.
        /// </summary>
        [Fact]
        public void CurrentCommitHash_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const string expectedHash = "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60";

            // Act
            _dependencyChange.CurrentCommitHash = expectedHash;
            var actualHash = _dependencyChange.CurrentCommitHash;

            // Assert
            Assert.Equal(expectedHash, actualHash);
        }

        /// <summary>
        /// Tests setting and getting the ChangeType property.
        /// </summary>
        [Fact]
        public void ChangeType_SetAndGetValue_ReturnsAssignedValue()
        {
            // Arrange
            const ChangeTypes expectedChangeType = ChangeTypes.New;

            // Act
            _dependencyChange.ChangeType = expectedChangeType;
            var actualChangeType = _dependencyChange.ChangeType;

            // Assert
            Assert.Equal(expectedChangeType, actualChangeType);
        }

        /// <summary>
        /// Tests that setting all properties on the DependencyChange object results in expected values.
        /// </summary>
        [Fact]
        public void AllProperties_SetAndGet_AllValuesAreAssignedCorrectly()
        {
            // Arrange
            var expectedJob = "load";
            var expectedId = "+kL3IPaqvdVHIVR8mUBvrw==";
            var expectedNames = new[] { "Microsoft.AspNetCore.App" };
            var expectedUrl = "https://github.com/dotnet/runtime";
            var expectedPreviousVersion = "6.0.0-preview.5.21228.5";
            var expectedCurrentVersion = "6.0.0-preview.5.21228.5";
            var expectedPreviousHash = "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60";
            var expectedCurrentHash = "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60";
            const ChangeTypes expectedChangeType = ChangeTypes.Removed;

            // Act
            _dependencyChange.Job = expectedJob;
            _dependencyChange.Id = expectedId;
            _dependencyChange.Names = expectedNames;
            _dependencyChange.RepositoryUrl = expectedUrl;
            _dependencyChange.PreviousVersion = expectedPreviousVersion;
            _dependencyChange.CurrentVersion = expectedCurrentVersion;
            _dependencyChange.PreviousCommitHash = expectedPreviousHash;
            _dependencyChange.CurrentCommitHash = expectedCurrentHash;
            _dependencyChange.ChangeType = expectedChangeType;

            // Assert
            Assert.Equal(expectedJob, _dependencyChange.Job);
            Assert.Equal(expectedId, _dependencyChange.Id);
            Assert.Equal(expectedNames, _dependencyChange.Names);
            Assert.Equal(expectedUrl, _dependencyChange.RepositoryUrl);
            Assert.Equal(expectedPreviousVersion, _dependencyChange.PreviousVersion);
            Assert.Equal(expectedCurrentVersion, _dependencyChange.CurrentVersion);
            Assert.Equal(expectedPreviousHash, _dependencyChange.PreviousCommitHash);
            Assert.Equal(expectedCurrentHash, _dependencyChange.CurrentCommitHash);
            Assert.Equal(expectedChangeType, _dependencyChange.ChangeType);
        }
    }
}
