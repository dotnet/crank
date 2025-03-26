using System;
using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Measurements"/> class.
    /// </summary>
    public class MeasurementsTests
    {
        /// <summary>
        /// Tests the <see cref="Measurements.GetBenchmarkProcessCpu(string)"/> method to ensure it returns the correctly formatted string
        /// when a valid process name is provided.
        /// </summary>
        [Fact]
        public void GetBenchmarkProcessCpu_WithValidProcessName_ReturnsCorrectFormattedString()
        {
            // Arrange
            string processName = "testProcess";
            string expected = "benchmarks/testProcess/cpu";

            // Act
            string actual = Measurements.GetBenchmarkProcessCpu(processName);

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests the <see cref="Measurements.GetBenchmarkProcessCpu(string)"/> method to ensure it returns the correctly formatted string
        /// when an empty string is provided as the process name.
        /// </summary>
        [Fact]
        public void GetBenchmarkProcessCpu_WithEmptyProcessName_ReturnsCorrectFormattedString()
        {
            // Arrange
            string processName = "";
            string expected = "benchmarks//cpu";

            // Act
            string actual = Measurements.GetBenchmarkProcessCpu(processName);

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Tests the <see cref="Measurements.GetBenchmarkProcessCpu(string)"/> method to ensure it returns the correctly formatted string
        /// when a null value is provided as the process name.
        /// </summary>
        [Fact]
        public void GetBenchmarkProcessCpu_WithNullProcessName_ReturnsCorrectFormattedString()
        {
            // Arrange
            string processName = null;
            string expected = "benchmarks//cpu";

            // Act
            string actual = Measurements.GetBenchmarkProcessCpu(processName);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
