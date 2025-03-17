using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<HttpClientHandler> _mockHttpClientHandler;

        public ProgramTests()
        {
            _mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientHandler = new Mock<HttpClientHandler>();
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it correctly processes arguments and returns the expected result.
        /// </summary>
        [TestMethod]
        public void Main_ValidArguments_ReturnsExpectedResult()
        {
            // Arrange
            string[] args = { "--config", "config.yml", "--scenario", "test-scenario" };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result, "Expected Main to return 0 for valid arguments.");
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it handles invalid arguments correctly.
        /// </summary>
        [TestMethod]
        public void Main_InvalidArguments_ReturnsErrorCode()
        {
            // Arrange
            string[] args = { "--invalid-arg" };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result, "Expected Main to return 1 for invalid arguments.");
        }

        /// <summary>
        /// Tests the <see cref="Program.BuildConfigurationAsync(IEnumerable{string}, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, JObject, IEnumerable{string}, IEnumerable{string}, int)"/> method to ensure it builds the configuration correctly.
        /// </summary>
        [TestMethod]
        public async Task BuildConfigurationAsync_ValidInputs_ReturnsConfiguration()
        {
            // Arrange
            var configFiles = new List<string> { "config1.yml", "config2.yml" };
            string scenarioName = "test-scenario";
            var customJobs = new List<string> { "job1", "job2" };
            var arguments = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("arg1", "value1") };
            var commandLineVariables = new JObject();
            var profileNames = new List<string> { "profile1" };
            var scripts = new List<string> { "script1" };
            int interval = 1;

            // Act
            var result = await Program.BuildConfigurationAsync(configFiles, scenarioName, customJobs, arguments, commandLineVariables, profileNames, scripts, interval);

            // Assert
            Assert.IsNotNull(result, "Expected BuildConfigurationAsync to return a valid configuration.");
        }

        /// <summary>
        /// Tests the <see cref="Program.GetRelayTokenAsync(Uri)"/> method to ensure it returns a valid token.
        /// </summary>
        [TestMethod]
        public async Task GetRelayTokenAsync_ValidUri_ReturnsToken()
        {
            // Arrange
            var uri = new Uri("https://example.servicebus.windows.net");

            // Act
            var result = await Program.GetRelayTokenAsync(uri);

            // Assert
            Assert.IsNotNull(result, "Expected GetRelayTokenAsync to return a valid token.");
        }

        /// <summary>
        /// Tests the <see cref="Program.GetRelayTokenAsync(Uri)"/> method to ensure it handles invalid URIs correctly.
        /// </summary>
        [TestMethod]
        public async Task GetRelayTokenAsync_InvalidUri_ReturnsNull()
        {
            // Arrange
            var uri = new Uri("https://invalid.uri");

            // Act
            var result = await Program.GetRelayTokenAsync(uri);

            // Assert
            Assert.IsNull(result, "Expected GetRelayTokenAsync to return null for an invalid URI.");
        }
    }
}

