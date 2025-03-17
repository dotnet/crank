// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Crank.Jobs.HttpClientClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WorkerResult"/> class.
    /// </summary>
    [TestClass]
    public class WorkerResultTests
    {
        private readonly WorkerResult _workerResult;

        public WorkerResultTests()
        {
            _workerResult = new WorkerResult();
        }

        /// <summary>
        /// Tests the <see cref="WorkerResult.AverageRps"/> property to ensure it correctly calculates the average requests per second.
        /// </summary>
        [TestMethod]
        public void AverageRps_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            _workerResult.Started = DateTime.Now;
            _workerResult.Stopped = _workerResult.Started.AddSeconds(10);
            _workerResult.Status1xx = 100;
            _workerResult.Status2xx = 200;
            _workerResult.Status3xx = 300;
            _workerResult.Status4xx = 400;
            _workerResult.Status5xx = 500;

            // Act
            long actualResult = _workerResult.AverageRps;

            // Assert
            Assert.AreEqual(150, actualResult, "AverageRps calculation is incorrect.");
        }

        /// <summary>
        /// Tests the <see cref="WorkerResult.TotalRequests"/> property to ensure it correctly calculates the total number of requests.
        /// </summary>
        [TestMethod]
        public void TotalRequests_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            _workerResult.Status1xx = 100;
            _workerResult.Status2xx = 200;
            _workerResult.Status3xx = 300;
            _workerResult.Status4xx = 400;
            _workerResult.Status5xx = 500;

            // Act
            long actualResult = _workerResult.TotalRequests;

            // Assert
            Assert.AreEqual(1500, actualResult, "TotalRequests calculation is incorrect.");
        }

        /// <summary>
        /// Tests the <see cref="WorkerResult.DurationMs"/> property to ensure it correctly calculates the duration in milliseconds.
        /// </summary>
        [TestMethod]
        public void DurationMs_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            _workerResult.Started = DateTime.Now;
            _workerResult.Stopped = _workerResult.Started.AddSeconds(10);

            // Act
            long actualResult = _workerResult.DurationMs;

            // Assert
            Assert.AreEqual(10000, actualResult, "DurationMs calculation is incorrect.");
        }

        /// <summary>
        /// Tests the <see cref="WorkerResult.BadResponses"/> property to ensure it correctly calculates the number of bad responses.
        /// </summary>
        [TestMethod]
        public void BadResponses_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            _workerResult.Status1xx = 100;
            _workerResult.Status4xx = 400;
            _workerResult.Status5xx = 500;

            // Act
            long actualResult = _workerResult.BadResponses;

            // Assert
            Assert.AreEqual(1000, actualResult, "BadResponses calculation is incorrect.");
        }
    }
}
