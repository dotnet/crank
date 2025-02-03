using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    [SupportedOSPlatform("linux")]
    internal class LinuxMachinePerformanceCounterEmitter : IMachinePerformanceCounterEmitter
    {
        private readonly MachineCountersEventSource _eventSource;

        private Timer _timer;
        private readonly TimeSpan _interval;

        public string MeasurementName { get; }
        public string CounterName { get; }

        public LinuxMachinePerformanceCounterEmitter(string measurementName, string counterName)
            : this(MachineCountersEventSource.Log, TimeSpan.FromSeconds(1), measurementName, counterName)
        {
        }

        public LinuxMachinePerformanceCounterEmitter(
            MachineCountersEventSource eventSource,
            TimeSpan interval,
            string measurementName,
            string counterName)
        {
            _eventSource = eventSource;
            _interval = interval;

            MeasurementName = measurementName;
            CounterName = counterName;
        }

        public void Start()
        {
            _timer = new Timer(async _ => await WritePerformanceData(), null, TimeSpan.Zero, _interval);
        }

        private async Task WritePerformanceData()
        {
            try
            {
                var counterValue = await GetCpuUsageAsync();
                if (counterValue is not null)
                {
                    _eventSource.WriteCounterValue(MeasurementName, counterValue.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"error during emitting machine-level counter {nameof(LinuxMachinePerformanceCounterEmitter)}");
            }
        }

        private async Task<double?> GetCpuUsageAsync()
        {
            var processResult = await ProcessUtil.RunAsync(filename: "vmstat", arguments: "");
            if (!string.IsNullOrEmpty(processResult.StandardError))
            {
                Log.Error(processResult.StandardError);
                return null;
            }

            string[] lines = processResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3)
            {
                Log.Warning("Unexpected vmstat output: " + processResult.StandardOutput);
                return null;
            }

            string lastLine = lines[^1]; // Last line contains the latest stats
            string[] columns = lastLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (columns.Length < 15)
            {
                Log.Warning("Unexpected vmstat output: " + lastLine);
                return null;
            }

            //All stats are represented as % of cpu-time (https://www.redhat.com/en/blog/linux-commands-vmstat)
            // - us: Time spent running non-kernel code. (user time, including nice time)
            // - sy: Time spent running kernel code. (system time)
            // - id: Time spent idle.Prior to Linux 2.5.41, this includes IO-wait time.
            // - wa: Time spent waiting for IO.Before Linux 2.5.41, included in idle.
            // - st: Time stolen from a virtual machine.Prior to Linux 2.6.11, unknown.
            // 
            // example:
            // procs -----------memory---------- ---swap-- -----io---- -system-- -------cpu--------
            // r  b   swpd free     buff cache   si so    bi  bo       in  cs    us sy id  wa st gu
            // 2  0   0    30065212 9252 897004  0  0     106 262      157 0     0  0  100 0  0  0
            var parsed = double.TryParse(columns[14], out var idleTime);
            parsed &= double.TryParse(columns[15], out var waitingTime);

            if (!parsed)
            {
                Log.Warning("Could not parse cpu stats: " + lastLine);
                return null;
            }

            return 100 - idleTime - waitingTime;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
