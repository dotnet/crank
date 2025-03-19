using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Tests that MergeVariables returns an empty JObject when all inputs are null or non-JObject.
        /// </summary>
        [Fact]
        public void MergeVariables_NullOrNonJObjectInputs_ReturnsEmptyJObject()
        {
            // Arrange
            object[] inputs = new object[] { null, "string", 123, new object() };

            // Act
            JObject result = Program.MergeVariables(inputs);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that MergeVariables correctly merges a single JObject input.
        /// </summary>
        [Fact]
        public void MergeVariables_SingleJObjectInput_ReturnsSameProperties()
        {
            // Arrange
            var input = new JObject
            {
                { "key1", "value1" },
                { "key2", 2 }
            };

            // Act
            JObject result = Program.MergeVariables(input);

            // Assert
            Assert.Equal("value1", (string)result["key1"]);
            Assert.Equal(2, (int)result["key2"]);
        }

        /// <summary>
        /// Tests that MergeVariables merges multiple JObjects with overlapping keys by following merge rules.
        /// </summary>
        [Fact]
        public void MergeVariables_MultipleJObjectInputs_MergesProperties()
        {
            // Arrange
            var input1 = new JObject
            {
                { "key1", "value1" },
                { "common", "fromInput1" }
            };
            var input2 = new JObject
            {
                { "key2", 100 },
                { "common", "fromInput2" }
            };

            // Act
            JObject result = Program.MergeVariables(input1, input2);

            // Assert
            // Since Merge uses MergeNullValueHandling.Merge and MergeArrayHandling.Replace, properties from later objects override earlier ones.
            Assert.Equal("value1", (string)result["key1"]);
            Assert.Equal(100, (int)result["key2"]);
            Assert.Equal("fromInput2", (string)result["common"]);
        }

        /// <summary>
        /// Tests that PatchObject correctly patches the source JObject with values from the patch JObject.
        /// </summary>
        [Fact]
        public void PatchObject_WithValidSourceAndPatch_UpdatesSourceObject()
        {
            // Arrange
            var source = new JObject
            {
                { "a", "1" },
                { "b", new JObject { { "c", "2" } } }
            };
            var patch = new JObject
            {
                { "a", "new" },
                { "b", new JObject { { "d", "3" } } },
                { "e", "4" }
            };

            // Act
            Program.PatchObject(source, patch);

            // Assert
            Assert.Equal("new", (string)source["a"]);
            Assert.True(source.ContainsKey("e"));
            Assert.Equal("4", (string)source["e"]);
            Assert.True(source["b"] is JObject);
            var bObj = (JObject)source["b"];
            // bObj should retain its original "c" and also have new "d"
            Assert.Equal("2", (string)bObj["c"]);
            Assert.Equal("3", (string)bObj["d"]);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync returns a valid JObject when provided with a valid JSON file path.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_ValidJsonFile_ReturnsJObject()
        {
            // Arrange
            string tempFile = Path.GetTempFileName() + ".json";
            try
            {
                // Create a minimal valid configuration JSON content.
                string jsonContent = "{\"jobs\": {}, \"scenarios\": { \"TestScenario\": {} }, \"Profiles\": {}}";
                await File.WriteAllTextAsync(tempFile, jsonContent);

                // Act
                JObject result = await Program.LoadConfigurationAsync(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.ContainsKey("jobs"));
                Assert.True(result.ContainsKey("scenarios"));
                Assert.True(result.ContainsKey("Profiles"));
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
        /// Tests that LoadConfigurationAsync throws a ControllerException when the file cannot be loaded.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_NonExistentFile_ThrowsControllerException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ControllerException>(() => Program.LoadConfigurationAsync(nonExistentFile));
            Assert.Contains("could not be loaded", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that BuildConfigurationAsync throws a ControllerException when the scenario is not found in the configuration.
        /// </summary>
        [Fact]
        public async Task BuildConfigurationAsync_InvalidScenario_ThrowsControllerException()
        {
            // Arrange
            // Create a temporary valid configuration file with a scenario "ValidScenario"
            string tempConfigFile = Path.GetTempFileName() + ".json";
            try
            {
                string jsonContent = "{\"jobs\": {\"Job1\": {}}, \"scenarios\": { \"ValidScenario\": { \"Service1\": { \"job\": \"Job1\" } } }, \"Profiles\": {}}";
                await File.WriteAllTextAsync(tempConfigFile, jsonContent);

                // Use a scenario name that does not exist.
                string invalidScenario = "NonExistentScenario";

                // Act & Assert
                var exception = await Assert.ThrowsAsync<ControllerException>(() =>
                    Program.BuildConfigurationAsync(
                        new string[] { tempConfigFile },
                        invalidScenario,
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<KeyValuePair<string, string>>(),
                        new JObject(),
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<string>(),
                        1));

                Assert.Contains($"The scenario `{invalidScenario}` was not found", exception.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(tempConfigFile))
                {
                    File.Delete(tempConfigFile);
                }
            }
        }

        /// <summary>
        /// Tests that BuildConfigurationAsync completes without throwing when a valid scenario is provided.
        /// This test temporarily creates a default configuration file in the assembly directory if needed.
        /// </summary>
        [Fact]
        public async Task BuildConfigurationAsync_ValidScenario_DoesNotThrow()
        {
            // Arrange
            // Create a temporary configuration file with a valid scenario.
            string tempConfigFile = Path.GetTempFileName() + ".json";
            try
            {
                string jsonContent = "{\"jobs\": {\"Job1\": {}}, \"scenarios\": { \"ValidScenario\": { \"Service1\": { \"job\": \"Job1\" } } }, \"Profiles\": {}}";
                await File.WriteAllTextAsync(tempConfigFile, jsonContent);

                // Ensure a default.config.yml exists in the assembly location.
                string assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                string defaultConfigPath = Path.Combine(assemblyDir, "default.config.yml");
                bool defaultConfigExisted = File.Exists(defaultConfigPath);
                string backupDefaultConfig = null;
                if (!defaultConfigExisted)
                {
                    // Create a minimal default configuration file.
                    backupDefaultConfig = "";
                    await File.WriteAllTextAsync(defaultConfigPath, "{}");
                }

                try
                {
                    // Act
                    var configuration = await Program.BuildConfigurationAsync(
                        new string[] { tempConfigFile },
                        "ValidScenario",
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<KeyValuePair<string, string>>(),
                        new JObject(),
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<string>(),
                        1);

                    // Assert
                    // Since the Configuration type structure is not fully defined, we check that the returned object is not null.
                    Assert.NotNull(configuration);
                }
                finally
                {
                    if (!defaultConfigExisted)
                    {
                        File.Delete(defaultConfigPath);
                    }
                    else if (backupDefaultConfig != null)
                    {
                        await File.WriteAllTextAsync(defaultConfigPath, backupDefaultConfig);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempConfigFile))
                {
                    File.Delete(tempConfigFile);
                }
            }
        }

        /// <summary>
        /// Tests that Main returns 1 when provided with no arguments (which should trigger help display).
        /// </summary>
        [Fact]
        public void Main_NoArguments_ReturnsOne()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
        }
    }
}
