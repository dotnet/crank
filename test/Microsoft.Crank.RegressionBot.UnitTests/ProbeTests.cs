using Microsoft.Crank.RegressionBot;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Probe"/> class.
    /// </summary>
    public class ProbeTests
    {
        /// <summary>
        /// Verifies that a new instance of <see cref="Probe"/> has the expected default property values.
        /// Expected: Path is null, Threshold equals 1, and Unit equals ThresholdUnits.StDev.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var probe = new Probe();

            // Assert
            Assert.Null(probe.Path);
            Assert.Equal(1, probe.Threshold);
            Assert.Equal(ThresholdUnits.StDev, probe.Unit);
        }

        /// <summary>
        /// Verifies that the <see cref="Probe.Path"/> property can be set and retrieved correctly.
        /// </summary>
        /// <param name="expectedPath">The value to set for the Path property.</param>
        [Theory]
        [InlineData("sample/path")]
        [InlineData("")]
        [InlineData(null)]
        public void Path_SetAndGet_ReturnsExpectedValue(string expectedPath)
        {
            // Arrange
            var probe = new Probe();

            // Act
            probe.Path = expectedPath;

            // Assert
            Assert.Equal(expectedPath, probe.Path);
        }

        /// <summary>
        /// Verifies that the <see cref="Probe.Threshold"/> property can be set and retrieved correctly.
        /// Tests various numeric values including edge cases.
        /// </summary>
        /// <param name="expectedThreshold">The numeric value to set as Threshold.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(100.5)]
        public void Threshold_SetAndGet_ReturnsExpectedValue(double expectedThreshold)
        {
            // Arrange
            var probe = new Probe();

            // Act
            probe.Threshold = expectedThreshold;

            // Assert
            Assert.Equal(expectedThreshold, probe.Threshold);
        }

        /// <summary>
        /// Verifies that the <see cref="Probe.Unit"/> property can be set and retrieved correctly for each enum value.
        /// </summary>
        /// <param name="expectedUnit">The enum value to set as Unit.</param>
        [Theory]
        [InlineData(ThresholdUnits.None)]
        [InlineData(ThresholdUnits.StDev)]
        [InlineData(ThresholdUnits.Percent)]
        [InlineData(ThresholdUnits.Absolute)]
        public void Unit_SetAndGet_ReturnsExpectedValue(ThresholdUnits expectedUnit)
        {
            // Arrange
            var probe = new Probe();

            // Act
            probe.Unit = expectedUnit;

            // Assert
            Assert.Equal(expectedUnit, probe.Unit);
        }
    }
}
