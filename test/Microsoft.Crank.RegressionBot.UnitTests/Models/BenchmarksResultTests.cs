using Microsoft.Crank.RegressionBot.Models;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace Microsoft.Crank.RegressionBot.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="BenchmarksResult"/> class.
    /// </summary>
    public class BenchmarksResultTests
    {
        private readonly BenchmarksResult _benchmarksResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarksResultTests"/> class.
        /// </summary>
        public BenchmarksResultTests()
        {
            _benchmarksResult = new BenchmarksResult();
        }

        /// <summary>
        /// Tests that the auto-implemented properties of BenchmarksResult 
        /// can be set and retrieved correctly.
        /// Expected Outcome: The properties contain the same values that were assigned.
        /// </summary>
        [Fact]
        public void AutoProperties_SetAndGet_ReturnsSameValues()
        {
            // Arrange
            int expectedId = 1;
            bool expectedExcluded = true;
            DateTimeOffset expectedDateTime = DateTimeOffset.UtcNow;
            string expectedSession = "TestSession";
            string expectedScenario = "TestScenario";
            string expectedDescription = "TestDescription";
            string expectedDocument = "{\"key\":\"value\"}";

            // Act
            _benchmarksResult.Id = expectedId;
            _benchmarksResult.Excluded = expectedExcluded;
            _benchmarksResult.DateTimeUtc = expectedDateTime;
            _benchmarksResult.Session = expectedSession;
            _benchmarksResult.Scenario = expectedScenario;
            _benchmarksResult.Description = expectedDescription;
            _benchmarksResult.Document = expectedDocument;

            // Assert
            Assert.Equal(expectedId, _benchmarksResult.Id);
            Assert.Equal(expectedExcluded, _benchmarksResult.Excluded);
            Assert.Equal(expectedDateTime, _benchmarksResult.DateTimeUtc);
            Assert.Equal(expectedSession, _benchmarksResult.Session);
            Assert.Equal(expectedScenario, _benchmarksResult.Scenario);
            Assert.Equal(expectedDescription, _benchmarksResult.Description);
            Assert.Equal(expectedDocument, _benchmarksResult.Document);
        }

        /// <summary>
        /// Tests that the Data property returns a correctly parsed JObject 
        /// when the Document contains valid JSON.
        /// Expected Outcome: Data property returns a JObject with the expected content.
        /// </summary>
        [Fact]
        public void Data_WhenDocumentIsValidJson_ReturnsParsedJObject()
        {
            // Arrange
            string validJson = "{\"key\":\"value\"}";
            _benchmarksResult.Document = validJson;

            // Act
            JObject data = _benchmarksResult.Data;

            // Assert
            Assert.NotNull(data);
            Assert.Equal("value", data["key"]?.ToString());
        }

        /// <summary>
        /// Tests that accessing the Data property twice returns the same cached instance.
        /// Expected Outcome: The same JObject instance is returned for subsequent Data property accesses.
        /// </summary>
        [Fact]
        public void Data_WhenAccessedTwice_ReturnsSameCachedInstance()
        {
            // Arrange
            string validJson = "{\"key\":\"value\"}";
            _benchmarksResult.Document = validJson;

            // Act
            JObject firstAccess = _benchmarksResult.Data;
            JObject secondAccess = _benchmarksResult.Data;

            // Assert
            Assert.Same(firstAccess, secondAccess);
        }

        /// <summary>
        /// Tests that the Data property throws an exception when Document contains invalid JSON.
        /// Expected Outcome: An exception is thrown when parsing invalid JSON.
        /// </summary>
        [Fact]
        public void Data_WhenDocumentIsInvalidJson_ThrowsException()
        {
            // Arrange
            string invalidJson = "invalid json";
            _benchmarksResult.Document = invalidJson;

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
            {
                var _ = _benchmarksResult.Data;
            });
        }

        /// <summary>
        /// Tests that once the Data property has been accessed, changing the Document property 
        /// does not refresh the cached JObject instance.
        /// Expected Outcome: The Data property continues to return the original parsed JObject.
        /// </summary>
        [Fact]
        public void Data_WhenDocumentChangedAfterAccess_DoesNotRefreshCachedInstance()
        {
            // Arrange
            string initialJson = "{\"key\":\"initial\"}";
            string updatedJson = "{\"key\":\"updated\"}";
            _benchmarksResult.Document = initialJson;

            // Act
            JObject initialData = _benchmarksResult.Data;
            // Update the Document after the Data has been cached.
            _benchmarksResult.Document = updatedJson;
            JObject cachedData = _benchmarksResult.Data;

            // Assert
            Assert.Same(initialData, cachedData);
            Assert.Equal("initial", cachedData["key"]?.ToString());
        }
    }
}
