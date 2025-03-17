using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Dumper"/> class.
    /// </summary>
    [TestClass]
    public class DumperTests
    {
        private readonly Dumper _dumper;

        public DumperTests()
        {
            _dumper = new Dumper();
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Collect(int, string, DumpTypeOption)"/> method to ensure it correctly handles a valid process ID and output file path.
        /// </summary>
        [TestMethod]
        public void Collect_ValidProcessIdAndOutputFilePath_ReturnsZero()
        {
            // Arrange
            int processId = Process.GetCurrentProcess().Id;
            string outputFilePath = Path.GetTempFileName();
            DumpTypeOption type = DumpTypeOption.Full;

            // Act
            int result = _dumper.Collect(processId, outputFilePath, type);

            // Assert
            Assert.AreEqual(0, result, "Expected Collect to return 0 for a valid process ID and output file path.");
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Collect(int, string, DumpTypeOption)"/> method to ensure it handles a non-existent process ID.
        /// </summary>
        [TestMethod]
        public void Collect_NonExistentProcessId_ReturnsOne()
        {
            // Arrange
            int processId = -1; // Invalid process ID
            string outputFilePath = Path.GetTempFileName();
            DumpTypeOption type = DumpTypeOption.Full;

            // Act
            int result = _dumper.Collect(processId, outputFilePath, type);

            // Assert
            Assert.AreEqual(1, result, "Expected Collect to return 1 for a non-existent process ID.");
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Collect(int, string, DumpTypeOption)"/> method to ensure it handles an invalid output file path.
        /// </summary>
        [TestMethod]
        public void Collect_InvalidOutputFilePath_ReturnsOne()
        {
            // Arrange
            int processId = Process.GetCurrentProcess().Id;
            string outputFilePath = "Z:\\invalid\\path\\dump.dmp"; // Invalid path
            DumpTypeOption type = DumpTypeOption.Full;

            // Act
            int result = _dumper.Collect(processId, outputFilePath, type);

            // Assert
            Assert.AreEqual(1, result, "Expected Collect to return 1 for an invalid output file path.");
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Collect(int, string, DumpTypeOption)"/> method to ensure it handles an unauthorized access exception.
        /// </summary>
        [TestMethod]
        public void Collect_UnauthorizedAccessException_ReturnsOne()
        {
            // Arrange
            int processId = Process.GetCurrentProcess().Id;
            string outputFilePath = Path.Combine(Path.GetTempPath(), "unauthorized.dmp");
            DumpTypeOption type = DumpTypeOption.Full;

            // Mock unauthorized access by creating a read-only file
            File.WriteAllText(outputFilePath, "content");
            File.SetAttributes(outputFilePath, FileAttributes.ReadOnly);

            try
            {
                // Act
                int result = _dumper.Collect(processId, outputFilePath, type);

                // Assert
                Assert.AreEqual(1, result, "Expected Collect to return 1 for unauthorized access.");
            }
            finally
            {
                // Cleanup
                File.SetAttributes(outputFilePath, FileAttributes.Normal);
                File.Delete(outputFilePath);
            }
        }

        /// <summary>
        /// Tests the <see cref="Dumper.Collect(int, string, DumpTypeOption)"/> method to ensure it handles a platform not supported exception.
        /// </summary>
        [TestMethod]
        public void Collect_PlatformNotSupportedException_ReturnsOne()
        {
            // Arrange
            int processId = Process.GetCurrentProcess().Id;
            string outputFilePath = Path.GetTempFileName();
            DumpTypeOption type = DumpTypeOption.Full;

            // Mock platform not supported by setting an unsupported OS platform
            var runtimeInfoMock = new Mock<IRuntimeInformation>();
            runtimeInfoMock.Setup(r => r.IsOSPlatform(It.IsAny<OSPlatform>())).Returns(false);

            // Act
            int result = _dumper.Collect(processId, outputFilePath, type);

            // Assert
            Assert.AreEqual(1, result, "Expected Collect to return 1 for platform not supported.");
        }
    }
}

