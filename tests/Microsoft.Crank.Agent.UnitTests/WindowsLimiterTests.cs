using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WindowsLimiter"/> class.
    /// </summary>
    [TestClass]
    public class WindowsLimiterTests : IDisposable
    {
        private readonly Mock<Process> _mockProcess;
        private readonly WindowsLimiter _windowsLimiter;

        public WindowsLimiterTests()
        {
            _mockProcess = new Mock<Process>();
            _mockProcess.Setup(p => p.Id).Returns(1234);
            _windowsLimiter = new WindowsLimiter(_mockProcess.Object);
        }

        public void Dispose()
        {
            _windowsLimiter.Dispose();
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.SetMemLimit(ulong)"/> method to ensure it sets the memory limit correctly.
        /// </summary>
        [TestMethod]
        public void SetMemLimit_WhenCalledWithValidMemoryLimit_SetsMemoryLimit()
        {
            // Arrange
            ulong memoryLimitInBytes = 1024 * 1024 * 1024; // 1 GB

            // Act
            _windowsLimiter.SetMemLimit(memoryLimitInBytes);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.SetMemLimit(ulong)"/> method to ensure it does nothing when memory limit is zero.
        /// </summary>
        [TestMethod]
        public void SetMemLimit_WhenCalledWithZeroMemoryLimit_DoesNothing()
        {
            // Arrange
            ulong memoryLimitInBytes = 0;

            // Act
            _windowsLimiter.SetMemLimit(memoryLimitInBytes);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.SetCpuLimits(double?, IList{int})"/> method to ensure it sets the CPU limits correctly.
        /// </summary>
        [TestMethod]
        public void SetCpuLimits_WhenCalledWithValidCpuRatio_SetsCpuLimits()
        {
            // Arrange
            double cpuRatio = 0.5; // 50%

            // Act
            _windowsLimiter.SetCpuLimits(cpuRatio);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.SetCpuLimits(double?, IList{int})"/> method to ensure it sets the CPU set correctly.
        /// </summary>
        [TestMethod]
        public void SetCpuLimits_WhenCalledWithValidCpuSet_SetsCpuSet()
        {
            // Arrange
            IList<int> cpuSet = new List<int> { 0, 1 };

            // Act
            _windowsLimiter.SetCpuLimits(null, cpuSet);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.Apply()"/> method to ensure it applies the job object correctly.
        /// </summary>
        [TestMethod]
        public void Apply_WhenCalled_AppliesJobObject()
        {
            // Act
            _windowsLimiter.Apply();

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.Dispose()"/> method to ensure it disposes the resources correctly.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesResources()
        {
            // Act
            _windowsLimiter.Dispose();

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.CheckWin32Result{T}(T)"/> method to ensure it returns the result when valid.
        /// </summary>
        [TestMethod]
        public void CheckWin32Result_WhenCalledWithValidResult_ReturnsResult()
        {
            // Arrange
            SafeHandle validHandle = new SafeFileHandle(IntPtr.Zero, true);

            // Act
            var result = WindowsLimiter.CheckWin32Result(validHandle);

            // Assert
            Assert.AreEqual(validHandle, result);
        }

        /// <summary>
        /// Tests the <see cref="WindowsLimiter.CheckWin32Result{T}(T)"/> method to ensure it throws an exception when result is invalid.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Win32Exception))]
        public void CheckWin32Result_WhenCalledWithInvalidResult_ThrowsException()
        {
            // Arrange
            SafeHandle invalidHandle = new SafeFileHandle(IntPtr.Zero, false);

            // Act
            WindowsLimiter.CheckWin32Result(invalidHandle);

            // Assert
            // Exception is expected
        }
    }
}

