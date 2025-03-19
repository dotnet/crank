using System.Collections.Generic;
using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CounterProfile"/> class.
    /// </summary>
    public class CounterProfileTests
    {
        /// <summary>
        /// Verifies that the properties of the CounterProfile class can be assigned and retrieved correctly.
        /// </summary>
        [Fact]
        public void PropertySet_GetProperties_ReturnsAssignedValues()
        {
            // Arrange
            const string expectedName = "TestCounter";
            const string expectedDisplayName = "Test Counter Display";
            const string expectedDescription = "Description for test counter";
            const string expectedFormat = "n2";
            Func<IEnumerable<double>, double> expectedCompute = values =>
            {
                double sum = 0;
                foreach (var value in values)
                {
                    sum += value;
                }
                return sum;
            };

            var profile = new CounterProfile();

            // Act
            profile.Name = expectedName;
            profile.DisplayName = expectedDisplayName;
            profile.Description = expectedDescription;
            profile.Format = expectedFormat;
            profile.Compute = expectedCompute;

            // Assert
            Assert.Equal(expectedName, profile.Name);
            Assert.Equal(expectedDisplayName, profile.DisplayName);
            Assert.Equal(expectedDescription, profile.Description);
            Assert.Equal(expectedFormat, profile.Format);
            Assert.Equal(expectedCompute, profile.Compute);
        }

        /// <summary>
        /// Verifies that the Compute delegate correctly aggregates values when assigned a sum delegate.
        /// </summary>
        [Fact]
        public void Compute_WhenAssignedWithSumDelegate_ReturnsCorrectSum()
        {
            // Arrange
            var profile = new CounterProfile
            {
                Compute = values =>
                {
                    double sum = 0;
                    foreach (var value in values)
                    {
                        sum += value;
                    }
                    return sum;
                }
            };

            var testValues = new List<double> { 1.0, 2.5, 3.5 };
            double expectedSum = 7.0;

            // Act
            double actualSum = profile.Compute(testValues);

            // Assert
            Assert.Equal(expectedSum, actualSum);
        }

        /// <summary>
        /// Verifies that the Compute delegate handles an empty collection correctly by returning zero.
        /// </summary>
        [Fact]
        public void Compute_WhenGivenEmptyCollection_ReturnsZero()
        {
            // Arrange
            var profile = new CounterProfile
            {
                Compute = values =>
                {
                    double sum = 0;
                    foreach (var value in values)
                    {
                        sum += value;
                    }
                    return sum;
                }
            };

            var testValues = new List<double>();

            // Act
            double actualSum = profile.Compute(testValues);

            // Assert
            Assert.Equal(0, actualSum);
        }

        /// <summary>
        /// Verifies that reference type properties of CounterProfile can be assigned null and retrieved as null.
        /// </summary>
        [Fact]
        public void Properties_WhenAssignedNullValues_ReturnsNullForReferenceTypes()
        {
            // Arrange
            var profile = new CounterProfile();

            // Act
            profile.Name = null;
            profile.DisplayName = null;
            profile.Description = null;
            profile.Format = null;
            profile.Compute = null;

            // Assert
            Assert.Null(profile.Name);
            Assert.Null(profile.DisplayName);
            Assert.Null(profile.Description);
            Assert.Null(profile.Format);
            Assert.Null(profile.Compute);
        }

        /// <summary>
        /// Verifies that invoking a null Compute delegate results in a NullReferenceException.
        /// </summary>
        [Fact]
        public void Compute_WhenDelegateIsNull_ThrowsNullReferenceException()
        {
            // Arrange
            var profile = new CounterProfile
            {
                Compute = null
            };
            var testValues = new List<double> { 1.0, 2.0 };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
            {
                var result = profile.Compute(testValues);
            });
        }
    }
}
