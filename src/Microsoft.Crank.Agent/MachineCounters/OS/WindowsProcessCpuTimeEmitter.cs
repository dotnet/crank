using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Crank.Agent.MachineCounters.OS
{
    internal class WindowsProcessCpuTimeEmitter : IMachinePerformanceCounterEmitter
    {
        private readonly MachineCountersEventSource _eventSource;
        private readonly string _processName;

        private Process _process;

        private Timer _timer;
        private TimeSpan _prevCpuTime;
        private DateTime _prevTime;

        public string MeasurementName { get; }
        public string CounterName => $"Process {_processName} Time (%)";

        public WindowsProcessCpuTimeEmitter(string processName, string measurementName)
            : this(MachineCountersEventSource.Log, processName, measurementName)
        {
        }

        public WindowsProcessCpuTimeEmitter(
            MachineCountersEventSource eventSource,
            string processName,
            string measurementName)
        {
            _eventSource = eventSource;
            _processName = processName;
            MeasurementName = measurementName;
        }

        public bool TryStart()
        {
            _process = GetProcessByName(_processName);
            if (_process == null)
            {
                Log.Warning($"Process '{_processName}' not found.");
                return false;
            }

            try
            {
                _prevCpuTime = _process.TotalProcessorTime;
                _prevTime = DateTime.UtcNow;
                _timer = new Timer(CalculateCpuUsage, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error starting {nameof(WindowsProcessCpuTimeEmitter)} for process '{_processName}'");
                return false;
            }
        }

        private void CalculateCpuUsage(object state)
        {
            if (_process.HasExited)
            {
                Log.Warning($"Process {_processName} exited.");
                return;
            }

            _process.Refresh();
            TimeSpan currCpuTime = _process.TotalProcessorTime;
            DateTime currTime = DateTime.UtcNow;

            var cpuUsage = (currCpuTime - _prevCpuTime).TotalMilliseconds /
                              (currTime - _prevTime).TotalMilliseconds * 100 / Environment.ProcessorCount;

            _prevCpuTime = currCpuTime;
            _prevTime = currTime;

            _eventSource.WriteCounterValue(MeasurementName, cpuUsage);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _process?.Dispose();
        }

        private static Process GetProcessByName(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);
            return processes.Length > 0 ? processes[0] : null;
        }
    }

}
