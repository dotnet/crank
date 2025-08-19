using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Crank.RegressionBot;
using Microsoft.Crank.RegressionBot.Models;
using Moq;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        private readonly string _tempDirectory;

        public ProgramTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Cleans up the temporary directory created for tests.
        /// </summary>
        ~ProgramTests()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Suppress any cleanup exceptions.
            }
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync throws a RegressionBotException when provided with a null or whitespace path.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_NullOrWhitespaceInput_ThrowsRegressionBotException()
        {
            // Arrange
            string nullInput = null;
            string whitespaceInput = "   ";

            // Act & Assert
            var exceptionNull = await Assert.ThrowsAsync<RegressionBotException>(() => Program.LoadConfigurationAsync(nullInput));
            Assert.Contains("Invalid file path or url", exceptionNull.Message);

            var exceptionWhite = await Assert.ThrowsAsync<RegressionBotException>(() => Program.LoadConfigurationAsync(whitespaceInput));
            Assert.Contains("Invalid file path or url", exceptionWhite.Message);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync throws a RegressionBotException when the file cannot be loaded.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_NonExistentFile_ThrowsRegressionBotException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.json");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RegressionBotException>(() => Program.LoadConfigurationAsync(nonExistentFile));
            Assert.Contains("could not be loaded", exception.Message);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync throws a RegressionBotException for unsupported configuration formats.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_UnsupportedExtension_ThrowsRegressionBotException()
        {
            // Arrange
            string filePath = Path.Combine(_tempDirectory, "config.txt");
            File.WriteAllText(filePath, "{}");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RegressionBotException>(() => Program.LoadConfigurationAsync(filePath));
            Assert.Contains("Unsupported configuration format", exception.Message);
        }

        /// <summary>
        /// Tests that LoadConfigurationAsync successfully parses a valid JSON configuration file.
        /// </summary>
        [Fact]
        public async Task LoadConfigurationAsync_ValidJsonConfiguration_ReturnsConfigurationObject()
        {
            // Arrange
            string filePath = Path.Combine(_tempDirectory, "config.json");
            // Minimal valid configuration JSON (assuming configuration has Sources and Templates properties which can be null or empty)
            string jsonContent = @"{ ""Sources"": [], ""Templates"": {} }";
            File.WriteAllText(filePath, jsonContent);

            // Act
            Configuration config = await Program.LoadConfigurationAsync(filePath);

            // Assert
            Assert.NotNull(config);
            // Verify that Sources is not null (may be empty) and Templates is not null.
            Assert.NotNull(config.Sources);
            Assert.NotNull(config.Templates);
        }

        /// <summary>
        /// Tests that Main returns exit code 1 when missing required command line arguments.
        /// </summary>
//         [Fact] [Error] (125-42)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_MissingRequiredArguments_ReturnsExitCodeOne()
//         {
//             // Arrange
//             // No args provided, so required options (--connectionstring and --config) are missing.
//             string[] args = new string[0];
// 
//             // Act
//             int exitCode = await Program.Main(args);
// 
//             // Assert
//             Assert.Equal(1, exitCode);
//         }

        /// <summary>
        /// Tests that Main returns exit code 1 when provided with a valid connection string and config file that contains no sources.
        /// </summary>
//         [Fact] [Error] (155-42)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_ValidArguments_NoSources_ReturnsExitCodeOne()
//         {
//             // Arrange
//             // Create a minimal valid JSON configuration file with no sources.
//             string configFilePath = Path.Combine(_tempDirectory, "config.json");
//             File.WriteAllText(configFilePath, @"{ ""Sources"": [], ""Templates"": {} }");
// 
//             // Provide required options.
//             // Using --debug to bypass credential creation.
//             string[] args = new string[]
//             {
//                 "--connectionstring", "DummyConnectionString",
//                 "--config", configFilePath,
//                 "--debug"
//             };
// 
//             // Set environment variable for connection string to empty so that original value is preserved.
//             Environment.SetEnvironmentVariable("DummyConnectionString", string.Empty);
// 
//             // Act
//             int exitCode = await Program.Main(args);
// 
//             // Assert
//             // Since there are no sources, the program prints "No source could be found." and returns exit code 1.
//             Assert.Equal(1, exitCode);
//         }
    }
}
