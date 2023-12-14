// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        // 5: Support for gzipped uploads

        public int ServerVersion { get; set; } = 5;

        public int Id { get; set; }

        /// <summary>
        /// Common identifier for all jobs in a scenario.
        /// Multiple jobs with the same RunId can be started on the same agent
        /// </summary>
        public string RunId { get; set; } = Guid.NewGuid().ToString("n");

        public string CrankArguments { get; set; }
        public string Origin { get; set; }

        public string Hardware { get; set; }

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

        // Delay from the process started to the console receiving "Application started"
        public TimeSpan StartupMainMethod { get; set; }
        public TimeSpan BuildTime { get; set; }
        public long PublishedSize { get; set; }

        [Obsolete("Source should be stored in Sources dictionary instead")]
        [JsonProperty(Order = 999, ObjectCreationHandling = ObjectCreationHandling.Replace)]
        // Getter and Setter is implemented for backwards compatibility with older configs and agents
        public Source Source
        {
            get
            {
                if (Sources.Count == 0)
                {
                    return new Source();
                }
                else if (Sources.Count == 1)
                {
                    return Sources.Values.Single();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                // Since source was intended to be cloned to the root of the working directory, set the destination to empty
                value.DestinationFolder = "";
                Sources = new Dictionary<string, Source> { [Source.DefaultSource] = value };
                Project = value.Project;
                DockerFile = value.DockerFile;
                DockerPull = value.DockerPull;
                DockerImageName = value.DockerImageName;
                DockerCommand = value.DockerCommand;
                DockerLoad = value.DockerLoad;
                DockerContextDirectory = value.DockerContextDirectory;
                DockerFetchPath = value.DockerFetchPath;
                NoBuild = value.NoBuild;
            }
        }

        /// <summary>
        /// The source information for the benchmarked application
        /// </summary>
        public Dictionary<string, Source> Sources { get; set; } = new Dictionary<string, Source>();
        public string BuildKey { get; set; }
        public string Project { get; set; }
        public string DockerFile { get; set; }
        public string DockerPull { get; set; }
        public string DockerImageName { get; set; }
        public string DockerCommand { get; set; } // Optional command arguments for 'docker run'
        public string DockerLoad { get; set; } // Relative to the docker folder
        public string DockerContextDirectory { get; set; }
        public string DockerFetchPath { get; set; }

        /// <summary>
        /// When SourceKey is defined, indicates whether a build should still occur. 
        /// </summary>
        public bool NoBuild { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }
        public bool NoArguments { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public JobState State { get; set; }

        public int? ExitCode { get; set; }

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

        // Dump
        public bool DumpProcess { get; set; }
        public DumpTypeOption DumpType { get; set; } = DumpTypeOption.Mini;
        public string DumpFile { get; set; }

        // Perfview/Perfcollect
        public bool Collect { get; set; }
        public string CollectArguments { get; set; }
        public bool CollectSwapMemory { get; set; }
        public string PerfViewTraceFile { get; set; }

        // Other collection options

        /// <summary>
        /// Whether to collect the startup phase or not.
        /// If <c>false</c> (default) the collection is triggered when the ready state is detected.
        /// </summary>
        public bool CollectStartup { get; set; }
        
        // For backward compatibility. Use Options.CollectCounters instead
        public bool CollectCounters { get; set; }

        /// <summary>
        /// The expected interval for each recurring measurements (dotnet counters, custom measurements, ...)
        /// </summary>
        public int MeasurementsIntervalSec { get; set; } = 1;
        
        /// <summary>
        /// The list of performance counter providers to be collected.
        /// </summary>
        public List<DotnetCounter> Counters { get; set; } = new List<DotnetCounter>();
        public string BasePath { get; set; }
        public int ProcessId { get; set; }
        public int ChildProcessId { get; set; }
        public int ActiveProcessId => ChildProcessId > 0 ? ChildProcessId : ProcessId;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PackageReferences { get; set; } = new Dictionary<string, string>();
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
        public string StoppingScript { get; set; }
        public ulong MemoryLimitInBytes { get; set; }
        public double CpuLimitRatio { get; set; }
        public string CpuSet { get; set; } // e.g., 0 or 0-3 or 1-4,6
        public ConcurrentQueue<Measurement> Measurements { get; set; } = new ConcurrentQueue<Measurement>();
        public ConcurrentQueue<MeasurementMetadata> Metadata { get; set; } = new ConcurrentQueue<MeasurementMetadata>();

        [JsonIgnore] 
        public int TrackedProcessId => Math.Max(ChildProcessId, ProcessId);

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

        // Custom CollectTimeout for the server job
        public TimeSpan CollectTimeout { get; set; } = TimeSpan.Zero;

        // Custom build timeout
        public TimeSpan BuildTimeout { get; set; } = TimeSpan.Zero;

        public Options Options { get; set; } = new Options();

        public List<string> Features { get; set; } = new List<string>();

        /// Script that is executed once the templates have been processed.
        public List<string> OnConfigure { get; set; } = new List<string>();

        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        public bool CollectDependencies { get; set; }

        /// <summary>
        /// Whether to patch the TFM of project references.
        /// </summary>
        public bool PatchReferences { get; set; } = false;

        public bool IsDocker()
        {
            return !String.IsNullOrEmpty(DockerFile) || !String.IsNullOrEmpty(DockerImageName) || !String.IsNullOrEmpty(DockerPull);
        }

        public string GetNormalizedImageName()
        {
            if (!string.IsNullOrEmpty(DockerPull))
            {
                return DockerPull.ToLowerInvariant();
            }

            // If DockerLoad option is used, the image must be set to the one used to build it
            if (!string.IsNullOrEmpty(DockerLoad))
            {
                return DockerImageName;
            }

            if (!string.IsNullOrEmpty(DockerImageName))
            {
                // If the docker image name already starts with benchmarks, reuse it
                // This prefix is used to clean any dangling container that would not have been stopped automatically
                if (DockerImageName.StartsWith("benchmarks_"))
                {
                    return DockerImageName;
                }
                else
                {
                    return $"benchmarks_{DockerImageName}".ToLowerInvariant();
                }
            }
            else
            {
                return $"benchmarks_{System.IO.Path.GetFileNameWithoutExtension(DockerFile)}".ToLowerInvariant();
            }
        }

        public BuildKeyData GetBuildKeyData()
        {
            return new BuildKeyData
            {
                Sources = Sources.ToDictionary(s => s.Key, s => (s.Value.DestinationFolder, s.Value.GetSourceKeyData())),
                Project = Project,
                RuntimeVersion = RuntimeVersion,
                DesktopVersion = DesktopVersion,
                AspNetCoreVersion = AspNetCoreVersion,
                SdkVersion = SdkVersion,
                Framework = Framework,
                Channel = Channel,
                PatchReferences = PatchReferences,
                PackageReferences = PackageReferences,
                NoGlobalJson = NoGlobalJson,
                UseRuntimeStore = UseRuntimeStore,
                BuildArguments = BuildArguments,
                SelfContained = SelfContained,
                Executable = Executable,
                Collect = Collect,
                UseMonoRuntime = UseMonoRuntime,
                BuildFiles = Options.BuildFiles,
                BuildArchives = Options.BuildArchives,
                OutputFiles = Options.OutputFiles,
                OutputArchives = Options.OutputArchives,
                CollectDependencies = CollectDependencies,
                DockerLoad = DockerLoad,
                DockerPull = DockerPull,
                DockerFile = DockerFile,
                DockerImageName = DockerImageName,
                DockerContextDirectory = DockerContextDirectory
            };
        }
    }

    /// <summary>
    /// Represents a set of properties that configure some behaviors on the driver.
    /// These options are not sent to the server.
    /// </summary>
    public class Options
    {
        public bool DisplayOutput { get; set; }
        public bool DownloadOutput { get; set; }
        public string DownloadOutputOutput { get; set; }
        public bool DownloadBuildLog { get; set; }
        public string DownloadBuildLogOutput { get; set; }
        public bool Fetch { get; set; }
        public string FetchOutput { get; set; }
        public List<string> DownloadFiles { get; set; } = new List<string>();
        public string DownloadFilesOutput { get; set; }
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
        public bool? CollectCounters { get; set; }
        public List<string> CounterProviders { get; set; } = new List<string>();

        // Don't clone and don't build if already cloned and built. 
        // Don't use with floating runtime versions.
        public bool ReuseBuild { get; set; }

        // Don't clone if already cloned.
        // Can be used with floating versions.
        public bool ReuseSource { get; set; }

        // Full, Heap, Mini
        public string DumpType { get; set; }
        public string DumpOutput { get; set; }
        public bool NoGitIgnore { get; set; }
    }

    /// <summary>
    /// A class that stores all the properties that can be used as part of a cache key for the build.
    /// </summary>
    public class BuildKeyData
    {
        public Dictionary<string, (string DestinationFolder, SourceKeyData SourceKeyData)> Sources { get; set; }
        public string Project { get; set; }
        public string RuntimeVersion { get; set; }
        public string DesktopVersion { get; set; }
        public string AspNetCoreVersion { get; set; }
        public string SdkVersion { get; set; }
        public string Framework { get; set; }
        public string Channel { get; set; }
        public bool PatchReferences { get; set; }
        public Dictionary<string, string> PackageReferences { get; set; }
        public bool NoGlobalJson { get; set; }
        public bool UseRuntimeStore { get; set; }
        public List<string> BuildArguments { get; set; }
        public bool SelfContained { get; set; }
        public string Executable { get; set; }
        public bool Collect { get; set; }
        public string UseMonoRuntime { get; set; }
        public List<string> BuildFiles { get; set; }
        public List<string> BuildArchives { get; set; }
        public List<string> OutputFiles { get; set; }
        public List<string> OutputArchives { get; set; }
        public bool CollectDependencies { get; set; }
        public string DockerLoad { get; set; }
        public string DockerPull { get; set; }
        public string DockerFile { get; set; }
        public string DockerImageName { get; set; }
        public string DockerContextDirectory { get; set; }
    }
}
