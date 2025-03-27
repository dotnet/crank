using Microsoft.Crank.Jobs.PipeliningClient;
using System;
using Xunit;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WorkerResult"/> class.
    /// </summary>
    public class WorkerResultTests
    {
        /// <summary>
        /// Tests that the default values of a new WorkerResult instance are zero.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_AreZero()
        {
            // Arrange & Act
            var workerResult = new WorkerResult();
            
            // Assert
            Assert.Equal(0, workerResult.Status1xx);
            Assert.Equal(0, workerResult.Status2xx);
            Assert.Equal(0, workerResult.Status3xx);
            Assert.Equal(0, workerResult.Status4xx);
            Assert.Equal(0, workerResult.Status5xx);
            Assert.Equal(0, workerResult.SocketErrors);
        }
        
        /// <summary>
        /// Tests that properties can be set and retrieved correctly with positive values.
        /// </summary>
        [Fact]
        public void Properties_SetPositiveValues_ReturnsCorrectValues()
        {
            // Arrange
            var workerResult = new WorkerResult();
            int expectedStatus1xx = 101;
            int expectedStatus2xx = 202;
            int expectedStatus3xx = 303;
            int expectedStatus4xx = 404;
            int expectedStatus5xx = 505;
            int expectedSocketErrors = 1;
            
            // Act
            workerResult.Status1xx = expectedStatus1xx;
            workerResult.Status2xx = expectedStatus2xx;
            workerResult.Status3xx = expectedStatus3xx;
            workerResult.Status4xx = expectedStatus4xx;
            workerResult.Status5xx = expectedStatus5xx;
            workerResult.SocketErrors = expectedSocketErrors;
            
            // Assert
            Assert.Equal(expectedStatus1xx, workerResult.Status1xx);
            Assert.Equal(expectedStatus2xx, workerResult.Status2xx);
            Assert.Equal(expectedStatus3xx, workerResult.Status3xx);
            Assert.Equal(expectedStatus4xx, workerResult.Status4xx);
            Assert.Equal(expectedStatus5xx, workerResult.Status5xx);
            Assert.Equal(expectedSocketErrors, workerResult.SocketErrors);
        }

        /// <summary>
        /// Tests that properties can be set and retrieved correctly with negative values.
        /// </summary>
        [Fact]
        public void Properties_SetNegativeValues_ReturnsAssignedNegativeValues()
        {
            // Arrange
            var workerResult = new WorkerResult();
            int negativeValue = -10;
            
            // Act
            workerResult.Status1xx = negativeValue;
            workerResult.Status2xx = negativeValue;
            workerResult.Status3xx = negativeValue;
            workerResult.Status4xx = negativeValue;
            workerResult.Status5xx = negativeValue;
            workerResult.SocketErrors = negativeValue;
            
            // Assert
            Assert.Equal(negativeValue, workerResult.Status1xx);
            Assert.Equal(negativeValue, workerResult.Status2xx);
            Assert.Equal(negativeValue, workerResult.Status3xx);
            Assert.Equal(negativeValue, workerResult.Status4xx);
            Assert.Equal(negativeValue, workerResult.Status5xx);
            Assert.Equal(negativeValue, workerResult.SocketErrors);
        }
    }
}
