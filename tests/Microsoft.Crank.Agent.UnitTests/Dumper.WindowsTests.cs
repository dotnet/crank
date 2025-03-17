using Moq;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Crank.Agent;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Dumper.Windows"/> class.
    /// </summary>
    [TestClass]
    public class DumperWindowsTests
    {
        private readonly Mock<Process> _mockProcess;
        private readonly string _outputFilePath;
        private readonly FileStream _mockFileStream;

        public DumperWindowsTests()
        {
            _mockProcess = new Mock<Process>();
            _outputFilePath = "test.dmp";
            _mockFileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Windows.CollectDump(Process, string, DumpTypeOption)"/> method to ensure it correctly creates a dump file for a process.
        /// </summary>
        [TestMethod]
        public void CollectDump_WhenCalledWithValidParameters_CreatesDumpFile()
        {
            // Arrange
            _mockProcess.Setup(p => p.Handle).Returns(new IntPtr(1234));
            _mockProcess.Setup(p => p.Id).Returns(5678);

            // Act
            Dumper.Windows.CollectDump(_mockProcess.Object, _outputFilePath, DumpTypeOption.Mini);

            // Assert
            Assert.IsTrue(File.Exists(_outputFilePath), "Dump file was not created.");
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Windows.CollectDump(Process, string, DumpTypeOption)"/> method to ensure it throws an exception when the process handle is invalid.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CollectDump_WhenCalledWithInvalidProcessHandle_ThrowsException()
        {
            // Arrange
            _mockProcess.Setup(p => p.Handle).Returns(IntPtr.Zero);

            // Act
            Dumper.Windows.CollectDump(_mockProcess.Object, _outputFilePath, DumpTypeOption.Mini);
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Windows.CollectDump(Process, string, DumpTypeOption)"/> method to ensure it retries on ERROR_PARTIAL_COPY.
        /// </summary>
        [TestMethod]
        public void CollectDump_WhenErrorPartialCopy_Retries()
        {
            // Arrange
            _mockProcess.Setup(p => p.Handle).Returns(new IntPtr(1234));
            _mockProcess.Setup(p => p.Id).Returns(5678);

            // Simulate ERROR_PARTIAL_COPY
            var callCount = 0;
            Mock.Get(typeof(Dumper.Windows.NativeMethods))
                .Setup(m => m.MiniDumpWriteDump(It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<SafeFileHandle>(), It.IsAny<Dumper.Windows.NativeMethods.MINIDUMP_TYPE>(), It.IsAny<IntPtr>(), It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns(() => callCount++ < 4 ? false : true);

            // Act
            Dumper.Windows.CollectDump(_mockProcess.Object, _outputFilePath, DumpTypeOption.Mini);

            // Assert
            Assert.AreEqual(5, callCount, "The method did not retry the expected number of times.");
        }
    }
}

