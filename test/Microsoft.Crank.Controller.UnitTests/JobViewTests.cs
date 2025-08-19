using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobView"/> class.
    /// </summary>
    public class JobViewTests
    {
        /// <summary>
        /// Tests the getter and setter of the Id property to ensure it correctly stores and retrieves an integer value.
        /// </summary>
        [Fact]
        public void IdProperty_SetAndGetValue_ReturnsExpectedValue()
        {
            // Arrange
            int expectedId = 42;
            var jobView = new JobView();

            // Act
            jobView.Id = expectedId;
            int actualId = jobView.Id;

            // Assert
            Assert.Equal(expectedId, actualId);
        }

        /// <summary>
        /// Tests the getter and setter of the RunId property to ensure it correctly stores and retrieves a string value.
        /// </summary>
        [Fact]
        public void RunIdProperty_SetAndGetValue_ReturnsExpectedValue()
        {
            // Arrange
            string expectedRunId = "run_001";
            var jobView = new JobView();

            // Act
            jobView.RunId = expectedRunId;
            string actualRunId = jobView.RunId;

            // Assert
            Assert.Equal(expectedRunId, actualRunId);
        }

        /// <summary>
        /// Tests the getter and setter of the State property to ensure it correctly stores and retrieves a string value.
        /// </summary>
        [Fact]
        public void StateProperty_SetAndGetValue_ReturnsExpectedValue()
        {
            // Arrange
            string expectedState = "Running";
            var jobView = new JobView();

            // Act
            jobView.State = expectedState;
            string actualState = jobView.State;

            // Assert
            Assert.Equal(expectedState, actualState);
        }

        /// <summary>
        /// Tests that the RunId property can be assigned a null value without causing errors.
        /// </summary>
        [Fact]
        public void RunIdProperty_SetNullValue_ReturnsNull()
        {
            // Arrange
            string expectedRunId = null;
            var jobView = new JobView();

            // Act
            jobView.RunId = expectedRunId;
            string actualRunId = jobView.RunId;

            // Assert
            Assert.Null(actualRunId);
        }

        /// <summary>
        /// Tests that the State property can be assigned a null value without causing errors.
        /// </summary>
        [Fact]
        public void StateProperty_SetNullValue_ReturnsNull()
        {
            // Arrange
            string expectedState = null;
            var jobView = new JobView();

            // Act
            jobView.State = expectedState;
            string actualState = jobView.State;

            // Assert
            Assert.Null(actualState);
        }
    }
}
