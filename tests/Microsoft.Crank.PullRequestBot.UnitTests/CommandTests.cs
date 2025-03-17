using Microsoft.VisualStudio.TestTools.UnitTesting;
using Octokit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Command"/> class.
    /// </summary>
    [TestClass]
    public class CommandTests
    {
        private readonly Command _command;

        public CommandTests()
        {
            _command = new Command();
        }

        /// <summary>
        /// Tests the <see cref="Command.PullRequest"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void PullRequest_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedPullRequest = new PullRequest();

            // Act
            _command.PullRequest = expectedPullRequest;
            var actualPullRequest = _command.PullRequest;

            // Assert
            Assert.AreEqual(expectedPullRequest, actualPullRequest, "The PullRequest property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Command.Benchmarks"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Benchmarks_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedBenchmarks = new[] { "Benchmark1", "Benchmark2" };

            // Act
            _command.Benchmarks = expectedBenchmarks;
            var actualBenchmarks = _command.Benchmarks;

            // Assert
            CollectionAssert.AreEqual(expectedBenchmarks, actualBenchmarks, "The Benchmarks property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Command.Profiles"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Profiles_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedProfiles = new[] { "Profile1", "Profile2" };

            // Act
            _command.Profiles = expectedProfiles;
            var actualProfiles = _command.Profiles;

            // Assert
            CollectionAssert.AreEqual(expectedProfiles, actualProfiles, "The Profiles property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Command.Components"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Components_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedComponents = new[] { "Component1", "Component2" };

            // Act
            _command.Components = expectedComponents;
            var actualComponents = _command.Components;

            // Assert
            CollectionAssert.AreEqual(expectedComponents, actualComponents, "The Components property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Command.Arguments"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Arguments_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedArguments = "arg1 arg2";

            // Act
            _command.Arguments = expectedArguments;
            var actualArguments = _command.Arguments;

            // Assert
            Assert.AreEqual(expectedArguments, actualArguments, "The Arguments property did not return the expected value.");
        }
    }
}
