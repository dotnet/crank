using System;
using System.Runtime.InteropServices;
using Microsoft.Crank.IntegrationTests;
using Xunit;

namespace Microsoft.Crank.IntegrationTests.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SkipOnLinuxAttribute"/> class.
    /// </summary>
    public class SkipOnLinuxAttributeTests
    {
        /// <summary>
        /// Tests the constructor of <see cref="SkipOnLinuxAttribute"/> when no custom skip message is provided.
        /// Validates that on Linux the Skip property is set to the default message, and on other OS platforms it remains null.
        /// </summary>
        [Fact]
        public void Constructor_WithNoCustomMessage_SetsDefaultSkipMessageOnLinux()
        {
            // Arrange
            string expectedDefaultMessage = "Test ignored on Linux";

            // Act
            SkipOnLinuxAttribute attribute = new SkipOnLinuxAttribute();

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal(expectedDefaultMessage, attribute.Skip);
            }
            else
            {
                Assert.Null(attribute.Skip);
            }
        }

        /// <summary>
        /// Tests the constructor of <see cref="SkipOnLinuxAttribute"/> when a custom skip message is provided.
        /// Validates that on Linux the Skip property is set to the provided custom message, and on other OS platforms it remains null.
        /// </summary>
        [Fact]
        public void Constructor_WithCustomMessage_SetsCustomSkipMessageOnLinux()
        {
            // Arrange
            string customMessage = "Custom skip message";

            // Act
            SkipOnLinuxAttribute attribute = new SkipOnLinuxAttribute(customMessage);

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal(customMessage, attribute.Skip);
            }
            else
            {
                Assert.Null(attribute.Skip);
            }
        }
    }
}
