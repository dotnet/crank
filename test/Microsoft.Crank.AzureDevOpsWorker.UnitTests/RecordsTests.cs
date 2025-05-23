using Microsoft.Crank.AzureDevOpsWorker;
using System;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Records"/> class.
    /// </summary>
    public class RecordsTests
    {
        /// <summary>
        /// Tests that the Count property of Records can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Count_WhenSet_GetReturnsCorrectValue()
        {
            // Arrange
            int expectedCount = 5;
            var records = new Records();

            // Act
            records.Count = expectedCount;
            int actualCount = records.Count;

            // Assert
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// Tests that the Value property of Records can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Value_WhenSet_GetReturnsCorrectValue()
        {
            // Arrange
            var record1 = new Record { Id = "1", State = "completed", Result = "succeeded" };
            var record2 = new Record { Id = "2", State = "pending", Result = null };
            Record[] expectedRecords = new Record[] { record1, record2 };
            var records = new Records();

            // Act
            records.Value = expectedRecords;
            Record[] actualRecords = records.Value;

            // Assert
            Assert.Equal(expectedRecords, actualRecords);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Record"/> class.
    /// </summary>
    public class RecordTests
    {
        /// <summary>
        /// Tests that the Id property of Record can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Id_WhenSet_GetReturnsSameValue()
        {
            // Arrange
            var expectedId = "12345";
            var record = new Record();

            // Act
            record.Id = expectedId;
            var actualId = record.Id;

            // Assert
            Assert.Equal(expectedId, actualId);
        }

        /// <summary>
        /// Tests that the State property of Record can be set and retrieved correctly with valid values.
        /// </summary>
        /// <param name="stateValue">The state value to test.</param>
        [Theory]
        [InlineData("completed")]
        [InlineData("pending")]
        public void State_WhenSetWithValidValues_GetReturnsSameValue(string stateValue)
        {
            // Arrange
            var record = new Record();

            // Act
            record.State = stateValue;
            var actualState = record.State;

            // Assert
            Assert.Equal(stateValue, actualState);
        }

        /// <summary>
        /// Tests that the State property of Record can be set to null and retrieved correctly.
        /// </summary>
        [Fact]
        public void State_WhenSetToNull_GetReturnsNull()
        {
            // Arrange
            var record = new Record();

            // Act
            record.State = null;
            var actualState = record.State;

            // Assert
            Assert.Null(actualState);
        }

        /// <summary>
        /// Tests that the Result property of Record can be set and retrieved correctly with valid values.
        /// </summary>
        /// <param name="resultValue">The result value to test.</param>
        [Theory]
        [InlineData("succeeded")]
        [InlineData("skipped")]
        [InlineData("failed")]
        public void Result_WhenSetWithValidValues_GetReturnsSameValue(string resultValue)
        {
            // Arrange
            var record = new Record();

            // Act
            record.Result = resultValue;
            var actualResult = record.Result;

            // Assert
            Assert.Equal(resultValue, actualResult);
        }

        /// <summary>
        /// Tests that the Result property of Record can be set to null and retrieved correctly.
        /// </summary>
        [Fact]
        public void Result_WhenSetToNull_GetReturnsNull()
        {
            // Arrange
            var record = new Record();

            // Act
            record.Result = null;
            var actualResult = record.Result;

            // Assert
            Assert.Null(actualResult);
        }
    }
}
