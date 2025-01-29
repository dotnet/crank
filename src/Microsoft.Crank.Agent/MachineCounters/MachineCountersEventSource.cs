using System.Diagnostics.Tracing;

namespace Microsoft.Crank.Agent.MachineCounters
{
    [EventSource(Name = "MachineCountersEventSource")]
    internal class MachineCountersEventSource : EventSource
    {
        public static readonly MachineCountersEventSource Log = new MachineCountersEventSource();

        public void EmitEvent(EventId eventId, string counterName, double value) 
            => WriteEvent((int)eventId, counterName, value);

        public enum EventId
        {
            CpuUsage = 1
        }
    }
}
