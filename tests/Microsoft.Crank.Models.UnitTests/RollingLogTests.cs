using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="RollingLog"/> class.
    /// </summary>
    [TestClass]
    public class RollingLogTests
    {
        private readonly RollingLog _rollingLog;
        private const int DefaultCapacity = 5;

        public RollingLogTests()
        {
            _rollingLog = new RollingLog(DefaultCapacity);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.AddLine(string)"/> method to ensure it correctly adds a line.
        /// </summary>
        [TestMethod]
        public void AddLine_WhenCalled_AddsLine()
        {
            // Arrange
            string line = "Test line";

            // Act
            _rollingLog.AddLine(line);

            // Assert
            Assert.AreEqual(line, _rollingLog.LastLine);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.AddLine(string)"/> method to ensure it discards the oldest line when capacity is exceeded.
        /// </summary>
        [TestMethod]
        public void AddLine_WhenCapacityExceeded_DiscardsOldestLine()
        {
            // Arrange
            for (int i = 0; i < DefaultCapacity; i++)
            {
                _rollingLog.AddLine($"Line {i}");
            }

            // Act
            _rollingLog.AddLine("New line");

            // Assert
            Assert.AreEqual("Line 1", _rollingLog.Get(0).First());
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.LastLine"/> property to ensure it returns the last added line.
        /// </summary>
        [TestMethod]
        public void LastLine_WhenCalled_ReturnsLastLine()
        {
            // Arrange
            string line = "Last line";
            _rollingLog.AddLine(line);

            // Act
            string result = _rollingLog.LastLine;

            // Assert
            Assert.AreEqual(line, result);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.LastLine"/> property to ensure it returns an empty string when no lines are present.
        /// </summary>
        [TestMethod]
        public void LastLine_WhenNoLines_ReturnsEmptyString()
        {
            // Act
            string result = _rollingLog.LastLine;

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.Get(int)"/> method to ensure it returns the correct lines.
        /// </summary>
        [TestMethod]
        public void Get_WithSkip_ReturnsCorrectLines()
        {
            // Arrange
            for (int i = 0; i < DefaultCapacity; i++)
            {
                _rollingLog.AddLine($"Line {i}");
            }

            // Act
            string[] result = _rollingLog.Get(2);

            // Assert
            CollectionAssert.AreEqual(new[] { "Line 2", "Line 3", "Line 4" }, result);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.Get(int, int)"/> method to ensure it returns the correct lines.
        /// </summary>
        [TestMethod]
        public void Get_WithSkipAndTake_ReturnsCorrectLines()
        {
            // Arrange
            for (int i = 0; i < DefaultCapacity; i++)
            {
                _rollingLog.AddLine($"Line {i}");
            }

            // Act
            string[] result = _rollingLog.Get(1, 2);

            // Assert
            CollectionAssert.AreEqual(new[] { "Line 1", "Line 2" }, result);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.Clear"/> method to ensure it clears all lines.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalled_ClearsAllLines()
        {
            // Arrange
            _rollingLog.AddLine("Line 1");

            // Act
            _rollingLog.Clear();

            // Assert
            Assert.AreEqual(string.Empty, _rollingLog.LastLine);
            Assert.AreEqual(0, _rollingLog.Get(0).Length);
        }

        /// <summary>
        /// Tests the <see cref="RollingLog.ToString"/> method to ensure it returns the correct string representation.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalled_ReturnsCorrectString()
        {
            // Arrange
            _rollingLog.AddLine("Line 1");
            _rollingLog.AddLine("Line 2");

            // Act
            string result = _rollingLog.ToString();

            // Assert
            Assert.AreEqual("Line 1\r\nLine 2\r\n", result);
        }
    }
}
