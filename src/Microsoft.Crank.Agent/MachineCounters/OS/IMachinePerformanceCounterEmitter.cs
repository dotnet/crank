using System;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    internal interface IMachinePerformanceCounterEmitter : IDisposable
    {
        string MeasurementName { get; }
        string CounterName { get; }

        void Start();
    }
}
