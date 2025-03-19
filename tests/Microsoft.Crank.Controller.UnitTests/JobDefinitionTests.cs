using Microsoft.Crank.Controller;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobDefinition"/> class.
    /// </summary>
    public class JobDefinitionTests
    {
        private readonly JobDefinition _jobDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobDefinitionTests"/> class.
        /// </summary>
        public JobDefinitionTests()
        {
            _jobDefinition = new JobDefinition();
        }

        /// <summary>
        /// Tests that the <see cref="JobDefinition"/> constructor initializes a case-insensitive dictionary.
        /// This test verifies that keys added with differing casing are treated as identical.
        /// </summary>
        [Fact]
        public void Constructor_WhenUsingDifferentCasingForKeys_DataIsCaseInsensitive()
        {
            // Arrange
            string keyOriginal = "TestKey";
            string keyDifferentCase = "testkey";
            var expectedValue = new JObject { ["Property"] = "Value" };

            // Act
            // Using the indexer to assign a value.
            _jobDefinition[keyOriginal] = expectedValue;

            // Assert
            // The dictionary should contain the key regardless of case.
            Assert.True(_jobDefinition.ContainsKey(keyDifferentCase), "The dictionary should treat keys case-insensitively.");
            Assert.Equal(expectedValue, _jobDefinition[keyDifferentCase]);

            // Act - Update value using a differently cased key.
            var newValue = new JObject { ["Property"] = "NewValue" };
            _jobDefinition[keyDifferentCase] = newValue;

            // Assert - The update should replace the original value without adding a new entry.
            Assert.Single(_jobDefinition);
            Assert.Equal(newValue, _jobDefinition[keyOriginal]);
        }

        /// <summary>
        /// Tests that the Add method throws an exception when trying to add duplicate keys with different casing.
        /// This test verifies that the underlying case-insensitive behavior prevents duplicate keys.
        /// </summary>
        [Fact]
        public void Add_WhenAddingDuplicateKeyWithDifferentCasing_ThrowsArgumentException()
        {
            // Arrange
            string keyOriginal = "DuplicateKey";
            string keyDifferentCase = "duplicatekey";
            var initialValue = new JObject { ["Data"] = "Initial" };

            // Act
            _jobDefinition.Add(keyOriginal, initialValue);

            // Assert
            // Attempting to add a key that is considered the same (due to case-insensitive comparer) should throw an exception.
            Assert.Throws<ArgumentException>(() => _jobDefinition.Add(keyDifferentCase, new JObject { ["Data"] = "ShouldFail" }));
        }
    }
}
