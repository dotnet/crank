using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Versioning;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    [SupportedOSPlatform("windows")]
    internal class WindowsMachinePerformanceCounterEmitter : IMachinePerformanceCounterEmitter
    {
        private readonly MachineCountersEventSource _eventSource;
        private readonly PerformanceCounter _performanceCounter;

        private Timer _timer;
        private readonly TimeSpan _interval;

        public string CounterName => _performanceCounter.CounterName;

        public WindowsMachinePerformanceCounterEmitter(PerformanceCounter performanceCounter)
            : this(MachineCountersEventSource.Log, TimeSpan.FromSeconds(1), performanceCounter)
        {
        }

        public WindowsMachinePerformanceCounterEmitter(
            MachineCountersEventSource eventSource,
            TimeSpan interval,
            PerformanceCounter performanceCounter)
        {
            _eventSource = eventSource;
            _performanceCounter = performanceCounter;

            _interval = interval;
        }

        public void Start()
        {
            _timer = new Timer(_ => WritePerformanceData(), null, TimeSpan.Zero, _interval);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void WritePerformanceData()
        {
            try
            {
                var currentCounterValue = _performanceCounter.NextValue();
                _eventSource.EmitEvent(MachineCountersEventSource.EventId.CpuUsage, _performanceCounter.CounterName, currentCounterValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"error during emitting machine-level counter value '{_performanceCounter.CategoryName}-{_performanceCounter.CounterName}-{_performanceCounter.InstanceName}'");
            }
        }
    }
}
