using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Crank.PullRequestBot.UnitTests
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
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it does not throw an exception when Debug is true.
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
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when both Repository and PullRequest are null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_RepositoryAndPullRequestAreNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.Debug = false;
            _botOptions.Repository = null;
            _botOptions.PullRequest = null;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("--repository or --pull-request is required", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when AppKey is not null or empty and AppId is null or empty.
        /// </summary>
        [TestMethod]
        public void Validate_AppKeyIsNotNullOrEmptyAndAppIdIsNullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.Debug = false;
            _botOptions.AppKey = "someAppKey";
            _botOptions.AppId = null;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("GitHubAppId argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it throws an <see cref="ArgumentException"/> when AppKey is not null or empty and InstallId is 0.
        /// </summary>
        [TestMethod]
        public void Validate_AppKeyIsNotNullOrEmptyAndInstallIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            _botOptions.Debug = false;
            _botOptions.AppKey = "someAppKey";
            _botOptions.AppId = "someAppId";
            _botOptions.InstallId = 0;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => _botOptions.Validate());
            Assert.AreEqual("GitHubInstallationId argument is missing", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="BotOptions.Validate"/> method to ensure it does not throw an exception when all required properties are set correctly.
        /// </summary>
        [TestMethod]
        public void Validate_AllRequiredPropertiesAreSetCorrectly_DoesNotThrowException()
        {
            // Arrange
            _botOptions.Debug = false;
            _botOptions.Repository = "someRepository";
            _botOptions.AppKey = "someAppKey";
            _botOptions.AppId = "someAppId";
            _botOptions.InstallId = 12345;

            // Act & Assert
            _botOptions.Validate();
        }
    }
}
