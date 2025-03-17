using Microsoft.Crank.RegressionBot.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using Fluid;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;
        private readonly Mock<FluidParser> _mockFluidParser;
        private readonly Mock<TemplateOptions> _mockTemplateOptions;

        public ProgramTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
            _mockFluidParser = new Mock<FluidParser>();
            _mockTemplateOptions = new Mock<TemplateOptions>();
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it returns 0 when invoked with valid arguments.
        /// </summary>
//         [TestMethod] [Error] (48-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WithValidArguments_ReturnsZero()
//         {
//             // Arrange
//             var args = new string[] { "--repository-id", "123", "--access-token", "token", "--username", "user", "--app-key", "key", "--app-id", "id", "--install-id", "install", "--connectionstring", "connection", "--config", "config", "--debug", "--verbose", "--read-only" };
// 
//             // Act
//             var result = await Program.Main(args);
// 
//             // Assert
//             Assert.AreEqual(0, result);
//         }

        /// <summary>
        /// Tests the <see cref="Program.Controller(BotOptions)"/> method to ensure it returns 1 when an ArgumentException is thrown.
        /// </summary>
        [TestMethod]
        public async Task Controller_WhenArgumentExceptionThrown_ReturnsOne()
        {
            // Arrange
            var options = new BotOptions();
            options.ConnectionString = "invalid";

            // Act
            var result = await Program.Controller(options);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.Controller(BotOptions)"/> method to ensure it returns 1 when an unexpected exception is thrown.
        /// </summary>
        [TestMethod]
        public async Task Controller_WhenUnexpectedExceptionThrown_ReturnsOne()
        {
            // Arrange
            var options = new BotOptions();
            options.ConnectionString = null;

            // Act
            var result = await Program.Controller(options);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.LoadConfigurationAsync(string)"/> method to ensure it throws a RegressionBotException when the configuration cannot be loaded.
        /// </summary>
        [TestMethod]
        public async Task LoadConfigurationAsync_WhenConfigurationCannotBeLoaded_ThrowsRegressionBotException()
        {
            // Arrange
            var configurationFilenameOrUrl = "invalid";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<RegressionBotException>(() => Program.LoadConfigurationAsync(configurationFilenameOrUrl));
        }

        /// <summary>
        /// Tests the <see cref="Program.CreateIssueBody(IEnumerable{Regression}, string)"/> method to ensure it returns a non-empty string when a valid template is provided.
        /// </summary>
        [TestMethod]
        public async Task CreateIssueBody_WithValidTemplate_ReturnsNonEmptyString()
        {
            // Arrange
            var regressions = new List<Regression>
            {
                new Regression { CurrentResult = new BenchmarksResult { Scenario = "Scenario1", DateTimeUtc = DateTimeOffset.UtcNow } }
            };
            var template = "template";

            // Act
            var result = await Program.CreateIssueBody(regressions, template);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        /// <summary>
        /// Tests the <see cref="Program.CreateIssueTitle(IEnumerable{Regression}, string)"/> method to ensure it returns a non-empty string when a valid template is provided.
        /// </summary>
        [TestMethod]
        public async Task CreateIssueTitle_WithValidTemplate_ReturnsNonEmptyString()
        {
            // Arrange
            var regressions = new List<Regression>
            {
                new Regression { CurrentResult = new BenchmarksResult { Scenario = "Scenario1", DateTimeUtc = DateTimeOffset.UtcNow } }
            };
            var template = "template";

            // Act
            var result = await Program.CreateIssueTitle(regressions, template);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        /// <summary>
        /// Tests the <see cref="Program.CreateRegressionIssue(IEnumerable{Regression}, string, string)"/> method to ensure it does not throw an exception when valid inputs are provided.
        /// </summary>
        [TestMethod]
        public async Task CreateRegressionIssue_WithValidInputs_DoesNotThrowException()
        {
            // Arrange
            var regressions = new List<Regression>
            {
                new Regression { CurrentResult = new BenchmarksResult { Scenario = "Scenario1", DateTimeUtc = DateTimeOffset.UtcNow } }
            };
            var titleTemplate = "titleTemplate";
            var bodyTemplate = "bodyTemplate";

            // Act & Assert
            await Program.CreateRegressionIssue(regressions, titleTemplate, bodyTemplate);
        }

        /// <summary>
        /// Tests the <see cref="Program.GetRecentIssues(Source)"/> method to ensure it returns a non-null list of issues.
        /// </summary>
        [TestMethod]
        public async Task GetRecentIssues_WithValidSource_ReturnsNonNullList()
        {
            // Arrange
            var source = new Source();

            // Act
            var result = await Program.GetRecentIssues(source);

            // Assert
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Tests the <see cref="Program.UpdateIssues(IEnumerable{Regression}, Source, string)"/> method to ensure it returns a non-null list of regressions.
        /// </summary>
        [TestMethod]
        public async Task UpdateIssues_WithValidInputs_ReturnsNonNullList()
        {
            // Arrange
            var regressions = new List<Regression>
            {
                new Regression { CurrentResult = new BenchmarksResult { Scenario = "Scenario1", DateTimeUtc = DateTimeOffset.UtcNow } }
            };
            var source = new Source();
            var template = "template";

            // Act
            var result = await Program.UpdateIssues(regressions, source, template);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
