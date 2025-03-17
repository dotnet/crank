using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluid;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<GitHubClient> _mockGitHubClient;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;
        private readonly Mock<FluidParser> _mockFluidParser;
        private readonly Mock<BotOptions> _mockBotOptions;
        private readonly Mock<Configuration> _mockConfiguration;

        public ProgramTests()
        {
            _mockGitHubClient = new Mock<GitHubClient>();
            _mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
            _mockFluidParser = new Mock<FluidParser>();
            _mockBotOptions = new Mock<BotOptions>();
            _mockConfiguration = new Mock<Configuration>();
        }

//         [TestMethod] [Error] (46-40)CS0122 'Program.Main(string[])' is inaccessible due to its protection level
//         public async Task Main_WithValidArgs_ReturnsZero()
//         {
//             // Arrange
//             var args = new string[] { "--config", "config.json" };
//             var rootCommand = new RootCommand();
//             rootCommand.Handler = CommandHandler.Create<BotOptions>(options => Task.FromResult(0));
// 
//             // Act
//             var result = await Program.Main(args);
// 
//             // Assert
//             Assert.AreEqual(0, result);
//         }

        [TestMethod]
        public async Task Controller_WithInvalidOptions_ReturnsOne()
        {
            // Arrange
            var options = new BotOptions();
            options.Config = "invalid_config.json";

            // Act
            var result = await Program.Controller(options);

            // Assert
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task Controller_WithValidOptions_ReturnsZero()
        {
            // Arrange
            var options = new BotOptions();
            options.Config = "valid_config.json";

            // Act
            var result = await Program.Controller(options);

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void CreateThumbprint_SetsThumbprint()
        {
            // Arrange
            Program._options = new BotOptions { Config = "config.json" };

            // Act
            Program.CreateThumbprint();

            // Assert
            Assert.IsNotNull(Program.Thumbprint);
        }

        [TestMethod]
        public void FormatResult_ReturnsFormattedString()
        {
            // Arrange
            var result = new Result("profile", "benchmark", "output");

            // Act
            var formattedResult = Program.FormatResult(result);

            // Assert
            Assert.IsTrue(formattedResult.Contains("profile"));
            Assert.IsTrue(formattedResult.Contains("benchmark"));
            Assert.IsTrue(formattedResult.Contains("output"));
        }

        [TestMethod]
        public async Task UpgradeAuthenticatedClient_UpgradesClient()
        {
            // Arrange
            Program._githubClient = new GitHubClient(new ProductHeaderValue("test"));
            Program._options = new BotOptions();

            // Act
            await Program.UpgradeAuthenticatedClient();

            // Assert
            Assert.IsNotNull(Program._githubClient);
        }

        [TestMethod]
        public void ApplyThumbprint_AppendsThumbprint()
        {
            // Arrange
            var text = "test";

            // Act
            var result = Program.ApplyThumbprint(text);

            // Assert
            Assert.IsTrue(result.Contains(Program.Thumbprint));
        }

        [TestMethod]
        public async Task LoadConfigurationAsync_WithInvalidUrl_ThrowsException()
        {
            // Arrange
            var invalidUrl = "http://invalid_url";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<PullRequestBotException>(() => Program.LoadConfigurationAsync(invalidUrl));
        }

        [TestMethod]
        public async Task LoadConfigurationAsync_WithValidUrl_ReturnsConfiguration()
        {
            // Arrange
            var validUrl = "http://valid_url";
            _mockHttpClient.Setup(client => client.GetStringAsync(validUrl)).ReturnsAsync("{}");

            // Act
            var config = await Program.LoadConfigurationAsync(validUrl);

            // Assert
            Assert.IsNotNull(config);
        }
    }
}
