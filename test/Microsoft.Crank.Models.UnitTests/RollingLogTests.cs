using System;
using System.Linq;
using System.Text;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="RollingLog"/> class.
    /// </summary>
    public class RollingLogTests
    {
        private readonly int _defaultCapacity;

        public RollingLogTests()
        {
            _defaultCapacity = 3;
        }

        /// <summary>
        /// Verifies that constructing the RollingLog with a negative capacity throws an ArgumentOutOfRangeException.
        /// </summary>
        [Fact]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int negativeCapacity = -1;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new RollingLog(negativeCapacity));
        }

        /// <summary>
        /// Verifies that AddLine adds a new line when the log has not reached its capacity.
        /// Expected outcome: The log contains the added line and LastLine returns it.
        /// </summary>
        [Fact]
        public void AddLine_WhenNotExceedingCapacity_AddsLineSuccessfully()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            string testLine = "Test line";

            // Act
            rollingLog.AddLine(testLine);

            // Assert
            Assert.Equal(testLine, rollingLog.LastLine);
            var lines = rollingLog.Get(0);
            Assert.Single(lines);
            Assert.Equal(testLine, lines[0]);
        }

        /// <summary>
        /// Verifies that AddLine discards the oldest line when the capacity is exceeded.
        /// Expected outcome: The first added line is discarded and the log retains only the most recent lines.
        /// </summary>
        [Fact]
        public void AddLine_WhenExceedingCapacity_DiscardsOldestLine()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("Line1");
            rollingLog.AddLine("Line2");
            rollingLog.AddLine("Line3");

            // Act
            rollingLog.AddLine("Line4"); // This addition should discard "Line1".

            // Assert
            var lines = rollingLog.Get(0);
            Assert.Equal(_defaultCapacity, lines.Length);
            Assert.DoesNotContain("Line1", lines);
            Assert.Equal("Line4", rollingLog.LastLine);
        }

        /// <summary>
        /// Verifies that LastLine returns an empty string when no lines have been added.
        /// Expected outcome: LastLine returns an empty string.
        /// </summary>
        [Fact]
        public void LastLine_WhenNoLinesAdded_ReturnsEmptyString()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);

            // Act
            string lastLine = rollingLog.LastLine;

            // Assert
            Assert.Equal(string.Empty, lastLine);
        }

        /// <summary>
        /// Verifies that LastLine returns the most recently added line when lines exist.
        /// Expected outcome: LastLine returns the last line added.
        /// </summary>
        [Fact]
        public void LastLine_WhenLinesPresent_ReturnsMostRecentLine()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("First line");
            rollingLog.AddLine("Second line");

            // Act
            string lastLine = rollingLog.LastLine;

            // Assert
            Assert.Equal("Second line", lastLine);
        }

        /// <summary>
        /// Verifies that Get(int skip) returns the correct subset of lines adjusted for discarded lines.
        /// Expected outcome: The returned array reflects the skip parameter accounting for discarded lines.
        /// </summary>
        [Fact]
        public void Get_WithSkipParameter_AdjustsForDiscardedLines()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("Line1");
            rollingLog.AddLine("Line2");
            rollingLog.AddLine("Line3");
            rollingLog.AddLine("Line4"); // "Line1" is discarded; Discarded becomes 1.

            // Act
            // When skip is 1, adjusted skip becomes max(0, 1 - 1) = 0 and returns all current lines.
            var resultAll = rollingLog.Get(1);
            // When skip is 2, adjusted skip becomes max(0, 2 - 1) = 1 and returns the log from the second element onward.
            var resultSkipOne = rollingLog.Get(2);

            // Assert
            Assert.Equal(new[] { "Line2", "Line3", "Line4" }, resultAll);
            Assert.Equal(new[] { "Line3", "Line4" }, resultSkipOne);
        }

        /// <summary>
        /// Verifies that Get(int skip, int take) returns the correct subset of lines.
        /// Expected outcome: The subset of lines returned matches the specified skip and take parameters after adjustment.
        /// </summary>
        [Fact]
        public void Get_WithSkipAndTakeParameters_ReturnsCorrectSubset()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("Line1");
            rollingLog.AddLine("Line2");
            rollingLog.AddLine("Line3");
            rollingLog.AddLine("Line4"); // "Line1" is discarded; Discarded becomes 1.

            // Act
            // For skip = 2, adjusted skip becomes 2 - 1 = 1, then taking 1 should yield "Line3".
            var subset = rollingLog.Get(2, 1);

            // Assert
            Assert.Single(subset);
            Assert.Equal("Line3", subset[0]);
        }

        /// <summary>
        /// Verifies that Clear removes all lines and resets the discarded count.
        /// Expected outcome: Subsequent calls to LastLine and Get return an empty result.
        /// </summary>
        [Fact]
        public void Clear_WhenCalled_RemovesAllLinesAndResetsState()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("Line1");
            rollingLog.AddLine("Line2");

            // Act
            rollingLog.Clear();

            // Assert
            var lines = rollingLog.Get(0);
            Assert.Empty(lines);
            Assert.Equal(string.Empty, rollingLog.LastLine);
        }

        /// <summary>
        /// Verifies that ToString returns all current log lines concatenated with a newline after each line.
        /// Expected outcome: The result string is equal to the concatenation of lines with Environment.NewLine after each.
        /// </summary>
        [Fact]
        public void ToString_ReturnsConcatenatedLinesWithNewlines()
        {
            // Arrange
            var rollingLog = new RollingLog(_defaultCapacity);
            rollingLog.AddLine("Line1");
            rollingLog.AddLine("Line2");
            rollingLog.AddLine("Line3");

            // Act
            string result = rollingLog.ToString();
            var expectedBuilder = new StringBuilder();
            expectedBuilder.AppendLine("Line1");
            expectedBuilder.AppendLine("Line2");
            expectedBuilder.AppendLine("Line3");
            string expected = expectedBuilder.ToString();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
