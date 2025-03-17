using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.RegressionBot.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Regression"/> class.
    /// </summary>
    [TestClass]
    public class RegressionTests
    {
        private readonly Regression _regression;
        private readonly Mock<BenchmarksResult> _previousResultMock;
        private readonly Mock<BenchmarksResult> _currentResultMock;
        private readonly Mock<BenchmarksResult> _recoveredResultMock;

        public RegressionTests()
        {
            _previousResultMock = new Mock<BenchmarksResult>();
            _currentResultMock = new Mock<BenchmarksResult>();
            _recoveredResultMock = new Mock<BenchmarksResult>();

            _regression = new Regression
            {
                PreviousResult = _previousResultMock.Object,
                CurrentResult = _currentResultMock.Object,
                RecoveredResult = _recoveredResultMock.Object
            };
        }

        /// <summary>
        /// Tests the <see cref="Regression.HasRecovered"/> property to ensure it returns true when RecoveredResult is not null.
        /// </summary>
        [TestMethod]
        public void HasRecovered_WhenRecoveredResultIsNotNull_ReturnsTrue()
        {
            // Act
            var result = _regression.HasRecovered;

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="Regression.HasRecovered"/> property to ensure it returns false when RecoveredResult is null.
        /// </summary>
        [TestMethod]
        public void HasRecovered_WhenRecoveredResultIsNull_ReturnsFalse()
        {
            // Arrange
            _regression.RecoveredResult = null;

            // Act
            var result = _regression.HasRecovered;

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="Regression.Identifier"/> property to ensure it returns the correct identifier string.
        /// </summary>
        [TestMethod]
        public void Identifier_WhenCalled_ReturnsCorrectIdentifier()
        {
            // Arrange
            _currentResultMock.SetupGet(r => r.Scenario).Returns("Scenario1");
            _currentResultMock.SetupGet(r => r.Description).Returns("Description1");
            _currentResultMock.SetupGet(r => r.DateTimeUtc).Returns(DateTime.UtcNow);

            var expectedIdentifier = $"Id:Scenario1Description1{_currentResultMock.Object.DateTimeUtc}";

            // Act
            var result = _regression.Identifier;

            // Assert
            Assert.AreEqual(expectedIdentifier, result);
        }

        /// <summary>
        /// Tests the <see cref="Regression.ComputeChanges"/> method to ensure it correctly computes changes between dependencies.
        /// </summary>
        [TestMethod]
        public void ComputeChanges_WhenCalled_ComputesChangesCorrectly()
        {
            // Arrange
            var previousJobResults = new JobResults
            {
                Jobs = new Dictionary<string, JobResult>
                {
                    { "Job1", new JobResult { Dependencies = new Dependency[0], Results = new Dictionary<string, object>() } }
                }
            };

            var currentJobResults = new JobResults
            {
                Jobs = new Dictionary<string, JobResult>
                {
                    { "Job1", new JobResult { Dependencies = new Dependency[0], Results = new Dictionary<string, object>() } }
                }
            };

            _previousResultMock.SetupGet(r => r.Document).Returns(JsonSerializer.Serialize(previousJobResults));
            _currentResultMock.SetupGet(r => r.Document).Returns(JsonSerializer.Serialize(currentJobResults));

            // Act
            _regression.ComputeChanges();

            // Assert
            Assert.IsTrue(_regression.Changes.Count > 0);
        }
    }
}

