using Moq;
using System;
using System.Diagnostics;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="LinuxMachineCpuUsageEmitter"/> class.
    /// </summary>
    [TestClass]
    public class LinuxMachineCpuUsageEmitterTests
    {
        private readonly Mock<MachineCountersEventSource> _mockEventSource;
        private readonly LinuxMachineCpuUsageEmitter _emitter;

        public LinuxMachineCpuUsageEmitterTests()
        {
            _mockEventSource = new Mock<MachineCountersEventSource>();
            _emitter = new LinuxMachineCpuUsageEmitter(_mockEventSource.Object, "TestMeasurement", "TestCounter");
        }

        /// <summary>
        /// Tests the <see cref="LinuxMachineCpuUsageEmitter.TryStart"/> method to ensure it starts the process successfully.
        /// </summary>
        [TestMethod]
        public void TryStart_WhenCalled_ReturnsTrue()
        {
            // Act
            var result = _emitter.TryStart();

            // Assert
            Assert.IsTrue(result, "Expected TryStart to return true.");
        }

        /// <summary>
        /// Tests the <see cref="LinuxMachineCpuUsageEmitter.TryStart"/> method to ensure it handles exceptions and returns false.
        /// </summary>
        [TestMethod]
        public void TryStart_WhenExceptionThrown_ReturnsFalse()
        {
            // Arrange
            var mockProcessUtil = new Mock<IProcessUtil>();
            mockProcessUtil.Setup(p => p.StreamOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>(), It.IsAny<Action<string>>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = _emitter.TryStart();

            // Assert
            Assert.IsFalse(result, "Expected TryStart to return false when an exception is thrown.");
        }

        /// <summary>
        /// Tests the <see cref="LinuxMachineCpuUsageEmitter.ParseCpuUsage"/> method to ensure it parses valid CPU usage correctly.
        /// </summary>
        [TestMethod]
        public void ParseCpuUsage_ValidOutput_ReturnsCpuUsage()
        {
            // Arrange
            var output = "2  0   0    30065212 9252 897004  0  0     106 262      157 0     0  0  100 0  0  0";

            // Act
            var result = _emitter.ParseCpuUsage(output);

            // Assert
            Assert.IsNotNull(result, "Expected ParseCpuUsage to return a non-null value.");
            Assert.AreEqual(0, result.Value, "Expected CPU usage to be 0.");
        }

        /// <summary>
        /// Tests the <see cref="LinuxMachineCpuUsageEmitter.ParseCpuUsage"/> method to ensure it returns null for invalid output.
        /// </summary>
        [TestMethod]
        public void ParseCpuUsage_InvalidOutput_ReturnsNull()
        {
            // Arrange
            var output = "invalid output";

            // Act
            var result = _emitter.ParseCpuUsage(output);

            // Assert
            Assert.IsNull(result, "Expected ParseCpuUsage to return null for invalid output.");
        }

        /// <summary>
        /// Tests the <see cref="LinuxMachineCpuUsageEmitter.Dispose"/> method to ensure it disposes the process correctly.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesProcess()
        {
            // Arrange
            var mockProcess = new Mock<Process>();
            _emitter.SetProcess(mockProcess.Object);

            // Act
            _emitter.Dispose();

            // Assert
            mockProcess.Verify(p => p.Kill(), Times.Once, "Expected Kill to be called once.");
            mockProcess.Verify(p => p.WaitForExit(), Times.Once, "Expected WaitForExit to be called once.");
            mockProcess.Verify(p => p.Dispose(), Times.Once, "Expected Dispose to be called once.");
        }
    }
}

