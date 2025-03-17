using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.Crank.Controller.UnitTests
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
        /// Tests that the <see cref="Configuration.Variables"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Variables_InitializedCorrectly()
        {
            // Act
            var variables = _configuration.Variables;

            // Assert
            Assert.IsNotNull(variables);
            Assert.AreEqual(0, variables.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Jobs"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Jobs_InitializedCorrectly()
        {
            // Act
            var jobs = _configuration.Jobs;

            // Assert
            Assert.IsNotNull(jobs);
            Assert.AreEqual(0, jobs.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Scenarios"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Scenarios_InitializedCorrectly()
        {
            // Act
            var scenarios = _configuration.Scenarios;

            // Assert
            Assert.IsNotNull(scenarios);
            Assert.AreEqual(0, scenarios.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Profiles"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Profiles_InitializedCorrectly()
        {
            // Act
            var profiles = _configuration.Profiles;

            // Assert
            Assert.IsNotNull(profiles);
            Assert.AreEqual(0, profiles.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Scripts"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Scripts_InitializedCorrectly()
        {
            // Act
            var scripts = _configuration.Scripts;

            // Assert
            Assert.IsNotNull(scripts);
            Assert.AreEqual(0, scripts.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.DefaultScripts"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void DefaultScripts_InitializedCorrectly()
        {
            // Act
            var defaultScripts = _configuration.DefaultScripts;

            // Assert
            Assert.IsNotNull(defaultScripts);
            Assert.AreEqual(0, defaultScripts.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.OnResultsCreating"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void OnResultsCreating_InitializedCorrectly()
        {
            // Act
            var onResultsCreating = _configuration.OnResultsCreating;

            // Assert
            Assert.IsNotNull(onResultsCreating);
            Assert.AreEqual(0, onResultsCreating.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Counters"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Counters_InitializedCorrectly()
        {
            // Act
            var counters = _configuration.Counters;

            // Assert
            Assert.IsNotNull(counters);
            Assert.AreEqual(0, counters.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Results"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Results_InitializedCorrectly()
        {
            // Act
            var results = _configuration.Results;

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.OnResultsCreated"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void OnResultsCreated_InitializedCorrectly()
        {
            // Act
            var onResultsCreated = _configuration.OnResultsCreated;

            // Assert
            Assert.IsNotNull(onResultsCreated);
            Assert.AreEqual(0, onResultsCreated.Count);
        }

        /// <summary>
        /// Tests that the <see cref="Configuration.Commands"/> property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void Commands_InitializedCorrectly()
        {
            // Act
            var commands = _configuration.Commands;

            // Assert
            Assert.IsNotNull(commands);
            Assert.AreEqual(0, commands.Count);
        }
    }
}
