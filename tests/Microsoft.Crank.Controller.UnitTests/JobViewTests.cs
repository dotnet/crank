using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobView"/> class.
    /// </summary>
    public class JobViewTests
    {
        private readonly JobView _jobView;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobViewTests"/> class.
        /// </summary>
        public JobViewTests()
        {
            _jobView = new JobView();
        }

        /// <summary>
        /// Tests that a new instance of <see cref="JobView"/> initializes all properties to their default values.
        /// Expected default values: Id should be 0, RunId and State should be null.
        /// </summary>
        [Fact]
        public void DefaultConstructor_ShouldInitializePropertiesToDefault()
        {
            // Arrange
            var jobView = new JobView();

            // Act & Assert
            Assert.Equal(0, jobView.Id);
            Assert.Null(jobView.RunId);
            Assert.Null(jobView.State);
        }

        /// <summary>
        /// Tests that the Id property can be set and retrieved correctly for various integer values.
        /// </summary>
        /// <param name="value">The integer value to assign to the Id property.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void IdProperty_SetAndGet_ReturnsSameValue(int value)
        {
            // Act
            _jobView.Id = value;

            // Assert
            Assert.Equal(value, _jobView.Id);
        }

        /// <summary>
        /// Tests that the RunId property can be set and retrieved correctly, including edge cases such as null and empty strings.
        /// </summary>
        /// <param name="value">The string value to assign to the RunId property.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("run123")]
        public void RunIdProperty_SetAndGet_ReturnsSameValue(string value)
        {
            // Act
            _jobView.RunId = value;

            // Assert
            Assert.Equal(value, _jobView.RunId);
        }

        /// <summary>
        /// Tests that the State property can be set and retrieved correctly, including edge cases such as null and empty strings.
        /// </summary>
        /// <param name="value">The string value to assign to the State property.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Pending")]
        [InlineData("Completed")]
        public void StateProperty_SetAndGet_ReturnsSameValue(string value)
        {
            // Act
            _jobView.State = value;

            // Assert
            Assert.Equal(value, _jobView.State);
        }
    }
}
