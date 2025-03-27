using System;
using System.Diagnostics;
using System.Threading;
using Moq;
using Xunit;
using Microsoft.Crank.Agent.MachineCounters.OS;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WindowsProcessCpuTimeEmitter"/> class.
    /// </summary>
    public class WindowsProcessCpuTimeEmitterTests : IDisposable
    {
        private readonly Mock<MachineCountersEventSource> _mockEventSource;
        private readonly string _existingProcessName;
        private readonly string _nonExistentProcessName;
        private readonly TimeSpan _timerWaitTime;

        public WindowsProcessCpuTimeEmitterTests()
        {
            _mockEventSource = new Mock<MachineCountersEventSource>();
            // Use the current process name to ensure a process exists.
            _existingProcessName = Process.GetCurrentProcess().ProcessName;
            // Use an unlikely name to simulate a non-existent process.
            _nonExistentProcessName = Guid.NewGuid().ToString();
            // Wait time a bit longer than the timer interval to ensure the callback has been triggered.
            _timerWaitTime = TimeSpan.FromMilliseconds(1500);
        }

        /// <summary>
        /// Tests that TryStart returns false when the process is not found.
        /// </summary>
        [Fact]
        public void TryStart_NonExistentProcess_ReturnsFalse()
        {
            // Arrange
            var measurementName = "TestMeasurement";
            var emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, _nonExistentProcessName, measurementName);

            // Act
            bool result = emitter.TryStart();

            // Assert
            Assert.False(result);
            emitter.Dispose();
        }

        /// <summary>
        /// Tests that TryStart returns true and the timer callback invokes WriteCounterValue when the process exists.
        /// </summary>
        [Fact]
        public void TryStart_ExistingProcess_StartsTimerAndCallsWriteCounterValue()
        {
            // Arrange
            var measurementName = "TestMeasurement";
            var emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, _existingProcessName, measurementName);

            _mockEventSource
                .Setup(es => es.WriteCounterValue(It.IsAny<string>(), It.IsAny<double>()))
                .Verifiable();

            // Act
            bool result = emitter.TryStart();
            Assert.True(result);

            // Wait for the timer callback to be executed at least once.
            Thread.Sleep(_timerWaitTime);

            // Assert that WriteCounterValue is called at least once.
            _mockEventSource.Verify(es => es.WriteCounterValue(measurementName, It.IsAny<double>()), Times.AtLeastOnce);

            emitter.Dispose();
        }

        /// <summary>
        /// Tests that the CounterName property returns the correctly formatted string.
        /// </summary>
        [Fact]
        public void CounterName_ReturnsProperFormat()
        {
            // Arrange
            var processName = "TestProcess";
            var measurementName = "Measurement";
            var emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, processName, measurementName);

            // Act
            string counterName = emitter.CounterName;

            // Assert
            Assert.Equal($"Process {processName} Time (%)", counterName);
            emitter.Dispose();
        }

        /// <summary>
        /// Tests that the MeasurementName property returns the configured measurement name.
        /// </summary>
        [Fact]
        public void MeasurementName_ReturnsProvidedMeasurementName()
        {
            // Arrange
            var processName = "AnyProcess";
            var measurementName = "MyMeasurement";
            var emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, processName, measurementName);

            // Act
            string resultMeasurementName = emitter.MeasurementName;

            // Assert
            Assert.Equal(measurementName, resultMeasurementName);
            emitter.Dispose();
        }

        /// <summary>
        /// Tests that calling Dispose multiple times does not throw any exceptions.
        /// </summary>
        [Fact]
        public void Dispose_MultipleCalls_NoExceptionThrown()
        {
            // Arrange
            var measurementName = "TestMeasurement";
            var emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, _nonExistentProcessName, measurementName);

            // Act & Assert
            // First dispose call.
            emitter.Dispose();

            // Second dispose call.
            var exception = Record.Exception(() => emitter.Dispose());
            Assert.Null(exception);
        }

        /// <summary>
        /// Performs cleanup after each test.
        /// </summary>
        public void Dispose()
        {
            // No unmanaged resources to clean up at the test class level.
        }
    }
}
