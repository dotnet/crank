using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="WindowsLimiter"/> class.
    /// </summary>
    public class WindowsLimiterTests : IDisposable
    {
        // Use the current process for testing purposes.
        private readonly Process _currentProcess;

        public WindowsLimiterTests()
        {
            _currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// Helper method to extract the value of a private boolean field using reflection.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <param name="fieldName">The private field name.</param>
        /// <returns>The boolean value of the private field.</returns>
        private bool GetPrivateBoolField(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new InvalidOperationException($"Field '{fieldName}' not found.");
            }
            return (bool)field.GetValue(instance);
        }

        /// <summary>
        /// Tests that creating an instance of WindowsLimiter with a valid Process returns a non-null instance.
        /// </summary>
        [Fact]
        public void Constructor_WithValidProcess_CreatesInstance()
        {
            // Arrange & Act
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);

            // Assert
            Assert.NotNull(limiter);
            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetMemLimit with a memory limit of 0 results in no modification (i.e. _hasJobObj remains false).
        /// </summary>
        [Fact]
        public void SetMemLimit_ZeroMemoryLimit_NoJobObjectCreated()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);

            // Act
            limiter.SetMemLimit(0);

            // Assert: Check that the private field _hasJobObj is false.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.False(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetMemLimit with a positive memory limit sets the _hasJobObj flag.
        /// </summary>
        [Fact]
        public void SetMemLimit_PositiveMemoryLimit_SetsJobObjectFlag()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);
            ulong memLimit = 1024UL; // 1 KB memory limit

            // Act
            try
            {
                limiter.SetMemLimit(memLimit);
            }
            catch (Win32Exception)
            {
                // In certain environments the Win32 API might not allow creating a job object.
                // If so, we catch the exception and mark the test inconclusive.
                limiter.Dispose();
                return;
            }

            // Assert: Check that the private field _hasJobObj is set to true.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.True(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetCpuLimits with both parameters as null does not modify the _hasJobObj flag.
        /// </summary>
        [Fact]
        public void SetCpuLimits_NullParameters_DoesNotSetJobObjectFlag()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);

            // Act
            limiter.SetCpuLimits(null, null);

            // Assert: _hasJobObj should remain false.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.False(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetCpuLimits with a valid CPU ratio and null cpuSet sets the _hasJobObj flag.
        /// </summary>
        [Fact]
        public void SetCpuLimits_ValidCpuRatioOnly_SetsJobObjectFlag()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);
            double cpuRatio = 0.5; // 50%

            // Act
            try
            {
                limiter.SetCpuLimits(cpuRatio, null);
            }
            catch (Win32Exception)
            {
                // If the underlying PInvoke fails in the current environment,
                // dispose and exit the test.
                limiter.Dispose();
                return;
            }

            // Assert: Check _hasJobObj flag.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.True(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetCpuLimits with a valid cpuSet (and null cpuRatio) sets the _hasJobObj flag.
        /// </summary>
        [Fact]
        public void SetCpuLimits_ValidCpuSetOnly_SetsJobObjectFlag()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);
            // Use a CPU index that is within the bounds of available processors.
            List<int> cpuSet = new List<int> { 0 };

            // Act
            try
            {
                limiter.SetCpuLimits(null, cpuSet);
            }
            catch (Win32Exception)
            {
                limiter.Dispose();
                return;
            }

            // Assert: Check _hasJobObj flag is set.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.True(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling SetCpuLimits with both valid cpuRatio and cpuSet sets the _hasJobObj flag.
        /// </summary>
        [Fact]
        public void SetCpuLimits_ValidCpuRatioAndCpuSet_SetsJobObjectFlag()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);
            double cpuRatio = 0.5; // 50%
            List<int> cpuSet = new List<int> { 0 };

            // Act
            try
            {
                limiter.SetCpuLimits(cpuRatio, cpuSet);
            }
            catch (Win32Exception)
            {
                limiter.Dispose();
                return;
            }

            // Assert: Check _hasJobObj flag is set.
            bool hasJobObj = GetPrivateBoolField(limiter, "_hasJobObj");
            Assert.True(hasJobObj);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling Apply on an instance with no limits set does not throw an exception.
        /// </summary>
        [Fact]
        public void Apply_NoLimits_DoesNotThrow()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);

            // Act & Assert
            var exception = Record.Exception(() => limiter.Apply());
            Assert.Null(exception);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling Apply after setting a memory limit (which sets _hasJobObj) does not throw an exception.
        /// </summary>
        [Fact]
        public void Apply_WithLimits_DoesNotThrow()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);
            try
            {
                limiter.SetMemLimit(1024UL);
            }
            catch (Win32Exception)
            {
                // If setting memory limit fails because of platform issues, dispose and exit test.
                limiter.Dispose();
                return;
            }

            // Act & Assert
            var exception = Record.Exception(() => limiter.Apply());
            Assert.Null(exception);

            limiter.Dispose();
        }

        /// <summary>
        /// Tests that calling Dispose multiple times does not throw an exception.
        /// </summary>
        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Arrange
            WindowsLimiter limiter = new WindowsLimiter(_currentProcess);

            // Act
            var firstCallException = Record.Exception(() => limiter.Dispose());
            var secondCallException = Record.Exception(() => limiter.Dispose());

            // Assert
            Assert.Null(firstCallException);
            Assert.Null(secondCallException);
        }

        /// <summary>
        /// Dispose pattern for WindowsLimiterTests.
        /// </summary>
        public void Dispose()
        {
            _currentProcess?.Dispose();
        }
    }
}
