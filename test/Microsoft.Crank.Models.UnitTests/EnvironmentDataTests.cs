using Microsoft.Crank.Models;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="EnvironmentData"/> class.
    /// </summary>
    public class EnvironmentDataTests
    {
        /// <summary>
        /// Tests that the Platform and Architecture properties return the expected values.
        /// The test computes the expected platform based on the runtime OS checks and the expected architecture from RuntimeInformation.
        /// It then asserts that a new instance of <see cref="EnvironmentData"/> returns matching property values.
        /// </summary>
        [Fact]
        public void EnvironmentData_Initialization_PropertiesReturnExpectedValues()
        {
            // Arrange
            string expectedPlatform;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedPlatform = "windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                expectedPlatform = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                expectedPlatform = "osx";
            }
            else
            {
                expectedPlatform = "other";
            }

            string expectedArchitecture = RuntimeInformation.OSArchitecture.ToString();

            // Act
            var environmentData = new EnvironmentData();

            // Assert
            Assert.Equal(expectedPlatform, environmentData.Platform);
            Assert.Equal(expectedArchitecture, environmentData.Architecture);
        }

        /// <summary>
        /// Tests that multiple instances of <see cref="EnvironmentData"/> consistently return the same static property values.
        /// This verifies that the static fields are initialized consistently across different instances.
        /// </summary>
        [Fact]
        public void EnvironmentData_MultipleInstances_ShouldReturnSameStaticProperties()
        {
            // Arrange
            var firstInstance = new EnvironmentData();
            var secondInstance = new EnvironmentData();

            // Act
            string firstPlatform = firstInstance.Platform;
            string firstArchitecture = firstInstance.Architecture;
            string secondPlatform = secondInstance.Platform;
            string secondArchitecture = secondInstance.Architecture;

            // Assert
            Assert.Equal(firstPlatform, secondPlatform);
            Assert.Equal(firstArchitecture, secondArchitecture);
        }
    }
}
