using Moq;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Git"/> class.
    /// </summary>
    [TestClass]
    public class GitTests
    {
        private readonly Mock<IProcessUtil> _processUtilMock;

        public GitTests()
        {
            _processUtilMock = new Mock<IProcessUtil>();
        }

        /// <summary>
        /// Tests the <see cref="Git.CloneAsync(string, string, bool, string, bool, CancellationToken)"/> method to ensure it correctly clones a repository.
        /// </summary>
        [TestMethod]
        public async Task CloneAsync_ValidParameters_ReturnsCorrectDirectory()
        {
            // Arrange
            string path = "some/path";
            string repository = "https://github.com/dotnet/runtime";
            string branch = "main";
            string expectedDirectory = "runtime";
            string gitOutput = $"Cloning into '{expectedDirectory}'...";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { StandardError = gitOutput, ExitCode = 0 });

            // Act
            string result = await Git.CloneAsync(path, repository, true, branch, false, CancellationToken.None);

            // Assert
            Assert.AreEqual(expectedDirectory, result);
        }

        /// <summary>
        /// Tests the <see cref="Git.CloneAsync(string, string, bool, string, bool, CancellationToken)"/> method to ensure it throws an exception when the directory cannot be parsed.
        /// </summary>
        [TestMethod]
        public async Task CloneAsync_InvalidGitOutput_ThrowsInvalidOperationException()
        {
            // Arrange
            string path = "some/path";
            string repository = "https://github.com/dotnet/runtime";
            string branch = "main";
            string gitOutput = "Invalid output";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { StandardError = gitOutput, ExitCode = 0 });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Git.CloneAsync(path, repository, true, branch, false, CancellationToken.None));
        }

        /// <summary>
        /// Tests the <see cref="Git.CheckoutAsync(string, string, CancellationToken)"/> method to ensure it correctly checks out a branch or commit.
        /// </summary>
        [TestMethod]
        public async Task CheckoutAsync_ValidParameters_CompletesSuccessfully()
        {
            // Arrange
            string path = "some/path";
            string branchOrCommit = "main";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { ExitCode = 0 });

            // Act
            await Git.CheckoutAsync(path, branchOrCommit, CancellationToken.None);

            // Assert
            _processUtilMock.Verify(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="Git.CommitHashAsync(string, CancellationToken)"/> method to ensure it correctly retrieves the commit hash.
        /// </summary>
        [TestMethod]
        public async Task CommitHashAsync_ValidParameters_ReturnsCommitHash()
        {
            // Arrange
            string path = "some/path";
            string expectedHash = "abc123";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { StandardOutput = expectedHash, ExitCode = 0 });

            // Act
            string result = await Git.CommitHashAsync(path, CancellationToken.None);

            // Assert
            Assert.AreEqual(expectedHash, result);
        }

        /// <summary>
        /// Tests the <see cref="Git.CommitHashAsync(string, CancellationToken)"/> method to ensure it returns null when the command fails.
        /// </summary>
        [TestMethod]
        public async Task CommitHashAsync_CommandFails_ReturnsNull()
        {
            // Arrange
            string path = "some/path";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { ExitCode = 1 });

            // Act
            string result = await Git.CommitHashAsync(path, CancellationToken.None);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="Git.InitSubModulesAsync(string, CancellationToken)"/> method to ensure it correctly initializes submodules.
        /// </summary>
        [TestMethod]
        public async Task InitSubModulesAsync_ValidParameters_CompletesSuccessfully()
        {
            // Arrange
            string path = "some/path";
            _processUtilMock.Setup(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult { ExitCode = 0 });

            // Act
            await Git.InitSubModulesAsync(path, CancellationToken.None);

            // Assert
            _processUtilMock.Verify(p => p.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

