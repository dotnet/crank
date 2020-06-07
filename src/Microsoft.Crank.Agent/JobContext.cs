using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    /// <summary>
    /// Contains the state of a Job when it's run by the Agent.
    /// </summary>
    public class JobContext
    {
        public Job Job { get; set; }

        public Process Process { get; set; }

        public string WorkingDirectory { get; set; }
        public Timer Timer { get; set; }
        public object ExecutionLock { get; set; } = new object();
        public bool Disposed { get; set; }
        public string BenchmarksDir { get; set; }
        public DateTime StartMonitorTime { get; set; } = DateTime.UtcNow;

        public string TempDir { get; set; }
        public string DockerImage { get; set; }
        public string DockerContainerId { get; set; }

        public ulong EventPipeSessionId { get; set; }
        public Task EventPipeTask { get; set; }
        public bool EventPipeTerminated { get; set; }

        public ulong MeasurementsSessionId { get; set; }
        public Task MeasurementsTask { get; set; }
        public bool MeasurementsTerminated { get; set; }
    }
}
