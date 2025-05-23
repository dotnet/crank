using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Crank.PullRequestBot;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        private readonly FieldInfo _configurationField;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramTests"/> class.
        /// </summary>
        public ProgramTests()
        {
            _configurationField = typeof(Program).GetField("_configuration", BindingFlags.Static | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Tests that GetHelp returns markdown formatted help text when markdown is true.
        /// </summary>
        [Fact]
        public void GetHelp_MarkdownTrue_ReturnsFormattedHelpWithBackticks()
        {
            // Arrange
            // Create a dummy configuration JSON string with required properties.
            string configJson = "{" +
                                "\"benchmarks\": {\"bm1\": {\"Description\": \"Benchmark1 description\"}}," +
                                "\"profiles\": {\"pf1\": {\"Description\": \"Profile1 description\"}}," +
                                "\"components\": {\"comp1\": {}}" +
                                "}";
            var config = JsonConvert.DeserializeObject<Configuration>(configJson);
            _configurationField.SetValue(null, config);

            // Act
            string helpText = Program.GetHelp(true);

            // Assert
            Assert.Contains("`/benchmark <benchmark[,...]> <profile[,...]> <component,[...]> <arguments>`", helpText);
            Assert.Contains("`bm1`", helpText);
            Assert.Contains("`pf1`", helpText);
            Assert.Contains("`comp1`", helpText);
        }

        /// <summary>
        /// Tests that GetHelp returns plain text help when markdown is false.
        /// </summary>
        [Fact]
        public void GetHelp_MarkdownFalse_ReturnsFormattedHelpWithoutBackticks()
        {
            // Arrange
            string configJson = "{" +
                                "\"benchmarks\": {\"bm1\": {\"Description\": \"Benchmark1 description\"}}," +
                                "\"profiles\": {\"pf1\": {\"Description\": \"Profile1 description\"}}," +
                                "\"components\": {\"comp1\": {}}" +
                                "}";
            var config = JsonConvert.DeserializeObject<Configuration>(configJson);
            _configurationField.SetValue(null, config);

            // Act
            string helpText = Program.GetHelp(false);

            // Assert
            // Backticks should be removed
            Assert.DoesNotContain("`", helpText);
            Assert.Contains("bm1", helpText);
            Assert.Contains("pf1", helpText);
            Assert.Contains("comp1", helpText);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync with a valid JSON file returns a proper Configuration object.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_ValidJsonFile_ReturnsValidConfiguration()
        {
            // Arrange
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            string jsonContent = "{" +
                                 "\"benchmarks\": {\"bm1\": {\"Description\": \"Benchmark1 description\"}}," +
                                 "\"profiles\": {\"pf1\": {\"Description\": \"Profile1 description\"}}," +
                                 "\"components\": {\"comp1\": {}}" +
                                 "}";
            File.WriteAllText(tempFile, jsonContent);

            // Act
            Configuration config = await Program.LoadConfigurationAsync(tempFile);

            // Clean up
            File.Delete(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.NotEmpty(config.Benchmarks);
            Assert.NotEmpty(config.Profiles);
            Assert.NotEmpty(config.Components);
            Assert.True(config.Benchmarks.ContainsKey("bm1"));
            Assert.True(config.Profiles.ContainsKey("pf1"));
            Assert.True(config.Components.ContainsKey("comp1"));
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync with an unsupported file extension throws a PullRequestBotException.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_UnsupportedFileExtension_ThrowsPullRequestBotException()
        {
            // Arrange
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            File.WriteAllText(tempFile, "Invalid configuration content");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PullRequestBotException>(async () =>
                await Program.LoadConfigurationAsync(tempFile));
            Assert.Contains("Unsupported configuration format", exception.Message);

            // Clean up
            File.Delete(tempFile);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync with a non-existent file throws a PullRequestBotException.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_NonExistentFile_ThrowsPullRequestBotException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PullRequestBotException>(async () =>
                await Program.LoadConfigurationAsync(nonExistentFile));
            Assert.Contains("could not be loaded", exception.Message);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Run"/> class.
    /// </summary>
    public class RunTests
    {
        /// <summary>
        /// Tests that the Run constructor properly assigns the Profile and Benchmark properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidParameters_PropertiesAreAssigned()
        {
            // Arrange
            string expectedProfile = "TestProfile";
            string expectedBenchmark = "TestBenchmark";

            // Act
            Run run = new Run(expectedProfile, expectedBenchmark);

            // Assert
            Assert.Equal(expectedProfile, run.Profile);
            Assert.Equal(expectedBenchmark, run.Benchmark);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Result"/> class.
    /// </summary>
    public class ResultTests
    {
        /// <summary>
        /// Tests that the Result constructor properly assigns Profile, Benchmark, and Output properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidParameters_PropertiesAreAssigned()
        {
            // Arrange
            string expectedProfile = "TestProfile";
            string expectedBenchmark = "TestBenchmark";
            string expectedOutput = "Test output";

            // Act
            Result result = new Result(expectedProfile, expectedBenchmark, expectedOutput);

            // Assert
            Assert.Equal(expectedProfile, result.Profile);
            Assert.Equal(expectedBenchmark, result.Benchmark);
            Assert.Equal(expectedOutput, result.Output);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="RunOptions"/> class.
    /// </summary>
    public class RunOptionsTests
    {
        /// <summary>
        /// Tests that the RunOptions constructor assigns the MaxRetries, CaptureOutput, and Variables properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_ValidParameters_PropertiesAreAssigned()
        {
            // Arrange
            int expectedMaxRetries = 3;
            bool expectedCaptureOutput = false;
            var expectedVariables = new Dictionary<string, object> { { "key", "value" } };

            // Act
            RunOptions options = new RunOptions(expectedMaxRetries, expectedCaptureOutput, expectedVariables);

            // Assert
            Assert.Equal(expectedMaxRetries, options.MaxRetries);
            Assert.Equal(expectedCaptureOutput, options.CaptureOutput);
            Assert.Equal(expectedVariables, options.Variables);
        }

        /// <summary>
        /// Tests that the Default static property of RunOptions returns a non-null instance.
        /// </summary>
        [Fact]
        public void DefaultProperty_ReturnsNonNullInstance()
        {
            // Act
            RunOptions defaultOptions = RunOptions.Default;

            // Assert
            Assert.NotNull(defaultOptions);
        }
    }
}
