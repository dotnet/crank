using System;
using System.Collections.Generic;
using Microsoft.Crank.PullRequestBot;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Configuration"/> class.
    /// </summary>
    public class ConfigurationTests
    {
        private readonly Configuration _configuration;

        public ConfigurationTests()
        {
            _configuration = new Configuration();
        }

        /// <summary>
        /// Tests that the Defaults property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void DefaultsProperty_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expected = "DefaultValue";

            // Act
            _configuration.Defaults = expected;
            string actual = _configuration.Defaults;

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests that the Variables dictionary is initialized, empty and case-insensitive.
        /// </summary>
        [Fact]
        public void VariablesDictionary_Initialized_IsEmptyAndCaseInsensitive()
        {
            // Act
            var variables = _configuration.Variables;

            // Assert
            Assert.NotNull(variables);
            Assert.Empty(variables);
            // Validate case-insensitivity: add with one casing and check with different casing.
            variables["Key"] = "Value";
            Assert.True(variables.ContainsKey("key"));
        }

        /// <summary>
        /// Tests that the Components dictionary can be set and retrieved correctly, including checking for case-insensitive keys.
        /// </summary>
        [Fact]
        public void ComponentsDictionary_SetAndGet_ReturnsSameDictionary()
        {
            // Arrange
            var build = new Build { Script = "build.sh", Arguments = "--arg" };
            var components = new Dictionary<string, Build>(StringComparer.OrdinalIgnoreCase)
            {
                { "Component1", build }
            };

            // Act
            _configuration.Components = components;
            var actual = _configuration.Components;

            // Assert
            Assert.Equal(components, actual);
            Assert.True(actual.ContainsKey("component1"));
        }

        /// <summary>
        /// Tests that the Profiles dictionary can be set and retrieved correctly, including checking for case-insensitive keys.
        /// </summary>
        [Fact]
        public void ProfilesDictionary_SetAndGet_ReturnsSameDictionary()
        {
            // Arrange
            var profile = new Profile { Description = "desc", Arguments = "--profile" };
            var profiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase)
            {
                { "Profile1", profile }
            };

            // Act
            _configuration.Profiles = profiles;
            var actual = _configuration.Profiles;

            // Assert
            Assert.Equal(profiles, actual);
            Assert.True(actual.ContainsKey("profile1"));
        }

        /// <summary>
        /// Tests that the Benchmarks dictionary can be set and retrieved correctly, including checking for case-insensitive keys.
        /// </summary>
        [Fact]
        public void BenchmarksDictionary_SetAndGet_ReturnsSameDictionary()
        {
            // Arrange
            var benchmark = new Benchmark { Description = "benchmark desc", Arguments = "--bench" };
            var benchmarks = new Dictionary<string, Benchmark>(StringComparer.OrdinalIgnoreCase)
            {
                { "Benchmark1", benchmark }
            };

            // Act
            _configuration.Benchmarks = benchmarks;
            var actual = _configuration.Benchmarks;

            // Assert
            Assert.Equal(benchmarks, actual);
            Assert.True(actual.ContainsKey("benchmark1"));
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Profile"/> class.
    /// </summary>
    public class ProfileTests
    {
        private readonly Profile _profile;

        public ProfileTests()
        {
            _profile = new Profile();
        }

        /// <summary>
        /// Tests that the Description and Arguments properties of Profile can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expectedDescription = "Unit test description";
            string expectedArguments = "--profile-arg";

            // Act
            _profile.Description = expectedDescription;
            _profile.Arguments = expectedArguments;

            // Assert
            Assert.Equal(expectedDescription, _profile.Description);
            Assert.Equal(expectedArguments, _profile.Arguments);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Benchmark"/> class.
    /// </summary>
    public class BenchmarkTests
    {
        private readonly Benchmark _benchmark;

        public BenchmarkTests()
        {
            _benchmark = new Benchmark();
        }

        /// <summary>
        /// Tests that the Description and Arguments properties of Benchmark can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expectedDescription = "Benchmark description";
            string expectedArguments = "--bench-arg";

            // Act
            _benchmark.Description = expectedDescription;
            _benchmark.Arguments = expectedArguments;

            // Assert
            Assert.Equal(expectedDescription, _benchmark.Description);
            Assert.Equal(expectedArguments, _benchmark.Arguments);
        }

        /// <summary>
        /// Tests that the Variables dictionary in Benchmark is initialized, empty and case-insensitive.
        /// </summary>
        [Fact]
        public void VariablesDictionary_Initialized_IsEmptyAndCaseInsensitive()
        {
            // Act
            var variables = _benchmark.Variables;

            // Assert
            Assert.NotNull(variables);
            Assert.Empty(variables);
            // Validate case-insensitivity.
            variables["Key"] = "Value";
            Assert.True(variables.ContainsKey("key"));
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Build"/> class.
    /// </summary>
    public class BuildTests
    {
        private readonly Build _build;

        public BuildTests()
        {
            _build = new Build();
        }

        /// <summary>
        /// Tests that the Script and Arguments properties of Build can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            string expectedScript = "build.sh";
            string expectedArguments = "--build-arg";

            // Act
            _build.Script = expectedScript;
            _build.Arguments = expectedArguments;

            // Assert
            Assert.Equal(expectedScript, _build.Script);
            Assert.Equal(expectedArguments, _build.Arguments);
        }
    }
}
