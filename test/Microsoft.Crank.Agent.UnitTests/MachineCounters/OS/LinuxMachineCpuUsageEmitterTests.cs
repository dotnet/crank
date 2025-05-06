using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Crank.Agent.MachineCounters.OS;
using Moq;
using Xunit;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="LinuxMachineCpuUsageEmitter"/> class.
    /// </summary>
    public class LinuxMachineCpuUsageEmitterTests
    {
        private readonly string _testMeasurement;
        private readonly string _testCounter;
        private readonly Mock<MachineCountersEventSource> _mockEventSource;

        /// <summary>
        /// Initializes test fields.
        /// </summary>
        public LinuxMachineCpuUsageEmitterTests()
        {
            _testMeasurement = "TestMeasurement";
            _testCounter = "TestCounter";
            // Assuming MachineCountersEventSource is overridable/mockable.
            _mockEventSource = new Mock<MachineCountersEventSource>();
        }

        /// <summary>
        /// Tests that the parameterless constructor which takes measurement and counter names correctly sets the properties.
        /// </summary>
        [Fact]
        public void Constructor_WithMeasurementAndCounterParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var emitter = new LinuxMachineCpuUsageEmitter(_testMeasurement, _testCounter);

            // Assert
            Assert.Equal(_testMeasurement, emitter.MeasurementName);
            Assert.Equal(_testCounter, emitter.CounterName);
        }

        /// <summary>
        /// Tests that the constructor which accepts an event source sets the properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithEventSource_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var emitter = new LinuxMachineCpuUsageEmitter(_mockEventSource.Object, _testMeasurement, _testCounter);

            // Assert
            Assert.Equal(_testMeasurement, emitter.MeasurementName);
            Assert.Equal(_testCounter, emitter.CounterName);
        }

        /// <summary>
        /// Tests the TryStart method.
        /// Depending on the OS platform and the availability of the "vmstat" command,
        /// the method is expected to return true on Linux when vmstat is available,
        /// and false on non-Linux platforms or when an exception is thrown.
        /// </summary>
        [Fact]
        public void TryStart_OnDifferentPlatforms_ReturnsExpectedResult()
        {
            // Arrange
            var emitter = new LinuxMachineCpuUsageEmitter(_testMeasurement, _testCounter);

            // Act
            bool result = emitter.TryStart();

            // Clean up if a process was started.
            if (result)
            {
                // Dispose will attempt to kill the process.
                emitter.Dispose();
            }

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux it is expected (in a proper test environment) that vmstat is available,
                // so the method should return true.
                Assert.True(result, "Expected TryStart to return true on Linux when vmstat is available.");
            }
            else
            {
                // On non-Linux platforms, the method should return false.
                Assert.False(result, "Expected TryStart to return false on non-Linux platforms.");
            }
        }

        /// <summary>
        /// Tests the Dispose method to ensure that the process's Kill, WaitForExit, and Dispose methods are invoked.
        /// This is achieved by injecting a fake process into the private field via reflection.
        /// </summary>
        [Fact]
        public void Dispose_WhenCalled_InvokesProcessMethods()
        {
            // Arrange
            var emitter = new LinuxMachineCpuUsageEmitter(_testMeasurement, _testCounter);
            var fakeProcess = new FakeProcess();

            // Set the private _vmstatProcess field via reflection
            FieldInfo field = typeof(LinuxMachineCpuUsageEmitter).GetField("_vmstatProcess", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(emitter, fakeProcess);

            // Act
            emitter.Dispose();

            // Assert
            Assert.True(fakeProcess.KillCalled, "Expected Kill to be called on the process during Dispose.");
            Assert.True(fakeProcess.WaitForExitCalled, "Expected WaitForExit to be called on the process during Dispose.");
            Assert.True(fakeProcess.DisposeCalled, "Expected Dispose to be called on the process during Dispose.");
        }

        /// <summary>
        /// A fake process class to simulate a Process that records method calls.
        /// This class inherits from Process in order to be assignable to the _vmstatProcess field.
        /// </summary>
        private class FakeProcess : Process
        {
            public bool KillCalled { get; private set; }
            public bool WaitForExitCalled { get; private set; }
            public bool DisposeCalled { get; private set; }

            /// <summary>
            /// Overrides Kill to record that it was called.
            /// </summary>
//             public override void Kill() [Error] (132-34)CS0506 'LinuxMachineCpuUsageEmitterTests.FakeProcess.Kill()': cannot override inherited member 'Process.Kill()' because it is not marked virtual, abstract, or override
//             {
//                 KillCalled = true;
//             }

            /// <summary>
            /// Overrides WaitForExit to record that it was called.
            /// </summary>
//             public override void WaitForExit() [Error] (140-34)CS0506 'LinuxMachineCpuUsageEmitterTests.FakeProcess.WaitForExit()': cannot override inherited member 'Process.WaitForExit()' because it is not marked virtual, abstract, or override
//             {
//                 WaitForExitCalled = true;
//             }

            /// <summary>
            /// Overrides Dispose to record that it was called.
            /// Note: 'new' is used since Dispose in Process is not virtual.
            /// </summary>
            public new void Dispose()
            {
                DisposeCalled = true;
            }
        }
    }
}
