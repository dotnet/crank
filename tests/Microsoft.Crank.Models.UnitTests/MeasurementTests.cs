using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Measurement"/> class.
    /// </summary>
    [TestClass]
    public class MeasurementTests
    {
        private readonly Measurement _measurement;

        public MeasurementTests()
        {
            _measurement = new Measurement();
        }

        /// <summary>
        /// Tests the <see cref="Measurement.IsDelimiter"/> property to ensure it returns true when the Name is equal to the Delimiter.
        /// </summary>
        [TestMethod]
        public void IsDelimiter_WhenNameIsEqualToDelimiter_ReturnsTrue()
        {
            // Arrange
            _measurement.Name = Measurement.Delimiter;

            // Act
            bool result = _measurement.IsDelimiter;

            // Assert
            Assert.IsTrue(result, "IsDelimiter should return true when Name is equal to the Delimiter.");
        }

        /// <summary>
        /// Tests the <see cref="Measurement.IsDelimiter"/> property to ensure it returns false when the Name is not equal to the Delimiter.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("SomeOtherName")]
        public void IsDelimiter_WhenNameIsNotEqualToDelimiter_ReturnsFalse(string name)
        {
            // Arrange
            _measurement.Name = name;

            // Act
            bool result = _measurement.IsDelimiter;

            // Assert
            Assert.IsFalse(result, "IsDelimiter should return false when Name is not equal to the Delimiter.");
        }
    }
}
