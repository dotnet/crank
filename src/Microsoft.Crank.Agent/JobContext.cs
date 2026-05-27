// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;

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
        public bool Disposed { get; set; }
        public string BenchmarksDir { get; set; }
        public DateTime StartMonitorTime { get; set; } = DateTime.UtcNow;
        public DateTime NextMeasurement { get; set; } = DateTime.UtcNow;

        public string TempDir { get; set; }
        public bool TempDirUsesSourceKey { get; set; }
        public Dictionary<string, string> SourceDirs { get; set; } = new();
        public string DockerImage { get; set; }
        public string DockerContainerId { get; set; }

        /// <summary>
        /// Per-job isolated dotnet home used for buildcache runs. When non-null, the agent should
        /// run the application using this root (so the BCS-overlaid runtime is loaded), and the
        /// global dotnet home is left untouched for concurrent jobs. The directory is owned by
        /// the job and deleted when the job completes.
        /// </summary>
        public string BuildCacheDotnetHome { get; set; }

        /// <summary>
        /// Temporary directory holding the extracted BCS archive for this job. Deleted at the
        /// end of the job; the underlying archive in the parent commit directory is kept so
        /// subsequent jobs for the same commit can reuse it without re-downloading.
        /// </summary>
        public string BuildCacheExtractDir { get; set; }

        public ulong EventPipeSessionId { get; set; }
        public Task EventPipeTask { get; set; }
        public bool EventPipeTerminated { get; set; }

        public EventPipeSession EventPipeSession { get; set; }
        public Task CountersTask { get; set; }
        public TaskCompletionSource<bool> CountersCompletionSource { get; set; }
    }
}
