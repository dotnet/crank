using System;
using System.Collections.Generic;
using Microsoft.Crank.RegressionBot;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SourceSection"/> class.
    /// </summary>
    public class SourceSectionTests
    {
        private readonly SourceSection _sourceSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceSectionTests"/> class.
        /// </summary>
        public SourceSectionTests()
        {
            _sourceSection = new SourceSection();
        }

        /// <summary>
        /// Tests that the constructor initializes all properties and fields with their default values.
        /// Expected default values: HealthCheck is false, Probes is an empty list, Labels and Owners are empty lists,
        /// and Template and Title are empty strings.
        /// </summary>
        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            // Arrange is done in the constructor.

            // Act
            // Use the _sourceSection as constructed.

            // Assert
            Assert.False(_sourceSection.HealthCheck);
            Assert.NotNull(_sourceSection.Probes);
            Assert.Empty(_sourceSection.Probes);
            Assert.NotNull(_sourceSection.Labels);
            Assert.Empty(_sourceSection.Labels);
            Assert.NotNull(_sourceSection.Owners);
            Assert.Empty(_sourceSection.Owners);
            Assert.Equal(string.Empty, _sourceSection.Template);
            Assert.Equal(string.Empty, _sourceSection.Title);
        }

        /// <summary>
        /// Tests that the HealthCheck property can be set and returns the correct value.
        /// </summary>
        [Fact]
        public void HealthCheck_Setter_ShouldUpdateValue()
        {
            // Arrange
            bool expectedValue = true;

            // Act
            _sourceSection.HealthCheck = expectedValue;

            // Assert
            Assert.Equal(expectedValue, _sourceSection.HealthCheck);
        }

        /// <summary>
        /// Tests that the Probes property can be assigned a new list and returns the same instance.
        /// </summary>
        [Fact]
        public void Probes_Setter_ShouldUpdateValue()
        {
            // Arrange
            var newProbeList = new List<Probe>();

            // Act
            _sourceSection.Probes = newProbeList;

            // Assert
            Assert.Same(newProbeList, _sourceSection.Probes);
        }

        /// <summary>
        /// Tests that the Template property can be set and returns the correct value.
        /// </summary>
        [Fact]
        public void Template_Setter_ShouldUpdateValue()
        {
            // Arrange
            string expectedTemplate = "RegressionTemplate";

            // Act
            _sourceSection.Template = expectedTemplate;

            // Assert
            Assert.Equal(expectedTemplate, _sourceSection.Template);
        }

        /// <summary>
        /// Tests that the Title property can be set and returns the correct value.
        /// </summary>
        [Fact]
        public void Title_Setter_ShouldUpdateValue()
        {
            // Arrange
            string expectedTitle = "Issue Title";

            // Act
            _sourceSection.Title = expectedTitle;

            // Assert
            Assert.Equal(expectedTitle, _sourceSection.Title);
        }

        /// <summary>
        /// Tests that the Labels field is modifiable by adding elements.
        /// </summary>
        [Fact]
        public void Labels_Field_Modification_ShouldAllowAddingElements()
        {
            // Arrange
            string label = "bug";

            // Act
            _sourceSection.Labels.Add(label);

            // Assert
            Assert.Single(_sourceSection.Labels);
            Assert.Equal(label, _sourceSection.Labels[0]);
        }

        /// <summary>
        /// Tests that the Owners field is modifiable by adding elements.
        /// </summary>
        [Fact]
        public void Owners_Field_Modification_ShouldAllowAddingElements()
        {
            // Arrange
            string owner = "team-lead";

            // Act
            _sourceSection.Owners.Add(owner);

            // Assert
            Assert.Single(_sourceSection.Owners);
            Assert.Equal(owner, _sourceSection.Owners[0]);
        }
    }
}
