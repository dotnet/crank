using System;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    [SupportedOSPlatform("linux")]
    internal class LinuxMachineCpuUsageEmitter : IMachinePerformanceCounterEmitter
    {
        private readonly MachineCountersEventSource _eventSource;
        private Process _vmstatProcess;

        public string MeasurementName { get; }
        public string CounterName { get; }

        public LinuxMachineCpuUsageEmitter(string measurementName, string counterName)
            : this(MachineCountersEventSource.Log, measurementName, counterName)
        {
        }

        public LinuxMachineCpuUsageEmitter(
            MachineCountersEventSource eventSource,
            string measurementName,
            string counterName)
        {
            _eventSource = eventSource;

            MeasurementName = measurementName;
            CounterName = counterName;
        }

        public void Start()
        {
            try
            {
                _vmstatProcess = ProcessUtil.StreamOutput(
                    filename: "vmstat",
                    arguments: "1 100", // 'x y' mean 'get vmstat for y seconds every x second'
                    outputDataReceivedCallback: output =>
                    {
                        var cpuUsage = ParseCpuUsage(output);
                        if (cpuUsage is not null)
                        {
                            _eventSource.WriteCounterValue(MeasurementName, cpuUsage.Value);
                        }
                    },
                    errorDataReceivedCallback: error =>
                    {
                        Log.Warning("vmstat error: " + error);
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"error during emitting machine-level counter {nameof(LinuxMachineCpuUsageEmitter)}");
            }
        }

        private double? ParseCpuUsage(string vmStatOutput)
        {
            if (string.IsNullOrEmpty(vmStatOutput))
            {
                // no output -> skip it
                return null;
            }

            string[] columns = vmStatOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 15)
            {
                // it is not a metrics values line, just skip it
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
                // probably a line of metric names, so just skip it
                return null;
            }

            return 100 - idleTime - waitingTime;
        }

        public void Dispose()
        {
            _vmstatProcess.Kill();
            _vmstatProcess.WaitForExit();
            Log.Info($"vmstat process killed and stopped");
            _vmstatProcess?.Dispose();
        }
    }
}
