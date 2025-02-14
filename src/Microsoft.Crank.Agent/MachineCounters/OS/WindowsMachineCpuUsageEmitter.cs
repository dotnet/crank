using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Versioning;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    [SupportedOSPlatform("windows")]
    internal class WindowsMachineCpuUsageEmitter : IMachinePerformanceCounterEmitter
    {
        private readonly MachineCountersEventSource _eventSource;
        private readonly PerformanceCounter _performanceCounter;

        private Timer _timer;
        private readonly TimeSpan _interval;

        public string MeasurementName { get; }

        public string CounterName
            => $"{_performanceCounter.CategoryName}({_performanceCounter.InstanceName})\\{_performanceCounter.CounterName}";

        public WindowsMachineCpuUsageEmitter(PerformanceCounter performanceCounter, string measurementName)
            : this(MachineCountersEventSource.Log, TimeSpan.FromSeconds(1), performanceCounter, measurementName)
        {
        }

        public WindowsMachineCpuUsageEmitter(
            MachineCountersEventSource eventSource,
            TimeSpan interval,
            PerformanceCounter performanceCounter,
            string measurementName)
        {
            _eventSource = eventSource;
            _interval = interval;

            _performanceCounter = performanceCounter;
            MeasurementName = measurementName;
        }

        public bool TryStart()
        {
            _timer = new Timer(_ => WritePerformanceData(), null, TimeSpan.Zero, _interval);
            return true;
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
                _eventSource.WriteCounterValue(MeasurementName, currentCounterValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"error during emitting machine-level counter value '{_performanceCounter.CategoryName}-{_performanceCounter.CounterName}-{_performanceCounter.InstanceName}'");
            }
        }
    }
}
