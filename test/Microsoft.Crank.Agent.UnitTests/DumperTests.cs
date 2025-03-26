using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Dumper"/> class.
    /// </summary>
    public class DumperTests
    {
        private readonly Dumper _dumper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DumperTests"/> class.
        /// </summary>
        public DumperTests()
        {
            _dumper = new Dumper();
        }

        /// <summary>
        /// Tests that the Collect method returns 0 on a successful dump on Windows.
        /// This test verifies the happy path on Windows where a valid process and file path are provided.
        /// Note: This test will early exit if the current OS is not Windows.
        /// </summary>
        [Fact]
        public void Collect_WindowsValidInput_Returns0()
        {
            // Arrange
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Skip this test if the current operating system is not Windows.
                return;
            }
            int processId = Process.GetCurrentProcess().Id;
            string tempPath = Path.Combine(Path.GetTempPath(), "dummy.dmp");
            DumpTypeOption dumpType = DumpTypeOption.Full;

            // Act
            int result = _dumper.Collect(processId, tempPath, dumpType);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Tests that the Collect method returns 0 on a successful dump on non-Windows platforms.
        /// This test verifies the happy path on non-Windows systems where a valid process and file path are provided.
        /// Note: This test will early exit if the current OS is Windows.
        /// </summary>
        [Fact]
        public void Collect_NonWindowsValidInput_Returns0()
        {
            // Arrange
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Skip this test if the current operating system is Windows.
                return;
            }
            int processId = Process.GetCurrentProcess().Id;
            string tempPath = Path.Combine(Path.GetTempPath(), "dummy.dmp");
            DumpTypeOption dumpType = DumpTypeOption.Heap;

            // Act
            int result = _dumper.Collect(processId, tempPath, dumpType);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Tests that the Collect method throws an ArgumentNullException when the output file path is null.
        /// This test confirms that invalid input for the file path correctly leads to an exception from Path.GetFullPath,
        /// which is not caught by the method.
        /// </summary>
        [Fact]
        public void Collect_NullOutputFilePath_ThrowsArgumentNullException()
        {
            // Arrange
            int processId = Process.GetCurrentProcess().Id;
            string outputFilePath = null;
            DumpTypeOption dumpType = DumpTypeOption.Mini;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _dumper.Collect(processId, outputFilePath, dumpType));
        }

        /// <summary>
        /// Tests that the Collect method propagates an ArgumentException when an invalid process ID is provided on Windows.
        /// On Windows, Process.GetProcessById is expected to throw an ArgumentException for an invalid process ID,
        /// which is not handled by the Collect method.
        /// </summary>
        [Fact]
        public void Collect_WindowsInvalidProcessId_ThrowsArgumentException()
        {
            // Arrange
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Skip this test if the current operating system is not Windows.
                return;
            }
            int invalidProcessId = -1;
            string tempPath = Path.Combine(Path.GetTempPath(), "dummy.dmp");
            DumpTypeOption dumpType = DumpTypeOption.Mini;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _dumper.Collect(invalidProcessId, tempPath, dumpType));
        }

        /// <summary>
        /// Tests that the Collect method propagates an exception when an invalid process ID is provided on non-Windows platforms.
        /// On non-Windows platforms, the DiagnosticsClient constructor or WriteDump method is expected to throw an exception
        /// for an invalid process ID, which is not caught by the Collect method.
        /// </summary>
        [Fact]
        public void Collect_NonWindowsInvalidProcessId_ThrowsException()
        {
            // Arrange
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Skip this test if the current operating system is Windows.
                return;
            }
            int invalidProcessId = -1;
            string tempPath = Path.Combine(Path.GetTempPath(), "dummy.dmp");
            DumpTypeOption dumpType = DumpTypeOption.Full;

            // Act & Assert
            Assert.Throws<Exception>(() => _dumper.Collect(invalidProcessId, tempPath, dumpType));
        }
    }
}
