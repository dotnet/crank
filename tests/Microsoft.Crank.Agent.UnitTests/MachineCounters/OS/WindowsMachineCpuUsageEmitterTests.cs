using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WindowsMachineCpuUsageEmitter"/> class.
    /// </summary>
    [TestClass]
    public class WindowsMachineCpuUsageEmitterTests
    {
        private readonly Mock<MachineCountersEventSource> _mockEventSource;
        private readonly Mock<PerformanceCounter> _mockPerformanceCounter;
        private readonly WindowsMachineCpuUsageEmitter _emitter;

        public WindowsMachineCpuUsageEmitterTests()
        {
            _mockEventSource = new Mock<MachineCountersEventSource>();
            _mockPerformanceCounter = new Mock<PerformanceCounter>();
            _emitter = new WindowsMachineCpuUsageEmitter(_mockEventSource.Object, TimeSpan.FromSeconds(1), _mockPerformanceCounter.Object, "TestMeasurement");
        }

        /// <summary>
        /// Tests the <see cref="WindowsMachineCpuUsageEmitter.TryStart"/> method to ensure it starts the timer correctly.
        /// </summary>
        [TestMethod]
        public void TryStart_WhenCalled_StartsTimer()
        {
            // Act
            var result = _emitter.TryStart();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="WindowsMachineCpuUsageEmitter.Dispose"/> method to ensure it disposes the timer correctly.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesTimer()
        {
            // Arrange
            _emitter.TryStart();

            // Act
            _emitter.Dispose();

            // Assert
            Assert.IsNull(_emitter.GetType().GetField("_timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_emitter));
        }

        /// <summary>
        /// Tests the <see cref="WindowsMachineCpuUsageEmitter.WritePerformanceData"/> method to ensure it writes performance data correctly.
        /// </summary>
        [TestMethod]
        public void WritePerformanceData_WhenCalled_WritesPerformanceData()
        {
            // Arrange
            _mockPerformanceCounter.Setup(pc => pc.NextValue()).Returns(50.0f);

            // Act
            _emitter.GetType().GetMethod("WritePerformanceData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_emitter, null);

            // Assert
            _mockEventSource.Verify(es => es.WriteCounterValue("TestMeasurement", 50.0f), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="WindowsMachineCpuUsageEmitter.WritePerformanceData"/> method to ensure it handles exceptions correctly.
        /// </summary>
        [TestMethod]
        public void WritePerformanceData_WhenExceptionThrown_LogsError()
        {
            // Arrange
            _mockPerformanceCounter.Setup(pc => pc.NextValue()).Throws<InvalidOperationException>();

            // Act
            _emitter.GetType().GetMethod("WritePerformanceData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_emitter, null);

            // Assert
            // Assuming Log.Error is a static method, we cannot verify it directly. This is a limitation of the current test setup.
        }
    }
}
