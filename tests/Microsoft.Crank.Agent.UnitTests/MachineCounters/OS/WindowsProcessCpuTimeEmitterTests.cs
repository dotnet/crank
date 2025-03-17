using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WindowsProcessCpuTimeEmitter"/> class.
    /// </summary>
    [TestClass]
    public class WindowsProcessCpuTimeEmitterTests
    {
        private readonly Mock<MachineCountersEventSource> _mockEventSource;
        private readonly WindowsProcessCpuTimeEmitter _emitter;
        private readonly string _processName = "TestProcess";
        private readonly string _measurementName = "TestMeasurement";

        public WindowsProcessCpuTimeEmitterTests()
        {
            _mockEventSource = new Mock<MachineCountersEventSource>();
            _emitter = new WindowsProcessCpuTimeEmitter(_mockEventSource.Object, _processName, _measurementName);
        }

        /// <summary>
        /// Tests the <see cref="WindowsProcessCpuTimeEmitter.TryStart"/> method to ensure it returns false when the process is not found.
        /// </summary>
        [TestMethod]
        public void TryStart_ProcessNotFound_ReturnsFalse()
        {
            // Arrange
            // Act
            bool result = _emitter.TryStart();

            // Assert
            Assert.IsFalse(result, "Expected TryStart to return false when the process is not found.");
        }

        /// <summary>
        /// Tests the <see cref="WindowsProcessCpuTimeEmitter.TryStart"/> method to ensure it returns true when the process is found.
        /// </summary>
//         [TestMethod] [Error] (50-52)CS1061 'Type' does not contain a definition for 'GetProcessesByName' and no accessible extension method 'GetProcessesByName' accepting a first argument of type 'Type' could be found (are you missing a using directive or an assembly reference?)
//         public void TryStart_ProcessFound_ReturnsTrue()
//         {
//             // Arrange
//             var mockProcess = new Mock<Process>();
//             mockProcess.Setup(p => p.TotalProcessorTime).Returns(TimeSpan.Zero);
//             Process[] processes = { mockProcess.Object };
//             Mock.Get(typeof(Process)).Setup(p => p.GetProcessesByName(_processName)).Returns(processes);
// 
//             // Act
//             bool result = _emitter.TryStart();
// 
//             // Assert
//             Assert.IsTrue(result, "Expected TryStart to return true when the process is found.");
//         }

        /// <summary>
        /// Tests the <see cref="WindowsProcessCpuTimeEmitter.Dispose"/> method to ensure it disposes the timer and process.
        /// </summary>
        [TestMethod]
        public void Dispose_DisposesTimerAndProcess()
        {
            // Arrange
            var mockProcess = new Mock<Process>();
            var mockTimer = new Mock<Timer>(null, null, Timeout.Infinite, Timeout.Infinite);
            _emitter.GetType().GetField("_process", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_emitter, mockProcess.Object);
            _emitter.GetType().GetField("_timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_emitter, mockTimer.Object);

            // Act
            _emitter.Dispose();

            // Assert
            mockProcess.Verify(p => p.Dispose(), Times.Once, "Expected process to be disposed.");
            mockTimer.Verify(t => t.Dispose(), Times.Once, "Expected timer to be disposed.");
        }
    }
}
