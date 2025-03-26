using System.Collections.Generic;
using Microsoft.Crank.RegressionBot;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Configuration"/> class.
    /// </summary>
    public class ConfigurationTests
    {
        private readonly Configuration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationTests"/> class.
        /// </summary>
        public ConfigurationTests()
        {
            _configuration = new Configuration();
        }

        /// <summary>
        /// Tests that the default constructor initializes the Templates and Sources properties to non-null, empty collections.
        /// </summary>
        [Fact]
        public void Ctor_InitializesTemplatesAndSourcesToNonNull()
        {
            // Act is already performed in the constructor.

            // Assert
            Assert.NotNull(_configuration.Templates);
            Assert.Empty(_configuration.Templates);
            Assert.NotNull(_configuration.Sources);
            Assert.Empty(_configuration.Sources);
        }

        /// <summary>
        /// Tests that setting and getting the Templates property returns the same dictionary.
        /// </summary>
        [Fact]
        public void TemplatesProperty_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            var testTemplates = new Dictionary<string, string>
            {
                { "TemplateKey1", "TemplateValue1" },
                { "TemplateKey2", "TemplateValue2" }
            };

            // Act
            _configuration.Templates = testTemplates;

            // Assert
            Assert.Equal(testTemplates, _configuration.Templates);
        }

        /// <summary>
        /// Tests that setting and getting the Sources property returns the same list.
        /// </summary>
        [Fact]
        public void SourcesProperty_SetAndGet_ReturnsSameValue()
        {
            // Arrange
            // Note: Assuming that a concrete implementation of Source is available in the project.
            var testSources = new List<Source>();

            // Act
            _configuration.Sources = testSources;

            // Assert
            Assert.Equal(testSources, _configuration.Sources);
        }
    }
}
