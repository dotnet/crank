using System;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="DotnetCounter"/> class.
    /// </summary>
    public class DotnetCounterTests
    {
        /// <summary>
        /// Tests that a new instance of DotnetCounter initializes its properties to null.
        /// Arrange: Create a new instance.
        /// Act: Retrieve the default property values.
        /// Assert: All properties should be null.
        /// </summary>
        [Fact]
        public void Constructor_DefaultProperties_ShouldBeNull()
        {
            // Arrange & Act
            var dotnetCounter = new DotnetCounter();

            // Assert
            Assert.Null(dotnetCounter.Provider);
            Assert.Null(dotnetCounter.Name);
            Assert.Null(dotnetCounter.Measurement);
        }

        /// <summary>
        /// Tests that the Provider property can be assigned and retrieved correctly.
        /// Arrange: Create an instance and define an expected provider value.
        /// Act: Set the Provider property.
        /// Assert: The property should return the assigned value.
        /// </summary>
        [Fact]
        public void Provider_SetAndGet_ShouldReturnSameValue()
        {
            // Arrange
            var expectedProvider = "System.Runtime";
            var dotnetCounter = new DotnetCounter();

            // Act
            dotnetCounter.Provider = expectedProvider;
            var actualProvider = dotnetCounter.Provider;

            // Assert
            Assert.Equal(expectedProvider, actualProvider);
        }

        /// <summary>
        /// Tests that the Name property can be assigned and retrieved correctly.
        /// Arrange: Create an instance and define an expected name value.
        /// Act: Set the Name property.
        /// Assert: The property should return the assigned value.
        /// </summary>
        [Fact]
        public void Name_SetAndGet_ShouldReturnSameValue()
        {
            // Arrange
            var expectedName = "cpu-usage";
            var dotnetCounter = new DotnetCounter();

            // Act
            dotnetCounter.Name = expectedName;
            var actualName = dotnetCounter.Name;

            // Assert
            Assert.Equal(expectedName, actualName);
        }

        /// <summary>
        /// Tests that the Measurement property can be assigned and retrieved correctly.
        /// Arrange: Create an instance and define an expected measurement value.
        /// Act: Set the Measurement property.
        /// Assert: The property should return the assigned value.
        /// </summary>
        [Fact]
        public void Measurement_SetAndGet_ShouldReturnSameValue()
        {
            // Arrange
            var expectedMeasurement = "runtime/cpu-usage";
            var dotnetCounter = new DotnetCounter();

            // Act
            dotnetCounter.Measurement = expectedMeasurement;
            var actualMeasurement = dotnetCounter.Measurement;

            // Assert
            Assert.Equal(expectedMeasurement, actualMeasurement);
        }

        /// <summary>
        /// Tests that setting properties to empty strings and then retrieving them returns empty strings.
        /// Arrange: Create an instance.
        /// Act: Set Provider, Name, and Measurement to empty strings.
        /// Assert: Each property should return an empty string.
        /// </summary>
        [Fact]
        public void Properties_SetToEmptyStrings_ShouldReturnEmptyStrings()
        {
            // Arrange
            var dotnetCounter = new DotnetCounter();

            // Act
            dotnetCounter.Provider = string.Empty;
            dotnetCounter.Name = string.Empty;
            dotnetCounter.Measurement = string.Empty;

            // Assert
            Assert.Equal(string.Empty, dotnetCounter.Provider);
            Assert.Equal(string.Empty, dotnetCounter.Name);
            Assert.Equal(string.Empty, dotnetCounter.Measurement);
        }

        /// <summary>
        /// Tests that properties can be explicitly set to null and retrieved as null.
        /// Arrange: Create an instance.
        /// Act: Set Provider, Name, and Measurement to null.
        /// Assert: Each property should be null.
        /// </summary>
        [Fact]
        public void Properties_SetToNull_ShouldReturnNull()
        {
            // Arrange
            var dotnetCounter = new DotnetCounter();

            // Act
            dotnetCounter.Provider = null;
            dotnetCounter.Name = null;
            dotnetCounter.Measurement = null;

            // Assert
            Assert.Null(dotnetCounter.Provider);
            Assert.Null(dotnetCounter.Name);
            Assert.Null(dotnetCounter.Measurement);
        }
    }
}
