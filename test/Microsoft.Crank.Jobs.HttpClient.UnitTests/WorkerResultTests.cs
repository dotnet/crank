using System;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WorkerResult"/> class.
    /// </summary>
    public class WorkerResultTests
    {
        private readonly DateTime _baseTime;

        public WorkerResultTests()
        {
            _baseTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Tests that AverageRps is correctly calculated when the duration is positive.
        /// Arrange: Sets the status codes and time interval to produce a known request total and duration.
        /// Act: Reads the AverageRps property.
        /// Assert: The calculated average requests per second matches the expected value.
        /// </summary>
        [Fact]
        public void AverageRps_HappyPath_CalculatesCorrectly()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Status1xx = 10,
                Status2xx = 20,
                Status3xx = 5,
                Status4xx = 3,
                Status5xx = 2,
                Started = _baseTime,
                Stopped = _baseTime.AddSeconds(10)
            };
            long totalRequests = 10 + 20 + 5 + 3 + 2; // 40 requests
            double durationSeconds = (workerResult.Stopped - workerResult.Started).TotalSeconds; // 10 seconds
            long expectedAverageRps = (long)(totalRequests / durationSeconds); // 4

            // Act
            long actualAverageRps = workerResult.AverageRps;

            // Assert
            Assert.Equal(expectedAverageRps, actualAverageRps);
        }

        /// <summary>
        /// Tests that DurationMs is correctly calculated based on the time interval.
        /// Arrange: Sets the Started and Stopped times.
        /// Act: Reads the DurationMs property.
        /// Assert: The returned duration in milliseconds matches the expected value.
        /// </summary>
        [Fact]
        public void DurationMs_HappyPath_CalculatesCorrectly()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Started = _baseTime,
                Stopped = _baseTime.AddSeconds(10)
            };
            long expectedDurationMs = (long)((workerResult.Stopped - workerResult.Started).TotalMilliseconds); // 10000

            // Act
            long actualDurationMs = workerResult.DurationMs;

            // Assert
            Assert.Equal(expectedDurationMs, actualDurationMs);
        }

        /// <summary>
        /// Tests that TotalRequests property correctly sums all status codes.
        /// Arrange: Sets various status codes.
        /// Act: Reads the TotalRequests property.
        /// Assert: The calculated total requests is the sum of all status code counts.
        /// </summary>
        [Fact]
        public void TotalRequests_CalculatesCorrectly()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Status1xx = 1,
                Status2xx = 2,
                Status3xx = 3,
                Status4xx = 4,
                Status5xx = 5
            };
            int expectedTotal = 1 + 2 + 3 + 4 + 5;

            // Act
            int actualTotal = (int)workerResult.TotalRequests;

            // Assert
            Assert.Equal(expectedTotal, actualTotal);
        }

        /// <summary>
        /// Tests that BadResponses property correctly sums only the status codes considered as bad responses.
        /// Arrange: Sets status codes for 1xx, 4xx, and 5xx.
        /// Act: Reads the BadResponses property.
        /// Assert: The calculated bad responses is the sum of Status1xx, Status4xx, and Status5xx.
        /// </summary>
        [Fact]
        public void BadResponses_CalculatesCorrectly()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Status1xx = 7,
                Status4xx = 3,
                Status5xx = 5
            };
            int expectedBadResponses = 7 + 3 + 5;

            // Act
            int actualBadResponses = (int)workerResult.BadResponses;

            // Assert
            Assert.Equal(expectedBadResponses, actualBadResponses);
        }

        /// <summary>
        /// Tests that LatencyMeanMs properly stores and retrieves a value.
        /// Arrange: Sets LatencyMeanMs value.
        /// Act: Retrieves the value from the property.
        /// Assert: The value remains unchanged.
        /// </summary>
        [Fact]
        public void LatencyMeanMs_GetSet_WorksCorrectly()
        {
            // Arrange
            var expectedLatency = 150.5;
            var workerResult = new WorkerResult
            {
                LatencyMeanMs = expectedLatency
            };

            // Act
            double actualLatency = workerResult.LatencyMeanMs;

            // Assert
            Assert.Equal(expectedLatency, actualLatency);
        }

        /// <summary>
        /// Tests that LatencyMaxMs properly stores and retrieves a value.
        /// Arrange: Sets LatencyMaxMs value.
        /// Act: Retrieves the value from the property.
        /// Assert: The value remains unchanged.
        /// </summary>
        [Fact]
        public void LatencyMaxMs_GetSet_WorksCorrectly()
        {
            // Arrange
            var expectedLatencyMax = 300.75;
            var workerResult = new WorkerResult
            {
                LatencyMaxMs = expectedLatencyMax
            };

            // Act
            double actualLatencyMax = workerResult.LatencyMaxMs;

            // Assert
            Assert.Equal(expectedLatencyMax, actualLatencyMax);
        }

        /// <summary>
        /// Tests that ThroughputBps properly stores and retrieves a value.
        /// Arrange: Sets ThroughputBps value.
        /// Act: Retrieves the value from the property.
        /// Assert: The value remains unchanged.
        /// </summary>
        [Fact]
        public void ThroughputBps_GetSet_WorksCorrectly()
        {
            // Arrange
            var expectedThroughput = 1024L;
            var workerResult = new WorkerResult
            {
                ThroughputBps = expectedThroughput
            };

            // Act
            long actualThroughput = workerResult.ThroughputBps;

            // Assert
            Assert.Equal(expectedThroughput, actualThroughput);
        }

        /// <summary>
        /// Tests that AverageRps property causes an OverflowException when the time interval is zero.
        /// Arrange: Sets Started and Stopped to the same time with non-zero total requests.
        /// Act: Attempts to access AverageRps.
        /// Assert: An OverflowException is thrown due to casting a division result of infinity to a long.
        /// </summary>
        [Fact]
        public void AverageRps_TimeIntervalZero_ThrowsOverflowException()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Status1xx = 1,
                Status2xx = 1,
                Status3xx = 1,
                Status4xx = 1,
                Status5xx = 1,
                Started = _baseTime,
                Stopped = _baseTime
            };

            // Act & Assert
            Assert.Throws<OverflowException>(() =>
            {
                var _ = workerResult.AverageRps;
            });
        }

        /// <summary>
        /// Tests that AverageRps property correctly handles a negative time interval.
        /// Arrange: Sets Started later than Stopped resulting in a negative duration.
        /// Act: Reads the AverageRps property.
        /// Assert: The computed average requests per second is negative as expected.
        /// </summary>
        [Fact]
        public void AverageRps_NegativeTimeInterval_CorrectCalculation()
        {
            // Arrange
            var workerResult = new WorkerResult
            {
                Status1xx = 10,
                Status2xx = 10,
                Status3xx = 10,
                Status4xx = 10,
                Status5xx = 10, // Total = 50
                // Set Started later than Stopped to get a negative duration of -10 seconds.
                Started = _baseTime.AddSeconds(10),
                Stopped = _baseTime
            };
            double durationSeconds = (workerResult.Stopped - workerResult.Started).TotalSeconds; // -10 seconds
            long expectedAverageRps = (long)(50 / durationSeconds); // -5

            // Act
            long actualAverageRps = workerResult.AverageRps;

            // Assert
            Assert.Equal(expectedAverageRps, actualAverageRps);
        }
    }
}
