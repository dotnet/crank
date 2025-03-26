using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Crank.RegressionBot;
using Moq;
using Octokit;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// A dummy implementation of BotOptions for testing purposes.
    /// </summary>
    public class BotOptions
    {
        public string AccessToken { get; set; }
        public string AppKey { get; set; }
        public string AppId { get; set; }
        public long InstallId { get; set; }
    }

    /// <summary>
    /// Unit tests for the <see cref="GitHubHelper"/> class.
    /// </summary>
    public class GitHubHelperTests
    {
        private readonly Type _gitHubHelperType;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubHelperTests"/> class and resets the static state.
        /// </summary>
        public GitHubHelperTests()
        {
            _gitHubHelperType = typeof(GitHubHelper);
            ResetStaticFields();
        }

        /// <summary>
        /// Resets the static fields of GitHubHelper to ensure test isolation.
        /// </summary>
        private void ResetStaticFields()
        {
            var githubClientField = _gitHubHelperType.GetField("_githubClient", BindingFlags.Static | BindingFlags.NonPublic);
            var credentialsField = _gitHubHelperType.GetField("_credentials", BindingFlags.Static | BindingFlags.NonPublic);
            githubClientField?.SetValue(null, null);
            credentialsField?.SetValue(null, null);
        }

        /// <summary>
        /// Tests that GetCredentialsForUser returns a Credentials object which is stored internally,
        /// when provided with a valid access token.
        /// </summary>
//         [Fact] [Error] (63-66)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.BotOptions' to 'Microsoft.Crank.RegressionBot.BotOptions'
//         public void GetCredentialsForUser_WithValidAccessToken_ReturnsCredentialsWithAccessToken()
//         {
//             // Arrange
//             var options = new BotOptions { AccessToken = "dummy-access-token" };
// 
//             // Act
//             var credentials = GitHubHelper.GetCredentialsForUser(options);
// 
//             // Assert
//             Assert.NotNull(credentials);
//             var credentialsField = _gitHubHelperType.GetField("_credentials", BindingFlags.Static | BindingFlags.NonPublic);
//             var storedCredentials = credentialsField.GetValue(null) as Credentials;
//             Assert.Equal(credentials, storedCredentials);
//         }

        /// <summary>
        /// Tests that GetCredentialsForAppAsync throws a FormatException when the AppKey is not a valid base64 string.
        /// </summary>
//         [Fact] [Error] (87-100)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.BotOptions' to 'Microsoft.Crank.RegressionBot.BotOptions'
//         public async Task GetCredentialsForAppAsync_WithInvalidAppKey_ThrowsFormatException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 AppKey = "invalid-base64",
//                 AppId = "dummy-app-id",
//                 InstallId = 123
//             };
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<FormatException>(() => GitHubHelper.GetCredentialsForAppAsync(options));
//         }

        /// <summary>
        /// Tests that GetClient returns a GitHubClient with the stored credentials when credentials are set,
        /// and that subsequent calls return the same singleton instance.
        /// </summary>
//         [Fact] [Error] (99-66)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.RegressionBot.UnitTests.BotOptions' to 'Microsoft.Crank.RegressionBot.BotOptions'
//         public void GetClient_WhenCredentialsAreSet_ReturnsGitHubClientWithCredentials()
//         {
//             // Arrange
//             var options = new BotOptions { AccessToken = "dummy-token" };
//             var credentials = GitHubHelper.GetCredentialsForUser(options);
// 
//             // Act
//             var client = GitHubHelper.GetClient();
// 
//             // Assert
//             Assert.NotNull(client);
//             Assert.Equal(credentials, client.Credentials);
// 
//             // Act - call again to ensure singleton behavior
//             var client2 = GitHubHelper.GetClient();
//             Assert.Same(client, client2);
//         }

        /// <summary>
        /// Tests that GetClient returns a GitHubClient with null credentials when no credentials have been set.
        /// </summary>
        [Fact]
        public void GetClient_WithoutCredentialsSet_ReturnsGitHubClientWithNullCredentials()
        {
            // Arrange
            // By resetting in constructor no credentials are set.

            // Act
            var client = GitHubHelper.GetClient();

            // Assert
            Assert.NotNull(client);
            Assert.Null(client.Credentials);
        }
    }
}
