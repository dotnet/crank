using Microsoft.Crank.PullRequestBot;
using Moq;
using Octokit;
using System;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Command"/> class.
    /// </summary>
    public class CommandTests
    {
        private readonly Command _command;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandTests"/> class.
        /// </summary>
        public CommandTests()
        {
            _command = new Command();
        }

        /// <summary>
        /// Tests that the PullRequest property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void PullRequest_SetAndGet_ReturnsSameInstance()
        {
            // Arrange
            var expectedPullRequest = new PullRequest();

            // Act
            _command.PullRequest = expectedPullRequest;
            var actualPullRequest = _command.PullRequest;

            // Assert
            Assert.Same(expectedPullRequest, actualPullRequest);
        }

        /// <summary>
        /// Tests that the Benchmarks property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Benchmarks_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            var expectedBenchmarks = new string[] { "Benchmark1", "Benchmark2" };

            // Act
            _command.Benchmarks = expectedBenchmarks;
            var actualBenchmarks = _command.Benchmarks;

            // Assert
            Assert.Equal(expectedBenchmarks, actualBenchmarks);
        }

        /// <summary>
        /// Tests that the Profiles property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Profiles_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            var expectedProfiles = new string[] { "Profile1", "Profile2" };

            // Act
            _command.Profiles = expectedProfiles;
            var actualProfiles = _command.Profiles;

            // Assert
            Assert.Equal(expectedProfiles, actualProfiles);
        }

        /// <summary>
        /// Tests that the Components property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Components_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            var expectedComponents = new string[] { "Component1", "Component2" };

            // Act
            _command.Components = expectedComponents;
            var actualComponents = _command.Components;

            // Assert
            Assert.Equal(expectedComponents, actualComponents);
        }

        /// <summary>
        /// Tests that the Arguments property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Arguments_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            var expectedArguments = "test-arguments";

            // Act
            _command.Arguments = expectedArguments;
            var actualArguments = _command.Arguments;

            // Assert
            Assert.Equal(expectedArguments, actualArguments);
        }

        /// <summary>
        /// Tests that all properties can be set to null (where applicable) and retrieved correctly.
        /// This verifies boundary conditions for nullable properties.
        /// </summary>
        [Fact]
        public void Properties_SetToNull_ReturnsNulls()
        {
            // Arrange
            // Note: PullRequest is a reference type, and string arrays and string are reference types as well.
            // Setting them to null should be allowed.
            
            // Act
            _command.PullRequest = null;
            _command.Benchmarks = null;
            _command.Profiles = null;
            _command.Components = null;
            _command.Arguments = null;

            // Assert
            Assert.Null(_command.PullRequest);
            Assert.Null(_command.Benchmarks);
            Assert.Null(_command.Profiles);
            Assert.Null(_command.Components);
            Assert.Null(_command.Arguments);
        }
    }
}
