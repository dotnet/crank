using Json.Schema;
using Microsoft.Crank.RegressionBot;
using System;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref = "BotOptions"/> class.
    /// </summary>
    public class BotOptionsTests
    {
        /// <summary>
        /// Tests that when Debug is true, Validate does not perform any validations.
        /// </summary>
//         [Fact] [Error] (22-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (23-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (26-17)CS0117 'BotOptions' does not contain a definition for 'Username' [Error] (29-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (30-17)CS0117 'BotOptions' does not contain a definition for 'Config' [Error] (31-17)CS0117 'BotOptions' does not contain a definition for 'Verbose' [Error] (32-17)CS0117 'BotOptions' does not contain a definition for 'ReadOnly' [Error] (35-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_DebugTrue_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = true,
//                 RepositoryId = 0,
//                 AccessToken = null,
//                 AppKey = null,
//                 Username = null,
//                 AppId = null,
//                 InstallId = 0,
//                 ConnectionString = null,
//                 Config = Array.Empty<string>(),
//                 Verbose = false,
//                 ReadOnly = false
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when RepositoryId is missing or invalid.
        /// </summary>
//         [Fact] [Error] (48-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (49-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (51-17)CS0117 'BotOptions' does not contain a definition for 'Username' [Error] (52-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (55-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugMissingRepositoryId_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 0,
//                 AccessToken = "validToken",
//                 Username = "user",
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("RepositoryId argument is missing or invalid", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when both AccessToken and AppKey are missing.
        /// </summary>
//         [Fact] [Error] (68-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (69-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (72-17)CS0117 'BotOptions' does not contain a definition for 'Username' [Error] (73-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (76-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugMissingAccessTokenAndAppKey_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AccessToken = "",
//                 AppKey = "",
//                 Username = "user",
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("AccessToken or GitHubAppKey is required", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when AppKey is provided but AppId is missing.
        /// </summary>
//         [Fact] [Error] (89-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (90-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (94-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (97-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugWithAppKeyMissingAppId_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AppKey = "ValidAppKey",
//                 AppId = "",
//                 InstallId = 456,
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("GitHubAppId argument is missing", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when AppKey is provided but InstallId is missing.
        /// </summary>
//         [Fact] [Error] (110-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (111-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (115-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (118-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugWithAppKeyMissingInstallId_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AppKey = "ValidAppKey",
//                 AppId = "ValidAppId",
//                 InstallId = 0,
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("GitHubInstallationId argument is missing", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when AccessToken is provided but Username is missing.
        /// </summary>
//         [Fact] [Error] (131-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (132-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (134-17)CS0117 'BotOptions' does not contain a definition for 'Username' [Error] (135-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (138-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugWithAccessTokenMissingUsername_ThrowsArgumentException()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AccessToken = "ValidAccessToken",
//                 Username = "",
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("Username argument is missing", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate throws an exception when the ConnectionString is missing.
        /// </summary>
        /// <param name = "useAccessToken">Determines whether to use the AccessToken or AppKey authentication method.</param>
//         [Theory] [Error] (154-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (155-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (156-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (161-25)CS1061 'BotOptions' does not contain a definition for 'Username' and no accessible extension method 'Username' accepting a first argument of type 'BotOptions' could be found (are you missing a using directive or an assembly reference?) [Error] (171-69)CS1501 No overload for method 'Validate' takes 0 arguments
//         [InlineData(true)]
//         [InlineData(false)]
//         public void Validate_NonDebugMissingConnectionString_ThrowsArgumentException(bool useAccessToken)
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 ConnectionString = ""
//             };
//             if (useAccessToken)
//             {
//                 options.AccessToken = "ValidAccessToken";
//                 options.Username = "user";
//             }
//             else
//             {
//                 options.AppKey = "ValidAppKey";
//                 options.AppId = "ValidAppId";
//                 options.InstallId = 456;
//             }
// 
//             // Act & Assert
//             var ex = Assert.Throws<ArgumentException>(() => options.Validate());
//             Assert.Equal("ConnectionString argument is missing", ex.Message);
//         }

        /// <summary>
        /// Tests that Validate passes successfully when all the required parameters are provided using the AccessToken method.
        /// </summary>
//         [Fact] [Error] (184-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (185-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (187-17)CS0117 'BotOptions' does not contain a definition for 'Username' [Error] (188-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (191-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugWithAccessToken_AllValid_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AccessToken = "ValidAccessToken",
//                 Username = "user",
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }

        /// <summary>
        /// Tests that Validate passes successfully when all the required parameters are provided using the AppKey method.
        /// </summary>
//         [Fact] [Error] (204-17)CS0117 'BotOptions' does not contain a definition for 'Debug' [Error] (205-17)CS0117 'BotOptions' does not contain a definition for 'RepositoryId' [Error] (209-17)CS0117 'BotOptions' does not contain a definition for 'ConnectionString' [Error] (212-60)CS1501 No overload for method 'Validate' takes 0 arguments
//         public void Validate_NonDebugWithAppKey_AllValid_DoesNotThrow()
//         {
//             // Arrange
//             var options = new BotOptions
//             {
//                 Debug = false,
//                 RepositoryId = 123,
//                 AppKey = "ValidAppKey",
//                 AppId = "ValidAppId",
//                 InstallId = 456,
//                 ConnectionString = "ValidConnectionString"
//             };
//             // Act & Assert
//             var exception = Record.Exception(() => options.Validate());
//             Assert.Null(exception);
//         }
    }
}