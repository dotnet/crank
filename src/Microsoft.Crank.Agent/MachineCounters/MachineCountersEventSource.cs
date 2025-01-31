using System.Diagnostics.Tracing;

namespace Microsoft.Crank.Agent.MachineCounters
{
    [EventSource(Name = "MachineCountersEventSource")]
    internal class MachineCountersEventSource : EventSource
    {
        public static readonly MachineCountersEventSource Log = new MachineCountersEventSource();

        public void WriteCounterValue(string counterName, double value) 
            => WriteEvent(1, counterName, value);
    }
}
