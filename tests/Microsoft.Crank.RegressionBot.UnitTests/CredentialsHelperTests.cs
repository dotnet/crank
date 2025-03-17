using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GitHubHelper"/> class.
    /// </summary>
    [TestClass]
    public class GitHubHelperTests
    {
        private readonly BotOptions _botOptions;
        private readonly Mock<GitHubClient> _mockGitHubClient;

        public GitHubHelperTests()
        {
            _botOptions = new BotOptions
            {
                AccessToken = "testAccessToken",
                AppKey = "testAppKey",
                AppId = "testAppId",
                InstallId = 12345
            };
            _mockGitHubClient = new Mock<GitHubClient>(new ProductHeaderValue("crank-regression-bot"));
        }

        /// <summary>
        /// Tests the <see cref="GitHubHelper.GetCredentialsForUser(BotOptions)"/> method to ensure it returns the correct credentials.
        /// </summary>
        [TestMethod]
        public void GetCredentialsForUser_ValidOptions_ReturnsCorrectCredentials()
        {
            // Act
            var credentials = GitHubHelper.GetCredentialsForUser(_botOptions);

            // Assert
            Assert.IsNotNull(credentials);
            Assert.AreEqual(_botOptions.AccessToken, credentials.Password);
        }

        /// <summary>
        /// Tests the <see cref="GitHubHelper.GetCredentialsForAppAsync(BotOptions)"/> method to ensure it returns the correct credentials.
        /// </summary>
        [TestMethod]
        public async Task GetCredentialsForAppAsync_ValidOptions_ReturnsCorrectCredentials()
        {
            // Arrange
            var expectedToken = "testToken";
            _mockGitHubClient.Setup(client => client.GitHubApps.CreateInstallationToken(It.IsAny<long>()))
                .ReturnsAsync(new AccessToken(expectedToken, DateTimeOffset.Now.AddMinutes(10)));

            // Act
            var credentials = await GitHubHelper.GetCredentialsForAppAsync(_botOptions);

            // Assert
            Assert.IsNotNull(credentials);
            Assert.AreEqual(expectedToken, credentials.Password);
        }

        /// <summary>
        /// Tests the <see cref="GitHubHelper.GetClient"/> method to ensure it returns a valid GitHub client.
        /// </summary>
        [TestMethod]
        public void GetClient_ValidCredentials_ReturnsGitHubClient()
        {
            // Arrange
            GitHubHelper.GetCredentialsForUser(_botOptions);

            // Act
            var client = GitHubHelper.GetClient();

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual(_botOptions.AccessToken, client.Credentials.Password);
        }
    }
}
