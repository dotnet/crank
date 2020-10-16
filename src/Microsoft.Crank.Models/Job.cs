﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Models
{
    public class Job
    {
        public int DriverVersion { get; set; } = 0;

        // 1: Introduced Initializing state
        // 2: Introduced Measurements/Metadata
        // 3: Output value not serialized
        // 4: Simplified rest endpoints
        public int ServerVersion { get; set; } = 4;

        public int Id { get; set; }

        /// <summary>
        /// Common identifier for all jobs in a scenario.
        /// Multiple jobs with the same RunId can be started on the same agent
        /// </summary>
        public string RunId { get; set; } = Guid.NewGuid().ToString("n");

        [JsonConverter(typeof(StringEnumConverter))]
        public Hardware? Hardware { get; set; }

        public string HardwareVersion { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OperatingSystem? OperatingSystem { get; set; }

        public string Service { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Scheme Scheme { get; set; }
        public int Port { get; set; } = 5000;
        public string Path { get; set; } = "/";

        // The client connections. Allows a json document to define this value for the client.
        public int Connections { get; set; }

        // The client threads. Allows a json document to define this value for the client.
        public int Threads { get; set; }

        public string ReadyStateText { get; set; }

        // A console application doesn't expose and endpoint that can be used to detect it is ready
        public bool IsConsoleApp { get; set; }
        public string AspNetCoreVersion { get; set; } = "";
        public string RuntimeVersion { get; set; } = "";
        public string DesktopVersion { get; set; } = "";
        public string SdkVersion { get; set; } = "";
        public string UseMonoRuntime { get; set; } = "";
        public bool NoGlobalJson { get; set; }
        public Database Database { get; set; } = Database.None;

        // Delay from the process started to the console receiving "Application started"
        public TimeSpan StartupMainMethod { get; set; }
        public TimeSpan BuildTime { get; set; }
        public long PublishedSize { get; set; }

        /// <summary>
        /// The source information for the benchmarked application
        /// </summary>
        public Source Source { get; set; } = new Source();

        public string Executable { get; set; }
        public string Arguments { get; set; }
        public bool NoArguments { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public JobState State { get; set; }

        public string Url { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public WebHost WebHost { get; set; } = WebHost.KestrelSockets;

        public bool UseRuntimeStore { get; set; }

        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public List<Attachment> BuildAttachments { get; set; } = new List<Attachment>();

        public DateTime LastDriverCommunicationUtc { get; set; } = DateTime.UtcNow;

        // dotnet-trace options
        public bool DotNetTrace { get; set; }
        public string DotNetTraceProviders { get; set; }

        // Perfview/Perfcollect
        public bool Collect { get; set; }
        public string CollectArguments { get; set; }
        public bool CollectSwapMemory { get; set; }
        public string PerfViewTraceFile { get; set; }

        // Other collection options
        public bool CollectStartup { get; set; }
        public bool CollectCounters { get; set; }

        /// <summary>
        /// The list of performance counter providers to be collected. Defaults to <c>System.Runtime</c>.
        /// </summary>
        public List<string> CounterProviders { get; set; } = new List<string>();
        public string BasePath { get; set; }
        public int ProcessId { get; set; }
        public int ChildProcessId { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public List<string> BuildArguments { get; set; } = new List<string>();
        public bool NoClean { get; set; }
        public string Framework { get; set; }
        public string Channel { get; set; }
        public string Error { get; set; }
        [JsonIgnore]
        public RollingLog Output { get; set; } = new RollingLog(10000);
        public bool SelfContained { get; set; }
        public string BeforeScript { get; set; }
        public string AfterScript { get; set; }
        public ulong MemoryLimitInBytes { get; set; }
        public double CpuLimitRatio { get; set; }
        public string CpuSet { get; set; } // e.g., 0 or 0-3 or 1-4,6
        public ConcurrentQueue<Measurement> Measurements { get; set; } = new ConcurrentQueue<Measurement>();
        public ConcurrentQueue<MeasurementMetadata> Metadata { get; set; } = new ConcurrentQueue<MeasurementMetadata>();

        /// <summary>
        /// The build log. This property is kept on the server side.
        /// </summary>
        [JsonIgnore]
        public RollingLog BuildLog { get; set; } = new RollingLog(10000);

        // These properties are used to map custom arguments to the scenario files

        [JsonIgnore]
        public string[] OutputFilesArgument { get; set; } = Array.Empty<string>();

        [JsonProperty("OutputFiles")]
        private string[] OutputFilesArgumentSetter { set { OutputFilesArgument = value; } }

        [JsonIgnore]
        public string[] OutputArchivesArgument { get; set; } = Array.Empty<string>();

        [JsonProperty("OutputArchives")]
        private string[] OutputArchivesArgumentSetter { set { OutputArchivesArgument = value; } }

        [JsonIgnore]
        public string[] BuildFilesArgument { get; set; } = Array.Empty<string>();

        [JsonProperty("BuildFiles")]
        private string[] BuildFilesArgumentSetter { set { BuildFilesArgument = value; } }

        [JsonIgnore]
        public string[] BuildArchivesArgument { get; set; } = Array.Empty<string>();

        [JsonProperty("BuildArchives")]
        private string[] BuildArchivesArgumentSetter { set { BuildArchivesArgument = value; } }

        // V2

        public List<string> Endpoints { get; set; } = new List<string>();

        public JObject Variables { get; set; }

        public bool WaitForExit { get; set; }
        /// <summary>
        /// Gets or sets the maximum time in seconds the job should be allowed to run.
        /// After that time the controller will stop the job.
        /// When set to 0, the controller never stops the job.
        /// </summary>
        public int Timeout { get; set; } = 0;

        // Custom StartTimeout for the server job
        public TimeSpan StartTimeout { get; set; } = TimeSpan.Zero;

        // Custom build timeout
        public TimeSpan BuildTimeout { get; set; } = TimeSpan.Zero;

        public Options Options { get; set; } = new Options();

        public List<string> Features { get; set; } = new List<string>();

    }

    /// <summary>
    /// Represents a set of properties that configure some behaviors on the driver.
    /// These options are not sent to the server.
    /// </summary>
    public class Options
    {
        public bool DisplayOutput { get; set; }
        public bool Fetch { get; set; }
        public string FetchOutput { get; set; }
        public List<string> DownloadFiles { get; set; } = new List<string>();
        public string TraceOutput { get; set; }
        public bool DisplayBuild { get; set; }
        public string RequiredOperatingSystem { get; set; }
        public string RequiredArchitecture { get; set; }
        public bool DiscardResults { get; set; }
        public List<string> BuildFiles { get; set; } = new List<string>();
        public List<string> OutputFiles { get; set; } = new List<string>();
        public List<string> BuildArchives { get; set; } = new List<string>();
        public List<string> OutputArchives { get; set; } = new List<string>();
        public bool BenchmarkDotNet { get; set; }
    }
}
