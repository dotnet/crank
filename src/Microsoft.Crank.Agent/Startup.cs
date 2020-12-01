// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Crank.Models;
using BenchmarksServer;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tools.Trace;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Repository;
using OperatingSystem = Microsoft.Crank.Models.OperatingSystem;
using NuGet.Versioning;

namespace Microsoft.Crank.Agent
{
    public class Startup
    {
        /*
         * List of accepted values for AspNetCoreVersion and RuntimeVersion
         *
            [Empty] The default channel
            Current The publicly released version
            Latest  The latest transitive version 
            Edge    The latest build
            
            // Legacy, this will be converted automatically to new channel/targetFramework semantics
            2.1     -> current
            2.1.*   -> edge
            2.1.8   -> specific version

            Based on the target framework
         */

        private static string DefaultTargetFramework = "net5.0";
        private static string DefaultChannel = "current";

        private const string PerfViewVersion = "P2.0.54";

        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;
        private static readonly string _dotnetInstallShUrl = "https://dot.net/v1/dotnet-install.sh";
        private static readonly string _dotnetInstallPs1Url = "https://dot.net/v1/dotnet-install.ps1";
        private static readonly string _aspNetCoreDependenciesUrl = "https://raw.githubusercontent.com/aspnet/AspNetCore/{0}";
        private static readonly string _perfviewUrl = $"https://github.com/Microsoft/perfview/releases/download/{PerfViewVersion}/PerfView.exe";
        private static readonly string _aspnet5FlatContainerUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/flat2/Microsoft.AspNetCore.App.Runtime.linux-x64/index.json";
        private static readonly string _aspnet6FlatContainerUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/flat2/Microsoft.AspNetCore.App.Runtime.linux-x64/index.json";

        // Safe-keeping these urls
        //private static readonly string _latestRuntimeApiUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/flat2/Microsoft.NetCore.App.Runtime.linux-x64/index.json";
        //private static readonly string _latestDesktopApiUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/flat2/Microsoft.NetCore.App.Runtime.win-x64/index.json";
        //private static readonly string _releaseMetadata = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json";

        // private static readonly string _latestSdkVersionUrl = "https://aka.ms/dotnet/net6/dev/Sdk/productCommit-win-x64.txt";
        
        private static readonly string _aspnetSdkVersionUrl = "https://raw.githubusercontent.com/dotnet/aspnetcore/master/global.json";
        private static readonly string[] _runtimeFeedUrls = new string[] {
            "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/flat2",
            "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/flat2",
            "https://dotnetfeed.blob.core.windows.net/dotnet-core/flatcontainer",
            "https://api.nuget.org/v3/flatcontainer" };

        // Cached lists of SDKs and runtimes already installed
        private static readonly HashSet<string> _installedAspNetRuntimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _installedDotnetRuntimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _installedDesktopRuntimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _ignoredDesktopRuntimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _installedSdks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private const string _defaultUrl = "http://*:5010";
        private static readonly string _defaultHostname = Environment.MachineName.ToLowerInvariant();
        private static string _perfviewPath;
        private static string _dotnetInstallPath;

        // https://docs.docker.com/config/containers/resource_constraints/
        private const double _defaultDockerCfsPeriod = 100000;

        private static readonly IJobRepository _jobs = new InMemoryJobRepository();
        private static string _rootTempDir;

        private static string _buildPath;
        private static string _dotnethome;
        private static bool _cleanup = true;
        private static Process perfCollectProcess;
        private static object _synLock = new object();

        private static Task dotnetTraceTask;
        private static ManualResetEvent dotnetTraceManualReset;

        public static OperatingSystem OperatingSystem { get; }
        public static Hardware Hardware { get; private set; }
        public static string HardwareVersion { get; private set; }
        public static Dictionary<Database, string> ConnectionStrings = new Dictionary<Database, string>();
        public static TimeSpan DriverTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan StartTimeout = TimeSpan.FromMinutes(3);
        public static TimeSpan DefaultBuildTimeout = TimeSpan.FromMinutes(10);
        public static TimeSpan DeletedTimeout = TimeSpan.FromHours(18);
        public static TimeSpan PerfCollectTimeout = TimeSpan.FromMinutes(2);

        private static string _startPerfviewArguments;

        private static ulong eventPipeSessionId = 0;
        private static Task eventPipeTask = null;
        private static bool eventPipeTerminated = false;

        private static ulong measurementsSessionId = 0;
        private static Task measurementsTask = null;
        private static bool measurementsTerminated = false;

        static Startup()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OperatingSystem = OperatingSystem.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OperatingSystem = OperatingSystem.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OperatingSystem = OperatingSystem.OSX;
            }
            else
            {
                throw new InvalidOperationException($"Invalid OSPlatform: {RuntimeInformation.OSDescription}");
            }


            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddSingleton(_jobs);
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime hostApplicationLifetime)
        {
            hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("jobs/{id}/state", JobsApis.GetState);
                endpoints.MapGet("jobs/{id}/touch", JobsApis.GetTouch);
                
                endpoints.MapDefaultControllerRoute();
            });
        }

        public static int Main(string[] args)
        {
            // Prevent unhandled exceptions in the benchmarked apps from displaying a popup that would block
            // the main process on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetErrorMode(ErrorModes.SEM_NONE);
            }

            var app = new CommandLineApplication()
            {
                Name = "BenchmarksServer",
                FullName = "ASP.NET Benchmark Server",
                Description = "REST APIs to run ASP.NET benchmark server",
                OptionsComparison = StringComparison.OrdinalIgnoreCase
            };

            app.HelpOption("-?|-h|--help");

            var urlOption = app.Option("-u|--url", $"URL for Rest APIs.  Default is '{_defaultUrl}'.",
                CommandOptionType.SingleValue);
            var hostnameOption = app.Option("-n|--hostname", $"Hostname for benchmark server.  Default is '{_defaultHostname}'.",
                CommandOptionType.SingleValue);
            var dockerHostnameOption = app.Option("-nd|--docker-hostname", $"Hostname for benchmark server when running Docker on a different hostname.",
                CommandOptionType.SingleValue);
            var hardwareOption = app.Option("--hardware", "Hardware (Cloud or Physical).  Required.",
                CommandOptionType.SingleValue);
            var hardwareVersionOption = app.Option("--hardware-version", "Hardware version (e.g, D3V2, Z420, ...).  Required.",
                CommandOptionType.SingleValue);
            var noCleanupOption = app.Option("--no-cleanup",
                "Don't kill processes or delete temp directories.", CommandOptionType.NoValue);
            var postgresqlConnectionStringOption = app.Option("--postgresql",
                "The connection string for PostgreSql.", CommandOptionType.SingleValue);
            var mysqlConnectionStringOption = app.Option("--mysql",
                "The connection string for MySql.", CommandOptionType.SingleValue);
            var mssqlConnectionStringOption = app.Option("--mssql",
                "The connection string for SqlServer.", CommandOptionType.SingleValue);
            var mongoDbConnectionStringOption = app.Option("--mongodb",
                "The connection string for MongoDb.", CommandOptionType.SingleValue);
            var buildPathOption = app.Option("--build-path", "The path where applications are built.", CommandOptionType.SingleValue);
            var buildTimeoutOption = app.Option("--build-timeout", "Maximum duration of build task in minutes. Default 10 minutes.",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (noCleanupOption.HasValue())
                {
                    _cleanup = false;
                }

                if (postgresqlConnectionStringOption.HasValue())
                {
                    ConnectionStrings[Database.PostgreSql] = postgresqlConnectionStringOption.Value();
                }
                if (mysqlConnectionStringOption.HasValue())
                {
                    ConnectionStrings[Database.MySql] = mysqlConnectionStringOption.Value();
                }
                if (mssqlConnectionStringOption.HasValue())
                {
                    ConnectionStrings[Database.SqlServer] = mssqlConnectionStringOption.Value();
                }
                if (mongoDbConnectionStringOption.HasValue())
                {
                    ConnectionStrings[Database.MongoDb] = mongoDbConnectionStringOption.Value();
                }
                if (hardwareVersionOption.HasValue() && !string.IsNullOrWhiteSpace(hardwareVersionOption.Value()))
                {
                    HardwareVersion = hardwareVersionOption.Value();
                }
                else
                {
                    HardwareVersion = "Unspecified";
                }
                if (Enum.TryParse(hardwareOption.Value(), ignoreCase: true, result: out Hardware hardware))
                {
                    Hardware = hardware;
                }
                else
                {
                    Hardware = Hardware.Unknown;
                }

                if (buildTimeoutOption.HasValue())
                {
                    if (int.TryParse(buildTimeoutOption.Value(), out var buildTimeout))
                    {
                        DefaultBuildTimeout = TimeSpan.FromMinutes(buildTimeout);
                    }
                }

                if (!buildPathOption.HasValue())
                {
                    // If no custom build path is provided, use the current user's temp dir
                    _buildPath = Path.Combine(Path.GetTempPath(), "benchmarks-agent");
                }
                else
                {
                    _buildPath = buildPathOption.Value();
                }

                if (!Directory.Exists(_buildPath))
                {
                    Directory.CreateDirectory(_buildPath);
                }

                var url = urlOption.HasValue() ? urlOption.Value() : _defaultUrl;
                var hostname = hostnameOption.HasValue() ? hostnameOption.Value() : _defaultHostname;
                var dockerHostname = dockerHostnameOption.HasValue() ? dockerHostnameOption.Value() : hostname;

                return Run(url, hostname, dockerHostname).Result;
            });

            return app.Execute(args);
        }

        private static async Task<int> Run(string url, string hostname, string dockerHostname)
        {
            var host = new WebHostBuilder()
                    .UseKestrel()
                    .ConfigureKestrel(o => o.Limits.MaxRequestBodySize = (long)10 * 1024 * 1024 * 1024)
                    .UseStartup<Startup>()
                    .UseUrls(url)
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.SetMinimumLevel(LogLevel.Error);
                        logging.AddConsole();
                    })
                    .Build();

            var hostTask = host.RunAsync();

            var processJobsCts = new CancellationTokenSource();
            var processJobsTask = ProcessJobs(hostname, dockerHostname, processJobsCts.Token);

            var completedTask = await Task.WhenAny(hostTask, processJobsTask);

            // Propagate exception (and exit process) if either task faulted
            await completedTask;

            // Host exited normally, so cancel job processor
            processJobsCts.Cancel();
            await processJobsTask;

            return 0;
        }

        private static async Task ProcessJobs(string hostname, string dockerHostname, CancellationToken cancellationToken)
        {

            try
            {
                CreateTemporaryFolders();

                Log.WriteLine($"Agent ready, waiting for jobs...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    string runId = null;

                    // Lookup expired jobs
                    var expiredJobs = _jobs.GetAll().Where(j => j.State == JobState.Deleted && DateTime.UtcNow - j.LastDriverCommunicationUtc > DeletedTimeout);

                    foreach (var expiredJob in expiredJobs)
                    {
                        Log.WriteLine($"Removing expired job {expiredJob.Id}");
                        _jobs.Remove(expiredJob.Id);
                    }

                    /*
                     * New job is created on the agent
                     * Driver still alive?
                     *      yes 
                     *          -> Initializing
                     *          Driver uploads all its attachments, or source files
                     *          Driver send the /start signal -> Waiting  
                     *          Server acknowledges by changing state to -> Starting
                     *          Server builds the application
                     *          -> Running
                     *          A timer is started to track the state of the app, and update its state if it
                     *          fails or the driver isn't responsive
                     *          
                     *      no -> Deleting
                     */

                    // Select the first job that is not yet Deleted, i.e. 
                    var group = new Dictionary<Job, JobContext>();

                    while (runId == null && !cancellationToken.IsCancellationRequested)
                    {
                        // Looking for the new group of jobs
                        var next = _jobs.GetAll().FirstOrDefault(x => x.State == JobState.New);

                        if (next != null)
                        {
                            runId = next.RunId;

                            foreach (var job in _jobs.GetAll().Where(x => x.RunId == runId))
                            {
                                group[job] = new JobContext { Job = job };
                            }

                            break;
                        }

                        await Task.Delay(1000);
                    }

                    while (runId != null)
                    {
                        // Update group to include new ones
                        foreach (var job in _jobs.GetAll().Where(x => x.RunId == runId))
                        {
                            if (!group.ContainsKey(job))
                            {
                                Log.WriteLine($"Adding job '{job.Service}' ({job.Id}) to group");
                                group[job] = new JobContext { Job = job };
                            }
                        }

                        // If the running group is all finished, start the next one
                        if (group.Keys.All(x => x.State == JobState.Deleted))
                        {
                            Log.WriteLine($"All jobs in group are finished");

                            runId = null;
                            break;
                        }

                        foreach (var job in group.Keys)
                        {
                            var context = group[job];

                            Log.WriteLine($"Processing job '{job.Service}' ({job.Id}) in state {job.State}");

                            // Restore context for the current job
                            var process = context.Process;

                            var workingDirectory = context.WorkingDirectory;
                            var benchmarksDir = context.BenchmarksDir;
                            var startMonitorTime = context.StartMonitorTime;

                            var tempDir = context.TempDir;
                            var dockerImage = context.DockerImage;
                            var dockerContainerId = context.DockerContainerId;

                            eventPipeSessionId = context.EventPipeSessionId;
                            eventPipeTask = context.EventPipeTask;
                            eventPipeTerminated = context.EventPipeTerminated;

                            measurementsSessionId = context.MeasurementsSessionId;
                            measurementsTask = context.MeasurementsTask;
                            measurementsTerminated = context.MeasurementsTerminated;

                            if (job.State == JobState.New)
                            {
                                var now = DateTime.UtcNow;

                                Log.WriteLine($"Acquiring Job '{job.Service}' ({job.Id})");

                                // Ensure all local assets are available
                                await EnsureDotnetInstallExistsAsync();

                                if (now - job.LastDriverCommunicationUtc > DriverTimeout)
                                {
                                    // The job needs to be deleted
                                    Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting job '{job.Service}' ({job.Id}).");
                                    Log.WriteLine($"{job.State} -> Deleting");
                                    job.State = JobState.Deleting;
                                }
                                else
                                {
                                    startMonitorTime = DateTime.UtcNow;
                                    Log.WriteLine($"{job.State} -> Initializing");
                                    job.State = JobState.Initializing;
                                }

                                lock (job.Metadata)
                                {
                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/cpu"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/cpu",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "Amount of time the process has utilized the CPU out of 100%",
                                            ShortDescription = "CPU Usage (%)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/cpu/raw"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/cpu/raw",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "Raw CPU value (not normalized by number of cores)",
                                            ShortDescription = "Cores usage (%)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/working-set"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/working-set",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "Amount of working set used by the process (MB)",
                                            ShortDescription = "Working Set (MB)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/build-time"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/build-time",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "How long it took to build the application",
                                            ShortDescription = "Build Time (ms)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/start-time"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/start-time",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "How long it took to start the application",
                                            ShortDescription = "Start Time (ms)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/published-size"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/published-size",
                                            Aggregate = Operation.Max,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "The size of the published application",
                                            ShortDescription = "Published Size (KB)"
                                        });
                                    }

                                    if (!job.Metadata.Any(x => x.Name == "benchmarks/swap"))
                                    {
                                        job.Metadata.Enqueue(new MeasurementMetadata
                                        {
                                            Source = "Host Process",
                                            Name = "benchmarks/swap",
                                            Aggregate = Operation.Delta,
                                            Reduce = Operation.Max,
                                            Format = "n0",
                                            LongDescription = "Amount of swapped memory (MB)",
                                            ShortDescription = "Swap (MB)"
                                        });
                                    }

                                }

                            }
                            else if (job.State == JobState.Failed)
                            {
                                var now = DateTime.UtcNow;

                                // Clean the job in case the driver is not running
                                if (now - job.LastDriverCommunicationUtc > DriverTimeout)
                                {
                                    Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting job.");
                                    Log.WriteLine($"{job.State} -> Deleting");
                                    job.State = JobState.Deleting;
                                }
                            }
                            else if (job.State == JobState.Waiting)
                            {
                                // TODO: Race condition if DELETE is called during this code
                                try
                                {
                                    if (OperatingSystem == OperatingSystem.Linux &&
                                        (job.WebHost == WebHost.IISInProcess ||
                                        job.WebHost == WebHost.IISOutOfProcess)
                                        )
                                    {
                                        Log.WriteLine($"Skipping job '{job.Service}' ({job.Id})");
                                        Log.WriteLine($"'{job.WebHost}' is not supported on this platform.");
                                        Log.WriteLine($"{job.State} -> NotSupported");
                                        job.State = JobState.NotSupported;
                                        continue;
                                    }

                                    Log.WriteLine($"Starting job '{job.Service}' ({job.Id})");
                                    Log.WriteLine($"{job.State} -> Starting");
                                    job.State = JobState.Starting;

                                    startMonitorTime = DateTime.UtcNow;

                                    Debug.Assert(tempDir == null);
                                    tempDir = GetTempDir();
                                    workingDirectory = null;
                                    dockerImage = null;

                                    var buildTimeout = job.BuildTimeout > DefaultBuildTimeout
                                        ? job.BuildTimeout
                                        : DefaultBuildTimeout;

                                    var buildStart = DateTime.UtcNow;
                                    var cts = new CancellationTokenSource();
                                    Task buildAndRunTask;

                                    if (job.Source.IsDocker())
                                    {
                                        buildAndRunTask = Task.Run(async () =>
                                        {
                                            (dockerContainerId, dockerImage, workingDirectory) = await DockerBuildAndRun(tempDir, job, dockerHostname, cancellationToken: cts.Token);
                                        });
                                    }
                                    else
                                    {
                                        buildAndRunTask = Task.Run(async () =>
                                        {
                                            benchmarksDir = await CloneRestoreAndBuild(tempDir, job, _dotnethome, cts.Token);

                                            if (benchmarksDir == null)
                                            {
                                                // Build error
                                                job.State = JobState.Failed;
                                            }

                                            if (job.State != JobState.Failed && benchmarksDir != null)
                                            {
                                                process = await StartProcess(hostname, Path.Combine(tempDir, benchmarksDir), job, _dotnethome);

                                                Log.WriteLine($"Process started: {job.ProcessId}");

                                                workingDirectory = process.StartInfo.WorkingDirectory;
                                            }
                                        });
                                    }

                                    while (job.State != JobState.Failed && !buildAndRunTask.IsCompleted)
                                    {
                                        await Task.Delay(1000);

                                        // Cancel the build if the driver timed out
                                        if (DateTime.UtcNow - job.LastDriverCommunicationUtc > DriverTimeout)
                                        {
                                            Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting build.");

                                            Log.WriteLine($"{job.State} -> Failed");
                                            job.State = JobState.Failed;

                                            cts.Cancel();
                                            await buildAndRunTask;
                                        }

                                        // Cancel the build if it's taking too long
                                        if (DateTime.UtcNow - buildStart > buildTimeout)
                                        {
                                            Log.WriteLine($"Build is taking too long. Halting build.");
                                            job.Error = "Build is taking too long. Halting build.";

                                            Log.WriteLine($"{job.State} -> Failed");
                                            job.State = JobState.Failed;

                                            cts.Cancel();
                                            await buildAndRunTask;
                                        }

                                        if (buildAndRunTask.IsFaulted)
                                        {
                                            Log.WriteLine($"An unexpected error occurred while building the job. {buildAndRunTask.Exception}");
                                            job.Error = $"An unexpected error occurred while building the job: {buildAndRunTask.Exception.Message}";

                                            Log.WriteLine($"{job.State} -> Failed");
                                            job.State = JobState.Failed;

                                            cts.Cancel();
                                            await buildAndRunTask;
                                        }
                                    }

                                    if (job.State != JobState.Failed)
                                    {
                                        startMonitorTime = DateTime.UtcNow;
                                        var lastMonitorTime = startMonitorTime;
                                        var oldCPUTime = TimeSpan.Zero;

                                        context.Timer = new Timer(_ =>
                                        {

                                            // If we couldn't get the lock it means one of 2 things are true:
                                            // - We're about to dispose so we don't care to run the scan callback anyways.
                                            // - The previous the computation took long enough that the next scan tried to run in parallel
                                            // In either case just do nothing and end the timer callback as soon as possible

                                            if (!Monitor.TryEnter(_synLock))
                                            {
                                                return;
                                            }

                                            try
                                            {
                                                if (context.Disposed || context.Timer == null)
                                                {
                                                    Log.WriteLine("[Warning!!!] Heartbeat still active while context is disposed");
                                                    return;
                                                }

                                                // Pause the timer while we're running
                                                context.Timer.Change(Timeout.Infinite, Timeout.Infinite);

                                                try
                                                {
                                                    var now = DateTime.UtcNow;

                                                    // Stops the job in case the driver is not running
                                                    if (now - job.LastDriverCommunicationUtc > DriverTimeout)
                                                    {
                                                        if (job.State == JobState.Running)
                                                        {
                                                            Log.WriteLine($"[Heartbeat] Driver didn't communicate for {DriverTimeout}. Halting job '{job.Service}' ({job.Id}).");
                                                            Log.WriteLine($"{job.State} -> Stopping");
                                                            job.State = JobState.Stopping;
                                                        }
                                                        else
                                                        {
                                                            Log.WriteLine($"Heartbeat is active, job is '{job.State}' and driver is AWOL. Job deletion must have failed.");
                                                        }
                                                    }

                                                    if (!String.IsNullOrEmpty(dockerImage))
                                                    {
                                                        // Check the container is still running
                                                        var inspectResult = ProcessUtil.RunAsync("docker", "inspect -f {{.State.Running}} " + dockerContainerId,
                                                                captureOutput: true,
                                                                log: false, throwOnError: false).GetAwaiter().GetResult();


                                                        if (String.Equals(inspectResult.StandardOutput.Trim(), "false"))
                                                        {
                                                            Log.WriteLine($"The Docker container has stopped");
                                                            Log.WriteLine($"{job.State} -> Stopping");
                                                            job.State = JobState.Stopping;
                                                        }
                                                        else if (job.State == JobState.Running)
                                                        {
                                                            // Get docker stats
                                                            var result = ProcessUtil.RunAsync("docker", "container stats --no-stream --format \"{{.CPUPerc}}-{{.MemUsage}}\" " + dockerContainerId,
                                                                    log: false, throwOnError: false, captureOutput: true, captureError: true).GetAwaiter().GetResult();

                                                            var stats = result.StandardOutput;

                                                            if (!String.IsNullOrEmpty(stats))
                                                            {
                                                                var data = stats.Trim().Split('-');

                                                                // Format is {value}%
                                                                var cpuPercentRaw = data[0];

                                                                // Format is {used}M/GiB/{total}M/GiB
                                                                var workingSetRaw = data[1];
                                                                var usedMemoryRaw = workingSetRaw.Split('/')[0].Trim();
                                                                var cpu = double.Parse(cpuPercentRaw.Trim('%'));
                                                                var rawCpu = cpu;

                                                                // On Windows the CPU already takes the number or HT into account
                                                                if (OperatingSystem == OperatingSystem.Linux)
                                                                {
                                                                    cpu = cpu / Environment.ProcessorCount;
                                                                }

                                                                cpu = Math.Round(cpu);

                                                                // MiB, GiB, B ?
                                                                var factor = 1;
                                                                double memory;

                                                                if (usedMemoryRaw.EndsWith("GiB"))
                                                                {
                                                                    factor = 1024 * 1024 * 1024;
                                                                    memory = double.Parse(usedMemoryRaw.Substring(0, usedMemoryRaw.Length - 3));
                                                                }
                                                                else if (usedMemoryRaw.EndsWith("MiB"))
                                                                {
                                                                    factor = 1024 * 1024;
                                                                    memory = double.Parse(usedMemoryRaw.Substring(0, usedMemoryRaw.Length - 3));
                                                                }
                                                                else
                                                                {
                                                                    memory = double.Parse(usedMemoryRaw.Substring(0, usedMemoryRaw.Length - 1));
                                                                }

                                                                var workingSet = (long)(memory * factor);

                                                                job.Measurements.Enqueue(new Measurement
                                                                {
                                                                    Name = "benchmarks/working-set",
                                                                    Timestamp = now,
                                                                    Value = Math.Ceiling((double)workingSet / 1024 / 1024) // < 1MB still needs to appear as 1MB
                                                                });

                                                                job.Measurements.Enqueue(new Measurement
                                                                {
                                                                    Name = "benchmarks/cpu",
                                                                    Timestamp = now,
                                                                    Value = cpu
                                                                });

                                                                job.Measurements.Enqueue(new Measurement
                                                                {
                                                                    Name = "benchmarks/cpu/raw",
                                                                    Timestamp = now,
                                                                    Value = Math.Round(rawCpu)
                                                                });

                                                                if (job.CollectSwapMemory && OperatingSystem == OperatingSystem.Linux)
                                                                {
                                                                    try
                                                                    {
                                                                        job.Measurements.Enqueue(new Measurement
                                                                        {
                                                                            Name = "benchmarks/swap",
                                                                            Timestamp = now,
                                                                            Value = GetSwapBytesAsync().GetAwaiter().GetResult() / 1024 / 1024
                                                                        });
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Log.WriteLine($"[ERROR] Could not get swap memory:" + e.ToString());
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (process != null)
                                                    {
                                                        if (process.HasExited)
                                                        {
                                                            if (process.ExitCode != 0)
                                                            {
                                                                Log.WriteLine($"Job failed");

                                                                job.Error = $"Job failed at runtime:\n{job.Output}";

                                                                if (job.State != JobState.Deleting)
                                                                {
                                                                    Log.WriteLine($"{job.State} -> Failed");
                                                                    job.State = JobState.Failed;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Log.WriteLine($"Process has exited ({process.ExitCode})");

                                                                // Don't revert a Deleting state by mistake
                                                                if (job.State != JobState.Deleting
                                                                    && job.State != JobState.Stopped
                                                                    && job.State != JobState.TraceCollected
                                                                    && job.State != JobState.TraceCollecting
                                                                    && job.State != JobState.Deleted
                                                                    )
                                                                {
                                                                    Log.WriteLine($"{job.State} -> Stopped");
                                                                    job.State = JobState.Stopped;
                                                                }
                                                            }
                                                        }
                                                        else if (job.State == JobState.Running)
                                                        {
                                                            // TODO: Accessing the TotalProcessorTime on OSX throws so just leave it as 0 for now
                                                            // We need to dig into this
                                                            Process trackProcess = null;
                                                            
                                                            if (job.ChildProcessId != 0)
                                                            {
                                                                try
                                                                {
                                                                    trackProcess = Process.GetProcessById(job.ChildProcessId);
                                                                }
                                                                catch
                                                                {
                                                                    // child process is done    
                                                                }
                                                            }
                                                            else
                                                            {
                                                                trackProcess = process;
                                                            }

                                                            if (trackProcess != null)
                                                            {
                                                                try
                                                                {
                                                                    trackProcess.Refresh();
                                                                }
                                                                catch
                                                                {
                                                                    trackProcess = null;
                                                                }
                                                            }

                                                            if (trackProcess != null)
                                                            {
                                                                var newCPUTime = OperatingSystem == OperatingSystem.OSX
                                                                    ? TimeSpan.Zero
                                                                    : trackProcess.TotalProcessorTime;
                                                                                                                        
                                                                var elapsed = now.Subtract(lastMonitorTime).TotalMilliseconds;
                                                                var rawCpu = (newCPUTime - oldCPUTime).TotalMilliseconds / elapsed * 100;
                                                                var cpu = Math.Round(rawCpu / Environment.ProcessorCount);
                                                                lastMonitorTime = now;

                                                                // Ignore first measure
                                                                if (oldCPUTime != TimeSpan.Zero && cpu <= 100)
                                                                {
                                                                    job.Measurements.Enqueue(new Measurement
                                                                    {
                                                                        Name = "benchmarks/working-set",
                                                                        Timestamp = now,
                                                                        Value = Math.Ceiling((double)trackProcess.WorkingSet64 / 1024 / 1024) // < 1MB still needs to appear as 1MB
                                                                    });

                                                                    job.Measurements.Enqueue(new Measurement
                                                                    {
                                                                        Name = "benchmarks/cpu",
                                                                        Timestamp = now,
                                                                        Value = cpu
                                                                    });

                                                                    job.Measurements.Enqueue(new Measurement
                                                                    {
                                                                        Name = "benchmarks/cpu/raw",
                                                                        Timestamp = now,
                                                                        Value = Math.Round(rawCpu)
                                                                    });

                                                                    if (job.CollectSwapMemory && OperatingSystem == OperatingSystem.Linux)
                                                                    {
                                                                        try
                                                                        {
                                                                            job.Measurements.Enqueue(new Measurement
                                                                            {
                                                                                Name = "benchmarks/swap",
                                                                                Timestamp = now,
                                                                                Value = GetSwapBytesAsync().GetAwaiter().GetResult() / 1024 / 1024
                                                                            });
                                                                        }
                                                                        catch (Exception e)
                                                                        {
                                                                            Log.WriteLine($"[ERROR] Could not get swap memory:" + e.ToString());
                                                                        }
                                                                    }
                                                                }

                                                                oldCPUTime = newCPUTime;
                                                            }
                                                        }
                                                    }
                                                }
                                                catch
                                                {
                                                    Log.WriteLine("An error occurred while tracking a process. Continuing...");
                                                }
                                                finally
                                                {
                                                    // Resume once we finished processing all connections
                                                    context.Timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                                                }
                                            }
                                            finally
                                            {
                                                Monitor.Exit(_synLock);
                                            }
                                        }, null, TimeSpan.FromTicks(0), TimeSpan.FromSeconds(1));
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine($"Error starting job '{job.Service}' ({job.Id}): {e}");

                                    if (job.State != JobState.Failed)
                                    {
                                        Log.WriteLine($"{job.State} -> Failed");
                                        job.State = JobState.Failed;
                                    }
                                }
                            }
                            else if (job.State == JobState.Stopping)
                            {
                                Log.WriteLine($"Stopping job '{job.Service}' ({job.Id})");

                                await StopJobAsync();
                            }
                            else if (job.State == JobState.Stopped)
                            {
                                Log.WriteLine($"Job '{job.Service}' ({job.Id}) has stopped, waiting for the driver to delete it");

                                if (DateTime.UtcNow - job.LastDriverCommunicationUtc > DriverTimeout)
                                {
                                    // The job needs to be deleted
                                    Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting job.");
                                    Log.WriteLine($"{job.State} -> Deleting");
                                    job.State = JobState.Deleting;
                                }
                            }
                            else if (job.State == JobState.Deleting)
                            {
                                Log.WriteLine($"Deleting job '{job.Service}' ({job.Id})");

                                await DeleteJobAsync();
                            }
                            else if (job.State == JobState.TraceCollecting)
                            {
                                // Stop Perfview
                                if (job.Collect)
                                {
                                    if (OperatingSystem == OperatingSystem.Windows)
                                    {
                                        RunPerfview($"stop /AcceptEula /NoNGenRundown /NoView {_startPerfviewArguments}", Path.Combine(tempDir, benchmarksDir));
                                    }
                                    else if (OperatingSystem == OperatingSystem.Linux)
                                    {
                                        await StopPerfcollectAsync(perfCollectProcess);
                                    }

                                    Log.WriteLine("Trace collected");
                                    Log.WriteLine($"{job.State} ->  TraceCollected");
                                    job.State = JobState.TraceCollected;
                                }

                                // Stop dotnet-trace
                                if (job.DotNetTrace)
                                {
                                    if (dotnetTraceTask != null)
                                    {
                                        if (!dotnetTraceTask.IsCompleted)
                                        {
                                            Log.WriteLine("Stopping dotnet-trace");

                                            dotnetTraceManualReset.Set();

                                            await dotnetTraceTask;

                                            dotnetTraceManualReset = null;
                                            dotnetTraceTask = null;
                                        }


                                        Log.WriteLine("Trace collected");
                                    }
                                    else
                                    {
                                        Log.WriteLine("Trace collection aborted, dotnet-trace was not started");
                                    }

                                    Log.WriteLine($"{job.State} ->  TraceCollected");
                                    job.State = JobState.TraceCollected;
                                }

                                StopCounters();

                            }
                            else if (job.State == JobState.Starting)
                            {
                                var startTimeout = job.StartTimeout > TimeSpan.Zero
                                    ? job.StartTimeout
                                    : StartTimeout
                                    ;

                                if (DateTime.UtcNow - startMonitorTime > startTimeout)
                                {
                                    Log.WriteLine($"Job didn't start during the expected delay");
                                    job.State = JobState.Failed;
                                    job.Error = "Job didn't start during the expected delay. Check that it outputs a startup message on the log.";
                                }

                                if (DateTime.UtcNow - job.LastDriverCommunicationUtc > DriverTimeout)
                                {
                                    // The job needs to be deleted
                                    Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting job.");
                                    Log.WriteLine($"{job.State} -> Deleting");
                                    job.State = JobState.Deleting;
                                }

                            }
                            else if (job.State == JobState.Initializing)
                            {
                                // Check the driver is still communicating
                                if (DateTime.UtcNow - job.LastDriverCommunicationUtc > DriverTimeout)
                                {
                                    // The job needs to be deleted
                                    Log.WriteLine($"Driver didn't communicate for {DriverTimeout}. Halting job.");
                                    Log.WriteLine($"{job.State} -> Deleting");
                                    job.State = JobState.Deleting;
                                }
                            }

                            void StopCounters()
                            {
                                // Releasing EventPipe
                                if (eventPipeTask != null)
                                {
                                    try
                                    {
                                        Log.Write($"Stopping counter event pipes for job '{job.Service}' ({job.Id})");
                                        if (process != null && !eventPipeTerminated && !process.HasExited)
                                        {
                                            EventPipeClient.StopTracing(process.Id, eventPipeSessionId);
                                        }
                                    }
                                    catch (EndOfStreamException)
                                    {
                                        // If the app we're monitoring exits abruptly, this may throw in which case we just swallow the exception and exit gracefully.
                                    }
                                                                        
                                    eventPipeTask = null;
                                    Log.WriteLine($"... Success!", false);
                                }
                            }

                            void StopMeasurement()
                            {
                                // Releasing Measurements
                                if (measurementsTask != null)
                                {
                                    try
                                    {
                                        Log.Write($"Stopping measurement event pipes for job '{job.Service}' ({job.Id})");
                                        if (process != null && !measurementsTerminated && !process.HasExited)
                                        {
                                            EventPipeClient.StopTracing(process.Id, measurementsSessionId);
                                        }
                                    }
                                    catch (EndOfStreamException)
                                    {
                                        // If the app we're monitoring exits abruptly, this may throw in which case we just swallow the exception and exit gracefully.
                                    }

                                    measurementsTask = null;
                                    Log.WriteLine($"... Success!", false);
                                }
                            }

                            async Task StopJobAsync(bool abortCollection = false)
                            {
                                // Check if we already passed here
                                if (context.Timer == null)
                                {
                                    return;
                                }
                                
                                Log.Write("Stopping heartbeat");
                                    
                                Monitor.Enter(_synLock);

                                try
                                {
                                    context.Timer?.Dispose();
                                    Log.WriteLine("... Success!", false);
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine("... Error!", false);
                                    Log.WriteLine(e.ToString());
                                }
                                finally
                                {
                                    context.Timer = null;
                                    context.Disposed = true;

                                    Monitor.Exit(_synLock);
                                }

                                // Delete the benchmarks group
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && (job.MemoryLimitInBytes > 0 || job.CpuLimitRatio > 0 || !String.IsNullOrEmpty(job.CpuSet)))
                                {
                                    var controller = GetCGroupController(job);

                                    await ProcessUtil.RunAsync("cgdelete", $"cpu,memory,cpuset:{controller}", log: true, throwOnError: false);
                                }

                                StopCounters();

                                StopMeasurement();

                                if (process != null && !process.HasExited)
                                {
                                    var processId = process.Id;

                                    // Invoking Stop should also abort collection only the job failed.
                                    // The normal workflow is to stop collection using the TraceCollecting state
                                    if (abortCollection)
                                    {
                                        if (job.Collect)
                                        {
                                            // Abort all perfview processes
                                            if (OperatingSystem == OperatingSystem.Windows)
                                            {
                                                var perfViewProcess = RunPerfview("abort", Path.GetPathRoot(_perfviewPath));
                                            }
                                            else if (OperatingSystem == OperatingSystem.Linux)
                                            {
                                                await StopPerfcollectAsync(perfCollectProcess);
                                            }
                                        }

                                        if (job.DotNetTrace)
                                        {
                                            // Stop dotnet-trace if still active
                                            if (dotnetTraceTask != null)
                                            {
                                                if (!dotnetTraceTask.IsCompleted)
                                                {
                                                    Log.WriteLine("Stopping dotnet-trace");

                                                    dotnetTraceManualReset.Set();

                                                    await dotnetTraceTask;

                                                    dotnetTraceManualReset = null;
                                                    dotnetTraceTask = null;
                                                }
                                            }
                                        }
                                    }

                                    if (OperatingSystem == OperatingSystem.Linux)
                                    {
                                        Log.WriteLine($"Invoking SIGINT ...");

                                        Mono.Unix.Native.Syscall.kill(process.Id, Mono.Unix.Native.Signum.SIGINT);

                                        // Tentatively invoke SIGINT
                                        var waitForShutdownDelay = Task.Delay(TimeSpan.FromSeconds(5));
                                        while (!process.HasExited && !waitForShutdownDelay.IsCompletedSuccessfully)
                                        {
                                            await Task.Delay(200);
                                        }
                                    }

                                    if (!process.HasExited)
                                    {
                                        if (OperatingSystem == OperatingSystem.Linux)
                                        {
                                            Log.WriteLine($"SIGINT was not handled, checking /shutdown endpoint ...");
                                        }

                                        try
                                        {
                                            // Tentatively invoke the shutdown endpoint on the client application
                                            var response = await _httpClient.GetAsync(new Uri(new Uri(job.Url), "/shutdown"));

                                            // Shutdown invoked successfully, wait for the application to stop by itself
                                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                            {
                                                var epoch = DateTime.UtcNow;

                                                do
                                                {
                                                    Log.WriteLine($"Shutdown successfully invoked, waiting for graceful shutdown ...");
                                                    await Task.Delay(1000);

                                                } while (!process.HasExited && (DateTime.UtcNow - epoch < TimeSpan.FromSeconds(5)));
                                            }
                                        }
                                        catch
                                        {
                                            Log.WriteLine($"/shutdown endpoint failed... '{job.Url}/shutdown'");
                                        }
                                    }

                                    if (!process.HasExited)
                                    {
                                        Log.WriteLine($"Forcing process to stop ...");
                                        process.CloseMainWindow();

                                        if (!process.HasExited)
                                        {
                                            process.Kill();
                                        }

                                        process.Dispose();

                                        do
                                        {
                                            Log.WriteLine($"Waiting for process {processId} to stop ...");

                                            await Task.Delay(1000);

                                            try
                                            {
                                                process = Process.GetProcessById(processId);
                                                process.Refresh();
                                            }
                                            catch
                                            {
                                                process = null;
                                            }

                                        } while (process != null && !process.HasExited);
                                    }

                                    Log.WriteLine($"Process has stopped");


                                    job.State = JobState.Stopped;

                                    process = null;
                                }
                                else if (!String.IsNullOrEmpty(dockerImage))
                                {

                                    await DockerCleanUpAsync(dockerContainerId, dockerImage, job);
                                }

                                // Running AfterScript
                                if (!String.IsNullOrEmpty(job.AfterScript))
                                {
                                    var segments = job.AfterScript.Split(' ', 2);
                                    var processResult = await ProcessUtil.RunAsync(segments[0], segments.Length > 1 ? segments[1] : "", log: true, workingDirectory: workingDirectory);

                                    // TODO: Update the output with the result of AfterScript, and change the driver so that it polls the job a last time even when the job is stopped
                                    // if there is an AfterScript
                                }

                                Log.WriteLine($"Process stopped ({job.State})");
                            }

                            async Task DeleteJobAsync()
                            {
                                await StopJobAsync(abortCollection: true);

                                if (_cleanup && !job.NoClean && tempDir != null)
                                {
                                    TryDeleteDir(tempDir, false);
                                }

                                tempDir = null;

                                Log.WriteLine($"{job.State} -> Deleted");

                                job.State = JobState.Deleted;
                            }

                            // Store context for the current job
                            context.Process = process;

                            context.WorkingDirectory = workingDirectory;
                            context.BenchmarksDir = benchmarksDir;
                            context.StartMonitorTime = startMonitorTime;

                            context.TempDir = tempDir;
                            context.DockerImage = dockerImage;
                            context.DockerContainerId = dockerContainerId;

                            context.EventPipeSessionId = eventPipeSessionId;
                            context.EventPipeTask = eventPipeTask;
                            context.EventPipeTerminated = eventPipeTerminated;

                            context.MeasurementsSessionId = measurementsSessionId;
                            context.MeasurementsTask = measurementsTask;
                            context.MeasurementsTerminated = measurementsTerminated;

                            await Task.Delay(1000);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine($"Unnexpected error: {e.ToString()}");
            }
        }

        private static string RunPerfview(string arguments, string workingDirectory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.WriteLine($"PerfView is only supported on Windows");
                return null;
            }

            Log.WriteLine($"Starting process '{_perfviewPath} {arguments}' in '{workingDirectory}'");

            var process = new Process()
            {
                StartInfo = {
                    FileName = _perfviewPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true
            };

            var perfviewDoneEvent = new ManualResetEvent(false);
            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e != null && e.Data != null)
                {
                    Log.WriteLine(e.Data);

                    if (e.Data.Contains("Press enter to close window"))
                    {
                        perfviewDoneEvent.Set();
                    }

                    output.Append(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            // Wait until PerfView is done
            perfviewDoneEvent.WaitOne();

            // Perfview is waiting for a keystroke to stop
            process.StandardInput.WriteLine();

            process.Close();
            return output.ToString();
        }

        private static Process RunPerfcollect(string arguments, string workingDirectory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Log.WriteLine($"PerfCollect is only supported on Linux");
                return null;
            }

            var process = new Process()
            {
                StartInfo = {
                    FileName = "perfcollect",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                }
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (e != null && e.Data != null)
                {
                    Log.WriteLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            Log.WriteLine($"Perfcollect started [{process.Id}]");

            return process;
        }

        private static async Task StopPerfcollectAsync(Process perfCollectProcess)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Log.WriteLine($"PerfCollect is only supported on Linux");
                return;
            }

            if (perfCollectProcess == null || perfCollectProcess.HasExited)
            {
                Log.WriteLine($"PerfCollect is not running");
                return;
            }

            var processId = perfCollectProcess.Id;

            Log.WriteLine($"Stopping PerfCollect");

            Mono.Unix.Native.Syscall.kill(processId, Mono.Unix.Native.Signum.SIGINT);

            // Max delay for perfcollect to stop
            var delay = Task.Delay(PerfCollectTimeout);

            while (!perfCollectProcess.HasExited && !delay.IsCompletedSuccessfully)
            {
                await Task.Delay(1000);
            }

            if (!perfCollectProcess.HasExited)
            {
                Log.WriteLine($"PerfCollect exceeded allowed time, stopping ...");
                perfCollectProcess.CloseMainWindow();

                if (!perfCollectProcess.HasExited)
                {
                    perfCollectProcess.Kill();
                }

                perfCollectProcess.Dispose();

                do
                {
                    Log.WriteLine($"Waiting for process {processId} to stop ...");

                    await Task.Delay(1000);

                    try
                    {
                        perfCollectProcess = Process.GetProcessById(processId);
                        perfCollectProcess.Refresh();
                    }
                    catch
                    {
                        perfCollectProcess = null;
                    }

                } while (perfCollectProcess != null && !perfCollectProcess.HasExited);
            }

            Log.WriteLine($"PerfCollect process has stopped");

            perfCollectProcess = null;

        }

        private static void ConvertLines(string path)
        {
            Log.WriteLine($"Converting '{path}' ...");

            var content = File.ReadAllText(path);

            if (path.IndexOf("\r\n") >= 0)
            {
                File.WriteAllText(path, path.Replace("\r\n", "\n"));
            }
        }

        private static async Task<(string containerId, string imageName, string workingDirectory)> DockerBuildAndRun(string path, Job job, string hostname, CancellationToken cancellationToken = default(CancellationToken))
        {
            var source = job.Source;
            string srcDir;

            // Docker image names must be lowercase
            var imageName = source.GetNormalizedImageName();

            if (source.SourceCode != null)
            {
                srcDir = Path.Combine(path, "src");
                Log.WriteLine($"Extracting source code to {srcDir}");

                ZipFile.ExtractToDirectory(job.Source.SourceCode.TempFilename, srcDir);

                // Convert CRLF to LF on Linux
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Log.WriteLine($"Converting text files ...");

                    foreach (var file in Directory.GetFiles(srcDir + Path.DirectorySeparatorChar, "*.*", SearchOption.AllDirectories))
                    {
                        ConvertLines(file);
                    }
                }

                File.Delete(job.Source.SourceCode.TempFilename);
            }
            else if (!String.IsNullOrEmpty(source.Repository))
            {
                var branchAndCommit = source.BranchOrCommit.Split('#', 2);

                var dir = await Git.CloneAsync(path, source.Repository, shallow: branchAndCommit.Length == 1, branch: branchAndCommit[0]);

                srcDir = Path.Combine(path, dir);

                if (branchAndCommit.Length > 1)
                {
                    await Git.CheckoutAsync(srcDir, branchAndCommit[1]);
                }

                if (source.InitSubmodules)
                {
                    await Git.InitSubModulesAsync(srcDir);
                }
            }
            else
            {
                srcDir = path;
            }

            if (String.IsNullOrEmpty(source.DockerContextDirectory))
            {
                source.DockerContextDirectory = Path.GetDirectoryName(source.DockerFile).Replace("\\", "/");
            }

            var workingDirectory = Path.Combine(srcDir, source.DockerContextDirectory);

            job.BasePath = workingDirectory;

            // Running BeforeScript
            if (!String.IsNullOrEmpty(job.BeforeScript))
            {
                var segments = job.BeforeScript.Split(' ', 2);
                var processResult = await ProcessUtil.RunAsync(segments[0], segments.Length > 1 ? segments[1] : "", workingDirectory: workingDirectory, log: true, outputDataReceived: text => job.Output.AddLine(text));
            }

            // Copy build files before building/publishing
            foreach (var attachment in job.BuildAttachments)
            {
                var filename = Path.Combine(srcDir, attachment.Filename.Replace("\\", "/"));

                Log.WriteLine($"Creating build file: {filename}");

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                File.Copy(attachment.TempFilename, filename);
                File.Delete(attachment.TempFilename);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // The DockerLoad argument contains the path of a tar file that can be loaded
            if (String.IsNullOrEmpty(source.DockerLoad))
            {
                string buildParameters = "";

                // Apply custom build arguments sent from the driver
                foreach (var argument in job.BuildArguments)
                {
                    buildParameters += $"--build-arg {argument} ";
                }

                var dockerBuildArguments = $"build --pull {buildParameters} -t {imageName} -f {source.DockerFile} {workingDirectory}";

                job.BuildLog.AddLine("docker " + dockerBuildArguments);

                var buildResults = await ProcessUtil.RunAsync("docker", dockerBuildArguments,
                    workingDirectory: srcDir,
                    cancellationToken: cancellationToken,
                    log: true,
                    outputDataReceived: text => job.BuildLog.AddLine(text)
                    );

                stopwatch.Stop();

                job.BuildTime = stopwatch.Elapsed;

                job.Measurements.Enqueue(new Measurement
                {
                    Name = "benchmarks/build-time",
                    Timestamp = DateTime.UtcNow,
                    Value = stopwatch.ElapsedMilliseconds
                });

                stopwatch.Reset();

                if (buildResults.ExitCode != 0)
                {
                    job.Error = job.BuildLog.ToString();
                }
            }
            else
            {
                Log.WriteLine($"Loading docker image {source.DockerLoad} from {srcDir}");

                var dockerLoadArguments = $"load -i {source.DockerLoad} ";

                job.BuildLog.AddLine("docker " + dockerLoadArguments);

                await ProcessUtil.RunAsync("docker", dockerLoadArguments, 
                    workingDirectory: srcDir, 
                    cancellationToken: cancellationToken, 
                    log: true,
                    outputDataReceived: text => job.BuildLog.AddLine(text)
                );
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return (null, null, null);
            }

            var environmentArguments = "";

            foreach (var env in job.EnvironmentVariables)
            {
                environmentArguments += $"--env {env.Key}={env.Value} ";
            }

            var containerName = $"{imageName}-{job.Id}";

            // TODO: Clean previous images 

            // Stop container in case it failed to stop earlier
            // await ProcessUtil.RunAsync("docker", $"stop {cont}", throwOnError: false);

            // Delete container if the same name already exists
            // await ProcessUtil.RunAsync("docker", $"rm {imageName}", throwOnError: false);

            var command = OperatingSystem == OperatingSystem.Linux
                ? $"run -d {environmentArguments} {job.Arguments} --label benchmarks --name {containerName} --privileged --network host {imageName} {source.DockerCommand}"
                : $"run -d {environmentArguments} {job.Arguments} --label benchmarks --name {containerName} --network SELF --ip {hostname} {imageName} {source.DockerCommand}";

            if (job.Collect && job.CollectStartup)
            {
                StartCollection(workingDirectory, job);
            }

            job.BuildLog.AddLine("docker " + command);

            var result = await ProcessUtil.RunAsync("docker", $"{command} ", 
                throwOnError: true, 
                onStart: _ => stopwatch.Start(), 
                captureOutput: true,
                log: true,
                outputDataReceived: text => job.BuildLog.AddLine(text)
            );

            var containerId = result.StandardOutput.Trim();

            job.Url = ComputeServerUrl(hostname, job);

            Log.WriteLine($"Intercepting Docker logs for '{containerId}' ...");

            var process = new Process()
            {
                StartInfo = {
                    FileName = "docker",
                    Arguments = $"logs -f {containerId}",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!String.IsNullOrEmpty(job.ReadyStateText))
            {
                Log.WriteLine($"Waiting for startup signal: '{job.ReadyStateText}'...");

                process.OutputDataReceived += (_, e) =>
                {
                    if (e != null && e.Data != null)
                    {
                        Log.WriteLine(e.Data);

                        job.Output.AddLine(e.Data);

                        if (job.State == JobState.Starting && e.Data.IndexOf(job.ReadyStateText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Log.WriteLine($"Ready state detected, application is now running...");
                            MarkAsRunning(hostname, job, stopwatch);

                            if (job.Collect && !job.CollectStartup)
                            {
                                StartCollection(workingDirectory, job);
                            }
                        }

                        ParseMeasurementOutput(job, job.Output);
                    }
                };

                // Also listen on the error output
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e != null && e.Data != null)
                    {
                        Log.WriteLine("[STDERR] " + e.Data);

                        job.Output.AddLine("[STDERR] " + e.Data);

                        if (job.State == JobState.Starting && e.Data.IndexOf(job.ReadyStateText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Log.WriteLine($"Ready state detected, application is now running...");
                            MarkAsRunning(hostname, job, stopwatch);

                            if (job.Collect && !job.CollectStartup)
                            {
                                StartCollection(workingDirectory, job);
                            }
                        }
                    }
                };
            }
            else
            {
                Log.WriteLine($"Trying to contact the application ...");

                process.OutputDataReceived += (_, e) =>
                {
                    if (e != null && e.Data != null)
                    {
                        Log.WriteLine(e.Data);
                        job.Output.AddLine(e.Data);

                        ParseMeasurementOutput(job, job.Output);
                    }
                };

                // Wait until the service is reachable to avoid races where the container started but isn't
                // listening yet. If it keeps failing we ignore it. If the port is unreachable then clients
                // will fail to connect and the job will be cleaned up properly
                if (await WaitToListen(job, hostname, 30))
                {
                    Log.WriteLine($"Application is responding...");
                }
                else
                {
                    Log.WriteLine($"Application MAY be running, continuing...");
                }

                MarkAsRunning(hostname, job, stopwatch);

                if (job.Collect && !job.CollectStartup)
                {
                    StartCollection(workingDirectory, job);
                }
            }

            return (containerId, imageName, workingDirectory);
        }

        private static void ParseMeasurementOutput(Job job, RollingLog standardOutput)
        {

            // Detected custom statistics in stdout, parse it
            if (standardOutput.LastLine.IndexOf("#EndJobStatistics", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Seek the beginning of statistics

                var lines = standardOutput.Get(0);

                var startIndex = lines.Length - 1;

                // Seek backward in case thre are multiple blocks of statistics
                for (; startIndex >= 0; startIndex--)
                {
                    if (lines[startIndex].Contains("#StartJobStatistics", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }

                if (startIndex == lines.Length - 1)
                {
                    Log.WriteLine($"Didn't find start of statistics");
                    return;
                }
                else
                {
                    Log.WriteLine($"Parsing custom measures...");
                }

                var jsonStatistics = String.Join(Environment.NewLine, lines.Skip(startIndex + 1).Take(lines.Length - startIndex - 2));

                try
                {
                    var jobStatistics = JsonConvert.DeserializeObject<JobStatistics>(jsonStatistics);

                    Log.WriteLine($"Found {jobStatistics.Metadata.Count} metadata and {jobStatistics.Measurements.Count} measurements");

                    foreach (var metadata in jobStatistics.Metadata)
                    {
                        job.Metadata.Enqueue(metadata);
                    }

                    foreach (var measurement in jobStatistics.Measurements)
                    {
                        job.Measurements.Enqueue(measurement);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine($"[ERROR] Invalid Json payload: " + e.Message);
                }
            }
        }

        private static async Task<bool> WaitToListen(Job job, string hostname, int maxRetries = 5)
        {
            if (job.IsConsoleApp)
            {
                Log.WriteLine($"Console application detected, not waiting");
                return true;
            }

            Log.WriteLine($"Polling server on {hostname}:{job.Port}");

            for (var i = 1; i <= maxRetries; ++i)
            {
                try
                {
                    using (var tcpClient = new TcpClient())
                    {
                        var connectTask = tcpClient.ConnectAsync(hostname, job.Port);
                        await Task.WhenAny(connectTask, Task.Delay(1000));
                        if (connectTask.IsCompleted)
                        {
                            Log.WriteLine($"Success!");
                            return true;
                        }

                        Log.WriteLine($"Attempt #{i} failed...");
                    }
                }
                catch
                {
                    await Task.Delay(300);
                }
            }

            return false;
        }

        private static async Task DockerCleanUpAsync(string containerId, string imageName, Job job)
        {
            var finalState = JobState.Stopped;

            try
            {
                var state = "";
                await ProcessUtil.RunAsync("docker", "inspect -f {{.State.Running}} " + containerId, throwOnError: false, outputDataReceived: text => state += text + "\n");

                // container is already stopped
                if (state.Contains("false"))
                {
                    var exitCode = "";
                    await ProcessUtil.RunAsync("docker", "inspect -f {{.State.ExitCode}} " + containerId, throwOnError: false, outputDataReceived: text => exitCode += text + "\n");

                    if (exitCode.Trim() != "0")
                    {
                        Log.WriteLine("Job failed");
                        job.Error = job.Output.ToString();
                        finalState = JobState.Failed;
                    }
                }
                else
                {
                    await ProcessUtil.RunAsync("docker", $"stop {containerId}", throwOnError: false);
                }
            }
            finally
            {
                try
                {
                    await ProcessUtil.RunAsync("docker", $"rm --force {containerId}", throwOnError: false);

                    if (job.NoClean)
                    {
                        await ProcessUtil.RunAsync("docker", $"rmi --force --no-prune {imageName}", throwOnError: false);
                    }
                    else
                    {                        
                        await ProcessUtil.RunAsync("docker", $"rmi --force {imageName}", throwOnError: false);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("An error occurred while deleting the docker container: " + e.Message);
                    finalState = JobState.Failed;
                }
                finally
                {
                    job.State = finalState;
                }
            }
        }

        private static async Task<string> CloneRestoreAndBuild(string path, Job job, string dotnetHome, CancellationToken cancellationToken = default)
        {
            // Clone
            string benchmarkedDir = null;

            if (job.Source.SourceCode != null)
            {
                benchmarkedDir = "src";

                var src = Path.Combine(path, benchmarkedDir);
                var published = Path.Combine(src, "published");

                if (String.IsNullOrEmpty(job.Executable))
                {
                    Log.WriteLine($"Extracting files to {src}");
                    ZipFile.ExtractToDirectory(job.Source.SourceCode.TempFilename, src);
                }
                else
                {
                    Log.WriteLine($"Extracting files to {published}");
                    ZipFile.ExtractToDirectory(job.Source.SourceCode.TempFilename, published);
                }

                File.Delete(job.Source.SourceCode.TempFilename);
            }
            else
            {
                // It's possible that the user specified a custom branch/commit for the benchmarks repo,
                // so we need to add that to the set of sources to restore if it's not already there.
                //
                // Note that this is also going to de-dupe the repos if the same one was specified twice at
                // the command-line (last first to support overrides).
                var repositories = new HashSet<Source>(SourceRepoComparer.Instance);

                repositories.Add(job.Source);

                foreach (var source in repositories)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    var branchAndCommit = source.BranchOrCommit.Split('#', 2);

                    var dir = await Git.CloneAsync(path, source.Repository, shallow: branchAndCommit.Length == 1, branch: branchAndCommit[0], cancellationToken);

                    var srcDir = Path.Combine(path, dir);

                    if (SourceRepoComparer.Instance.Equals(source, job.Source))
                    {
                        benchmarkedDir = dir;
                    }

                    if (branchAndCommit.Length > 1)
                    {
                        await Git.CheckoutAsync(srcDir, branchAndCommit[1], cancellationToken);
                    }

                    if (source.InitSubmodules)
                    {
                        await Git.InitSubModulesAsync(srcDir, cancellationToken);
                    }
                }
            }

            Debug.Assert(benchmarkedDir != null);


            // Computes the location of the benchmarked app
            var benchmarkedApp = Path.Combine(path, benchmarkedDir);

            if (!String.IsNullOrEmpty(job.Source.Project))
            {
                benchmarkedApp = Path.Combine(benchmarkedApp, Path.GetDirectoryName(FormatPathSeparators(job.Source.Project)));
            }

            Log.WriteLine($"Benchmarked Application in {benchmarkedApp}");

            var requireDotnetBuild =
                !String.IsNullOrEmpty(job.Source.Project) ||
                String.Equals("dotnet", job.Executable, StringComparison.OrdinalIgnoreCase)
                ;

            // Skip installing dotnet or building project if not necessary
            if (!requireDotnetBuild)
            {
                return benchmarkedDir;
            }

            var env = new Dictionary<string, string>
            {
                // used by recent SDKs
                ["DOTNET_ROOT"] = dotnetHome,
            };

            Log.WriteLine("Downloading build tools");

            // Install latest SDK and runtime
            // * Use custom install dir to avoid changing the default install, which is impossible if other processes
            //   are already using it.
            var buildToolsPath = Path.Combine(path, "buildtools");
            if (!Directory.Exists(buildToolsPath))
            {
                Directory.CreateDirectory(buildToolsPath);
            }

            Log.WriteLine($"Installing dotnet runtimes and sdk");

            // Define which Runtime and SDK will be installed.

            string targetFramework = DefaultTargetFramework;
            string channel = DefaultChannel;

            string runtimeVersion = job.RuntimeVersion;
            string desktopVersion = job.DesktopVersion;
            string aspNetCoreVersion = job.AspNetCoreVersion;
            string sdkVersion = job.SdkVersion;

            ConvertLegacyVersions(ref targetFramework, ref runtimeVersion, ref aspNetCoreVersion);

            var projectFileName = Path.Combine(benchmarkedApp, Path.GetFileName(FormatPathSeparators(job.Source.Project)));

            // If a specific framework is set, use it instead of the detected one
            if (!String.IsNullOrEmpty(job.Framework))
            {
                targetFramework = job.Framework;
            }

            // If no version is set for runtime, check project's default tfm
            else if (!IsVersionPrefix(job.RuntimeVersion))
            {
                targetFramework = ResolveProjectTFM(job, projectFileName, targetFramework);
            }

            await PatchProjectFrameworkReferenceAsync(job, projectFileName, targetFramework);

            // If a specific channel is set, use it instead of the detected one
            if (!String.IsNullOrEmpty(job.Channel))
            {
                channel = job.Channel;
            }

            // Until there is a "current" version of net6.0, use "edge"
            if (targetFramework.Equals("net6.0"))
            {
                channel = "edge";
            }

            if (String.IsNullOrEmpty(runtimeVersion))
            {
                runtimeVersion = channel;
            }

            if (String.IsNullOrEmpty(desktopVersion))
            {
                desktopVersion = channel;
            }

            if (String.IsNullOrEmpty(aspNetCoreVersion))
            {
                aspNetCoreVersion = channel;
            }

            if (String.IsNullOrEmpty(sdkVersion))
            {
                sdkVersion = channel;
            }

            // Retrieve current versions
            var (currentRuntimeVersion, currentDesktopVersion, currentAspNetCoreVersion, currentSdkVersion) = await GetCurrentVersions(targetFramework);

            runtimeVersion = await ResolveRuntimeVersion(buildToolsPath, targetFramework, runtimeVersion, currentRuntimeVersion);

            sdkVersion = await ResolveSdkVersion(sdkVersion, currentSdkVersion);

            aspNetCoreVersion = await ResolveAspNetCoreVersion(aspNetCoreVersion, currentAspNetCoreVersion, targetFramework);

            sdkVersion = PatchOrCreateGlobalJson(job, benchmarkedApp, sdkVersion);

            var installAspNetSharedFramework = job.UseRuntimeStore 
                || aspNetCoreVersion.StartsWith("3.0") 
                || aspNetCoreVersion.StartsWith("3.1") 
                || aspNetCoreVersion.StartsWith("5.0")
                || aspNetCoreVersion.StartsWith("6.0")
                ;

            var dotnetInstallStep = "";

            try
            {
                if (OperatingSystem == OperatingSystem.Windows)
                {
                    desktopVersion = ResolveDestopVersion(desktopVersion, currentDesktopVersion);

                    if (!_installedSdks.Contains(sdkVersion))
                    {
                        dotnetInstallStep = $"SDK '{sdkVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install latest SDK version (and associated runtime)
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("powershell", $"-NoProfile -ExecutionPolicy unrestricted [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; .\\dotnet-install.ps1 -Version {sdkVersion} -NoPath -SkipNonVersionedFiles -InstallDir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedSdks.Add(sdkVersion);
                    }

                    if (!_installedDotnetRuntimes.Contains(runtimeVersion))
                    {
                        dotnetInstallStep = $"Runtime '{runtimeVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install runtimes required for this scenario
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("powershell", $"-NoProfile -ExecutionPolicy unrestricted [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; .\\dotnet-install.ps1 -Version {runtimeVersion} -Runtime dotnet -NoPath -SkipNonVersionedFiles -InstallDir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedDotnetRuntimes.Add(runtimeVersion);
                    }

                    try
                    {
                        // This is not required for < 3.0
                        var beforeDesktop = new[] { "netcoreapp2.1", "netcoreapp2.2", "netcoreapp3.0" };

                        if (!beforeDesktop.Contains(targetFramework))
                        {
                            if (!_installedDesktopRuntimes.Contains(desktopVersion) &&
                                !_ignoredDesktopRuntimes.Contains(desktopVersion))
                            {
                                dotnetInstallStep = $"Desktop runtime '{desktopVersion}'";
                                Log.WriteLine($"Installing {dotnetInstallStep} ...");

                                await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("powershell", $"-NoProfile -ExecutionPolicy unrestricted [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; .\\dotnet-install.ps1 -Version {desktopVersion} -Runtime windowsdesktop -NoPath -SkipNonVersionedFiles -InstallDir {dotnetHome}",
                                log: false,
                                workingDirectory: _dotnetInstallPath,
                                environmentVariables: env,
                                    cancellationToken: cancellationToken),
                                cancellationToken);

                                _installedDesktopRuntimes.Add(desktopVersion);
                            }
                            else
                            {
                                desktopVersion = SeekCompatibleDesktopRuntime(dotnetHome, targetFramework, desktopVersion);
                            }
                        }
                    }
                    catch
                    {
                        // Record that we don't need to try to download this version next time
                        _ignoredDesktopRuntimes.Add(desktopVersion);

                        // if the specified SDK can't be installed

                        // Seeking already installed Desktop runtimes
                        // c.f. https://github.com/dotnet/sdk/issues/4237

                        desktopVersion = SeekCompatibleDesktopRuntime(dotnetHome, targetFramework, desktopVersion);
                    }

                    // The aspnet core runtime is only available for >= 2.1, in 2.0 the dlls are contained in the runtime store
                    if (installAspNetSharedFramework && !_installedAspNetRuntimes.Contains(aspNetCoreVersion))
                    {
                        dotnetInstallStep = $"ASP.NET runtime '{aspNetCoreVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install aspnet runtime required for this scenario
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("powershell", $"-NoProfile -ExecutionPolicy unrestricted [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; .\\dotnet-install.ps1 -Version {aspNetCoreVersion} -Runtime aspnetcore -NoPath -SkipNonVersionedFiles -InstallDir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedAspNetRuntimes.Add(aspNetCoreVersion);
                    }
                }
                else
                {
                    if (!_installedSdks.Contains(sdkVersion))
                    {
                        dotnetInstallStep = $"SDK '{sdkVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install latest SDK version (and associated runtime)
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("/usr/bin/env", $"bash dotnet-install.sh --version {sdkVersion} --no-path --skip-non-versioned-files --install-dir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedSdks.Add(sdkVersion);
                    }

                    if (!_installedDotnetRuntimes.Contains(runtimeVersion))
                    {
                        dotnetInstallStep = $"Runtime '{runtimeVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install required runtime
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("/usr/bin/env", $"bash dotnet-install.sh --version {runtimeVersion} --runtime dotnet --no-path --skip-non-versioned-files --install-dir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedDotnetRuntimes.Add(runtimeVersion);
                    }

                    // The aspnet core runtime is only available for >= 2.1, in 2.0 the dlls are contained in the runtime store
                    if (installAspNetSharedFramework && !_installedAspNetRuntimes.Contains(aspNetCoreVersion))
                    {
                        dotnetInstallStep = $"ASP.NET runtime '{aspNetCoreVersion}'";
                        Log.WriteLine($"Installing {dotnetInstallStep} ...");

                        // Install required runtime
                        await ProcessUtil.RetryOnExceptionAsync(3, () => ProcessUtil.RunAsync("/usr/bin/env", $"bash dotnet-install.sh --version {aspNetCoreVersion} --runtime aspnetcore --no-path --skip-non-versioned-files --install-dir {dotnetHome}",
                        log: false,
                        workingDirectory: _dotnetInstallPath,
                        environmentVariables: env,
                                cancellationToken: cancellationToken),
                            cancellationToken);

                        _installedAspNetRuntimes.Add(aspNetCoreVersion);
                    }
                }
            }
            catch
            {
                job.Error = $"dotnet-install could not install a component: {dotnetInstallStep}";

                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var dotnetDir = dotnetHome;

            // Updating Job to reflect actual versions used
            job.AspNetCoreVersion = aspNetCoreVersion;
            job.RuntimeVersion = runtimeVersion;
            job.DesktopVersion = desktopVersion;
            job.SdkVersion = sdkVersion;

            if (!job.Metadata.Any(x => x.Name == "netSdkVersion"))
            {
                job.Metadata.Enqueue(new MeasurementMetadata
                {
                    Source = "Host Process",
                    Name = "netSdkVersion",
                    Aggregate = Operation.Last,
                    Reduce = Operation.Last,
                    Format = "",
                    LongDescription = ".NET Core SDK Version",
                    ShortDescription = ".NET Core SDK Version"
                });

                job.Measurements.Enqueue(new Measurement
                {
                    Name = "netSdkVersion",
                    Timestamp = DateTime.UtcNow,
                    Value = sdkVersion
                });
            }

            // Build and Restore
            var dotnetExecutable = GetDotNetExecutable(dotnetDir);

            var buildParameters =
                $"/p:MicrosoftNETCoreAppPackageVersion={runtimeVersion} " +
                $"/p:MicrosoftAspNetCoreAppPackageVersion={aspNetCoreVersion} " +
                // The following properties could be removed in a future version
                $"/p:BenchmarksNETStandardImplicitPackageVersion={aspNetCoreVersion} " +
                $"/p:BenchmarksNETCoreAppImplicitPackageVersion={aspNetCoreVersion} " +
                $"/p:BenchmarksRuntimeFrameworkVersion={runtimeVersion} " +
                $"/p:BenchmarksTargetFramework={targetFramework} " +
                $"/p:BenchmarksAspNetCoreVersion={aspNetCoreVersion} " +
                $"/p:MicrosoftAspNetCoreAllPackageVersion={aspNetCoreVersion} " +
                $"/p:NETCoreAppMaximumVersion=99.9 "; // Force the SDK to accept the TFM even if it's an unknown one. For instance using SDK 2.1 to build a netcoreapp2.2 TFM.

            if (OperatingSystem == OperatingSystem.Windows)
            {
                buildParameters += $"/p:MicrosoftWindowsDesktopAppPackageVersion={desktopVersion} ";
            }

            if (targetFramework == "netcoreapp2.1")
            {
                buildParameters += $"/p:MicrosoftNETCoreApp21PackageVersion={runtimeVersion} ";
                if (!job.UseRuntimeStore)
                {
                    buildParameters += $"/p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App ";
                }
            }
            else if (targetFramework == "netcoreapp2.2")
            {
                buildParameters += $"/p:MicrosoftNETCoreApp22PackageVersion={runtimeVersion} ";
                if (!job.UseRuntimeStore)
                {
                    buildParameters += $"/p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App ";
                }
            }
            else if (targetFramework == "netcoreapp3.0")
            {
                buildParameters += $"/p:MicrosoftNETCoreApp30PackageVersion={runtimeVersion} ";
                if (!job.UseRuntimeStore)
                {
                    buildParameters += $"/p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App ";
                }
            }
            else if (targetFramework == "netcoreapp3.1")
            {
                buildParameters += $"/p:MicrosoftNETCoreApp31PackageVersion={runtimeVersion} ";
                if (!job.UseRuntimeStore)
                {
                    buildParameters += $"/p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App ";
                }
            }
            else if (targetFramework == "netcoreapp5.0" || targetFramework == "net5.0" || targetFramework == "net6.0")
            {
                buildParameters += $"/p:MicrosoftNETCoreApp50PackageVersion={runtimeVersion} ";
                buildParameters += $"/p:GenerateErrorForMissingTargetingPacks=false ";
                if (!job.UseRuntimeStore)
                {
                    buildParameters += $"/p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App ";
                }
            }
            else
            {
                job.Error = $"Unsupported framework: {targetFramework}";
                return null;
            }

            // #1445 force no cache for restore to avoid restore failures for packages published within last 30 minutes
            buildParameters += "/p:RestoreNoCache=true ";

            // Apply custom build arguments sent from the driver
            foreach (var argument in job.BuildArguments)
            {
                buildParameters += $"{argument} ";
            }

            // Specify tfm in case the project targets multiple one
            buildParameters += $"--framework {targetFramework} ";

            if (job.SelfContained)
            {
                buildParameters += $"--self-contained ";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (job.Hardware == Hardware.ARM64)
                    {
                        buildParameters += "-r win-arm64 ";
                    }
                    else
                    {
                        buildParameters += "-r win-x64 ";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    buildParameters += "-r osx-x64 ";
                }
                else
                {
                    if (job.Hardware == Hardware.ARM64)
                    {
                        buildParameters += "-r linux-arm64 ";
                    }
                    else
                    {
                        buildParameters += "-r linux-x64 ";
                    }
                }
            }

            // Copy build files before building/publishing
            foreach (var attachment in job.BuildAttachments)
            {
                var filename = Path.Combine(benchmarkedApp, attachment.Filename.Replace("\\", "/"));

                Log.WriteLine($"Creating build file: {filename}");

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                File.Copy(attachment.TempFilename, filename);
                File.Delete(attachment.TempFilename);
            }

            var outputFolder = Path.Combine(benchmarkedApp);

            if (String.IsNullOrEmpty(job.Executable))
            {
                outputFolder = Path.Combine(benchmarkedApp, "published");

                var projectName = Path.GetFileName(FormatPathSeparators(job.Source.Project));

                var arguments = $"publish {projectName} -c Release -o {outputFolder} {buildParameters}";

                // This might be set already, and the SDK will then use it for some targets files
                // https://github.com/dotnet/sdk/blob/e2faebad758a7d38b5965cda755a17e9e9881599/src/Cli/Microsoft.DotNet.Cli.Utils/MSBuildForwardingAppWithoutLogging.cs#L75
                env["MSBuildSDKsPath"] = Path.Combine(Path.GetDirectoryName(dotnetExecutable), $"sdk/{sdkVersion}/Sdks");

                Log.WriteLine($"Working directory: {benchmarkedApp}");
                Log.WriteLine($"Command line: {dotnetExecutable} {arguments}");

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                job.BuildLog.AddLine($"\nCommand:\ndotnet {arguments}");

                var buildResults = await ProcessUtil.RunAsync(dotnetExecutable, arguments,
                    workingDirectory: benchmarkedApp,
                    environmentVariables: env,
                    throwOnError: false,
                    outputDataReceived: text => job.BuildLog.AddLine(text),
                    cancellationToken: cancellationToken
                    );

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                job.BuildLog.AddLine($"Exit code: {buildResults.ExitCode}");

                if (buildResults.ExitCode != 0)
                {
                    job.Error = job.BuildLog.ToString();
                    return null;
                }

                stopwatch.Stop();

                job.BuildTime = stopwatch.Elapsed;

                job.Measurements.Enqueue(new Measurement
                {
                    Name = "benchmarks/build-time",
                    Timestamp = DateTime.UtcNow,
                    Value = stopwatch.ElapsedMilliseconds
                });

                Log.WriteLine($"Application published successfully in {job.BuildTime.TotalMilliseconds} ms");

                PatchRuntimeConfig(job, outputFolder, aspNetCoreVersion, runtimeVersion);
            }

            var publishedSize = DirSize(new DirectoryInfo(outputFolder)) / 1024;

            if (publishedSize != 0)
            {
                job.PublishedSize = publishedSize;

                job.Measurements.Enqueue(new Measurement
                {
                    Name = "benchmarks/published-size",
                    Timestamp = DateTime.UtcNow,
                    Value = publishedSize
                });
            }

            Log.WriteLine($"Published size: {job.PublishedSize}");

            // Copy crossgen in the app folder
            if (job.Collect && OperatingSystem == OperatingSystem.Linux)
            {
                // https://dotnetfeed.blob.core.windows.net/dotnet-core/flatcontainer/microsoft.netcore.app.runtime.linux-x64/index.json
                // This is because the package names were changed.For 3.0 +, look for ~/.nuget/packages/microsoft.netcore.app.runtime.linux-x64/<version>/tools/crossgen.

                Log.WriteLine("Copying crossgen to application folder");

                try
                {
                    // Downloading corresponding package
                    var runtimePath = Path.Combine(_rootTempDir, "RuntimePackages", $"microsoft.netcore.app.runtime.linux-x64.{runtimeVersion}.nupkg");

                    // Ensure the folder already exists
                    Directory.CreateDirectory(Path.GetDirectoryName(runtimePath));

                    if (!File.Exists(runtimePath))
                    {
                        Log.WriteLine($"Downloading runtime package");

                        var found = false;
                        foreach (var feed in _runtimeFeedUrls)
                        {
                            var url = $"{feed}/microsoft.netcore.app.runtime.linux-x64/{runtimeVersion}/microsoft.netcore.app.runtime.linux-x64.{runtimeVersion}.nupkg";

                            if (await DownloadFileAsync(url, runtimePath, maxRetries: 3, timeout: 60, throwOnError: false))
                            {
                                found = true;
                                break;
                            }
                            else
                            {
                                continue;

                            }
                        }

                        if (!found)
                        {
                            throw new Exception("Linux runtime package not found");
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Found runtime package at '{runtimePath}'");
                    }

                    using (var archive = ZipFile.OpenRead(runtimePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.EndsWith("/crossgen", StringComparison.OrdinalIgnoreCase))
                            {
                                var crossgenFolder = job.SelfContained
                                    ? outputFolder
                                    : Path.Combine(dotnetDir, "shared", "Microsoft.NETCore.App", runtimeVersion)
                                    ;

                                var crossgenFilename = Path.Combine(crossgenFolder, "crossgen");

                                if (!File.Exists(crossgenFilename))
                                {
                                    // Ensure the target folder is created
                                    Directory.CreateDirectory(Path.GetDirectoryName(crossgenFilename));

                                    entry.ExtractToFile(crossgenFilename);
                                    Log.WriteLine($"Copied crossgen to {crossgenFolder}");
                                }

                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("ERROR: Failed to download crossgen. " + e.ToString());
                }
            }

            // Download mono runtime
            if (!string.IsNullOrEmpty(job.UseMonoRuntime) && !string.Equals(job.UseMonoRuntime, "false", StringComparison.OrdinalIgnoreCase))
            {
                if (!job.SelfContained)
                {
                    throw new Exception("The job is trying to use the mono runtime but was not configured as self-contained.");
                }

                await UseMonoRuntimeAsync(runtimeVersion, outputFolder, job.UseMonoRuntime, job.Hardware);
            }

            // Copy all output attachments
            foreach (var attachment in job.Attachments)
            {
                var filename = Path.Combine(outputFolder, attachment.Filename.Replace("\\", "/"));

                Log.WriteLine($"Creating output file: {filename}");

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                File.Copy(attachment.TempFilename, filename);
                File.Delete(attachment.TempFilename);
            }

            // AOT binaries from output folder using mono
            if (!string.IsNullOrEmpty(job.UseMonoRuntime) && !string.Equals(job.UseMonoRuntime, "false", StringComparison.OrdinalIgnoreCase))
            {
                if (job.UseMonoRuntime.Equals("llvm-aot"))
                {
                    await AOT4Mono(sdkVersion, runtimeVersion, outputFolder);
                }
            }

            return benchmarkedDir;

            long DirSize(DirectoryInfo d)
            {
                long size = 0;
                // Add file sizes.
                var fis = d.GetFiles();
                foreach (var fi in fis)
                {
                    size += fi.Length;
                }
                // Add subdirectory sizes.
                var dis = d.GetDirectories();
                foreach (var di in dis)
                {
                    size += DirSize(di);
                }
                return size;
            }
        }

        private static string GetAssemblyName(Job job, string projectFileName)
        {
            if (File.Exists(projectFileName))
            {
                var project = XDocument.Load(projectFileName);
                var assemblyNameElement = project.Root
                    .Elements("PropertyGroup")
                    .Select(x => x.Element("AssemblyName"))
                    .FirstOrDefault();

                if (assemblyNameElement != null)
                {
                    Log.WriteLine($"Detected custom assembly name: '{assemblyNameElement.Value}'");
                    return assemblyNameElement.Value;
                }
            }

            return Path.GetFileNameWithoutExtension(FormatPathSeparators(job.Source.Project));
        }
        private static string ResolveProjectTFM(Job job, string projectFileName, string targetFramework)
        {
            if (File.Exists(projectFileName))
            {
                var project = XDocument.Load(projectFileName);
                var targetFrameworkElement = project.Root
                    .Elements("PropertyGroup")
                    .Where(p => !p.Attributes("Condition").Any())
                    .SelectMany(x => x.Elements("TargetFramework"))
                    .FirstOrDefault();

                if (targetFrameworkElement != null)
                {
                    targetFramework = targetFrameworkElement.Value;

                    Log.WriteLine($"Detected target framework: '{targetFramework}'");
                }
                else
                {
                    var targetFrameworksElement = project.Root
                        .Elements("PropertyGroup")
                        .Where(p => !p.Attributes("Condition").Any())
                        .SelectMany(x => x.Elements("TargetFrameworks"))
                        .FirstOrDefault();

                    if (targetFrameworksElement != null)
                    {
                        targetFramework = targetFrameworksElement.Value.Split(';').FirstOrDefault();

                        Log.WriteLine($"Detected target framework: '{targetFramework}'");
                    }
                }
            }

            return targetFramework;
        }

        private static bool IsVersionPrefix(string version)
        {
            return !String.IsNullOrEmpty(version) && char.IsDigit(version[0]);
        }

        private static async Task PatchProjectFrameworkReferenceAsync(Job job, string projectFileName, string targetFramework)
        {
            // Alters the csproj to force the TFM and the framework versions defined in the job. 

            if (File.Exists(projectFileName))
            {
                Log.WriteLine("Patching project file with Framework References");

                await ProcessUtil.RetryOnExceptionAsync(3, async () =>
                {
                    XDocument project;

                    using (var projectFileStream = File.OpenRead(projectFileName))
                    {
                        project = await XDocument.LoadAsync(projectFileStream, LoadOptions.None, new CancellationTokenSource(3000).Token);
                    }

                    // Remove existing <TargetFramework(s)> element

                    var targetFrameworksElements = project.Root.Elements("PropertyGroup").Elements("TargetFrameworks");

                    if (targetFrameworksElements.Any())
                    {
                        var targetFrameworksElement = targetFrameworksElements.First();
                        ((XElement)targetFrameworksElement).Value = targetFramework;
                    }
                    else
                    {
                        var targetFrameworkElements = project.Root.Elements("PropertyGroup").Elements("TargetFramework");

                        if (targetFrameworkElements.Any())
                        {
                            var targetFrameworkElement = targetFrameworkElements.First();
                            ((XElement)targetFrameworkElement).Value = targetFramework;
                        }
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        project.Root.Add(
                            new XElement("ItemGroup",
                                new XAttribute("Condition", "$(TargetFramework) == 'netcoreapp3.0' or $(TargetFramework) == 'netcoreapp3.1' or $(TargetFramework) == 'netcoreapp5.0' or $(TargetFramework) == 'net5.0' or $(TargetFramework) == 'net6.0'"),
                                new XElement("FrameworkReference",
                                    new XAttribute("Update", "Microsoft.AspNetCore.App"),
                                    new XAttribute("RuntimeFrameworkVersion", "$(MicrosoftAspNetCoreAppPackageVersion)")
                                    ),
                                new XElement("FrameworkReference",
                                    new XAttribute("Update", "Microsoft.NETCore.App"),
                                    new XAttribute("RuntimeFrameworkVersion", "$(MicrosoftNETCoreAppPackageVersion)")
                                )
                            )
                        );
                    }
                    else
                    {
                        project.Root.Add(
                            new XElement("ItemGroup",
                                new XAttribute("Condition", "$(TargetFramework) == 'netcoreapp3.0' or $(TargetFramework) == 'netcoreapp3.1' or $(TargetFramework) == 'netcoreapp5.0' or $(TargetFramework) == 'net5.0' or $(TargetFramework) == 'net6.0'"),
                                new XElement("FrameworkReference",
                                    new XAttribute("Update", "Microsoft.AspNetCore.App"),
                                    new XAttribute("RuntimeFrameworkVersion", "$(MicrosoftAspNetCoreAppPackageVersion)")
                                    ),
                                new XElement("FrameworkReference",
                                    new XAttribute("Update", "Microsoft.NETCore.App"),
                                    new XAttribute("RuntimeFrameworkVersion", "$(MicrosoftNETCoreAppPackageVersion)")
                                    ),
                                new XElement("FrameworkReference",
                                    new XAttribute("Update", "Microsoft.WindowsDesktop.App"),
                                    new XAttribute("RuntimeFrameworkVersion", "$(MicrosoftWindowsDesktopAppPackageVersion)")
                                    )
                            )
                        );
                    }

                    using (var projectFileStream = File.CreateText(projectFileName))
                    {
                        await project.SaveAsync(projectFileStream, SaveOptions.None, new CancellationTokenSource(3000).Token);
                    }
                });
            }
        }

        private static void ConvertLegacyVersions(ref string targetFramework, ref string runtimeVersion, ref string aspNetCoreVersion)
        {
            // Converting legacy values

            if (runtimeVersion.EndsWith("*")) // 2.1.*, 2.*, 5.0.*
            {
                var major = int.Parse(runtimeVersion.Split('.')[0]);

                if (major >= 5)
                {
                    targetFramework = "net" + runtimeVersion.Substring(0, 3);
                }
                else
                {
                    targetFramework = "netcoreapp" + runtimeVersion.Substring(0, 3);
                }
                
                runtimeVersion = "edge";
            }
            else if (runtimeVersion.Split('.').Length == 2) // 2.1, 5.0
            {
                var major = int.Parse(runtimeVersion.Split('.')[0]);

                if (major >= 5)
                {
                    targetFramework = "net" + runtimeVersion.Substring(0, 3);
                }
                else
                {
                    targetFramework = "netcoreapp" + runtimeVersion.Substring(0, 3);
                }
                
                runtimeVersion = "current";
            }

            if (aspNetCoreVersion.EndsWith("*")) // 2.1.*, 2.*
            {
                aspNetCoreVersion = "edge";
            }
            else if (aspNetCoreVersion.Split('.').Length == 2) // 2.1, 5.0
            {
                aspNetCoreVersion = "current";
            }
        }

        private static string SeekCompatibleDesktopRuntime(string dotnetHome, string targetFramework, string desktopVersion)
        {
            var versionPrefix = targetFramework.Substring(targetFramework.Length - 3);

            foreach (var dir in Directory.GetDirectories(Path.Combine(dotnetHome, "shared", "Microsoft.WindowsDesktop.App")))
            {
                var version = new DirectoryInfo(dir).Name;
                _installedDesktopRuntimes.Add(version);

                // At least one matching Desktop runtime should be found as the sdk was installed before
                if (version.StartsWith(versionPrefix))
                {
                    desktopVersion = version;
                }
            }

            return desktopVersion;
        }

        private static async Task<string> ResolveAspNetCoreVersion(string aspNetCoreVersion, string currentAspNetCoreVersion, string targetFramework)
        {
            var versionPrefix = targetFramework.Substring(targetFramework.Length - 3);

            // Define which ASP.NET Core packages version to use

            if (String.Equals(aspNetCoreVersion, "Current", StringComparison.OrdinalIgnoreCase))
            {
                aspNetCoreVersion = currentAspNetCoreVersion;
                Log.WriteLine($"ASP.NET: {aspNetCoreVersion} (Current)");
            }
            else if (String.Equals(aspNetCoreVersion, "Latest", StringComparison.OrdinalIgnoreCase)
                || String.Equals(aspNetCoreVersion, "Edge", StringComparison.OrdinalIgnoreCase))
            {
                // aspnet runtime service releases are not published on feeds
                if (versionPrefix == "6.0")
                {
                    aspNetCoreVersion = await GetFlatContainerVersion(_aspnet6FlatContainerUrl, versionPrefix);
                    Log.WriteLine($"ASP.NET: {aspNetCoreVersion} (Latest - From 6.0 feed)");
                }
                else if (versionPrefix == "5.0")
                {
                    aspNetCoreVersion = await GetFlatContainerVersion(_aspnet5FlatContainerUrl, versionPrefix);
                    Log.WriteLine($"ASP.NET: {aspNetCoreVersion} (Latest - From 5.0 feed)");
                }
                else
                {
                    aspNetCoreVersion = currentAspNetCoreVersion;
                    Log.WriteLine($"ASP.NET: {aspNetCoreVersion} (Latest - Fallback on Current)");
                }
            }
            else
            {
                Log.WriteLine($"ASP.NET: {aspNetCoreVersion} (Specific)");
            }

            return aspNetCoreVersion;
        }

        private static string PatchOrCreateGlobalJson(Job job, string benchmarkedApp, string sdkVersion)
        {
            // Looking for the first existing global.json file to update

            var globalJsonPath = new DirectoryInfo(benchmarkedApp);

            while (globalJsonPath != null && !File.Exists(Path.Combine(globalJsonPath.FullName, "global.json")) && globalJsonPath != null)
            {
                globalJsonPath = globalJsonPath.Parent;
            }

            globalJsonPath = globalJsonPath ?? new DirectoryInfo(benchmarkedApp);

            var globalJsonFilename = Path.Combine(globalJsonPath.FullName, "global.json");

            if (job.NoGlobalJson)
            {
                if (!File.Exists(globalJsonFilename))
                {
                    Log.WriteLine($"Could not find global.json file");
                }
                else
                {
                    Log.WriteLine($"Searching SDK version in global.json");

                    var globalObject = JObject.Parse(File.ReadAllText(globalJsonFilename));
                    sdkVersion = globalObject["sdk"]["version"].ToString();

                    Log.WriteLine($"Detecting global.json SDK version: {sdkVersion}");
                }
            }
            else
            {
                if (!File.Exists(globalJsonFilename))
                {
                    // No global.json found
                    Log.WriteLine($"Creating custom global.json");

                    var globalJson = "{ \"sdk\": { \"version\": \"" + sdkVersion + "\" } }";
                    File.WriteAllText(Path.Combine(benchmarkedApp, "global.json"), globalJson);
                    }
                else
                {
                    // File found, we need to update it
                    Log.WriteLine($"Patching existing global.json file");

                    var globalObject = JObject.Parse(File.ReadAllText(globalJsonFilename));

                    // Create the "sdk" property if it doesn't exist
                    globalObject.TryAdd("sdk", new JObject());

                    globalObject["sdk"]["version"] = new JValue(sdkVersion);

                    File.WriteAllText(globalJsonFilename, globalObject.ToString());
                }
            }

            return sdkVersion;
        }

        private static void PatchRuntimeConfig(Job job, string publishFolder, string aspnetcoreversion, string runtimeversion)
        {
            var folder = new DirectoryInfo(publishFolder);
            var runtimeConfigFilename = folder.GetFiles("*.runtimeconfig.json").FirstOrDefault()?.FullName;

            if (!File.Exists(runtimeConfigFilename))
            {
                Log.WriteLine("Ignoring runtimeconfig.json. File not found.");
                return;
            }

            // File found, we need to update it
            Log.WriteLine($"Patching {Path.GetFileName(runtimeConfigFilename)} ");

            var runtimeObject = JObject.Parse(File.ReadAllText(runtimeConfigFilename));

            var runtimeOptions = runtimeObject["runtimeOptions"] as JObject;

            if (runtimeOptions.ContainsKey("includedFrameworks"))
            {
                Log.WriteLine("Application is self-contained, skipping runtimeconfig.json");
                return;
            }

            // Remove exising "framework" (singular) node
            runtimeOptions.Remove("framework");

            // Create the "frameworks" property instead
                var frameworks = new JArray();
            runtimeOptions.TryAdd("frameworks", frameworks);

            frameworks.Add(
                    new JObject(
                        new JProperty("name", "Microsoft.NETCore.App"),
                        new JProperty("version", runtimeversion)
                    ));
            frameworks.Add(
                    new JObject(
                        new JProperty("name", "Microsoft.AspNetCore.App"),
                        new JProperty("version", aspnetcoreversion)
                    ));

            File.WriteAllText(runtimeConfigFilename, runtimeObject.ToString());
        }

        private static async Task<string> ResolveSdkVersion(string sdkVersion, string currentSdkVersion)
        {
            if (String.Equals(sdkVersion, "Current", StringComparison.OrdinalIgnoreCase))
            {
                sdkVersion = currentSdkVersion;
                Log.WriteLine($"SDK: {sdkVersion} (Current)");
            }
            else if (String.Equals(sdkVersion, "Latest", StringComparison.OrdinalIgnoreCase))
            {
                sdkVersion = await GetAspNetSdkVersion();
                Log.WriteLine($"SDK: {sdkVersion} (Latest)");
            }
            else if (String.Equals(sdkVersion, "Edge", StringComparison.OrdinalIgnoreCase))
            {
                // hard-coded value until https://github.com/dotnet/sdk/issues/14660 is fixed
                sdkVersion = "6.0.100-alpha.1.20568.5";
                Log.WriteLine($"SDK: {sdkVersion} (Edge - hard-coded)");

                // sdkVersion = await ParseLatestVersionFile(_latestSdkVersionUrl);
                // Log.WriteLine($"SDK: {sdkVersion} (Edge)");
            }
            else
            {
                Log.WriteLine($"SDK: {sdkVersion} (Specific)");
            }

            return sdkVersion;
        }

        private static async Task<string> ResolveRuntimeVersion(string buildToolsPath, string targetFramework, string runtimeVersion, string currentRuntimeVersion)
        {
            if (String.Equals(runtimeVersion, "Current", StringComparison.OrdinalIgnoreCase))
            {
                runtimeVersion = currentRuntimeVersion;
                Log.WriteLine($"Runtime: {runtimeVersion} (Current)");
            }
            else if (String.Equals(runtimeVersion, "Latest", StringComparison.OrdinalIgnoreCase))
            {
                // Get the version that is defined by the ASP.NET repository
                // Note: to use the latest build available, use Edge channel
                runtimeVersion = await GetAspNetRuntimeVersion(buildToolsPath, targetFramework);
                Log.WriteLine($"Runtime: {runtimeVersion} (Latest)");
            }
            else if (String.Equals(runtimeVersion, "Edge", StringComparison.OrdinalIgnoreCase))
            {
                // Older versions are still published on old feed. Including service releases

                foreach (var runtimeApiUrl in _runtimeFeedUrls)
                {
                    try
                    {
                        runtimeVersion = await GetFlatContainerVersion(runtimeApiUrl + "/microsoft.netcore.app.runtime.win-x64/index.json", targetFramework.Substring(targetFramework.Length - 3));

                        if (!String.IsNullOrEmpty(runtimeVersion))
                        {
                            Log.WriteLine($"Runtime: {runtimeVersion} (Edge)");
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                // Custom version
                Log.WriteLine($"Runtime: {runtimeVersion} (Specific)");
            }

            return runtimeVersion;
        }

        private static string ResolveDestopVersion(string desktopVersion, string currentDesktopVersion)
        {
            if (String.Equals(desktopVersion, "Current", StringComparison.OrdinalIgnoreCase))
            {
                desktopVersion = currentDesktopVersion;
                Log.WriteLine($"Desktop: {desktopVersion} (Current)");
            }
            else if (String.Equals(desktopVersion, "Latest", StringComparison.OrdinalIgnoreCase))
            {
                desktopVersion = currentDesktopVersion;
                Log.WriteLine($"Desktop: {currentDesktopVersion} (Latest)");
            }
            else if (String.Equals(desktopVersion, "Edge", StringComparison.OrdinalIgnoreCase))
            {
                desktopVersion = currentDesktopVersion;
                Log.WriteLine($"Desktop: {currentDesktopVersion} (Edge)");
            }
            else
            {
                // Custom version
                desktopVersion = currentDesktopVersion;
                Log.WriteLine($"Desktop: {desktopVersion} (Current)");
            }

            return desktopVersion;
        }

        public static async Task<string> GetAspNetSdkVersion()
        {
            var globalJson = await DownloadContentAsync(_aspnetSdkVersionUrl, maxRetries: 5, timeout: 10);
            var globalObject = JObject.Parse(globalJson);
            return globalObject["sdk"]["version"].ToString();
        }

        /// <summary>
        /// Retrieves the runtime version used on ASP.NET Coherence builds
        /// </summary>
        private static async Task<string> GetAspNetRuntimeVersion(string buildToolsPath, string targetFramework)
        {
            var aspNetCoreDependenciesPath = Path.Combine(buildToolsPath, Path.GetFileName(_aspNetCoreDependenciesUrl));

            string latestRuntimeVersion = "";

            switch (targetFramework)
            {
                case "netcoreapp2.1":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "release/2.1/build/dependencies.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppPackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;

                case "netcoreapp2.2":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "release/2.2/build/dependencies.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppPackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;

                case "netcoreapp3.0":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "release/3.0/eng/Versions.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppRefPackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;

                case "netcoreapp3.1":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "release/3.1/eng/Versions.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppRuntimewinx64PackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;

                case "netcoreapp5.0":
                case "net5.0":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "master/5.0/Versions.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppRuntimewinx64PackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;

                case "net6.0":

                    await DownloadFileAsync(String.Format(_aspNetCoreDependenciesUrl, "master/eng/Versions.props"), aspNetCoreDependenciesPath, maxRetries: 5, timeout: 10);
                    latestRuntimeVersion = XDocument.Load(aspNetCoreDependenciesPath).Root
                        .Elements("PropertyGroup")
                        .Select(x => x.Element("MicrosoftNETCoreAppRuntimewinx64PackageVersion"))
                        .Where(x => x != null)
                        .FirstOrDefault()
                        .Value;

                    break;
            }

            Log.WriteLine($"Detecting AspNetCore repository runtime version: {latestRuntimeVersion}");
            return latestRuntimeVersion;
        }

        /// <summary>
        /// Retrieves the Current runtime and sdk versions for a tfm
        /// </summary>
        private static async Task<(string Runtime, string Desktop, string AspNet, string Sdk)> GetCurrentVersions(string targetFramework)
        {
            // There are currently no release for net6.0
            // Remove once there is at least a preview and a "release-metadata" file
            if (targetFramework.Equals("net6.0", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null, null);
            }

            var frameworkVersion = targetFramework.Substring(targetFramework.Length - 3); // 3.1
            var metadataUrl = $"https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/{frameworkVersion}/releases.json";

            try
            {
                var content = await DownloadContentAsync(metadataUrl);
                var index = JObject.Parse(content);

                var aspnet = index.SelectToken($"$.releases[0].aspnetcore-runtime.version").ToString();
                var sdk = index.SelectToken($"$.releases[0].sdk.version").ToString();
                var runtime = index.SelectToken($"$.releases[0].runtime.version").ToString();
                var desktop = index.SelectToken($"$.releases[0].windowsdesktop.version").ToString();

                return (runtime, desktop, aspnet, sdk);
            }
            catch
            {
                Log.WriteLine("Could not load release metadata file for current versions");

                return (null, null, null, null);
            }
        }

        /// <summary>
        /// Parses files that contain two lines: a sha and a version
        /// </summary>
        private static async Task<string> ParseLatestVersionFile(string url)
        {
            var content = await DownloadContentAsync(url);

            string latestSdk;
            using (var sr = new StringReader(content))
            {
                sr.ReadLine();
                latestSdk = sr.ReadLine();

            }

            return latestSdk;
        }

        private static async Task<bool> DownloadFileAsync(string url, string outputPath, int maxRetries, int timeout = 5, bool throwOnError = true)
        {
            Log.WriteLine($"Downloading {url}");

            HttpResponseMessage response = null;

            for (var i = 0; i < maxRetries; ++i)
            {
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                    response.EnsureSuccessStatusCode();

                    // This probably won't use async IO on windows since the stream
                    // needs to created with the right flags
                    using (var stream = File.Create(outputPath))
                    {
                        // Copy the response stream directly to the file stream
                        await response.Content.CopyToAsync(stream);
                    }

                    return true;
                }
                catch (OperationCanceledException)
                {
                    Log.WriteLine($"Timeout trying to download {url}, attempt {i + 1}");
                }
                catch (HttpRequestException)
                {
                    // No need to retry on a 404
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine($"Failed to download {url}, attempt {i + 1}, Exception: {ex}");
                }
            }

            if (throwOnError)
            {
                throw new InvalidOperationException($"Failed to download {url} after {maxRetries} attempts");
            }

            return false;
        }

        private static string GetTempDir()
        {
            var temp = Path.Combine(_rootTempDir, Path.GetRandomFileName());

            if (Directory.Exists(temp))
            {
                // Retry
                return GetTempDir();
            }
            else
            {
                Directory.CreateDirectory(temp);
                Log.WriteLine($"Created temp directory '{temp}'");
                return temp;
            }
        }

        private static void TryDeleteDir(string path, bool rethrow = true)
        {
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            Log.WriteLine($"Deleting directory '{path}'");

            // Delete occasionally fails with the following exception:
            //
            // System.UnauthorizedAccessException: Access to the path 'Benchmarks.dll' is denied.
            //
            // If delete fails, retry once every second up to 10 times.
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var dir = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };
                    foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        info.Attributes = FileAttributes.Normal;
                    }
                    dir.Delete(recursive: true);
                    Log.WriteLine("SUCCESS");
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    Log.WriteLine("Nothing to do");
                    break;
                }
                catch
                {
                    Log.WriteLine("Error, retrying ...");

                    if (i < 9)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Log.WriteLine("All retries failed");

                        if (rethrow)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        private static string GetDotNetExecutable(string dotnetHome)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(dotnetHome, "dotnet.exe")
                : Path.Combine(dotnetHome, "dotnet");
        }

        private static async Task<Process> StartProcess(string hostname, string benchmarksRepo, Job job, string dotnetHome)
        {
            var workingDirectory = !String.IsNullOrEmpty(job.Source.Project)
                ? Path.Combine(benchmarksRepo, Path.GetDirectoryName(FormatPathSeparators(job.Source.Project)))
                : benchmarksRepo
                ;

            var scheme = (job.Scheme == Scheme.H2 || job.Scheme == Scheme.Https) ? "https" : "http";
            var serverUrl = $"{scheme}://{hostname}:{job.Port}";
            var executable = GetDotNetExecutable(dotnetHome);

            var projectFileName = Path.Combine(benchmarksRepo, FormatPathSeparators(job.Source.Project));
            var assemblyName = GetAssemblyName(job, projectFileName);

            var benchmarksDll = !String.IsNullOrEmpty(assemblyName)
                ? Path.Combine(workingDirectory, "published", $"{assemblyName}.dll")
                : Path.Combine(workingDirectory, "published")
                ;

            var iis = job.WebHost == WebHost.IISInProcess || job.WebHost == WebHost.IISOutOfProcess;

            // Running BeforeScript
            if (!String.IsNullOrEmpty(job.BeforeScript))
            {
                var segments = job.BeforeScript.Split(' ', 2);
                var result = await ProcessUtil.RunAsync(segments[0], segments.Length > 1 ? segments[1] : "", workingDirectory: workingDirectory, log: true, outputDataReceived: text => job.Output.AddLine(text));
            }

            var commandLine = benchmarksDll ?? "";

            if (job.SelfContained)
            {
                workingDirectory = Path.Combine(workingDirectory, "published");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    executable = Path.Combine(workingDirectory, $"{assemblyName}.exe");
                }
                else
                {
                    executable = Path.Combine(workingDirectory, assemblyName);
                }

                commandLine = "";
            }

            if (!String.IsNullOrEmpty(job.Executable))
            {
                executable = job.Executable;

                if (String.Equals(executable, "dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    executable = GetDotNetExecutable(dotnetHome);
                }
                else
                {
                    // we need the full path to run this, as it is not in the path
                    executable = Path.Combine(workingDirectory, executable);
                }
            }

            job.BasePath = workingDirectory;

            commandLine += $" {job.Arguments}";

            // Benchmarkdotnet needs the actual cli path to generate its benchmarked app
            commandLine = commandLine.Replace("{{benchmarks-cli}}", executable);

            if (iis)
            {
                Log.WriteLine($"Generating application host config for '{executable} {commandLine}'");

                var apphost = GenerateApplicationHostConfig(job, job.BasePath, executable, commandLine, hostname);
                commandLine = $"-h \"{apphost}\"";
                executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\inetsrv\w3wp.exe");
            }

            // The cgroup limits are set on the root group as .NET is reading these only, and not the ones that it would run inside

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && (job.MemoryLimitInBytes > 0 || job.CpuLimitRatio > 0 || !String.IsNullOrEmpty(job.CpuSet)))
            {
                var controller = GetCGroupController(job);

                var cgcreate = await ProcessUtil.RunAsync("cgcreate", $"-g memory,cpu,cpuset:{controller}", log: true);

                if (cgcreate.ExitCode > 0)
                {
                    job.Error += "Could not create cgroup";
                    return null;
                }

                if (job.MemoryLimitInBytes > 0)
                {
                    await ProcessUtil.RunAsync("cgset", $"-r memory.limit_in_bytes={job.MemoryLimitInBytes} {controller}", log: true);
                }
                else
                {
                    await ProcessUtil.RunAsync("cgset", $"-r memory.limit_in_bytes=-1 {controller}", log: true);
                }

                if (job.CpuLimitRatio > 0)
                {
                    // Ensure the cfs_period_us is the same as what docker would use
                    await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_period_us={_defaultDockerCfsPeriod} {controller}", log: true);
                    await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_quota_us={Math.Floor(job.CpuLimitRatio * _defaultDockerCfsPeriod)} {controller}", log: true);
                }
                else
                {
                    await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_quota_us=-1 {controller}", log: true);
                }


                if (!String.IsNullOrEmpty(job.CpuSet))
                {

                    await ProcessUtil.RunAsync("cgset", $"-r cpuset.cpus={job.CpuSet} {controller}", log: true);
                }
                else
                {
                    await ProcessUtil.RunAsync("cgset", $"-r cpuset.cpus=0-{Environment.ProcessorCount - 1} {controller}", log: true);
                }

                // The cpuset.mems value for the 'benchmarks' controller needs to match the root one
                // to be compatible with the allowed nodes
                var memsRoot = File.ReadAllText("/sys/fs/cgroup/cpuset/cpuset.mems");

                // Both cpus and mems need to be initialized
                await ProcessUtil.RunAsync("cgset", $"-r cpuset.mems={memsRoot} {controller}", log: true);

                commandLine = $"-g memory,cpu,cpuset:{controller} {executable} {commandLine}";
                executable = "cgexec";
            }

            Log.WriteLine($"Invoking executable: {executable}");
            Log.WriteLine($"  Arguments: {commandLine}");
            Log.WriteLine($"  Working directory: {workingDirectory}");

            var process = new Process()
            {
                StartInfo = {
                    FileName = executable,
                    Arguments = commandLine,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    UseShellExecute = false,
                },
                EnableRaisingEvents = true
            };

            process.StartInfo.Environment.Add("COREHOST_SERVER_GC", "1");

            // Force Kestrel server urls
            process.StartInfo.Environment.Add("ASPNETCORE_URLS", serverUrl);

            if (job.Database != Database.None)
            {
                if (ConnectionStrings.ContainsKey(job.Database))
                {
                    process.StartInfo.Environment.Add("Database", job.Database.ToString());
                    process.StartInfo.Environment.Add("ConnectionString", ConnectionStrings[job.Database]);
                }
                else
                {
                    Log.WriteLine($"Could not find connection string for {job.Database}");
                }
            }

            if (job.Collect && OperatingSystem == OperatingSystem.Linux)
            {
                // c.f. https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/linux-performance-tracing.md#collecting-a-trace
                // The Task library EventSource events are distorting the trace quite a bit.
                // It is better at least for now to turn off EventSource events when collecting linux data.
                // Thus don’t set COMPlus_EnableEventLog = 1
                process.StartInfo.Environment.Add("COMPlus_PerfMapEnabled", "1");
            }

            foreach (var env in job.EnvironmentVariables)
            {
                Log.WriteLine($"Setting ENV: {env.Key} = {env.Value}");
                process.StartInfo.Environment.Add(env.Key, env.Value);
            }

            var stopwatch = new Stopwatch();

            process.OutputDataReceived += (_, e) =>
            {
                if (e != null && e.Data != null)
                {
                    Log.WriteLine(e.Data);

                    job.Output.AddLine(e.Data);

                    if (job.State == JobState.Starting &&
                        ((!String.IsNullOrEmpty(job.ReadyStateText) && e.Data.IndexOf(job.ReadyStateText, StringComparison.OrdinalIgnoreCase) >= 0) || job.IsConsoleApp))
                    {
                        RunAndTrace();
                    }

                    ParseMeasurementOutput(job, job.Output);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e != null && e.Data != null)
                {
                    var log = "[STDERR] " + e.Data;

                    Log.WriteLine(log);

                    job.Output.AddLine(log);

                    if (job.State == JobState.Starting &&
                        ((!String.IsNullOrEmpty(job.ReadyStateText) && e.Data.IndexOf(job.ReadyStateText, StringComparison.OrdinalIgnoreCase) >= 0) || job.IsConsoleApp))
                    {
                        MarkAsRunning(hostname, job, stopwatch);

                        if (!job.CollectStartup)
                        {
                            if (job.Collect)
                            {
                                StartCollection(Path.Combine(benchmarksRepo, job.BasePath), job);
                            }

                            if (job.DotNetTrace)
                            {
                                StartDotNetTrace(process.Id, job);
                            }
                        }
                    }

                    // Detect the app is wrapping a child process
                    var processIdMarker = "##ChildProcessId:";
                    if (e.Data.StartsWith(processIdMarker) 
                        && int.TryParse(e.Data.Substring(processIdMarker.Length), out var childProcessId))
                    {
                        Log.WriteLine($"Tracking child process id: {childProcessId}");
                        job.ChildProcessId = childProcessId;
                    }
                }
            };

            if (job.CollectStartup)
            {
                if (job.Collect)
                {
                    StartCollection(Path.Combine(benchmarksRepo, job.BasePath), job);
                }

                if (job.DotNetTrace)
                {
                    StartDotNetTrace(process.Id, job);
                }
            }

            stopwatch.Start();
            process.Start();

            job.ProcessId = process.Id;
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // A Console App that has no ReadyStateText should be assumed as started
            if (String.IsNullOrEmpty(job.ReadyStateText) && job.IsConsoleApp)
            {
                RunAndTrace();
            }

            if (job.Counters.Any())
            {
                StartCounters(job);
            }

            StartMeasurement(job);

            if (job.MemoryLimitInBytes > 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Log.WriteLine($"Creating job object with memory limits: {job.MemoryLimitInBytes}");

                    var manager = new ChildProcessManager(job.MemoryLimitInBytes);
                    manager.AddProcess(process);

                    process.Exited += (sender, e) =>
                    {
                        Log.WriteLine("Releasing job object");
                        manager.Dispose();
                    };
                }
            }

            // We try to detect an endpoint is ready if we are running in IIS (no console logs)
            // or if no ReadyStateText is provided and the application is not a ConsoleApp
            if (iis || (String.IsNullOrEmpty(job.ReadyStateText) && !job.IsConsoleApp))
            {
                await WaitToListen(job, hostname);

                RunAndTrace();
            }

            return process;

            void RunAndTrace()
            {
                if (MarkAsRunning(hostname, job, stopwatch))
                {
                    if (!job.CollectStartup)
                    {
                        if (job.Collect)
                        {
                            StartCollection(Path.Combine(benchmarksRepo, job.BasePath), job);
                        }

                        if (job.DotNetTrace)
                        {
                            StartDotNetTrace(process.Id, job);
                        }
                    }
                }
            }
        }

        private static string GetCGroupController(Job job)
        {
            // Create a unique cgroup controller per agent
            return $"benchmarks-{Process.GetCurrentProcess().Id}-{job.Id}";
        }

        private static void StartCounters(Job job)
        {
            eventPipeTerminated = false;
            eventPipeTask = new Task(async () =>
            {
                var providerNames = job.Counters.Select(x => x.Provider).Distinct().ToArray();

                Log.WriteLine($"Listening to counter event pipes (providers: {string.Join(", ", providerNames)})");

                try
                {
                    var providerList = providerNames
                        .Select(p => new EventPipeProvider(
                            name: p,
                            eventLevel: EventLevel.Informational,
                            arguments: new Dictionary<string, string>() 
                                { { "EventCounterIntervalSec", "1" } })
                        )
                        .ToList();

                    var configuration = new EventPipeSessionConfiguration(
                            circularBufferSizeMB: 1000,
                            format: EventPipeSerializationFormat.NetTrace,
                            providers: providerList);

                    EventPipeEventSource source = null;
                    Stream binaryReader = null;

                    var retries = 10;
                    while (retries-- > 0)
                    {
                        try
                        {
                            binaryReader = EventPipeClient.CollectTracing(job.ProcessId, configuration, out eventPipeSessionId);
                            break;
                        }
                        catch (TimeoutException)
                        {
                        }

                        await Task.Delay(100);
                    }

                    if (retries == -1)
                    {
                        Log.WriteLine("[ERROR] Failed to create counters event pipe client");
                        return;
                    }

                    source = new EventPipeEventSource(binaryReader);

                    source.Dynamic.All += (eventData) =>
                    {
                        // We only track event counters
                        if (!eventData.EventName.Equals("EventCounters"))
                        {
                            return;
                        }

                        var payloadVal = (IDictionary<string, object>)(eventData.PayloadValue(0));
                        var payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);

                        var counterName = payloadFields["Name"].ToString();

                        // Skip value if the provider is unknown
                        if (!providerNames.Contains(eventData.ProviderName, StringComparer.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        // TODO: optimize by pre-computing a searchable structure
                        var counter = job.Counters.FirstOrDefault(x => x.Provider.Equals(eventData.ProviderName, StringComparison.OrdinalIgnoreCase) && x.Name.Equals(counterName, StringComparison.OrdinalIgnoreCase));

                        if (counter == null)
                        {
                            // The counter is not tracked
                            return;
                        }

                        var measurement = new Measurement();

                        measurement.Name = counter.Measurement;

                        switch (payloadFields["CounterType"])
                        {
                            case "Sum":
                                measurement.Value = payloadFields["Increment"];
                                break;
                            case "Mean":
                                measurement.Value = payloadFields["Mean"];
                                break;
                            default:
                                Log.WriteLine($"Unknown CounterType: {payloadFields["CounterType"]}");
                                break;
                        }

                        measurement.Timestamp = eventData.TimeStamp;

                        job.Measurements.Enqueue(measurement);
                    };

                    source.Process();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Read past end of stream.")
                    {
                        // Expected if the process has exited by itself
                        // and the event pipe is till trying to read from it
                    }
                    else
                    {
                        Log.WriteLine($"[ERROR] {ex.ToString()}");
                    }
                }
                finally
                {
                    eventPipeTerminated = true; // This indicates that the runtime is done. We shouldn't try to talk to it anymore.
                }
            });

            eventPipeTask.Start();
        }

        private static void StartMeasurement(Job job)
        {
            if (job.ProcessId == 0)
            {
                throw new ArgumentException($"Undefined process id for '{job.Service}'");
            }

            measurementsTerminated = false;
            measurementsTask = new Task(async () =>
            {
                Log.WriteLine("Starting measurement");

                try
                {
                    var providerList = new List<EventPipeProvider>()
                        {
                            new EventPipeProvider(
                                name: "Benchmarks",
                                eventLevel: EventLevel.Verbose),
                        };

                    var configuration = new EventPipeSessionConfiguration(
                            circularBufferSizeMB: 1000,
                            format: EventPipeSerializationFormat.NetTrace,
                            providers: providerList);

                    EventPipeEventSource source = null;
                    Stream binaryReader = null;

                    var retries = 10;
                    while (retries-- > 0)
                    {
                        try
                        {
                            binaryReader = EventPipeClient.CollectTracing(job.ProcessId, configuration, out measurementsSessionId);
                            break;
                        }
                        catch (TimeoutException)
                        {
                        }

                        await Task.Delay(100);
                    }

                    if (retries == -1)
                    {
                        Log.WriteLine("[ERROR] Failed to create measurements event pipe client");
                        return;
                    }

                    source = new EventPipeEventSource(binaryReader);

                    source.Dynamic.All += (eventData) =>
                    {
                        // We only track event counters for System.Runtime
                        if (eventData.ProviderName == "Benchmarks")
                        {
                            // TODO: Catch all event counters automatically
                            // And configure the filterData in the provider
                            //if (!eventData.EventName.Equals("EventCounters"))
                            //{
                            //job.Measurements.Enqueue(new Measurement
                            //{
                            //    Timestamp = eventData.TimeStamp,
                            //    Name = eventData.PayloadByName("name").ToString(),
                            //    Value = eventData.PayloadByName("value")
                            //});
                            //}

                            if (eventData.EventName.StartsWith("Measure"))
                            {
                                job.Measurements.Enqueue(new Measurement
                                {
                                    Timestamp = eventData.TimeStamp,
                                    Name = eventData.PayloadByName("name").ToString(),
                                    Value = eventData.PayloadByName("value")
                                });
                            }
                            else if (eventData.EventName == "Metadata")
                            {
                                job.Metadata.Enqueue(new MeasurementMetadata
                                {
                                    Source = "Benchmark",
                                    Name = eventData.PayloadByName("name").ToString(),
                                    Aggregate = Enum.Parse<Operation>(eventData.PayloadByName("aggregate").ToString(), true),
                                    Reduce = Enum.Parse<Operation>(eventData.PayloadByName("reduce").ToString(), true),
                                    ShortDescription = eventData.PayloadByName("shortDescription").ToString(),
                                    LongDescription = eventData.PayloadByName("longDescription").ToString(),
                                    Format = eventData.PayloadByName("format").ToString(),
                                });
                            }
                        }
                    };

                    source.Process();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Read past end of stream.")
                    {
                        // Expected if the process has exited by itself
                        // and the event pipe is till trying to read from it
                    }
                    else
                    {
                        Log.WriteLine($"[ERROR] {ex.ToString()}");
                    }
                }
                finally
                {
                    measurementsTerminated = true; // This indicates that the runtime is done. We shouldn't try to talk to it anymore.
                }
            });

            measurementsTask.Start();
        }

        private static void StartCollection(string workingDirectory, Job job)
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                job.PerfViewTraceFile = Path.Combine(job.BasePath, "benchmarks.etl.zip");
                var perfViewArguments = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(job.CollectArguments))
                {
                    foreach (var tuple in job.CollectArguments.Split(';'))
                    {
                        var values = tuple.Split(new char[] { '=' }, 2);
                        perfViewArguments[values[0]] = values.Length > 1 ? values[1] : "";
                    }
                }

                _startPerfviewArguments = $"";

                foreach (var customArg in perfViewArguments)
                {
                    var value = String.IsNullOrEmpty(customArg.Value) ? "" : $"={customArg.Value}";
                    _startPerfviewArguments += $" /{customArg.Key}{value}";
                }

                RunPerfview($"start /AcceptEula /NoGui {_startPerfviewArguments} \"{Path.Combine(job.BasePath, "benchmarks.trace")}\"", workingDirectory);
                Log.WriteLine($"Starting PerfView {_startPerfviewArguments}");
            }
            else
            {
                var perfViewArguments = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(job.CollectArguments))
                {
                    foreach (var tuple in job.CollectArguments.Split(';'))
                    {
                        var values = tuple.Split(new char[] { '=' }, 2);
                        perfViewArguments[values[0]] = values.Length > 1 ? values[1] : "";
                    }
                }

                var perfviewArguments = "collect benchmarks";

                foreach (var customArg in perfViewArguments)
                {
                    var value = String.IsNullOrEmpty(customArg.Value) ? "" : $" {customArg.Value.ToLowerInvariant()}";
                    perfviewArguments += $" -{customArg.Key}{value}";
                }

                job.PerfViewTraceFile = Path.Combine(job.BasePath, "benchmarks.trace.zip");
                perfCollectProcess = RunPerfcollect(perfviewArguments, workingDirectory);
            }
        }

        private static void StartDotNetTrace(int processId, Job job)
        {
            job.PerfViewTraceFile = Path.Combine(job.BasePath, "trace.nettrace");

            dotnetTraceManualReset = new ManualResetEvent(false);
            dotnetTraceTask = Collect(dotnetTraceManualReset, processId, new FileInfo(job.PerfViewTraceFile), 256, job.DotNetTraceProviders, default(TimeSpan));

            if (dotnetTraceTask == null)
            {
                throw new Exception("NULL!!!");
            }
        }

        private static async Task UseMonoRuntimeAsync(string runtimeVersion, string outputFolder, string mode, Hardware? hardware)
        {
            if (String.IsNullOrEmpty(mode))
            {
                return;
            }

            string pkgNameSuffix = "";
            if (hardware == Hardware.ARM64)
            {
                pkgNameSuffix = "arm64";
            }
            else
            {
                pkgNameSuffix = "x64";
            }

            try
            {
                var packageName = "";
                
                switch (mode) 
                {
                    case "jit":
                        packageName = $"Microsoft.NETCore.App.Runtime.Mono.linux-{pkgNameSuffix}".ToLowerInvariant();
                        break;
                    case "llvm-jit":
                    case "llvm-aot":
                        packageName = $"Microsoft.NETCore.App.Runtime.Mono.LLVM.AOT.linux-{pkgNameSuffix}".ToLowerInvariant();
                        break;
                    default:
                        throw new Exception("Invalid mono runtime moniker: " + mode);
                }

                var runtimePath = Path.Combine(_rootTempDir, "RuntimePackages", $"{packageName}.{runtimeVersion}.nupkg");

                // Ensure the folder already exists
                Directory.CreateDirectory(Path.GetDirectoryName(runtimePath));

                if (!File.Exists(runtimePath))
                {
                    Log.WriteLine($"Downloading mono runtime package");

                    var found = false;
                    foreach (var feed in _runtimeFeedUrls)
                    {
                        var url = $"{feed}/{packageName}/{runtimeVersion}/{packageName}.{runtimeVersion}.nupkg";

                        if (await DownloadFileAsync(url, runtimePath, maxRetries: 3, timeout: 60, throwOnError: false))
                        {
                            found = true;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!found)
                    {
                        throw new Exception("Mono runtime package not found");
                    }
                }
                else
                {
                    Log.WriteLine($"Found mono runtime package at '{runtimePath}'");
                }

                using (var archive = ZipFile.OpenRead(runtimePath))
                {
                    var systemCoreLib = archive.GetEntry($"runtimes/linux-{pkgNameSuffix}/native/System.Private.CoreLib.dll");
                    systemCoreLib.ExtractToFile(Path.Combine(outputFolder, "System.Private.CoreLib.dll"), true);

                    var libcoreclr = archive.GetEntry($"runtimes/linux-{pkgNameSuffix}/native/libcoreclr.so");
                    libcoreclr.ExtractToFile(Path.Combine(outputFolder, "libcoreclr.so"), true);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine("ERROR: Failed to download mono runtime. " + e.ToString());
                throw;
            }
        }

        private static async Task AOT4Mono(string dotnetSdkVersion, string runtimeVersion, string outputFolder)
        {
            try
            {
                var fileName = "/bin/bash";

                //Download dotnet sdk package
                var dotnetMonoPath = Path.Combine(_rootTempDir, "dotnet-mono", $"dotnet-sdk-{dotnetSdkVersion}-linux-x64.tar.gz");
                var packageName = "Microsoft.NETCore.App.Runtime.Mono.LLVM.AOT.linux-x64".ToLowerInvariant();
                var runtimePath = Path.Combine(_rootTempDir, "RuntimePackages", $"{packageName}.{runtimeVersion}.nupkg");
                var llvmExtractDir = Path.Combine(Path.GetDirectoryName(runtimePath), "mono-llvm");
		
                if (!Directory.Exists(Path.GetDirectoryName(dotnetMonoPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dotnetMonoPath));

                    Log.WriteLine($"Downloading dotnet skd package for mono AOT");

                    var found = false;
                    var url = $"https://dotnetcli.azureedge.net/dotnet/Sdk/{dotnetSdkVersion}/dotnet-sdk-{dotnetSdkVersion}-linux-x64.tar.gz";

                    if (await DownloadFileAsync(url, dotnetMonoPath, maxRetries: 3, timeout: 60, throwOnError: false))
                    {
                        found = true;
                    }

                    if (!found)
                    {
                        throw new Exception("dotnet sdk package not found");
                    }
                    else
                    {
                        var strCmdTar = $"tar -xf dotnet-sdk-{dotnetSdkVersion}-linux-x64.tar.gz";
                        var resultTar = await ProcessUtil.RunAsync(fileName, 
                            ConvertCmd2Arg(strCmdTar),
                            workingDirectory: Path.GetDirectoryName(dotnetMonoPath),
                            log: true);
                    }

                    Log.WriteLine($"Patching local dotnet with mono runtime and extracting llvm");

                    if (Directory.Exists(llvmExtractDir))
                    {
                        Directory.Delete(llvmExtractDir);
                    }

                    Directory.CreateDirectory(llvmExtractDir);

                    using (var archive = ZipFile.OpenRead(runtimePath))
                    {
                        var llcExe = archive.GetEntry("runtimes/linux-x64/native/llc");
                        llcExe.ExtractToFile(Path.Combine(llvmExtractDir, "llc"), true);

                        var optExe = archive.GetEntry("runtimes/linux-x64/native/opt");
                        optExe.ExtractToFile(Path.Combine(llvmExtractDir, "opt"), true);
                    }
                    
                    var strCmdChmod = "chmod +x opt llc";
                    var resultChmod = await ProcessUtil.RunAsync(
                        fileName, ConvertCmd2Arg(strCmdChmod),
                        workingDirectory: llvmExtractDir,
                        log: true);
                }
                else
                {
                    Log.WriteLine($"Found local dotnet with mono runtime at '{Path.GetDirectoryName(dotnetMonoPath)}'");
                }

                // Copy over mono runtime
                var strCmdGetVer = "./dotnet --list-runtimes | grep -i \"Microsoft.NETCore.App\"";
                var resultGetVer = await ProcessUtil.RunAsync(
                    fileName, 
                    ConvertCmd2Arg(strCmdGetVer),
                    workingDirectory: Path.GetDirectoryName(dotnetMonoPath),
                    log: true,
                    captureOutput: true);

                var MicrosoftNETCoreAppPackageVersion = resultGetVer.StandardOutput.Split(' ')[1];
                File.Copy(Path.Combine(outputFolder, "System.Private.CoreLib.dll"), Path.Combine(Path.GetDirectoryName(dotnetMonoPath), "shared", "Microsoft.NETCore.App", MicrosoftNETCoreAppPackageVersion, "System.Private.CoreLib.dll"), true);
                File.Copy(Path.Combine(outputFolder, "libcoreclr.so"), Path.Combine(Path.GetDirectoryName(dotnetMonoPath), "shared", "Microsoft.NETCore.App", MicrosoftNETCoreAppPackageVersion, "libcoreclr.so"), true);

                Log.WriteLine("Pre-compile assemblies inside publish folder");

                var strCmdPreCompile = $@"for assembly in {outputFolder}/*.dll; do
                                              PATH={llvmExtractDir}:${{PATH}} MONO_ENV_OPTIONS=--aot=llvm,mcpu=native ./dotnet $assembly;
                                           done";
                var resultPreCompile = await ProcessUtil.RunAsync(fileName, ConvertCmd2Arg(strCmdPreCompile),
                                                   workingDirectory: Path.GetDirectoryName(dotnetMonoPath),
                                                   log: true,
                                                   captureOutput: true);
            }
            catch (Exception e)
            {
                Log.WriteLine("ERROR: Failed to AOT for mono. " + e.ToString());
                throw;
            }
        }

        private static string ConvertCmd2Arg(string cmd)
        {
            cmd.Replace("\"", "\"\"");
            var result = $"-c \"{cmd}\"";
            return result;
        }

        private static bool MarkAsRunning(string hostname, Job job, Stopwatch stopwatch)
        {
            // Already executed this method?
            if (job.State == JobState.Running)
            {
                return false;
            }

            job.StartupMainMethod = stopwatch.Elapsed;

            job.Measurements.Enqueue(new Measurement
            {
                Name = "benchmarks/start-time",
                Timestamp = DateTime.UtcNow,
                Value = stopwatch.ElapsedMilliseconds
            });

            Log.WriteLine($"Running job '{job.Service}' ({job.Id})");
            job.Url = ComputeServerUrl(hostname, job);

            // Mark the job as running to allow the Client to start the test
            job.State = JobState.Running;

            return true;
        }

        private static string GenerateApplicationHostConfig(Job job, string publishedFolder, string executable, string arguments,
            string hostname)
        {
            void SetAttribute(XDocument doc, string path, string name, string value)
            {
                var element = doc.XPathSelectElement(path);
                if (element == null)
                {
                    throw new InvalidOperationException("Element not found");
                }

                element.SetAttributeValue(name, value);
            }

            using (var resourceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("BenchmarksServer.applicationHost.config"))
            {
                var applicationHostConfig = XDocument.Load(resourceStream);
                SetAttribute(applicationHostConfig, "/configuration/system.webServer/aspNetCore", "processPath", executable);
                SetAttribute(applicationHostConfig, "/configuration/system.webServer/aspNetCore", "arguments", arguments);

                var ancmPath = Path.Combine(publishedFolder, "x64\\aspnetcorev2.dll");
                SetAttribute(applicationHostConfig, "/configuration/system.webServer/globalModules/add[@name='AspNetCoreModuleV2']", "image", ancmPath);

                SetAttribute(applicationHostConfig, "/configuration/system.applicationHost/sites/site/bindings/binding", "bindingInformation", $"*:{job.Port}:");
                SetAttribute(applicationHostConfig, "/configuration/system.applicationHost/sites/site/application/virtualDirectory", "physicalPath", job.BasePath);
                //\runtimes\win-x64\nativeassets\netcoreapp2.1\aspnetcorerh.dll

                if (job.WebHost == WebHost.IISInProcess)
                {
                    SetAttribute(applicationHostConfig, "/configuration/system.webServer/aspNetCore", "hostingModel", "inprocess");
                }

                var fileName = executable + ".apphost.config";
                applicationHostConfig.Save(fileName);

                // The SDK generates a web.config file on publish, which will conflict with apphost.config
                try
                {
                    File.Delete(Path.Combine(publishedFolder, "web.config"));
                }
                catch (Exception)
                {
                }

                // But if a specific web.benchmnarks.config is provided, use it
                try
                {
                    File.Move(Path.Combine(publishedFolder, "web.config"), Path.Combine(publishedFolder, "web.benchmarks.config"));
                }
                catch (Exception)
                {
                }

                return fileName;
            }
        }

        private static async Task<long> GetSwapBytesAsync()
        {
            var result = await ProcessUtil.RunAsync("cat", "/proc/meminfo", throwOnError: false, captureOutput: true);

            // SwapTotal:       8388604 kB
            // SwapFree:        8310012 kB

            long swapTotal = 0, swapFree = 0;
            bool totalFound = false, freeFound = false;

            using (var sr = new StringReader(result.StandardOutput))
            {
                var line = sr.ReadLine();

                while (line != null && !(totalFound && freeFound))
                {
                    if (line.StartsWith("SwapTotal"))
                    {
                        totalFound = true;
                        swapTotal = ParseMeminfo(line);
                    }

                    if (line.StartsWith("SwapFree"))
                    {
                        freeFound = true;
                        swapFree = ParseMeminfo(line);
                    }

                    line = sr.ReadLine();
                }
            }

            if (!totalFound || !freeFound)
            {
                return -1;
            }

            var swapkB = swapTotal - swapFree;

            return swapkB * 1024;

            long ParseMeminfo(string line)
            {
                return long.Parse(line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);
            }
        }

        private static string ComputeServerUrl(string hostname, Job job)
        {
            var scheme = (job.Scheme == Scheme.H2 || job.Scheme == Scheme.Https) ? "https" : "http";
            return $"{scheme}://{hostname}:{job.Port}/{job.Path.TrimStart('/')}";
        }

        private static string GetRepoName(Source source)
        {
            // Attempt to parse a string like
            // - http://<host>.com/<user>/<repo>.git OR
            // - http://<host>.com/<user>/<repo>
            var repository = source.Repository;
            var lastSlash = repository.LastIndexOf('/');
            var dot = repository.LastIndexOf('.');

            if (lastSlash == -1)
            {
                throw new InvalidOperationException($"Couldn't parse repository name from {source.Repository}");
            }

            var start = lastSlash + 1; // +1 to skip over the slash.
            var name = dot > lastSlash ? repository.Substring(start, dot - start) : repository.Substring(start);
            return name;
        }

        private static async Task<string> DownloadContentAsync(string url, int maxRetries = 3, int timeout = 5)
        {
            for (var i = 0; i < maxRetries; ++i)
            {
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    return await _httpClient.GetStringAsync(url);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while downloading {url}:");
                    Console.WriteLine(e);
                }
            }

            throw new ApplicationException($"Error while downloading {url} after {maxRetries} attempts");
        }

        private static async Task<string> GetLatestPackageVersion(string packageIndexUrl, string versionPrefix)
        {
            Log.WriteLine($"Downloading package metadata ...");
            var index = JObject.Parse(await DownloadContentAsync(packageIndexUrl));

            var compatiblePages = index["items"].Where(t => ((string)t["lower"]).StartsWith(versionPrefix)).ToArray();

            // All versions might be comprised in a single page, with lower and upper bounds not matching the prefix
            if (!compatiblePages.Any())
            {
                compatiblePages = index["items"].ToArray();
            }

            foreach (var page in compatiblePages.Reverse())
            {
                var lastPageUrl = (string)page["@id"];

                var lastPage = JObject.Parse(await DownloadContentAsync(lastPageUrl));

                var entries = packageIndexUrl.Contains("myget", StringComparison.OrdinalIgnoreCase)
                                    ? lastPage["items"]
                                    : lastPage["items"][0]["items"]
                                    ;

                // Extract the highest version
                var lastEntry = entries
                    .Where(t => ((string)t["catalogEntry"]["version"]).StartsWith(versionPrefix)).LastOrDefault();

                if (lastEntry != null)
                {
                    return (string)lastEntry["catalogEntry"]["version"];
                }
            }

            return null;
        }

        private static async Task<string> GetFlatContainerVersion(string packageIndexUrl, string versionPrefix)
        {
            Log.WriteLine($"Downloading flatcontainer ...");
            var root = JObject.Parse(await DownloadContentAsync(packageIndexUrl));

            var matchingVersions = root["versions"]
                .Select(x => x.ToString())
                // Unlisting these versions manually as they are breaking the order of 5.0.0-alpha.X
                .Where(x => !x.StartsWith("5.0.0-alpha1"))
                .Where(t => t.StartsWith(versionPrefix))
                .Select(x => new NuGetVersion(x))
                .ToArray()
                ;

            // Extract the highest version
            var latest = matchingVersions
                .OrderByDescending(v => v, VersionComparer.Default)
                .FirstOrDefault();

            return latest?.OriginalVersion;
        }

        // Compares just the repository name
        private class SourceRepoComparer : IEqualityComparer<Source>
        {
            public static readonly SourceRepoComparer Instance = new SourceRepoComparer();

            public bool Equals(Source x, Source y)
            {
                return string.Equals(GetRepoName(x), GetRepoName(y), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Source obj)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(GetRepoName(obj));
            }
        }

        private static string FormatPathSeparators(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return "";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return path.Replace("/", "\\");
            }
            else
            {
                return path.Replace("\\", "/");
            }
        }

        public enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000,
            SEM_NONE = SEM_FAILCRITICALERRORS | SEM_NOALIGNMENTFAULTEXCEPT | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX
        }

        [DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        /// <param name="providers">
        /// A profile name, or a list of comma separated EventPipe providers to be enabled.
        /// c.f. https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md
        /// </param>
        private static async Task<int> Collect(ManualResetEvent shouldExit, int processId, FileInfo output, int buffersize, string providers, TimeSpan duration)
        {
            if (String.IsNullOrWhiteSpace(providers))
            {
                providers = "cpu-sampling";
            }

            var providerArguments = providers.Split(new [] { ',', ' '}, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<EventPipeProvider> providerCollection = new List<EventPipeProvider>();

            foreach (var providerArgument in providerArguments)
            {
                // Is it a profile (cpu-sampling, ...)?
                if (TraceExtensions.DotNETRuntimeProfiles.TryGetValue(providerArgument, out var profile))
                {
                    Log.WriteLine($"Adding dotnet-trace profiles: {providerArgument}");
                    providerCollection = TraceExtensions.Merge(providerCollection, profile);
                }
                else
                {
                    // Is it a CLREvent set (GC+GCHandle)?
                    var clrEvents = TraceExtensions.ToCLREventPipeProviders(providerArgument);
                    if (clrEvents.Any())
                    {
                        Log.WriteLine($"Adding dotnet-trace clrEvents: {providerArgument}");
                        providerCollection = TraceExtensions.Merge(providerCollection, clrEvents);
                    }
                    else
                    {
                        // Is it a known provider (KnownProviderName[:Keywords[:Level][:KeyValueArgs]])?
                        var knownProvider = TraceExtensions.ToProvider(providerArgument);
                        if (knownProvider.Any())
                        {
                            Log.WriteLine($"Adding dotnet-trace provider: {providerArgument}");
                            providerCollection = TraceExtensions.Merge(providerCollection, knownProvider);
                        }
                    }
                }
            }


            if (!providerCollection.Any())
            {
                Log.WriteLine($"Tracing arguments not valid: {providers}");

                return -1;
            }
            else
            {
                Log.WriteLine($"dotnet-trace providers: ");


                foreach (var provider in providerCollection)
                {
                    Log.WriteLine(provider.ToString());
                }
            }

            var process = Process.GetProcessById(processId);
            var configuration = new EventPipeSessionConfiguration(
                circularBufferSizeMB: buffersize,
                format: EventPipeSerializationFormat.NetTrace,
                providers: providerCollection.ToList().AsReadOnly());

            var shouldStopAfterDuration = duration != default(TimeSpan);
            var failed = false;
            var terminated = false;
            System.Timers.Timer durationTimer = null;

            Log.WriteLine($"Tracing process {processId} on file {output.FullName}");

            ulong sessionId = 0;
            using (Stream stream = EventPipeClient.CollectTracing(processId, configuration, out sessionId))
            {
                if (sessionId == 0)
                {
                    return -1;
                }

                if (shouldStopAfterDuration)
                {
                    durationTimer = new System.Timers.Timer(duration.TotalMilliseconds);
                    durationTimer.Elapsed += (s, e) => shouldExit.Set();
                    durationTimer.AutoReset = false;
                }

                var collectingTask = new Task(() =>
                {
                    try
                    {
                        var stopwatch = new Stopwatch();
                        durationTimer?.Start();
                        stopwatch.Start();

                        using (var fs = new FileStream(output.FullName, FileMode.Create, FileAccess.Write))
                        {
                            var buffer = new byte[16 * 1024];

                            while (true)
                            {
                                int nBytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (nBytesRead <= 0)
                                    break;
                                fs.Write(buffer, 0, nBytesRead);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine($"Tracing failed with exception {ex}");

                        failed = true;
                    }
                    finally
                    {
                        terminated = true;
                        shouldExit.Set();

                        Log.WriteLine($"Tracing terminated.");
                    }
                });
                collectingTask.Start();

                do
                {
                    await Task.Delay(100);
                } while (!shouldExit.WaitOne(0));

                Log.WriteLine($"Tracing stopped");

                if (!terminated)
                {
                    durationTimer?.Stop();
                    EventPipeClient.StopTracing(processId, sessionId);
                }

                await collectingTask;
            }

            durationTimer?.Dispose();

            return failed ? -1 : 0;
        }

        public static void CreateTemporaryFolders()
        {
            if (String.IsNullOrEmpty(_rootTempDir))
            {
                // From the /tmp folder (in Docker, should be mounted to /mnt/benchmarks) use a specific 'benchmarksserver' root folder to isolate from other services
                // that use the temp folder, and create a sub-folder (process-id) for each server running.
                // The cron job is responsible for cleaning the folders
                _rootTempDir = Path.Combine(_buildPath, $"benchmarks-server-{Process.GetCurrentProcess().Id}");

                if (Directory.Exists(_rootTempDir))
                {
                    Directory.Delete(_rootTempDir, true);
                }
            }

            Directory.CreateDirectory(_rootTempDir);

            if (String.IsNullOrEmpty(_dotnethome))
            {
                _dotnethome = GetTempDir();
            }
        }

        public static Task EnsureDotnetInstallExistsAsync()
        {
            Log.WriteLine($"Checking requirements...");

            // Add a NuGet.config for the self-contained deployments to be able to find the runtime packages on the CI feeds

            var rootNugetConfig = Path.Combine(_rootTempDir, "NuGet.Config");

            if (!File.Exists(rootNugetConfig))
            {
                File.WriteAllText(rootNugetConfig, @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""benchmarks-dotnet6"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json"" />
    <add key=""benchmarks-dotnet6-transport"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6-transport/nuget/v3/index.json"" />
    <add key=""benchmarks-dotnet5"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json"" />
    <add key=""benchmarks-dotnet5-transport"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5-transport/nuget/v3/index.json"" />
    <add key=""benchmarks-dotnet3.1"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet3.1/nuget/v3/index.json"" />
    <add key=""benchmarks-dotnet3.1-transport"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet3.1-transport/nuget/v3/index.json"" />
    <add key=""benchmarks-aspnetcore"" value=""https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore/index.json"" />
    <add key=""benchmarks-dotnet-core"" value=""https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json"" />
    <add key=""benchmarks-extensions"" value=""https://dotnetfeed.blob.core.windows.net/aspnet-extensions/index.json"" />
    <add key=""benchmarks-aspnetcore-tooling"" value=""https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore-tooling/index.json"" />
    <add key=""benchmarks-entityframeworkcore"" value=""https://dotnetfeed.blob.core.windows.net/aspnet-entityframeworkcore/index.json"" />
    <add key=""benchmarks-nuget"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>
");
            }

            // Download PerfView
            if (OperatingSystem == OperatingSystem.Windows)
            {
                if (String.IsNullOrEmpty(_perfviewPath))
                {
                    _perfviewPath = Path.Combine(Path.GetTempPath(), PerfViewVersion, Path.GetFileName(_perfviewUrl));
                }

                // Ensure the folder already exists
                Directory.CreateDirectory(Path.GetDirectoryName(_perfviewPath));

                if (!File.Exists(_perfviewPath))
                {
                    Log.WriteLine($"Downloading PerfView to '{_perfviewPath}'");
                    DownloadFileAsync(_perfviewUrl, _perfviewPath, maxRetries: 5, timeout: 60).GetAwaiter().GetResult();
                }
            }

            if (String.IsNullOrEmpty(_dotnetInstallPath))
            {
                // Download dotnet-install at startup, once.
                _dotnetInstallPath = Path.Combine(_rootTempDir, Path.GetRandomFileName());
            }

            // Ensure the folder already exists
            Directory.CreateDirectory(_dotnetInstallPath);

            var _dotnetInstallUrl = OperatingSystem == OperatingSystem.Windows
                ? _dotnetInstallPs1Url
                : _dotnetInstallShUrl
                ;

            var dotnetInstallFilename = Path.Combine(_dotnetInstallPath, Path.GetFileName(_dotnetInstallUrl));

            if (!File.Exists(dotnetInstallFilename))
            {
                Log.WriteLine($"Downloading dotnet-install to '{dotnetInstallFilename}'");
                return DownloadFileAsync(_dotnetInstallUrl, dotnetInstallFilename, maxRetries: 5, timeout: 60);
            }

            return Task.CompletedTask;
        }

        private void OnShutdown()
        {
            try
            {
                Log.WriteLine("Cleaning up temporary folder...");

                // build servers will hold locks on dotnet.exe otherwise
                // c.f. https://github.com/dotnet/sdk/issues/9487

                // If dotnet hasn't yet been installed, don't try to shutdown the build servers
                if (File.Exists(GetDotNetExecutable(_dotnethome)))
                {
                    ProcessUtil.RunAsync(
                        GetDotNetExecutable(_dotnethome),
                        "build-server shutdown",
                        workingDirectory: _dotnethome,
                        timeout: TimeSpan.FromSeconds(20),
                        log: true).GetAwaiter().GetResult();
                }

                if (_cleanup && Directory.Exists(_rootTempDir))
                {
                    TryDeleteDir(_rootTempDir, false);
                }
            }
            finally
            {
            }
        }
    }
}
