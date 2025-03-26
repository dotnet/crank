using System;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Dependency"/> class.
    /// </summary>
    public class DependencyTests
    {
        /// <summary>
        /// Tests that the properties of a <see cref="Dependency"/> instance can be set and retrieved correctly.
        /// The test sets values for all properties and then asserts that the getter returns the same values.
        /// </summary>
        [Fact]
        public void Properties_SetAndGetValues_ReturnsCorrectValues()
        {
            // Arrange
            var expectedId = "123";
            var expectedNames = new[] { "Name1", "Name2" };
            var expectedRepositoryUrl = "https://github.com/dotnet/runtime";
            var expectedVersion = "1.0.0";
            var expectedCommitHash = "abcdef123456";

            // Act
            var dependency = new Dependency
            {
                Id = expectedId,
                Names = expectedNames,
                RepositoryUrl = expectedRepositoryUrl,
                Version = expectedVersion,
                CommitHash = expectedCommitHash
            };

            // Assert
            Assert.Equal(expectedId, dependency.Id);
            Assert.Equal(expectedNames, dependency.Names);
            Assert.Equal(expectedRepositoryUrl, dependency.RepositoryUrl);
            Assert.Equal(expectedVersion, dependency.Version);
            Assert.Equal(expectedCommitHash, dependency.CommitHash);
        }

        /// <summary>
        /// Tests that a newly created <see cref="Dependency"/> instance has default property values (null).
        /// This verifies the default behavior of the auto-properties.
        /// </summary>
        [Fact]
        public void Constructor_DefaultProperties_AreNull()
        {
            // Arrange & Act
            var dependency = new Dependency();

            // Assert
            Assert.Null(dependency.Id);
            Assert.Null(dependency.Names);
            Assert.Null(dependency.RepositoryUrl);
            Assert.Null(dependency.Version);
            Assert.Null(dependency.CommitHash);
        }
    }
}
