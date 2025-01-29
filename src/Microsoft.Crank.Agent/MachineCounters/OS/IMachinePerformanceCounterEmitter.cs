using System;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    internal interface IMachinePerformanceCounterEmitter : IDisposable
    {
        string CounterName { get; }

        void Start();
    }
}
