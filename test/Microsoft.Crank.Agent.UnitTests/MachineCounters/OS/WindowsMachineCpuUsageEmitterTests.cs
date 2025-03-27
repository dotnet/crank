using Microsoft.Crank.Agent.MachineCounters.OS;
using Moq;
using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Microsoft.Crank.Agent.MachineCounters.OS.UnitTests
{
    /// <summary>
    /// A fake PerformanceCounter that returns a predetermined NextValue or throws an exception.
    /// </summary>
//     public class FakePerformanceCounter : PerformanceCounter [Error] (13-43)CS0509 'FakePerformanceCounter': cannot derive from sealed type 'PerformanceCounter' [Error] (24-120)CS1729 'object' does not contain a constructor that takes 4 arguments [Error] (37-124)CS1729 'object' does not contain a constructor that takes 4 arguments
//     {
//         private readonly float _nextValue;
//         private readonly bool _throwException;
//         /// <summary>
//         /// Initializes a new instance of the <see cref = "FakePerformanceCounter"/> class that returns a fixed value.
//         /// </summary>
//         /// <param name = "nextValue">The value to return from NextValue.</param>
//         /// <param name = "categoryName">The category name.</param>
//         /// <param name = "counterName">The counter name.</param>
//         /// <param name = "instanceName">The instance name.</param>
//         public FakePerformanceCounter(float nextValue, string categoryName, string counterName, string instanceName) : base(categoryName, counterName, instanceName, readOnly: true)
//         {
//             _nextValue = nextValue;
//             _throwException = false;
//         }
// 
//         /// <summary>
//         /// Initializes a new instance of the <see cref = "FakePerformanceCounter"/> class that throws an exception from NextValue.
//         /// </summary>
//         /// <param name = "exception">A dummy exception instance (not used, just to differentiate constructor signatures).</param>
//         /// <param name = "categoryName">The category name.</param>
//         /// <param name = "counterName">The counter name.</param>
//         /// <param name = "instanceName">The instance name.</param>
//         public FakePerformanceCounter(Exception exception, string categoryName, string counterName, string instanceName) : base(categoryName, counterName, instanceName, readOnly: true)
//         {
//             _throwException = true;
//         }
// 
//         /// <summary>
//         /// Returns the predetermined counter value or throws an exception based on configuration.
//         /// </summary>
//         /// <returns>A float value representing the performance counter value.</returns>
//         public override float NextValue() [Error] (46-31)CS0115 'FakePerformanceCounter.NextValue()': no suitable method found to override
//         {
//             if (_throwException)
//             {
//                 throw new Exception("Fake exception");
//             }
// 
//             return _nextValue;
//         }
//     }

    /// <summary>
    /// Unit tests for the <see cref = "WindowsMachineCpuUsageEmitter"/> class.
    /// </summary>
    public class WindowsMachineCpuUsageEmitterTests : IDisposable
    {
        private readonly TimeSpan _shortInterval = TimeSpan.FromMilliseconds(50);
        private readonly string _measurementName = "TestMeasurement";
        private readonly string _categoryName = "TestCategory";
        private readonly string _counterName = "TestCounter";
        private readonly string _instanceName = "TestInstance";
        /// <summary>
        /// Tests that the constructor sets the properties correctly and that CounterName is formatted as expected.
        /// </summary>
//         [Fact] [Error] (77-61)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.Agent.MachineCounters.OS.UnitTests.FakePerformanceCounter' to 'System.Diagnostics.PerformanceCounter'
//         public void Constructor_SetsPropertiesProperly()
//         {
//             // Arrange
//             float dummyValue = 10.0f;
//             var fakePerformanceCounter = new FakePerformanceCounter(dummyValue, _categoryName, _counterName, _instanceName);
//             // Act
//             var emitter = new WindowsMachineCpuUsageEmitter(fakePerformanceCounter, _measurementName);
//             // Assert
//             Assert.Equal(_measurementName, emitter.MeasurementName);
//             string expectedCounterName = $"{_categoryName}({_instanceName})\\{_counterName}";
//             Assert.Equal(expectedCounterName, emitter.CounterName);
//         }

        /// <summary>
        /// Tests that TryStart returns true and that the event source emits the expected counter value via WriteCounterValue.
        /// </summary>
//         [Fact] [Error] (96-101)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.Agent.MachineCounters.OS.UnitTests.FakePerformanceCounter' to 'System.Diagnostics.PerformanceCounter'
//         public void TryStart_ReturnsTrue_AndEmitsCounterValue()
//         {
//             // Arrange
//             float expectedValue = 42.0f;
//             var fakePerformanceCounter = new FakePerformanceCounter(expectedValue, _categoryName, _counterName, _instanceName);
//             var mockEventSource = new Mock<MachineCountersEventSource>();
//             // Setup expectation for WriteCounterValue to be called with the expected measurement name and counter value.
//             mockEventSource.Setup(es => es.WriteCounterValue(_measurementName, expectedValue));
//             var emitter = new WindowsMachineCpuUsageEmitter(mockEventSource.Object, _shortInterval, fakePerformanceCounter, _measurementName);
//             // Act
//             bool started = emitter.TryStart();
//             // Allow the timer to trigger multiple times.
//             Thread.Sleep(150);
//             emitter.Dispose();
//             // Assert
//             Assert.True(started);
//             // Verify that WriteCounterValue was called at least once.
//             mockEventSource.Verify(es => es.WriteCounterValue(_measurementName, expectedValue), Times.AtLeastOnce);
//         }

        /// <summary>
        /// Tests that when PerformanceCounter.NextValue throws an exception, the emitter swallows the exception.
        /// </summary>
//         [Fact] [Error] (117-101)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.Agent.MachineCounters.OS.UnitTests.FakePerformanceCounter' to 'System.Diagnostics.PerformanceCounter'
//         public void TryStart_WhenPerformanceCounterThrows_ExceptionIsSwallowed()
//         {
//             // Arrange
//             var fakePerformanceCounter = new FakePerformanceCounter(new Exception("Fake"), _categoryName, _counterName, _instanceName);
//             var mockEventSource = new Mock<MachineCountersEventSource>();
//             var emitter = new WindowsMachineCpuUsageEmitter(mockEventSource.Object, _shortInterval, fakePerformanceCounter, _measurementName);
//             // Act
//             bool started = emitter.TryStart();
//             // Allow some time for the timer callback to attempt execution.
//             Thread.Sleep(150);
//             emitter.Dispose();
//             // Assert
//             Assert.True(started);
//             // Verify that WriteCounterValue was never called because NextValue threw an exception.
//             mockEventSource.Verify(es => es.WriteCounterValue(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
//         }

        /// <summary>
        /// Tests that calling Dispose stops the emitter's timer and can be safely called multiple times without exception.
        /// </summary>
//         [Fact] [Error] (139-101)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.Agent.MachineCounters.OS.UnitTests.FakePerformanceCounter' to 'System.Diagnostics.PerformanceCounter'
//         public void Dispose_CanBeCalledMultipleTimes_WithoutException()
//         {
//             // Arrange
//             float expectedValue = 5.0f;
//             var fakePerformanceCounter = new FakePerformanceCounter(expectedValue, _categoryName, _counterName, _instanceName);
//             var mockEventSource = new Mock<MachineCountersEventSource>();
//             var emitter = new WindowsMachineCpuUsageEmitter(mockEventSource.Object, _shortInterval, fakePerformanceCounter, _measurementName);
//             emitter.TryStart();
//             // Act & Assert
//             var exception1 = Record.Exception(() => emitter.Dispose());
//             var exception2 = Record.Exception(() => emitter.Dispose());
//             Assert.Null(exception1);
//             Assert.Null(exception2);
//         }

        /// <summary>
        /// Disposes resources used by tests.
        /// </summary>
        public void Dispose()
        {
        // Cleanup logic can be added here if needed.
        }
    }
}