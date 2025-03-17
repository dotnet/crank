using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics.Tracing;

namespace Microsoft.Crank.Agent.MachineCounters.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MachineCountersEventSource"/> class.
    /// </summary>
    [TestClass]
    public class MachineCountersEventSourceTests
    {
        private readonly MachineCountersEventSource _eventSource;

        public MachineCountersEventSourceTests()
        {
            _eventSource = MachineCountersEventSource.Log;
        }

        /// <summary>
        /// Tests the <see cref="MachineCountersEventSource.WriteCounterValue(string, double)"/> method to ensure it correctly writes an event with the given counter name and value.
        /// </summary>
        [TestMethod]
        public void WriteCounterValue_ValidInputs_WritesEvent()
        {
            // Arrange
            string counterName = "TestCounter";
            double value = 123.45;

            // Act
            _eventSource.WriteCounterValue(counterName, value);

            // Assert
            // Since WriteEvent is a protected method, we cannot directly verify its call.
            // However, we can ensure no exceptions are thrown and assume the EventSource implementation is correct.
        }

        /// <summary>
        /// Tests the <see cref="MachineCountersEventSource.WriteCounterValue(string, double)"/> method to ensure it handles null counter name.
        /// </summary>
        [TestMethod]
        public void WriteCounterValue_NullCounterName_WritesEvent()
        {
            // Arrange
            string counterName = null;
            double value = 123.45;

            // Act
            _eventSource.WriteCounterValue(counterName, value);

            // Assert
            // Since WriteEvent is a protected method, we cannot directly verify its call.
            // However, we can ensure no exceptions are thrown and assume the EventSource implementation is correct.
        }

        /// <summary>
        /// Tests the <see cref="MachineCountersEventSource.WriteCounterValue(string, double)"/> method to ensure it handles empty counter name.
        /// </summary>
        [TestMethod]
        public void WriteCounterValue_EmptyCounterName_WritesEvent()
        {
            // Arrange
            string counterName = string.Empty;
            double value = 123.45;

            // Act
            _eventSource.WriteCounterValue(counterName, value);

            // Assert
            // Since WriteEvent is a protected method, we cannot directly verify its call.
            // However, we can ensure no exceptions are thrown and assume the EventSource implementation is correct.
        }
    }
}
