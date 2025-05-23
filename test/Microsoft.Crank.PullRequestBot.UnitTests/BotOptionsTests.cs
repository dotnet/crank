using Json.Schema;
using Microsoft.Crank.PullRequestBot;
using System;
using Xunit;

namespace Microsoft.Crank.PullRequestBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref = "BotOptions"/> class.
    /// </summary>
    public class BotOptionsTests
    {
        /// <summary>
        /// Tests that when Debug is true, Validate does not throw any exceptions regardless of other properties.
        /// </summary>
//         [Fact] [Error] (22-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (23-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (24-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (30-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugTrue_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = true,
//                 Repository = null,
//                 PullRequest = null,
//                 AppKey = null,
//                 AppId = null,
//                 InstallId = 0
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that when Debug is false and both Repository and PullRequest are empty, Validate throws an ArgumentException.
        /// </summary>
//         [Fact] [Error] (43-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (44-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (45-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (48-76)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_MissingRepositoryAndPullRequest_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = string.Empty,
//                 PullRequest = string.Empty
//             };
//             // Act & Assert
//             var exception = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("--repository or --pull-request is required", exception.Message);
//         }

        /// <summary>
        /// Tests that when Debug is false and Repository is provided without AppKey, Validate does not throw an exception.
        /// </summary>
//         [Fact] [Error] (61-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (62-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (63-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (69-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_ProvidedRepositoryWithoutAppKey_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = "https://github.com/example/repo",
//                 PullRequest = string.Empty,
//                 AppKey = null,
//                 AppId = null,
//                 InstallId = 0
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that when Debug is false and AppKey is provided without AppId, Validate throws an ArgumentException.
        /// </summary>
//         [Fact] [Error] (82-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (83-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (84-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (90-76)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_AppKeyProvidedWithoutAppId_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = "https://github.com/example/repo",
//                 PullRequest = string.Empty,
//                 AppKey = "exampleAppKey",
//                 AppId = string.Empty,
//                 InstallId = 12345
//             };
//             // Act & Assert
//             var exception = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("GitHubAppId argument is missing", exception.Message);
//         }

        /// <summary>
        /// Tests that when Debug is false and AppKey is provided with AppId but InstallId is zero, Validate throws an ArgumentException.
        /// </summary>
//         [Fact] [Error] (103-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (104-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (105-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (111-76)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_AppKeyProvidedWithAppIdButZeroInstallId_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = "https://github.com/example/repo",
//                 PullRequest = string.Empty,
//                 AppKey = "exampleAppKey",
//                 AppId = "exampleAppId",
//                 InstallId = 0
//             };
//             // Act & Assert
//             var exception = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("GitHubInstallationId argument is missing", exception.Message);
//         }

        /// <summary>
        /// Tests that when Debug is false and AppKey is provided with valid AppId and non-zero InstallId, Validate does not throw an exception.
        /// </summary>
//         [Fact] [Error] (124-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (125-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (126-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (132-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_AppKeyProvidedWithValidAppIdAndInstallId_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = "https://github.com/example/repo",
//                 PullRequest = string.Empty,
//                 AppKey = "exampleAppKey",
//                 AppId = "exampleAppId",
//                 InstallId = 12345
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that when Debug is false and only PullRequest is provided (repository empty) without AppKey, Validate does not throw an exception.
        /// </summary>
//         [Fact] [Error] (145-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (146-17)CS0117 'BotOptions' does not contain a definition for 'Repository' [Error] (147-17)CS0117 'BotOptions' does not contain a definition for 'PullRequest' [Error] (153-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugFalse_ProvidedPullRequestWithoutAppKey_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 Repository = string.Empty,
//                 PullRequest = "42",
//                 AppKey = null,
//                 AppId = null,
//                 InstallId = 0
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }
    }
}