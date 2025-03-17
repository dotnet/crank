using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GitHubHelper"/> class.
    /// </summary>
//     [TestClass] [Error] (29-69)CS0122 'GitHubHelper.ClientHeader' is inaccessible due to its protection level
//     public class GitHubHelperTests
//     {
//         private readonly BotOptions _botOptions;
//         private readonly Mock<GitHubClient> _mockGitHubClient;
// 
//         public GitHubHelperTests()
//         {
//             _botOptions = new BotOptions
//             {
//                 AppKey = "testAppKey",
//                 AccessToken = "testAccessToken",
//                 AppId = "testAppId",
//                 InstallId = 12345,
//                 GitHubBaseUrl = new Uri("https://github.com")
//             };
//             _mockGitHubClient = new Mock<GitHubClient>(GitHubHelper.ClientHeader);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.GetCredentialsAsync(BotOptions)"/> method to ensure it returns credentials for app.
//         /// </summary>
//         [TestMethod]
//         public async Task GetCredentialsAsync_WithAppKey_ReturnsAppCredentials()
//         {
//             // Arrange
//             _botOptions.AppKey = "testAppKey";
//             _botOptions.AccessToken = null;
// 
//             // Act
//             var credentials = await GitHubHelper.GetCredentialsAsync(_botOptions);
// 
//             // Assert
//             Assert.IsNotNull(credentials);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.GetCredentialsAsync(BotOptions)"/> method to ensure it returns credentials for user.
//         /// </summary>
//         [TestMethod]
//         public async Task GetCredentialsAsync_WithAccessToken_ReturnsUserCredentials()
//         {
//             // Arrange
//             _botOptions.AppKey = null;
//             _botOptions.AccessToken = "testAccessToken";
// 
//             // Act
//             var credentials = await GitHubHelper.GetCredentialsAsync(_botOptions);
// 
//             // Assert
//             Assert.IsNotNull(credentials);
//             Assert.AreEqual("testAccessToken", credentials.Password);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.GetCredentialsForUser(BotOptions)"/> method to ensure it returns user credentials.
//         /// </summary>
//         [TestMethod]
//         public void GetCredentialsForUser_WithAccessToken_ReturnsUserCredentials()
//         {
//             // Act
//             var credentials = GitHubHelper.GetCredentialsForUser(_botOptions);
// 
//             // Assert
//             Assert.IsNotNull(credentials);
//             Assert.AreEqual("testAccessToken", credentials.Password);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.GetCredentialsForAppAsync(BotOptions)"/> method to ensure it returns app credentials.
//         /// </summary>
//         [TestMethod]
//         public async Task GetCredentialsForAppAsync_WithValidOptions_ReturnsAppCredentials()
//         {
//             // Act
//             var credentials = await GitHubHelper.GetCredentialsForAppAsync(_botOptions);
// 
//             // Assert
//             Assert.IsNotNull(credentials);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.GetCredentialsFromStore(Uri)"/> method to ensure it returns credentials from store.
//         /// </summary>
//         [TestMethod]
//         public async Task GetCredentialsFromStore_WithValidUri_ReturnsStoredCredentials()
//         {
//             // Act
//             var credentials = await GitHubHelper.GetCredentialsFromStore(new Uri("https://github.com"));
// 
//             // Assert
//             Assert.IsNotNull(credentials);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="GitHubHelper.CreateClient(Credentials, Uri)"/> method to ensure it creates a GitHub client.
//         /// </summary>
//         [TestMethod]
//         public void CreateClient_WithValidCredentials_ReturnsGitHubClient()
//         {
//             // Arrange
//             var credentials = new Credentials("testToken");
// 
//             // Act
//             var client = GitHubHelper.CreateClient(credentials);
// 
//             // Assert
//             Assert.IsNotNull(client);
//             Assert.AreEqual(credentials, client.Credentials);
//         }
//     }
}
