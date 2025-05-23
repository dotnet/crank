using System;
using System.Runtime.InteropServices;
using Microsoft.Crank.IntegrationTests;
using Xunit;

namespace Microsoft.Crank.IntegrationTests.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SkipOnMacOsAttribute"/> class.
    /// </summary>
    public class SkipOnMacOsAttributeTests
    {
        /// <summary>
        /// Tests the constructor of SkipOnMacOsAttribute when no custom message is provided.
        /// Verifies that if the operating system is OSX, the Skip property is set to the default skip message,
        /// otherwise the Skip property remains null.
        /// </summary>
        /// <param name="message">Test message parameter, null in this case.</param>
        [Theory]
        [InlineData(null)]
        public void Constructor_WithNullMessage_SetsSkipProperly(string message)
        {
            // Arrange
            string expectedSkip = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Test ignored on OSX" : null;
            
            // Act
            var attribute = new SkipOnMacOsAttribute(message);
            
            // Assert
            Assert.Equal(expectedSkip, attribute.Skip);
        }

        /// <summary>
        /// Tests the constructor of SkipOnMacOsAttribute when a custom message is provided.
        /// Verifies that if the operating system is OSX, the Skip property is set to the provided custom message,
        /// otherwise the Skip property remains null.
        /// </summary>
        /// <param name="message">Custom skip message.</param>
        [Theory]
        [InlineData("Custom skip message")]
        public void Constructor_WithCustomMessage_SetsSkipProperly(string message)
        {
            // Arrange
            string expectedSkip = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? message : null;
            
            // Act
            var attribute = new SkipOnMacOsAttribute(message);
            
            // Assert
            Assert.Equal(expectedSkip, attribute.Skip);
        }
    }
}
