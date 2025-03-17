using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="BotOptions"/> class.
    /// </summary>
    [TestClass]
    public class BotOptionsTests
    {
        private readonly BotOptions _botOptions;

        public BotOptionsTests()
        {
            _botOptions = new BotOptions();
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when RepositoryId is zero.
        /// </summary>
        [TestMethod]
        public void Validate_RepositoryIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 0;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("RepositoryId argument is missing or invalid", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when both AccessToken and AppKey are null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_AccessTokenAndAppKeyAreNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AccessToken = null;
            _botOptions.AppKey = null;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("AccessToken or GitHubAppKey is required", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when AppKey is provided but AppId is null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_AppKeyProvidedButAppIdIsNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AppKey = "someAppKey";
            _botOptions.AppId = null;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("GitHubAppId argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when AppKey is provided but InstallId is zero.
        /// </summary>
        [TestMethod]
        public void Validate_AppKeyProvidedButInstallIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AppKey = "someAppKey";
            _botOptions.AppId = "someAppId";
            _botOptions.InstallId = 0;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("GitHubInstallationId argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when AccessToken is provided but Username is null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_AccessTokenProvidedButUsernameIsNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AccessToken = "someAccessToken";
            _botOptions.Username = null;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("Username argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when ConnectionString is null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_ConnectionStringIsNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AccessToken = "someAccessToken";
            _botOptions.Username = "someUsername";
            _botOptions.ConnectionString = null;
            _botOptions.Debug = false;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("ConnectionString argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it does not throw any exception when Debug is true.
        /// </summary>
        [TestMethod]
        public void Validate_DebugIsTrue_DoesNotThrowException()
        {
            // Arrange
            _botOptions.Debug = true;

            // Act & Assert
            _botOptions.Validate();
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it does not throw any exception when all required properties are set correctly.
        /// </summary>
        [TestMethod]
        public void Validate_AllRequiredPropertiesSetCorrectly_DoesNotThrowException()
        {
            // Arrange
            _botOptions.RepositoryId = 123;
            _botOptions.AccessToken = "someAccessToken";
            _botOptions.Username = "someUsername";
            _botOptions.ConnectionString = "someConnectionString";
            _botOptions.Debug = false;

            // Act & Assert
            _botOptions.Validate();
        }
    }
}
