using System;
using System.Runtime.InteropServices;
using Microsoft.Crank.IntegrationTests;
using Xunit;

namespace Microsoft.Crank.IntegrationTests.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="SkipOnWindowsAttribute"/> class.
    /// </summary>
    public class SkipOnWindowsAttributeTests
    {
        /// <summary>
        /// Tests the constructor of <see cref="SkipOnWindowsAttribute"/> when running on Windows with a custom skip message.
        /// Expected Outcome: The Skip property is set to the provided custom message.
        /// </summary>
        [Fact]
        public void Constructor_OnWindowsWithCustomMessage_SetsSkipProperty()
        {
            // Arrange
            const string customMessage = "Custom skip message";
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            // Act
            var attribute = new SkipOnWindowsAttribute(customMessage);
            
            // Assert
            if (isWindows)
            {
                Assert.Equal(customMessage, attribute.Skip);
            }
            else
            {
                // On non-Windows platforms, the Skip property should remain null.
                Assert.Null(attribute.Skip);
            }
        }

        /// <summary>
        /// Tests the constructor of <see cref="SkipOnWindowsAttribute"/> when running on Windows with a null message.
        /// Expected Outcome: The Skip property is set to the default message "Test ignored on Windows".
        /// </summary>
        [Fact]
        public void Constructor_OnWindowsWithNullMessage_SetsSkipPropertyToDefault()
        {
            // Arrange
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            // Act
            var attribute = new SkipOnWindowsAttribute(null);
            
            // Assert
            if (isWindows)
            {
                Assert.Equal("Test ignored on Windows", attribute.Skip);
            }
            else
            {
                // On non-Windows platforms, the Skip property should remain null.
                Assert.Null(attribute.Skip);
            }
        }

        /// <summary>
        /// Tests the constructor of <see cref="SkipOnWindowsAttribute"/> when running on a non-Windows platform.
        /// Expected Outcome: The Skip property remains null regardless of the provided message.
        /// </summary>
        [Fact]
        public void Constructor_OnNonWindowsEnvironment_SkipPropertyRemainsNull()
        {
            // Arrange
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            // If the current platform is Windows, then this test scenario is not applicable.
            if (isWindows)
            {
                // This branch ensures the test exits early on Windows without failing.
                return;
            }

            // Act
            var attributeWithMessage = new SkipOnWindowsAttribute("Any message");
            var attributeWithNull = new SkipOnWindowsAttribute(null);
            
            // Assert
            Assert.Null(attributeWithMessage.Skip);
            Assert.Null(attributeWithNull.Skip);
        }
    }
}
