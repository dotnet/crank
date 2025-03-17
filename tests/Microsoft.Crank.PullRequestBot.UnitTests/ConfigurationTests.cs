using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Configuration"/> class.
    /// </summary>
    [TestClass]
    public class ConfigurationTests
    {
        private readonly Configuration _configuration;

        public ConfigurationTests()
        {
            _configuration = new Configuration();
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Defaults"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Defaults_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedDefaults = "default";

            // Act
            _configuration.Defaults = expectedDefaults;
            string actualDefaults = _configuration.Defaults;

            // Assert
            Assert.AreEqual(expectedDefaults, actualDefaults, "The Defaults property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Variables"/> property to ensure it is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Variables_Initialized_ReturnsEmptyDictionary()
        {
            // Act
            var variables = _configuration.Variables;

            // Assert
            Assert.IsNotNull(variables, "The Variables property should not be null.");
            Assert.AreEqual(0, variables.Count, "The Variables dictionary should be empty upon initialization.");
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Components"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Components_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedComponents = new Dictionary<string, Build>(StringComparer.OrdinalIgnoreCase)
            {
                { "component1", new Build { Script = "script1", Arguments = "args1" } }
            };

            // Act
            _configuration.Components = expectedComponents;
            var actualComponents = _configuration.Components;

            // Assert
            CollectionAssert.AreEquivalent(expectedComponents, actualComponents, "The Components property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Profiles"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Profiles_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedProfiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase)
            {
                { "profile1", new Profile { Description = "desc1", Arguments = "args1" } }
            };

            // Act
            _configuration.Profiles = expectedProfiles;
            var actualProfiles = _configuration.Profiles;

            // Assert
            CollectionAssert.AreEquivalent(expectedProfiles, actualProfiles, "The Profiles property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Benchmarks"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Benchmarks_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var expectedBenchmarks = new Dictionary<string, Benchmark>(StringComparer.OrdinalIgnoreCase)
            {
                { "benchmark1", new Benchmark { Description = "desc1", Arguments = "args1" } }
            };

            // Act
            _configuration.Benchmarks = expectedBenchmarks;
            var actualBenchmarks = _configuration.Benchmarks;

            // Assert
            CollectionAssert.AreEquivalent(expectedBenchmarks, actualBenchmarks, "The Benchmarks property did not return the expected value.");
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Profile"/> class.
    /// </summary>
    [TestClass]
    public class ProfileTests
    {
        private readonly Profile _profile;

        public ProfileTests()
        {
            _profile = new Profile();
        }

        /// <summary>
        /// Tests the <see cref="Profile.Description"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Description_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedDescription = "description";

            // Act
            _profile.Description = expectedDescription;
            string actualDescription = _profile.Description;

            // Assert
            Assert.AreEqual(expectedDescription, actualDescription, "The Description property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Profile.Arguments"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Arguments_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedArguments = "arguments";

            // Act
            _profile.Arguments = expectedArguments;
            string actualArguments = _profile.Arguments;

            // Assert
            Assert.AreEqual(expectedArguments, actualArguments, "The Arguments property did not return the expected value.");
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Benchmark"/> class.
    /// </summary>
    [TestClass]
    public class BenchmarkTests
    {
        private readonly Benchmark _benchmark;

        public BenchmarkTests()
        {
            _benchmark = new Benchmark();
        }

        /// <summary>
        /// Tests the <see cref="Benchmark.Description"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Description_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedDescription = "description";

            // Act
            _benchmark.Description = expectedDescription;
            string actualDescription = _benchmark.Description;

            // Assert
            Assert.AreEqual(expectedDescription, actualDescription, "The Description property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Benchmark.Arguments"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Arguments_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedArguments = "arguments";

            // Act
            _benchmark.Arguments = expectedArguments;
            string actualArguments = _benchmark.Arguments;

            // Assert
            Assert.AreEqual(expectedArguments, actualArguments, "The Arguments property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Benchmark.Variables"/> property to ensure it is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Variables_Initialized_ReturnsEmptyDictionary()
        {
            // Act
            var variables = _benchmark.Variables;

            // Assert
            Assert.IsNotNull(variables, "The Variables property should not be null.");
            Assert.AreEqual(0, variables.Count, "The Variables dictionary should be empty upon initialization.");
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Build"/> class.
    /// </summary>
    [TestClass]
    public class BuildTests
    {
        private readonly Build _build;

        public BuildTests()
        {
            _build = new Build();
        }

        /// <summary>
        /// Tests the <see cref="Build.Script"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Script_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedScript = "script";

            // Act
            _build.Script = expectedScript;
            string actualScript = _build.Script;

            // Assert
            Assert.AreEqual(expectedScript, actualScript, "The Script property did not return the expected value.");
        }

        /// <summary>
        /// Tests the <see cref="Build.Arguments"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Arguments_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            string expectedArguments = "arguments";

            // Act
            _build.Arguments = expectedArguments;
            string actualArguments = _build.Arguments;

            // Assert
            Assert.AreEqual(expectedArguments, actualArguments, "The Arguments property did not return the expected value.");
        }
    }
}
