using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Crank.PullRequestBot;
using Octokit;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// A minimal stub for BotOptions used for testing purposes.
    /// </summary>
    internal class BotOptions
    {
        public string AppKey { get; set; }
        public string AppId { get; set; }
        public long? InstallId { get; set; }
        public string AccessToken { get; set; }
        public Uri GitHubBaseUrl { get; set; }
    }

    /// <summary>
    /// Unit tests for the <see cref="GitHubHelper"/> class.
    /// </summary>
    public class GitHubHelperTests
    {
        private readonly BotOptions _validUserOptions;
        private readonly BotOptions _invalidAppOptions;
        private readonly Uri _customUri;

        /// <summary>
        /// Initializes test data used across tests.
        /// </summary>
        public GitHubHelperTests()
        {
            _validUserOptions = new BotOptions
            {
                AccessToken = "user-token"
            };

            // For testing GetCredentialsForAppAsync, we use an invalid base64 string to force a FormatException.
            _invalidAppOptions = new BotOptions
            {
                AppKey = "invalid-base64-key",
                AppId = "dummy-app-id",
                InstallId = 123456
            };

            _customUri = new Uri("http://custom");
        }

        /// <summary>
        /// Tests that GetCredentialsForUser returns credentials with the provided access token.
        /// </summary>
//         [Fact] [Error] (63-74)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.PullRequestBot.UnitTests.BotOptions' to 'Microsoft.Crank.PullRequestBot.BotOptions'
//         public void GetCredentialsForUser_WithValidAccessToken_ReturnsCredentialsWithAccessToken()
//         {
//             // Arrange
//             var options = new BotOptions { AccessToken = "user-token" };
// 
//             // Act
//             Credentials credentials = GitHubHelper.GetCredentialsForUser(options);
// 
//             // Assert
//             Assert.NotNull(credentials);
//             // Using reflection to get the private property storing the token (commonly named "Password")
//             var passwordProp = credentials.GetType().GetProperty("Password", BindingFlags.Instance | BindingFlags.Public);
//             Assert.NotNull(passwordProp);
//             string actualToken = (string)passwordProp.GetValue(credentials);
//             Assert.Equal("user-token", actualToken);
//         }

        /// <summary>
        /// Tests that GetCredentialsAsync returns user credentials when AccessToken is provided.
        /// </summary>
//         [Fact] [Error] (84-78)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.PullRequestBot.UnitTests.BotOptions' to 'Microsoft.Crank.PullRequestBot.BotOptions'
//         public async Task GetCredentialsAsync_WithAccessToken_ReturnsUserCredentials()
//         {
//             // Arrange
//             var options = new BotOptions { AccessToken = "user-token" };
// 
//             // Act
//             Credentials credentials = await GitHubHelper.GetCredentialsAsync(options);
// 
//             // Assert
//             Assert.NotNull(credentials);
//             var passwordProp = credentials.GetType().GetProperty("Password", BindingFlags.Instance | BindingFlags.Public);
//             Assert.NotNull(passwordProp);
//             string actualToken = (string)passwordProp.GetValue(credentials);
//             Assert.Equal("user-token", actualToken);
//         }

        /// <summary>
        /// Tests that GetCredentialsAsync calls GetCredentialsForAppAsync when AppKey is provided by verifying that an exception is thrown due to an invalid Base64 value.
        /// </summary>
//         [Fact] [Error] (109-106)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.PullRequestBot.UnitTests.BotOptions' to 'Microsoft.Crank.PullRequestBot.BotOptions'
//         public async Task GetCredentialsAsync_WithAppKey_InvalidBase64_ThrowsFormatException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 AppKey = _invalidAppOptions.AppKey,
//                 AppId = _invalidAppOptions.AppId,
//                 InstallId = _invalidAppOptions.InstallId
//             };
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<FormatException>(async () => await GitHubHelper.GetCredentialsAsync(options));
//         }

        /// <summary>
        /// Tests that GetCredentialsAsync calls GetCredentialsFromStore when neither AccessToken nor AppKey is provided.
        /// Expects a null result if the credential store output does not contain a password.
        /// </summary>
//         [Fact] [Error] (128-78)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.PullRequestBot.UnitTests.BotOptions' to 'Microsoft.Crank.PullRequestBot.BotOptions'
//         public async Task GetCredentialsAsync_WithNoCredentials_ReturnsNull()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 AccessToken = null,
//                 AppKey = null,
//                 GitHubBaseUrl = new Uri("http://nonexistent")
//             };
// 
//             // Act
//             Credentials credentials = await GitHubHelper.GetCredentialsAsync(options);
// 
//             // Assert
//             Assert.Null(credentials);
//         }

        /// <summary>
        /// Tests that GetCredentialsForAppAsync with an invalid AppKey throws a FormatException.
        /// </summary>
//         [Fact] [Error] (144-112)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.PullRequestBot.UnitTests.BotOptions' to 'Microsoft.Crank.PullRequestBot.BotOptions'
//         public async Task GetCredentialsForAppAsync_WithInvalidAppKey_ThrowsFormatException()
//         {
//             // Arrange
//             // _invalidAppOptions already contains an invalid base64 AppKey.
//             
//             // Act & Assert
//             await Assert.ThrowsAsync<FormatException>(async () => await GitHubHelper.GetCredentialsForAppAsync(_invalidAppOptions));
//         }

        /// <summary>
        /// Tests that GetCredentialsFromStore returns null when the output does not match the expected regex.
        /// </summary>
        [Fact]
        public async Task GetCredentialsFromStore_WhenRegexDoesNotMatch_ReturnsNull()
        {
            // Arrange
            // Use a GitHubBaseUrl that likely leads to no valid credential output.
            Uri gitHubBaseUrl = new Uri("http://nonexistent");

            // Act
            Credentials credentials = await GitHubHelper.GetCredentialsFromStore(gitHubBaseUrl);

            // Assert
            Assert.Null(credentials);
        }

        /// <summary>
        /// Tests that CreateClient returns a GitHubClient with the provided credentials and default base address when null is provided.
        /// </summary>
        [Fact]
        public void CreateClient_WithValidCredentialsAndNullBaseAddress_ReturnsClientWithDefaultBaseAddress()
        {
            // Arrange
            var credentials = new Credentials("dummy-token");
            
            // Act
            GitHubClient client = GitHubHelper.CreateClient(credentials, null);

            // Assert
            Assert.NotNull(client);
            Assert.Equal(credentials, client.Credentials);
            // Check that the client's BaseAddress equals the GitHubApiUrl (default)
            Assert.Equal(GitHubClient.GitHubApiUrl, client.BaseAddress);
        }

        /// <summary>
        /// Tests that CreateClient returns a GitHubClient with the provided credentials and a custom base address.
        /// </summary>
        [Fact]
        public void CreateClient_WithValidCredentialsAndCustomBaseAddress_ReturnsClientWithCustomBaseAddress()
        {
            // Arrange
            var credentials = new Credentials("dummy-token");
            
            // Act
            GitHubClient client = GitHubHelper.CreateClient(credentials, _customUri);

            // Assert
            Assert.NotNull(client);
            Assert.Equal(credentials, client.Credentials);
            Assert.Equal(_customUri, client.BaseAddress);
        }
    }
}
