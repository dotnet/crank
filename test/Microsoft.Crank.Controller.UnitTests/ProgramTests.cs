using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Crank.Controller;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests the MergeVariables method with valid JObject inputs.
        /// Expects the merged JObject to contain properties from all provided JObject arguments.
        /// </summary>
        [Fact]
        public void MergeVariables_WithValidJObjects_ReturnsMergedJObject()
        {
            // Arrange
            var obj1 = new JObject { ["Key1"] = "Value1" };
            var obj2 = new JObject { ["Key2"] = "Value2" };

            // Act
            JObject result = Program.MergeVariables(obj1, obj2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Value1", result["Key1"]?.ToString());
            Assert.Equal("Value2", result["Key2"]?.ToString());
        }

        /// <summary>
        /// Tests the MergeVariables method when passed a mix of JObject, non-JObject, and null values.
        /// Expects that only the JObject arguments are merged into the resulting JObject.
        /// </summary>
        [Fact]
        public void MergeVariables_WithNonJObjectAndNullInputs_ReturnsMergedJObject()
        {
            // Arrange
            var obj = new JObject { ["Key"] = "Value" };

            // Act
            JObject result = Program.MergeVariables(obj, 123, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Value", result["Key"]?.ToString());
            // Only one property should be present since non-JObject and null are skipped.
            Assert.Single(result.Properties());
        }

        /// <summary>
        /// Tests the BuildConfigurationAsync method with a valid minimal configuration and existing scenario.
        /// Expects a non-null Configuration instance containing the provided scenario.
        /// </summary>
        [Fact]
        public async Task BuildConfigurationAsync_WithValidConfigAndScenario_ReturnsConfiguration()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                // Create a minimal configuration in JSON format.
                // The configuration contains a scenario "TestScenario" and a job definition for "TestJob".
                string minimalConfig = @"
{
    ""scenarios"": {
        ""TestScenario"": {
            ""TestService"": { ""job"": ""TestJob"" }
        }
    },
    ""Jobs"": {
        ""TestJob"": {
            ""Endpoints"": [ ""http://localhost"" ],
            ""Project"": ""TestProject"",
            ""Options"": {},
            ""Sources"": {}
        }
    },
    ""Profiles"": {},
    ""OnResultsCreating"": [],
    ""Commands"": {},
    ""Scripts"": {},
    ""Counters"": [],
    ""Results"": []
}";
                File.WriteAllText(tempFile, minimalConfig);

                // Prepare parameters for BuildConfigurationAsync.
                IEnumerable<string> configFiles = new List<string> { tempFile };
                string scenarioName = "TestScenario";
                IEnumerable<string> customJobs = Array.Empty<string>();
                var arguments = new List<KeyValuePair<string, string>>();
                JObject commandLineVariables = new JObject();
                IEnumerable<string> profileNames = Array.Empty<string>();
                IEnumerable<string> scripts = Array.Empty<string>();
                int interval = 1;

                // Act
                object configuration = await Program.BuildConfigurationAsync(
                    configFiles,
                    scenarioName,
                    customJobs,
                    arguments,
                    commandLineVariables,
                    profileNames,
                    scripts,
                    interval);

                // Assert
                Assert.NotNull(configuration);
                // Using reflection to check for the 'Scenarios' property and verify it contains the provided scenario.
                PropertyInfo scenariosProp = configuration.GetType().GetProperty("Scenarios", BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(scenariosProp);
                object scenariosValue = scenariosProp.GetValue(configuration);
                Assert.NotNull(scenariosValue);
                // Convert to dynamic to verify the property exists.
                dynamic dynScenarios = scenariosValue;
                Assert.NotNull(dynScenarios.TestScenario);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests the Main method when called with an empty argument array.
        /// Expects the application to display help and return a code of 1.
        /// </summary>
        [Fact]
        public void Main_WithEmptyArguments_Returns1()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
        }

        /// <summary>
        /// Tests the Main method when both --scenario and --job arguments are provided.
        /// Expects the method to print an error and return a code of -1.
        /// </summary>
        [Fact]
        public void Main_WithScenarioAndJobProvided_ReturnsMinus1()
        {
            // Arrange
            string[] args = new string[] { "--scenario", "TestScenario", "--job", "DummyJob" };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(-1, result);
        }
    }
}
