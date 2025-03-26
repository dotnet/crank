using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Crank.Agent.MachineCounters;
using Xunit;

namespace Microsoft.Crank.Agent.MachineCounters.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MachineCountersEventSource"/> class.
    /// </summary>
    public class MachineCountersEventSourceTests
    {
        /// <summary>
        /// A custom event listener to capture events from an EventSource for testing purposes.
        /// </summary>
        private sealed class TestEventListener : EventListener, IDisposable
        {
            private readonly List<EventWrittenEventArgs> _events = new List<EventWrittenEventArgs>();

            /// <summary>
            /// Gets the captured events.
            /// </summary>
            public IReadOnlyList<EventWrittenEventArgs> Events => _events.AsReadOnly();

            /// <summary>
            /// Overrides the OnEventWritten to capture event data.
            /// </summary>
            /// <param name="eventData">The event data being written.</param>
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                lock (_events)
                {
                    _events.Add(eventData);
                }
            }

            /// <summary>
            /// Disposes the event listener.
            /// </summary>
            public new void Dispose()
            {
                base.Dispose();
            }
        }

        /// <summary>
        /// Tests that WriteCounterValue writes an event with the expected event ID and payload
        /// when provided with various valid and boundary inputs.
        /// </summary>
        /// <param name="counterName">
        /// The counter name to write; this can be a typical string or null.
        /// </param>
        /// <param name="value">
        /// The counter value to write; this includes normal numbers as well as extreme values.
        /// </param>
        [Theory]
        [InlineData("TestCounter", 42.0)]
        [InlineData(null, 0.0)]
        [InlineData("Extreme", double.NaN)]
        [InlineData("Extreme", double.PositiveInfinity)]
        [InlineData("Extreme", double.NegativeInfinity)]
        public void WriteCounterValue_InputVarious_WritesExpectedEvent(string counterName, double value)
        {
            // Arrange: Create an event listener to capture events from MachineCountersEventSource.
            using var listener = new TestEventListener();
            listener.EnableEvents(MachineCountersEventSource.Log, EventLevel.LogAlways);

            // Act: Invoke the WriteCounterValue method with the specified inputs.
            MachineCountersEventSource.Log.WriteCounterValue(counterName, value);

            // Assert: Verify that an event was written exactly once with the expected data.
            Assert.Single(listener.Events);

            var eventData = listener.Events[0];

            // Verify that the event has the correct event ID.
            Assert.Equal(1, eventData.EventId);

            // Verify that the payload contains exactly two items: the counter name and the counter value.
            Assert.NotNull(eventData.Payload);
            Assert.Equal(2, eventData.Payload.Count);
            Assert.Equal(counterName, eventData.Payload[0]);
            Assert.Equal(value, eventData.Payload[1]);
        }
    }
}
