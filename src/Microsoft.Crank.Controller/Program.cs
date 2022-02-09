// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Models;
using Microsoft.Crank.Controller.Serializers;
using Fluid;
using Fluid.Values;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using System.Text;
using Manatee.Json.Schema;
using Manatee.Json;
using Jint;
using System.Security.Cryptography;
using Microsoft.Azure.Relay;

namespace Microsoft.Crank.Controller
{
    public class Program
    {
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;
        private static readonly FluidParser FluidParser = new FluidParser();

        private static string _tableName = "Benchmarks";
        private static string _sqlConnectionString = "";

        private const string DefaultBenchmarkDotNetArguments = "--inProcess --cli {{benchmarks-cli}} --join --exporters briefjson markdown";

        // Default to arguments which should be sufficient for collecting trace of default Plaintext run
        // c.f. https://github.com/Microsoft/perfview/blob/main/src/PerfView/CommandLineArgs.cs
        private const string _defaultTraceArguments = "BufferSizeMB=1024;CircularMB=4096;TplEvents=None;Providers=Microsoft-Diagnostics-DiagnosticSource:0:0;KernelEvents=default+ThreadTime-NetworkTCPIP";

        private static ScriptConsole _scriptConsole = new ScriptConsole();

        private static CommandOption
            _configOption,
            _scenarioOption,
            _jobOption,
            _profileOption,
            _jsonOption,
            _csvOption,
            _compareOption,
            _variableOption,
            _sqlConnectionStringOption,
            _sqlTableOption,
            _relayConnectionStringOption,
            _sessionOption,
            _descriptionOption,
            _propertyOption,
            _excludeMetadataOption,
            _excludeMeasurementsOption,
            _autoflushOption,
            _repeatOption,
            _spanOption,
            _renderChartOption,
            _chartTypeOption,
            _chartScaleOption,
            _iterationsOption,
            _intervalOption,
            _verboseOption,
            _quietOption,
            _scriptOption,
            _excludeOption,
            _excludeOrderOption,
            _debugOption
            ;

        // The dynamic arguments that will alter the configurations
        private static List<KeyValuePair<string, string>> Arguments = new List<KeyValuePair<string, string>>();

        private static Dictionary<string, string> _deprecatedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "--output", "--json" }, { "-o", "-j" } // todo: remove in subsequent version prefix
        };

        private static Dictionary<string, string> _synonymArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        };

        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);

            TemplateOptions.Default.MemberAccessStrategy.Register<JObject, object>((obj, name) => obj[name]);
            TemplateOptions.Default.ValueConverters.Add(x => x is JObject o ? new ObjectValue(o) : null);
            TemplateOptions.Default.ValueConverters.Add(x => x is JValue v ? v.Value : null);
            TemplateOptions.Default.ValueConverters.Add(x => x is DateTime v ? new ObjectValue(v) : null);
        }

        private static char[] barChartChars = " ▁▂▃▄▅▆▇█".ToCharArray();
        private static char[] hexChartChars = " 123456789ABCDEF".ToCharArray();

        public static int Main(string[] args)
        {
            // Replace deprecated arguments with new ones
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (_deprecatedArguments.TryGetValue(arg, out var mappedArg))
                {
                    Log.WriteWarning($"WARNING: '{arg}' has been deprecated, in the future please use '{mappedArg}'.");
                    args[i] = mappedArg;
                }
                else if (_synonymArguments.TryGetValue(arg, out var synonymArg))
                {
                    // We don't need to display a warning
                    args[i] = synonymArg;
                }
            }

            var app = new CommandLineApplication()
            {
                Name = "crank",
                FullName = "Crank Benchmarks Controller",
                ExtendedHelpText = Documentation.Content,
                Description = "The Crank controller orchestrates benchmark jobs on Crank agents.",
                ResponseFileHandling = ResponseFileHandling.ParseArgsAsSpaceSeparated,
                OptionsComparison = StringComparison.OrdinalIgnoreCase,
            };

            app.HelpOption("-?|-h|--help");

            _configOption = app.Option("-c|--config", "Configuration file or url", CommandOptionType.MultipleValue);
            _scenarioOption = app.Option("-s|--scenario", "Scenario to execute", CommandOptionType.SingleValue);
            _jobOption = app.Option("-j|--job", "Name of job to define", CommandOptionType.MultipleValue);
            _profileOption = app.Option("--profile", "Profile name", CommandOptionType.MultipleValue);
            _scriptOption = app.Option("--script", "Execute a named script available in the configuration files. Can be used multiple times.", CommandOptionType.MultipleValue);
            _jsonOption = app.Option("-j|--json", "Saves the results as json in the specified file.", CommandOptionType.SingleValue);
            _csvOption = app.Option("--csv", "Saves the results as csv in the specified file.", CommandOptionType.SingleValue);
            _compareOption = app.Option("--compare", "An optional filename to compare the results to. Can be used multiple times.", CommandOptionType.MultipleValue);
            _variableOption = app.Option("--variable", "Variable", CommandOptionType.MultipleValue);
            _sqlConnectionStringOption = app.Option("--sql",
                "Connection string or environment variable name of the SQL Server Database to store results in.", CommandOptionType.SingleValue);
            _sqlTableOption = app.Option("--table",
                "Table name or environment variable name of the SQL table to store results in.", CommandOptionType.SingleValue);
            _relayConnectionStringOption = app.Option("--relay", "Connection string or environment variable name of the Azure Relay namespace used to access the Crank Agent endpoints. e.g., 'Endpoint=sb://mynamespace.servicebus.windows.net;...', 'MY_AZURE_RELAY_ENV'", CommandOptionType.SingleValue);
            _sessionOption = app.Option("--session", "A logical identifier to group related jobs.", CommandOptionType.SingleValue);
            _descriptionOption = app.Option("--description", "A string describing the job.", CommandOptionType.SingleValue);
            _propertyOption = app.Option("-p|--property", "Some custom key/value that will be added to the results, .e.g. --property arch=arm --property os=linux", CommandOptionType.MultipleValue);
            _excludeMeasurementsOption = app.Option("--no-measurements", "Remove all measurements from the stored results. For instance, all samples of a measure won't be stored, only the final value.", CommandOptionType.SingleOrNoValue);
            _excludeMetadataOption = app.Option("--no-metadata", "Remove all metadata from the stored results. The metadata is only necessary for being to generate friendly outputs.", CommandOptionType.SingleOrNoValue);
            _autoflushOption = app.Option("--auto-flush", "Runs a single long-running job and flushes measurements automatically.", CommandOptionType.NoValue);
            _repeatOption = app.Option("--repeat", "The job to repeat using the '--span' or '--iterations' argument.", CommandOptionType.SingleValue);
            _spanOption = app.Option("--span", "The duration while the job is repeated.", CommandOptionType.SingleValue);
            _renderChartOption = app.Option("--chart", "Renders a chart for multi-value results.", CommandOptionType.NoValue);
            _chartTypeOption = app.Option("--chart-type", "Type of chart to render. Values are 'bar' (default) or 'hex'", CommandOptionType.SingleValue);
            _chartScaleOption = app.Option("--chart-scale", "Scale for chart. Values are 'off' (default) or 'auto'. When scale is off, the min value starts at 0.", CommandOptionType.SingleValue);
            _iterationsOption = app.Option("-i|--iterations", "The number of iterations.", CommandOptionType.SingleValue);
            _intervalOption = app.Option("-m|--interval", "The measurements interval in seconds. Default is 1.", CommandOptionType.SingleValue);
            _verboseOption = app.Option("-v|--verbose", "Verbose output", CommandOptionType.NoValue);
            _quietOption = app.Option("--quiet", "Quiet output, only the results are displayed", CommandOptionType.NoValue);
            _excludeOption = app.Option("-x|--exclude", "Excludes the specified number of high and low results, e.g., 1, 1:0 (exclude the lowest), 0:3 (exclude the 3 highest)", CommandOptionType.SingleValue);
            _excludeOrderOption = app.Option("-xo|--exclude-order", "The result to use to detect the high and low results, e.g., 'load:http/rps/mean'", CommandOptionType.SingleValue);
            _debugOption = app.Option("-d|--debug", "Saves the final configuration to a file and skips the execution of the benchmark, e.g., '-d debug.json'", CommandOptionType.SingleValue);

            app.Command("compare", compareCmd =>
            {
                compareCmd.Description = "Compares result files";
                var files = compareCmd.Argument("Files", "Files to compare", multipleValues: true).IsRequired();

                compareCmd.OnExecute(() =>
                {
                    return ResultComparer.Compare(files.Values);
                });
            });

            // Store arguments before the dynamic ones are removed
            var commandLineArguments = String.Join(' ', args.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.StartsWith('-') ? x : '"' + x + '"'));

            // Extract dynamic arguments
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--") && !app.Options.Any(option => arg.StartsWith("--" + option.LongName)))
                {
                    // Remove this argument from the command line
                    args[i] = "";

                    // Dynamic arguments always come in pairs 
                    if (i + 1 < args.Length)
                    {
                        Arguments.Add(KeyValuePair.Create(arg.Substring(2), args[i + 1]));
                        args[i + 1] = "";

                        i++;
                    }
                }
            }

            app.OnExecuteAsync(async (t) =>
            {
                Log.IsQuiet = _quietOption.HasValue();
                Log.IsVerbose = _verboseOption.HasValue();

                var session = _sessionOption.Value();
                var iterations = 1;
                var exclude = ExcludeOptions.Empty;

                var span = TimeSpan.Zero;

                if (string.IsNullOrEmpty(session))
                {
                    session = Guid.NewGuid().ToString("n");
                }

                var description = _descriptionOption.Value() ?? "";

                var interval = 1;

                if (_intervalOption.HasValue())
                {
                    if (!int.TryParse(_intervalOption.Value(), out interval))
                    {
                        Console.WriteLine($"The option --interval must be a valid integer.");
                        return -1;
                    }
                    else
                    {
                        if (interval < 1)
                        {
                            Console.WriteLine($"The option --interval must be greater than 1.");
                            return -1;
                        }
                    }
                }

                if (_iterationsOption.HasValue())
                {
                    if (_spanOption.HasValue())
                    {
                        Console.WriteLine($"The options --iterations and --span can't be used together.");
                        return -1;
                    }

                    if (!Int32.TryParse(_iterationsOption.Value(), out iterations) || iterations < 1)
                    {
                        Console.WriteLine($"Invalid value for iterations arguments. A positive integer was expected.");
                        return -1;
                    }
                }

                var excludeOptions = 
                    Convert.ToInt32(_excludeOption.HasValue()) 
                    + Convert.ToInt32(_excludeOrderOption.HasValue())
                    ;

                if (_excludeOrderOption.HasValue() && !_excludeOption.HasValue())
                {
                    Console.WriteLine("--exclude [hi:low] needs to be set when using --exclude-order.");
                    return -1;
                }

                if (_excludeOption.HasValue() && !_excludeOrderOption.HasValue())
                {
                    Console.WriteLine("--exclude can't be used without --exclude-order. e.g., --exclude-order load:http/rps/mean");
                    return -1;
                }

                int excludeLow = 0, excludeHigh = 0;

                if (_excludeOption.HasValue())
                {
                    if (!_iterationsOption.HasValue())
                    {
                        Console.WriteLine("The option --exclude can only be used with --iterations.");
                        return -1;
                    }

                    var segments = _excludeOption.Value().Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (segments.Length == 1)
                    {
                        if (!Int32.TryParse(segments[0], out excludeLow) || excludeLow < 0)
                        {
                            Console.WriteLine($"Invalid value for --exclude <x> option. An integer value greater or equal to 0 was expected.");
                            return -1;
                        }

                        excludeHigh = excludeLow;
                    }
                    else if (segments.Length == 2)
                    {
                        if (!Int32.TryParse(segments[0], out excludeLow) || excludeLow < 0)
                        {
                            Console.WriteLine($"Invalid value for --exclude <low:high> option. An integer value greater or equal to 0 was expected.");
                            return -1;
                        }

                        if (!Int32.TryParse(segments[1], out excludeHigh) || excludeHigh < 0)
                        {
                            Console.WriteLine($"Invalid value for --exclude <low:high> option. An integer value greater or equal to 0 was expected.");
                            return -1;
                        }
                    }

                    if (iterations <= excludeLow + excludeHigh)
                    {
                        Console.WriteLine($"Invalid value for --exclude option. Can't exclude more results than the iterations.");
                        return -1;
                    }

                    var excludeOrder = _excludeOrderOption.Value().Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (excludeOrder.Length != 2)
                    {
                        Console.WriteLine("The option -xo|--exclude-order format is <job>:<result>, e.g., 'load:http/rps/mean'");
                        return -1;
                    }

                    exclude.Low = excludeLow;
                    exclude.High = excludeHigh;
                    exclude.Job = excludeOrder[0];
                    exclude.Result = excludeOrder[1];
                }

                if (_spanOption.HasValue() && !TimeSpan.TryParse(_spanOption.Value(), out span))
                {
                    Console.WriteLine($"Invalid value for --span. Format is 'HH:mm:ss'");
                    return -1;
                }

                if (_sqlTableOption.HasValue())
                {
                    _tableName = _sqlTableOption.Value();

                    if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(_tableName)))
                    {
                        _tableName = Environment.GetEnvironmentVariable(_tableName);
                    }
                }

                if (_sqlConnectionStringOption.HasValue())
                {
                    _sqlConnectionString = _sqlConnectionStringOption.Value();

                    if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(_sqlConnectionString)))
                    {
                        _sqlConnectionString = Environment.GetEnvironmentVariable(_sqlConnectionString);
                    }
                }

                if (!_configOption.HasValue())
                {
                    if (!_jobOption.HasValue())
                    {
                        app.ShowHelp();
                        return 1;
                    }
                }
                else
                {
                    if (!_scenarioOption.HasValue())
                    {
                        Console.Error.WriteLine("No jobs were found. Are you missing the --scenario argument?");
                        return 1;
                    }
                }

                if (_scenarioOption.HasValue() && _jobOption.HasValue())
                {
                    Console.Error.WriteLine("The arguments --scenario and --job can't be used together. They both define which jobs to run.");
                    return 1;
                }

                var results = new ExecutionResult();

                var scenarioName = _scenarioOption.Value();
                var jobNames = _jobOption.Values;

                var variables = new JObject();

                foreach (var variable in _variableOption.Values)
                {
                    var segments = variable.Split('=', 2);

                    if (segments.Length != 2)
                    {
                        Console.WriteLine($"Invalid variable argument: '{variable}', format is \"[NAME]=[VALUE]\"");

                        app.ShowHelp();
                        return -1;
                    }

                    // Try to parse as integer, or the value would be a string
                    if (long.TryParse(segments[1], out var intVariable))
                    {
                        variables[segments[0]] = intVariable;
                    }
                    else
                    {
                        variables[segments[0]] = segments[1];
                    }
                }

                foreach (var property in _propertyOption.Values)
                {
                    var segments = property.Split('=', 2);

                    if (segments.Length != 2)
                    {
                        Console.WriteLine($"Invalid property argument: '{property}', format is \"[NAME]=[VALUE]\"");

                        app.ShowHelp();
                        return -1;
                    }
                }

                var configuration = await BuildConfigurationAsync(_configOption.Values, scenarioName, _jobOption.Values, Arguments, variables, _profileOption.Values, _scriptOption.Values, interval);

                // Storing the list of services to run as part of the selected scenario
                var dependencies = String.IsNullOrEmpty(scenarioName)
                    ? _jobOption.Values.ToArray()
                    : configuration.Scenarios[scenarioName].Select(x => x.Key).ToArray()
                    ;

                var serializer = new Serializer();

                string groupId = Guid.NewGuid().ToString("n");

                // Verifying jobs
                foreach (var jobName in dependencies)
                {
                    var service = configuration.Jobs[jobName];

                    service.RunId = groupId;
                    service.Origin = Environment.MachineName;
                    service.CrankArguments = commandLineArguments;

                    if (String.IsNullOrEmpty(service.Source.Project) &&
                        String.IsNullOrEmpty(service.Source.DockerFile) &&
                        String.IsNullOrEmpty(service.Source.DockerLoad) &&
                        String.IsNullOrEmpty(service.Executable))
                    {
                        Console.WriteLine($"The service '{jobName}' is missing some properties to start the job.");
                        Console.WriteLine($"Check that any of these properties is set: project, executable, dockerFile, dockerLoad");
                        return -1;
                    }

                    if (!service.Endpoints.Any())
                    {
                        Console.WriteLine($"The service '{jobName}' is missing an endpoint to deploy on.");

                        // Only display as a warning if in debug mode
                        if (!_debugOption.HasValue())
                        {
                            return -1;
                        }
                    }

                    foreach (var endpoint in service.Endpoints)
                    {
                        try
                        {
                            using (var cts = new CancellationTokenSource(10000))
                            {
                                var response = await _httpClient.GetAsync(endpoint, cts.Token);
                                
                                if (!_relayConnectionStringOption.HasValue())
                                {
                                    response.EnsureSuccessStatusCode();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"The specified endpoint url '{endpoint}' for '{jobName}' is invalid or not responsive: \"{e.Message}\"");

                            // Only display as a warning if in debug mode
                            if (!_debugOption.HasValue())
                            {
                                return -1;
                            }
                        }
                    }
                }

                if (_debugOption.HasValue())
                {
                    File.WriteAllText(_debugOption.Value(), JsonConvert.SerializeObject(configuration, Formatting.Indented));
                    Console.WriteLine($"Configuration saved in file {Path.GetFullPath(_debugOption.Value())}");
                    return 0;
                }

                // Initialize database
                if (!String.IsNullOrWhiteSpace(_sqlConnectionString))
                {
                    await JobSerializer.InitializeDatabaseAsync(_sqlConnectionString, _tableName);
                }

                #pragma warning disable CS4014
                // Don't block on version checks
                VersionChecker.CheckUpdateAsync(_httpClient);
                #pragma warning restore CS4014
                
                Log.Write($"Running session '{session}' with description '{_descriptionOption.Value()}'");

                var isBenchmarkDotNet = dependencies.Any(x => configuration.Jobs[x].Options.BenchmarkDotNet);

                if (_autoflushOption.HasValue())
                {
                    // No job is restarted, but a snapshot of results is done
                    // after "span" has passed.
                    
                    results = await RunAutoFlush(
                        configuration,
                        dependencies,
                        session,
                        span
                        );
                }
                else if (isBenchmarkDotNet)
                {                    
                    results = await RunBenchmarkDotNet(
                        configuration,
                        dependencies,
                        session
                        );
                }
                else
                {
                    results = await Run(
                        configuration,
                        dependencies,
                        session,
                        iterations,
                        exclude,
                        _scriptOption.Values
                        );
                }

                // Display diff

                if (_compareOption.HasValue())
                {
                    var jobName = "Current";

                    if (_scenarioOption.HasValue())
                    {
                        if (_jsonOption.HasValue())
                        {
                            jobName = Path.GetFileNameWithoutExtension(_jsonOption.Value());
                        }
                        else if (_csvOption.HasValue())
                        {
                            jobName = Path.GetFileNameWithoutExtension(_csvOption.Value());
                        }
                    }

                    ResultComparer.Compare(_compareOption.Values, results.JobResults, results.Benchmarks, jobName);
                }

                return results.ReturnCode;
            });

            try
            {
                return app.Execute(args.Where(x => !String.IsNullOrEmpty(x)).ToArray());
            }
            catch (ControllerException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
            catch (CommandParsingException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        private static async Task<ExecutionResult> Run(
            Configuration configuration,
            string[] dependencies,
            string session,
            int iterations,
            ExcludeOptions exclude,
            IEnumerable<string> scripts
            )
        {
            
            var executionResults = new List<ExecutionResult>();
            var iterationStart = DateTime.UtcNow;
            var jobsByDependency = new Dictionary<string, List<JobConnection>>();
            var i = 1; // current iteration

            do
            {
                var executionResult = new ExecutionResult();

                if (iterations > 1)
                {
                    Log.Write($"Iteration {i} of {iterations}");
                }

                // Deadlock detection loop
                while (true)
                {

                    var deadlockDetected = false;

                    try
                    {
                        foreach (var jobName in dependencies)
                        {
                            var service = configuration.Jobs[jobName];
                            service.DriverVersion = 2;

                            List<JobConnection> jobs;

                            // Create a new list of JobConnection instances if the service is
                            // not already running from a previous loop

                            if (jobsByDependency.ContainsKey(jobName) && SpanShouldKeepJobRunning(jobName))
                            {
                                jobs = jobsByDependency[jobName];

                                // Clear measurements, only if the service is not a console app as it
                                // would already be stopped

                                if (!service.WaitForExit)
                                {
                                    await Task.WhenAll(jobs.Select(job => job.ClearMeasurements()));
                                }
                            }
                            else
                            {
                                jobs = service.Endpoints.Select(endpoint => new JobConnection(service, new Uri(endpoint))).ToList();

                                foreach (var jobConnection in jobs)
                                {
                                    var relayToken = await GetRelayTokenAsync(jobConnection.ServerUri);

                                    if (!String.IsNullOrEmpty(relayToken))
                                    {
                                        jobConnection.ConfigureRelay(relayToken);
                                    }
                                }

                                jobsByDependency[jobName] = jobs;

                                // Check os and architecture requirements
                                if (!await EnsureServerRequirementsAsync(jobs, service))
                                {
                                    Log.Write($"Scenario skipped as the agent doesn't match the operating and architecture constraints for '{jobName}' ({String.Join("/", new[] { service.Options.RequiredArchitecture, service.Options.RequiredOperatingSystem })})");
                                    return new ExecutionResult { ReturnCode = 0 };
                                }

                                // Check that we are not creating a deadlock by starting this job

                                // Start this service on all configured agent endpoints
                                await Task.WhenAll(
                                    jobs.Select(async job =>
                                    {
                                        // Before starting a job, we need to check if it could block another run
                                        var queue = await job.GetQueueAsync();

                                        var runningJob = queue.FirstOrDefault(x => x.State == "Running" && x.RunId != job.Job.RunId);

                                        // If there is a running job, we check if we are not blocking another one from the same run
                                        if (runningJob != null)
                                        {
                                            foreach (var jobName in dependencies)
                                            {
                                                if (jobsByDependency.ContainsKey(jobName))
                                                {
                                                    foreach (var j in jobsByDependency[jobName])
                                                    {
                                                        var otherRunningJobs = await j.GetQueueAsync();

                                                        // Find a job waiting for us on the other endpoint
                                                        var jobA = otherRunningJobs.FirstOrDefault(x => x.State == "New" && x.RunId == runningJob.RunId);
                                                        
                                                        // If the job we are running is waitForExit, we don't need to interrupt it
                                                        // as it will stop by itself

                                                        var jobB = otherRunningJobs.FirstOrDefault(x => x.State == "Running" && x.RunId == j.Job.RunId && !j.Job.WaitForExit);

                                                        if (jobA != null && jobB != null)
                                                        {
                                                            Log.Write($"Found deadlock on {j.ServerJobUri}, interrupting ...");

                                                            foreach (var name in dependencies.Reverse())
                                                            {
                                                                var service = configuration.Jobs[name];

                                                                // Skip failed jobs
                                                                if (!jobsByDependency.ContainsKey(name))
                                                                {
                                                                    continue;
                                                                }

                                                                var jobs = jobsByDependency[name];

                                                                if (!service.WaitForExit)
                                                                {
                                                                    await Task.WhenAll(jobs.Select(job => job.StopAsync()));

                                                                    await Task.WhenAll(jobs.Select(job => job.DeleteAsync()));
                                                                }
                                                            }

                                                            // Wait until another runId has started, or the current runId could still be picked up

                                                            var queueStarted = DateTime.UtcNow;

                                                            while (true) 
                                                            {
                                                                otherRunningJobs = await j.GetQueueAsync();

                                                                var otherRunningJob = otherRunningJobs.FirstOrDefault(x => x.State == "Running" || x.State == "Initializing" || x.State == "Starting" && x.RunId != job.Job.RunId);

                                                                if (otherRunningJob != null || (DateTime.UtcNow - queueStarted > TimeSpan.FromSeconds(3)))
                                                                {
                                                                    break;
                                                                }
                                                            } 

                                                            throw new JobDeadlockException();
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        // Start job on agent
                                        return await job.StartAsync(jobName);
                                    })
                                );

                                if (service.WaitForExit)
                                {
                                    // Wait for all clients to stop
                                    while (true)
                                    {
                                        var stop = true;

                                        foreach (var job in jobs)
                                        {
                                            var state = await job.GetStateAsync();

                                            stop = stop && (
                                                state == JobState.Stopped ||
                                                state == JobState.Failed ||
                                                state == JobState.Deleted
                                                );
                                        }

                                        if (stop)
                                        {
                                            break;
                                        }

                                        await Task.Delay(1000);
                                    }

                                    // Stop a blocking job
                                    await Task.WhenAll(jobs.Select(job => job.StopAsync()));

                                    await Task.WhenAll(jobs.Select(job => job.TryUpdateJobAsync()));

                                    // Display error message if job failed
                                    foreach (var job in jobs)
                                    {
                                        if (!String.IsNullOrEmpty(job.Job.Error))
                                        {
                                            Log.WriteError(job.Job.Error, notime: true);

                                            // It might be necessary to get the formal exit code from the remove job
                                            executionResult.ReturnCode = 1;
                                        }
                                    }

                                    await Task.WhenAll(jobs.Select(job => job.DownloadDumpAsync()));

                                    await Task.WhenAll(jobs.Select(job => job.DownloadTraceAsync()));

                                    await Task.WhenAll(jobs.Select(job => job.DownloadBuildLogAsync()));

                                    await Task.WhenAll(jobs.Select(job => job.DownloadOutputAsync()));

                                    await Task.WhenAll(jobs.Select(job => job.DownloadAssetsAsync(jobName)));

                                    await Task.WhenAll(jobs.Select(job => job.DeleteAsync()));
                                }
                            }

                            var aJobFailed = false;

                            // Skipped other services if a job has failed
                            foreach (var job in jobs)
                            {
                                var state = await job.GetStateAsync();

                                if (state == JobState.Failed)
                                {
                                    aJobFailed = true;
                                    break;
                                }
                            }

                            if (aJobFailed)
                            {
                                Log.Write($"Job has failed, interrupting benchmarks ...");
                                executionResult.ReturnCode = 1;
                                break;
                            }
                        }
                    }
                    catch (JobDeadlockException)
                    {
                        deadlockDetected = true;
                        Log.Write("Deadlock detected, restarting scenario ...");
                    }

                    if (!deadlockDetected)
                    {
                        break;
                    }
                }

                // Stop all non-blocking jobs in reverse dependency order (clients first)
                foreach (var jobName in dependencies.Reverse())
                {
                    var service = configuration.Jobs[jobName];

                    // Skip failed jobs
                    if (!jobsByDependency.ContainsKey(jobName))
                    {
                        continue;
                    }


                    var jobs = jobsByDependency[jobName];

                    if (!service.WaitForExit)
                    {
                        // Unless the jobs can't be stopped
                        if (!SpanShouldKeepJobRunning(jobName) || IsLastIteration())
                        {
                            await Task.WhenAll(jobs.Select(job => job.StopAsync()));
                        }

                        await Task.WhenAll(jobs.Select(job => job.TryUpdateJobAsync()));

                        // Unless the jobs can't be stopped
                        if (!SpanShouldKeepJobRunning(jobName) || IsLastIteration())
                        {
                            await Task.WhenAll(jobs.Select(job => job.DownloadDumpAsync()));

                            await Task.WhenAll(jobs.Select(job => job.DownloadBuildLogAsync()));

                            await Task.WhenAll(jobs.Select(job => job.DownloadOutputAsync()));

                            await Task.WhenAll(jobs.Select(job => job.DownloadTraceAsync()));

                            await Task.WhenAll(jobs.Select(job => job.DownloadAssetsAsync(jobName)));

                            await Task.WhenAll(jobs.Select(job => job.DeleteAsync()));
                        }
                    }
                }

                // Normalize results
                foreach (var jobName in dependencies)
                {
                    var service = configuration.Jobs[jobName];

                    // Skip failed jobs
                    if (!jobsByDependency.TryGetValue(jobName, out var jobConnections))
                    {
                        continue;
                    }

                    // Convert any json result to an object
                    NormalizeResults(jobConnections);
                }

                var jobResults = await CreateJobResultsAsync(configuration, dependencies, jobsByDependency);

                // Display results
                foreach (var jobName in dependencies)
                {
                    var service = configuration.Jobs[jobName];

                    if (service.Options.DiscardResults)
                    {
                        continue;
                    }

                    // The subsequent jobs of a failed job might not have run
                    if (jobResults.Jobs.TryGetValue(jobName, out var job))
                    {
                        Console.WriteLine();
                        WriteResults(jobName, job);
                    }
                }

                foreach (var property in _propertyOption.Values)
                {
                    var segments = property.Split('=', 2);

                    jobResults.Properties[segments[0]] = segments[1];
                }

                executionResult.JobResults = jobResults;
                executionResults.Add(executionResult);
                
                // If last iteration, create average and display results
                if (iterations > 1 && i == iterations)
                {
                    // Exclude highs and lows

                    if (exclude.High + exclude.Low > 0)
                    {
                        if (executionResults.Any(x => !x.JobResults.Jobs.ContainsKey(exclude.Job)))
                        {
                            Log.WriteWarning($"A benchmark didn't contain the expected job '{exclude.Job}', the exclusion will be ignored.");
                        }
                        else
                        {
                            if (executionResults.Any(x => !x.JobResults.Jobs[exclude.Job].Results.ContainsKey(exclude.Result)))
                            {
                                Log.WriteWarning($"A benchmark didn't contain the expected result ('{exclude.Result}'), the iteration will be ignored.");
                            }

                            // Keep the iterations with the results it can be ordered on
                            var validResults = executionResults.Where(x => x.JobResults.Jobs[exclude.Job].Results.ContainsKey(exclude.Result)).ToArray();

                            var orderedResults = validResults.OrderBy(x => x.JobResults.Jobs[exclude.Job].Results[exclude.Result]).ToArray();
                            var includedResults = orderedResults.Skip(exclude.Low).SkipLast(exclude.High).ToList();
                            var excludedresults = validResults.Except(includedResults).ToArray();

                            Console.WriteLine();
                            Console.WriteLine($"Values of {exclude.Job}->{exclude.Result}:");
                            Console.WriteLine();

                            for (var iter = 0; iter < executionResults.Count; iter++)
                            {
                                var result = executionResults[iter];
                                if (!result.JobResults.Jobs[exclude.Job].Results.ContainsKey(exclude.Result))
                                {
                                    Console.WriteLine($"{iter + 1}/{iterations}: No result - Skipped");
                                }
                                else if (includedResults.Contains(result))
                                {
                                    Console.WriteLine($"{iter + 1}/{iterations}: {result.JobResults.Jobs[exclude.Job].Results[exclude.Result]} - Included");
                                }
                                else
                                {
                                    Console.WriteLine($"{iter + 1}/{iterations}: {result.JobResults.Jobs[exclude.Job].Results[exclude.Result]} - Excluded");
                                }
                            }

                            executionResults = includedResults;
                        }
                    }

                    // Compute averages

                    executionResult = ComputeAverages(executionResults);
                    executionResults.Clear();
                    executionResults.Add(executionResult);

                    // Display results
                    
                    Console.WriteLine();
                    Console.WriteLine("Average results:");
                    Console.WriteLine();
                    
                    WriteExecutionResults(executionResult);
                }

                // Save results

                if (i == iterations)
                {
                    CleanMeasurements(jobResults);
                }

                if (_jsonOption.HasValue())
                {
                    // Skip saving the file if running with iterations and not the last run
                    if (i == iterations)
                    {
                        var filename = _jsonOption.Value();
                    
                        var directory = Path.GetDirectoryName(filename);
                        if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        await File.WriteAllTextAsync(filename, JsonConvert.SerializeObject(executionResults.First(), Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));

                        Log.Write("", notime: true);
                        Log.Write($"Results saved in '{new FileInfo(filename).FullName}'", notime: true);
                    }
                } 
                
                if (_csvOption.HasValue())
                {
                    // Skip saving the file if running with iterations and not the last run
                    if (i == iterations)
                    {
                        var filename = _csvOption.Value();

                        var directory = Path.GetDirectoryName(filename);
                        if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        var result = executionResults.First();

                        // Create the headers
                        if (!File.Exists(filename))
                        {
                            using (var w = File.CreateText(filename))
                            {
                                await w.WriteLineAsync(String.Join(",", GetHeaders().Select(EscapeCsvValue)));
                            }
                        }

                        await File.AppendAllTextAsync(filename, String.Join(",", GetValues().Select(EscapeCsvValue)) + Environment.NewLine);

                        Log.Write("", notime: true);
                        Log.Write($"Results saved in '{new FileInfo(filename).FullName}'", notime: true);


                        IEnumerable<string> GetHeaders()
                        {
                            yield return "Session";
                            yield return "DateTimeUtc";
                            yield return "Description";

                            foreach (var job in result.JobResults.Jobs)
                            {
                                foreach (var m in job.Value.Metadata)
                                {
                                    // application.aspnetCoreVersion
                                    yield return job.Key + "." + m.Name;
                                }

                                foreach (var e in job.Value.Environment)
                                {
                                    yield return job.Key + "." + e.Key;
                                }
                            }

                            foreach (var p in result.JobResults.Properties)
                            {
                                yield return p.Key;
                            }
                        }

                        IEnumerable<string> GetValues()
                        {
                            yield return session;
                            yield return DateTime.UtcNow.ToString("s");
                            yield return _descriptionOption.Value();

                            foreach (var job in result.JobResults.Jobs)
                            {
                                foreach (var m in job.Value.Metadata)
                                {
                                    yield return job.Value.Results.ContainsKey(m.Name)
                                    ? Convert.ToString(job.Value.Results[m.Name], System.Globalization.CultureInfo.InvariantCulture)
                                    : ""
                                    ;
                                }

                                foreach (var e in job.Value.Environment)
                                {
                                    yield return Convert.ToString(e.Value, System.Globalization.CultureInfo.InvariantCulture);
                                }
                            }

                            foreach (var p in result.JobResults.Properties)
                            {
                                yield return Convert.ToString(p.Value, System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }

                        string EscapeCsvValue(string value)
                        {
                            if (String.IsNullOrEmpty(value))
                            {
                                return "";
                            }
                            
                            if (value.Contains("\""))
                            {
                                return "\"" + value.Replace("\"", "\"\"") + "\"";
                            }
                            else
                            {
                                return "\"" + value + "\"";
                            }
                        }
                    }
                }

                // Store data

                if (!String.IsNullOrEmpty(_sqlConnectionString))
                {
                    // Skip storing results if running with iterations and not the last run
                    if (i == iterations)
                    {
                        await JobSerializer.WriteJobResultsToSqlAsync(executionResult.JobResults, _sqlConnectionString, _tableName, session, _scenarioOption.Value(), _descriptionOption.Value());
                    }
                }

                i = i + 1;
            }
            while (!IsRepeatOver());

            return executionResults.First();

            bool IsRepeatOver()
            {
                if (iterations > 1)
                {
                    return i > iterations; 
                }

                return true;
            }

            bool IsLastIteration()
            {
                return i >= iterations;
            }

            bool SpanShouldKeepJobRunning(string jobName)
            {
                if (IsRepeatOver() || IsRepeatOver())
                {
                    return false;
                }

                // If no job is marked for repeat, use the last one

                var repeatAfterJob = _repeatOption.HasValue() 
                    ? _repeatOption.Value()
                    : dependencies.Last()
                    ;

                var jobKeptRunning = dependencies.TakeWhile(x => !String.Equals(repeatAfterJob, x, StringComparison.OrdinalIgnoreCase));

                return jobKeptRunning.Any(x => String.Equals(jobName, x, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static async Task<ExecutionResult> RunBenchmarkDotNet(
            Configuration configuration,
            string[] dependencies,
            string session
            )
        {
            var jobsByDependency = new Dictionary<string, List<JobConnection>>();

            // Repeat until the span duration is over

            var jobName = dependencies.Single();
            var service = configuration.Jobs[jobName];
            service.DriverVersion = 2;
            
            var job = new JobConnection(service, new Uri(service.Endpoints.Single()));

            var relayToken = await GetRelayTokenAsync(job.ServerUri);

            if (!String.IsNullOrEmpty(relayToken))
            {
                job.ConfigureRelay(relayToken);
            }
            
            // Check os and architecture requirements
            if (!await EnsureServerRequirementsAsync(new[] { job }, service))
            {
                Log.Write($"Scenario skipped as the agent doesn't match the operating and architecture constraints for '{jobName}' ({String.Join("/", new[] { service.Options.RequiredArchitecture, service.Options.RequiredOperatingSystem })})");
                return new ExecutionResult { ReturnCode = -1 };
            }

            // Required structure for helper methods
            jobsByDependency[jobName] = new List<JobConnection>() { job };

            // Start job on agent
            await job.StartAsync(jobName);
                                
            // Wait for the client to stop
            while (true)
            {
                var state = await job.GetStateAsync();

                var stop = 
                    state == JobState.Stopped ||
                    state == JobState.Failed ||
                    state == JobState.Deleted
                    ;

                if (stop)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            await job.StopAsync();

            await job.TryUpdateJobAsync();

            await job.DownloadBenchmarkDotNetResultsAsync();
            
            if (!String.IsNullOrEmpty(job.Job.Error))
            {
                Log.WriteError(job.Job.Error, notime: true);
            }

            await job.DownloadBuildLogAsync();

            await job.DownloadOutputAsync();

            await job.DownloadDumpAsync();

            await job.DownloadTraceAsync();

            await job.DownloadAssetsAsync(jobName);

            await job.DeleteAsync();

            if (!service.Options.DiscardResults)
            {
                Console.WriteLine();
                job.DisplayBenchmarkDotNetResults();
            }

            var benchmarks = job.GetBenchmarkDotNetBenchmarks();

            // Save results as a single file

            if (_jsonOption.HasValue())
            {
                var filename = _jsonOption.Value();

                var directory = Path.GetDirectoryName(filename);
                if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var j = new JObject();

                j.Add("Benchmarks", new JArray(benchmarks));

                await File.WriteAllTextAsync(filename, j.ToString(Formatting.Indented));

                Log.Write("", notime: true);
                Log.Write($"Results saved in '{new FileInfo(filename).FullName}'", notime: true);
            }
            
            // Store a result for each benchmark

            foreach (var benchmark in benchmarks)
            {
                var jobResults = await CreateJobResultsAsync(configuration, dependencies, jobsByDependency);

                // Assign custom properties

                foreach (var property in _propertyOption.Values)
                {
                    var segments = property.Split('=', 2);

                    jobResults.Properties[segments[0]] = segments[1];
                }

                jobResults.Jobs[jobName].Results["benchmarks"] = benchmark;

                // "Micro.Md5VsSha256.Sha256(N: 100)"
                var fullName = benchmark.Property("FullName").Value.ToString();

                // Assign parameters as properties

                // N=100;M=5
                var parameters = benchmark.Property("Parameters").Value.ToString();

                foreach (var parameter in parameters.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var segments = parameter.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                    jobResults.Properties[segments[0]] = segments[1];
                }

                // Store data

                CleanMeasurements(jobResults);

                if (!String.IsNullOrEmpty(_sqlConnectionString))
                {
                    var executionResult = new ExecutionResult();
                    executionResult.JobResults = jobResults;
                    
                    await JobSerializer.WriteJobResultsToSqlAsync(executionResult.JobResults, _sqlConnectionString, _tableName, session, _scenarioOption.Value(), String.Join(" ", _descriptionOption.Value(), fullName));
                }
            }

            return new ExecutionResult
            {
                ReturnCode = String.IsNullOrEmpty(job.Job.Error) ? 0 : 1
            };
        }

        private static async Task<ExecutionResult> RunAutoFlush(
            Configuration configuration,
            string[] dependencies,
            string session,
            TimeSpan span
            )
        {
            var executionResults = new ExecutionResult();

            if (dependencies.Length != 1)
            {
                Log.Write($"With --auto-flush a single job is required.");
                return executionResults;
            }

            var jobName = dependencies.First();
            var service = configuration.Jobs[jobName];

            if (service.Endpoints.Count() != 1)
            {
                Log.Write($"With --auto-flush a single endpoint is required.");
                return executionResults;
            }

            if (!service.WaitForExit && span == TimeSpan.Zero)
            {
                Log.Write($"With --auto-flush a --span duration or a blocking job is required (missing 'waitForExit' option).");
                return executionResults;
            }

            service.DriverVersion = 2;

            var job = new JobConnection(service, new Uri(service.Endpoints.First()));

            // Check os and architecture requirements
            if (!await EnsureServerRequirementsAsync(new[] { job }, service))
            {
                Log.Write($"Scenario skipped as the agent doesn't match the operating and architecture constraints for '{jobName}' ({String.Join("/", new[] { service.Options.RequiredArchitecture, service.Options.RequiredOperatingSystem })})");
                return new ExecutionResult();
            }

            // Start this service on the configured agent endpoint
            await job.StartAsync(jobName);

            var start = DateTime.UtcNow;

            // Wait for the job to stop
            while (true)
            {
                await Task.Delay(5000);

                await job.TryUpdateJobAsync();

                var stop =
                    job.Job.State == JobState.Stopped ||
                    job.Job.State == JobState.Deleted ||
                    job.Job.State == JobState.Failed
                    ;

                if (start + span > DateTime.UtcNow)
                {
                    stop = true;
                }

                if (job.Job.Measurements.Any(x => x.IsDelimiter))
                {
                    // Remove all values after the delimiter locally
                    Measurement measurement;
                    var measurements = new List<Measurement>();

                    do
                    {
                        job.Job.Measurements.TryDequeue(out measurement);
                        measurements.Add(measurement);
                    } while (!measurement.IsDelimiter);

                    job.Job.Measurements = new ConcurrentQueue<Measurement>(measurements);

                    // Removes all values before the delimiter on the server
                    await job.FlushMeasurements();

                    // Convert any json result to an object
                    NormalizeResults(new[] { job });

                    var jobResults = await CreateJobResultsAsync(configuration, dependencies, new Dictionary<string, List<JobConnection>> { [jobName] = new List<JobConnection> { job } });

                    if (!service.Options.DiscardResults)
                    {
                        WriteResults(jobName, jobResults.Jobs[jobName]);
                    }

                    foreach (var property in _propertyOption.Values)
                    {
                        var segments = property.Split('=', 2);

                        jobResults.Properties[segments[0]] = segments[1];
                    }

                    // Save results

                    CleanMeasurements(jobResults);

                    if (_jsonOption.HasValue())
                    {
                        var filename = _jsonOption.Value();

                        var index = 1;

                        do
                        {
                            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                            filename = filenameWithoutExtension + "-" + index++ + Path.GetExtension(filename);
                        } while (File.Exists(filename));

                        await File.WriteAllTextAsync(filename, JsonConvert.SerializeObject(jobResults, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));

                        Log.Write("", notime: true);
                        Log.Write($"Results saved in '{new FileInfo(filename).FullName}'", notime: true);
                    }

                    // Store data

                    if (!String.IsNullOrEmpty(_sqlConnectionString))
                    {
                        await JobSerializer.WriteJobResultsToSqlAsync(jobResults, _sqlConnectionString, _tableName, session, _scenarioOption.Value(), _descriptionOption.Value());
                    }
                }

                if (stop)
                {
                    break;
                }
            }

            await job.StopAsync();

            await job.TryUpdateJobAsync();

            await job.DownloadBuildLogAsync();

            await job.DownloadOutputAsync();

            await job.DownloadDumpAsync();

            await job.DownloadTraceAsync();
            
            await job.DownloadAssetsAsync(jobName);

            await job.DeleteAsync();

            return executionResults;
        }

        public static JObject MergeVariables(params object[] variableObjects)
        {
            var mergeOptions = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

            var result = new JObject();

            foreach (var variableObject in variableObjects)
            {
                if (variableObject == null || !(variableObject is JObject))
                {
                    continue;
                }

                result.Merge(JObject.FromObject(variableObject), mergeOptions);
            }

            return result;
        }

        /// <summary>
        /// Applies all command line argument to alter the configuration files and build a final Configuration instance.
        /// 1- Merges the configuration files in the same order as requested
        /// 2- For each scenario's job, clone it in the Configuration's jobs list
        /// 3- Patch the new job with the scenario's properties
        /// 4- Add custom job entries 
        /// </summary>
        public static async Task<Configuration> BuildConfigurationAsync(
            IEnumerable<string> configurationFileOrUrls,
            string scenarioName,
            IEnumerable<string> customJobs,
            IEnumerable<KeyValuePair<string, string>> arguments,
            JObject commandLineVariables,
            IEnumerable<string> profileNames,
            IEnumerable<string> scripts,
            int interval
            )
        {
            JObject configuration = null;
            
            var defaultConfigFilename = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "default.config.yml");

            configurationFileOrUrls = new [] { defaultConfigFilename }.Union(configurationFileOrUrls);

            // Merge all configuration sources
            foreach (var configurationFileOrUrl in configurationFileOrUrls)
            {
                var localconfiguration = await LoadConfigurationAsync(configurationFileOrUrl);

                if (configuration != null)
                {
                    var mergeOptions = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

                    configuration.Merge(localconfiguration);
                }
                else
                {
                    configuration = localconfiguration;
                }
            }

            // Roundtrip the JObject such that it contains all the extra properties of the Configuration class that are not in the configuration file
            var configurationInstance = configuration.ToObject<Configuration>();

            // After that point we only modify the concrete instance of Configuration
            if (!configurationInstance.Scenarios.ContainsKey(scenarioName))
            {
                var availableScenarios = String.Join("', '", configurationInstance.Scenarios.Keys);
                throw new ControllerException($"The scenario `{scenarioName}` was not found. Possible values: '{availableScenarios}'");
            }

            if (!String.IsNullOrEmpty(scenarioName))
            {
                var scenario = configurationInstance.Scenarios[scenarioName];

                // Clone each service from the selected scenario inside the Jobs property of the Configuration
                foreach (var service in scenario)
                {
                    var jobName = service.Value.Job;
                    var serviceName = service.Key;

                    if (!configurationInstance.Jobs.ContainsKey(jobName))
                    {
                        throw new ControllerException($"The job named `{jobName}` was not found for `{serviceName}`");
                    }

                    var jobObject = JObject.FromObject(configurationInstance.Jobs[jobName]);
                    var dependencyObject = (JObject)configuration["scenarios"][scenarioName][serviceName];

                    PatchObject(jobObject, dependencyObject);

                    configurationInstance.Jobs[serviceName] = jobObject.ToObject<Job>();
                }
            }

            foreach (var jobName in customJobs)
            {
                configurationInstance.Jobs[jobName] = new Job();
            }

            // Pre-configuration
            foreach (var job in configurationInstance.Jobs)
            {
                // Force all jobs as self-contained by default. This can be overriden by command line config.
                // This can't be done in ServerJob for backward compatibility
                job.Value.SelfContained = true;

                // Update the job's interval based on the common config
                job.Value.MeasurementsIntervalSec = interval;
                
                job.Value.Service = job.Key;
            }

            // After that point we only modify the JObject representation of Configuration
            configuration = JObject.FromObject(configurationInstance);

            // Apply the profiles to the configuration
            // 1- The default values of the profile is applied to all jobs
            // 2- The job specific values are applied accordingly to each named job

            foreach (var profileName in profileNames)
            {
                // Check the requested profile name exists
                if (!configurationInstance.Profiles.ContainsKey(profileName))
                {
                    var availableProfiles = String.Join("', '", configurationInstance.Profiles.Keys);
                    throw new ControllerException($"Could not find a profile named '{profileName}'. Possible values: '{availableProfiles}'");
                }

                var profile = (JObject) configuration["Profiles"][profileName];

                // Patch all jobs with the profile's default values (Step 1)
                var profileWithoutJob = (JObject) profile.DeepClone();
                profileWithoutJob.Remove("jobs"); // remove both pascal and camel case properties
                profileWithoutJob.Remove("Jobs");
                profileWithoutJob.Remove("agents"); // 'jobs' was renamed 'agents' when name mapping was introduced
                profileWithoutJob.Remove("Agents");

                foreach (JProperty jobProperty in configuration["Jobs"] ?? new JObject())
                {
                    PatchObject((JObject) jobProperty.Value, profileWithoutJob);
                }

                // Patch each specific job (Step 2)

                ;

                foreach (var serviceEntry in configurationInstance.Scenarios[scenarioName])
                {
                    var serviceName = serviceEntry.Key;

                    var service = configuration["Jobs"][serviceName] as JObject;

                    if (service == null)
                    {
                        throw new ControllerException($"Could not find a service named '{serviceName}' while applying profiles");
                    }

                    // Apply profile on each service:
                    // 1- if a service has an agent property use it to match an agent name or its aliases
                    // 2- otherwise use the service name to match an agent name or its aliases

                    string agentName = null;

                    if (!String.IsNullOrEmpty(serviceEntry.Value.Agent))
                    {
                        agentName = serviceEntry.Value.Agent;
                    }
                    else
                    {
                        agentName = serviceName;
                    }

                    var agents = (profile["agents"] ?? profile["jobs"]) as JObject;


                    // Seek the definition for this profile and agent
                    var profileAgent = agents.Properties().FirstOrDefault(x =>
                    {
                        if (x.Name == agentName)
                        {
                            return true;
                        }

                        if (x.Value is JObject v
                            && v.TryGetValue("aliases", out var aliases)
                            && aliases is JArray aliasesArray
                            && aliasesArray.Values().Select(a => a.Value<string>()).Contains(agentName))
                        {
                            return true;
                        }

                        return false;
                    })?.Value as JObject;

                    if (profileAgent == null)
                    {
                        throw new ControllerException($"Could not find an agent named '{agentName}'.");
                    }

                    PatchObject(service, profileAgent);
                }
            }

            // Apply custom arguments
            foreach (var argument in arguments)
            {
                JToken node = configuration["Jobs"];

                var segments = argument.Key.Split('.');

                foreach (var segment in segments)
                {
                    node = ((JObject)node).GetValue(segment, StringComparison.OrdinalIgnoreCase);

                    if (node == null)
                    {
                        throw new ControllerException($"Could not find part of the configuration path: '{argument}'");
                    }
                }

                if (node is JArray jArray)
                {
                    jArray.Add(argument.Value);
                }
                else if (node is JValue jValue)
                {
                    // The value is automatically converted to the destination type
                    jValue.Value = argument.Value;
                }
                else if (node is JObject jObject)
                {
                    // String to Object mapping -> try to parse as KEY=VALUE
                    var argumentSegments = argument.Value.ToString().Split('=', 2);

                    if (argumentSegments.Length != 2)
                    {
                        throw new ControllerException($"Argument value '{argument.Value}' could not assigned to `{segments.Last()}`.");
                    }

                    jObject[argumentSegments[0]] = argumentSegments[1];
                }
            }

            // Evaluate templates

            var rootVariables = configuration["Variables"];

            foreach (JProperty property in configuration["Jobs"] ?? new JObject())
            {
                var job = property.Value;

                var jobVariables = job["Variables"];

                var variables = MergeVariables(rootVariables, jobVariables, commandLineVariables);

                // Apply templates on variables first
                ApplyTemplates(variables, new TemplateContext(variables.DeepClone()));

                ApplyTemplates(job, new TemplateContext(variables).SetValue("job", job));

                // Variable are merged again in the job such that all variables (root, job, command line) be
                // available in scripts
                job["Variables"] = variables;
            }

            var result = configuration.ToObject<Configuration>();

            // Validates that the scripts defined in the command line exist
            foreach (var script in scripts)
            {
                if (!configurationInstance.Scripts.ContainsKey(script))
                {
                    var availablescripts = String.Join("', '", configurationInstance.Scripts.Keys);
                    throw new ControllerException($"Could not find a script named '{script}'. Possible values: '{availablescripts}'");
                }
            }

            // Jobs post configuration for all jobs in the scenario

            var dependencies = result.Scenarios[scenarioName].Select(x => x.Key).ToArray();

            // Share the same engine instance between all jobs so they can share some state
            var engine = new Engine();

            engine.SetValue("console", _scriptConsole);
            engine.SetValue("configuration", result);

            foreach (var jobName in dependencies)
            {
                var job = result.Jobs[jobName];

                if (job.OnConfigure != null && job.OnConfigure.Any())
                {
                    engine.SetValue("job", job);

                    foreach(var script in job.OnConfigure)
                    {
                        engine.Execute(script);
                    }                    
                }

                // Set default trace arguments if none is specified
                if (job.Collect && String.IsNullOrEmpty(job.CollectArguments))
                {
                    job.CollectArguments = _defaultTraceArguments;
                }

                // If the job is a BenchmarkDotNet application, define default arguments so we can download the results as JSon
                if (job.Options.BenchmarkDotNet)
                {
                    job.WaitForExit = true;
                    job.ReadyStateText ??= "BenchmarkRunner: Start";
                    job.Arguments = DefaultBenchmarkDotNetArguments + " " + job.Arguments;
                }

                if (job.Options.ReuseSource || job.Options.ReuseBuild)
                {
                    var source = job.Source;

                    // Compute a custom source key
                    source.SourceKey = source.Repository
                        + source.Project
                        + source.LocalFolder
                        + source.BranchOrCommit
                        + source.DockerImageName
                        + source.DockerFile
                        + source.InitSubmodules.ToString()
                        + source.Repository
                        ;

                    using (var sha1 = SHA1.Create())  
                    {  
                        // Assume no collision since it's verified on the server
                        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(job.Source.SourceKey));
                        source.SourceKey = String.Concat(bytes.Select(b => b.ToString("x2"))).Substring(0, 8);
                    }

                    if (job.Options.ReuseBuild)
                    {
                        source.NoBuild = true;
                    }
                }

                if (job.CollectCounters)
                {
                    Log.WriteWarning($"WARNING: '{jobName}.collectCounters' has been deprecated, in the future please use '{jobName}.options.collectCounters'.");
                    job.Options.CollectCounters = true;
                }

                // if CollectCounters is set and no provider are defined, use System.Runtime as the default provider
                if (job.Options.CollectCounters == true && !job.Options.CounterProviders.Any())
                {
                    job.Options.CounterProviders.Add("System.Runtime");
                }

                if (!String.IsNullOrEmpty(job.Options.DumpType))
                {
                    if (!Enum.TryParse<DumpTypeOption>(job.Options.DumpType, ignoreCase: true, out var dumpType))
                    {
                        dumpType = DumpTypeOption.Mini;
                        Log.WriteWarning($"WARNING: Invalid value for 'DumpType'. Using 'Mini'.");
                    }

                    job.DumpProcess = true;
                    job.DumpType = dumpType;
                }

                // Copy the dotnet counters from the list of providers
                if (job.Options.CollectCounters != false && job.Options.CounterProviders.Any())
                {
                    foreach (var provider in job.Options.CounterProviders)
                    {
                        var allProviderSections = configurationInstance.Counters.Where(x => x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));

                        if (!allProviderSections.Any())
                        {
                            throw new ControllerException($"Could not find the counters provider named '{provider}'. Possible values: {String.Join(", ", configurationInstance.Counters.Select(x => x.Provider))}");
                        }

                        foreach (var providerSection in allProviderSections)
                        {
                            foreach (var counter in providerSection.Values)
                            {
                                job.Counters.Add(new DotnetCounter { Provider = providerSection.Provider, Name = counter.Name, Measurement = counter.Measurement });
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static void ApplyTemplates(JToken node, TemplateContext templateContext)
        {
            foreach (var token in node.Children())
            {
                if (token is JValue jValue)
                {
                    if (jValue.Type == JTokenType.String)
                    {
                        var template = jValue.ToString();

                        if (template.Contains("{"))
                        {
                            if (FluidParser.TryParse(template, out var tree))
                            {
                                jValue.Value = tree.Render(templateContext);
                            }
                        }
                    }
                }
                else
                {
                    ApplyTemplates(token, templateContext);
                }
            }
        }

        public static async Task<JObject> LoadConfigurationAsync(string configurationFilenameOrUrl)
        {
            JObject localconfiguration;

            if (!string.IsNullOrWhiteSpace(configurationFilenameOrUrl))
            {
                string configurationContent;

                // Load the job definition from a url or locally
                try
                {
                    if (configurationFilenameOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        configurationContent = await _httpClient.GetStringAsync(configurationFilenameOrUrl);
                    }
                    else
                    {
                        configurationContent = File.ReadAllText(configurationFilenameOrUrl);
                    }
                }
                catch
                {
                    throw new ControllerException($"Configuration '{Path.GetFullPath(configurationFilenameOrUrl)}' could not be loaded.");
                }

                localconfiguration = null;

                string configurationExtension = null;

                if (configurationFilenameOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove any query string to detect the correct extension
                    var questionMarkIndex = configurationFilenameOrUrl.IndexOf("?");
                    if (questionMarkIndex != -1)
                    {
                        var filename = configurationFilenameOrUrl.Substring(0, questionMarkIndex);
                        configurationExtension = Path.GetExtension(filename);
                    }
                    else
                    {
                        configurationExtension = Path.GetExtension(configurationFilenameOrUrl);
                    }
                }
                else
                {
                    configurationExtension = Path.GetExtension(configurationFilenameOrUrl);
                }

                switch (configurationExtension)
                {
                    case ".json":
                        localconfiguration = JObject.Parse(configurationContent);
                        break;

                    case ".yml":
                    case ".yaml":

                        var deserializer = new DeserializerBuilder()
                            .WithNodeTypeResolver(new JsonTypeResolver())
                            .Build();

                        var yamlObject = deserializer.Deserialize(new StringReader(configurationContent));

                        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

                        var json = serializer.Serialize(yamlObject);
                        // Format json in case the schema validation fails and we need to render error line numbers
                        localconfiguration = JObject.Parse(json);

                        var schemaJson = File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "benchmarks.schema.json"));
                        var schema = new Manatee.Json.Serialization.JsonSerializer().Deserialize<JsonSchema>(JsonValue.Parse(schemaJson));

                        var jsonToValidate = JsonValue.Parse(json);
                        var validationResults = schema.Validate(jsonToValidate, new JsonSchemaOptions { OutputFormat = SchemaValidationOutputFormat.Detailed });

                        if (!validationResults.IsValid)
                        {
                            // Create a json debug file with the schema
                            localconfiguration.AddFirst(new JProperty("$schema", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.Controller/benchmarks.schema.json"));

                            var debugFilename = Path.Combine(Path.GetTempPath(), "crank-debug.json");
                            File.WriteAllText(debugFilename, localconfiguration.ToString(Formatting.Indented));

                            var errorBuilder = new StringBuilder();

                            errorBuilder.AppendLine($"Invalid configuration file '{configurationFilenameOrUrl}' at '{validationResults.InstanceLocation}'");
                            errorBuilder.AppendLine($"{validationResults.ErrorMessage}");
                            errorBuilder.AppendLine($"Debug file created at '{debugFilename}'");

                            throw new ControllerException(errorBuilder.ToString());
                        }

                        break;
                    default:
                        throw new ControllerException($"Unsupported configuration format: {configurationExtension}");
                }

                // Resolves local paths
                if (!configurationFilenameOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && localconfiguration.ContainsKey("jobs"))
                {
                    foreach (JProperty job in localconfiguration["jobs"])
                    {
                        var jobObject = (JObject)job.Value;
                        if (jobObject.ContainsKey("source"))
                        {
                            var source = (JObject)jobObject["source"];
                            if (source.ContainsKey("localFolder"))
                            {
                                var localFolder = source["localFolder"].ToString();

                                if (!localFolder.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    var configurationFilename = new FileInfo(configurationFilenameOrUrl).FullName;
                                    var resolvedFilename = new FileInfo(Path.Combine(Path.GetDirectoryName(configurationFilename), localFolder)).FullName;

                                    source["localFolder"] = resolvedFilename;
                                }
                            }
                        }
                    }
                }

                // Process imports
                if (localconfiguration.ContainsKey("imports"))
                {
                    var mergeOptions = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

                    // start from a clear document
                    var result = new JObject();
                    
                    // merge each import
                    foreach (JValue import in (JArray)localconfiguration.GetValue("imports"))
                    {
                        var importFilenameOrUrl = import.ToString();

                        var importedConfiguration = await LoadConfigurationAsync(importFilenameOrUrl);

                        if (importedConfiguration != null)
                        {
                            result.Merge(importedConfiguration, mergeOptions);
                        }
                    }

                    // merge local configuration last to win over imports
                    result.Merge(localconfiguration, mergeOptions);
                    localconfiguration = result;
                }

                localconfiguration.Remove("imports");

                return localconfiguration;
            }
            else
            {
                throw new ControllerException($"Invalid file path or url: '{configurationFilenameOrUrl}'");
            }
        }

        /// <summary>
        /// Merges a JObject into another one.
        /// </summary>
        public static void PatchObject(JObject source, JObject patch)
        {
            foreach (var patchProperty in patch)
            {
                var sourceProperty = source.Properties().Where(x => x.Name.Equals(patchProperty.Key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                // The property to patch exists
                if (sourceProperty != null)
                {
                    // if it's an object, patch it recursively
                    if (sourceProperty.Value.Type == JTokenType.Object)
                    {
                        if (patchProperty.Value.Type == JTokenType.Object)
                        {
                            // JObject to JObject mapping
                            PatchObject((JObject)sourceProperty.Value, (JObject)patchProperty.Value);
                        }
                    }
                    else if (sourceProperty.Value.Type == JTokenType.Array)
                    {
                        if (patchProperty.Value.Type == JTokenType.Array)
                        {
                            foreach (var value in (JArray)patchProperty.Value)
                            {
                                ((JArray)sourceProperty.Value).Add(value.DeepClone());
                            }
                        }
                    }
                    else
                    {
                        sourceProperty.Value = patchProperty.Value;
                    }
                }
                else
                {
                    source.Add(patchProperty.Key, patchProperty.Value.DeepClone());
                }
            }
        }

        private async static Task<bool> EnsureServerRequirementsAsync(IEnumerable<JobConnection> jobs, Job service)
        {
            if (String.IsNullOrEmpty(service.Options.RequiredOperatingSystem)
                && String.IsNullOrEmpty(service.Options.RequiredArchitecture))
            {
                return true;
            }

            foreach (var job in jobs)
            {
                var info = await job.GetInfoAsync();

                var os = info["os"]?.ToString();
                var arch = info["arch"]?.ToString();

                if (!String.IsNullOrEmpty(service.Options.RequiredOperatingSystem) && !String.Equals(os, service.Options.RequiredOperatingSystem, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!String.IsNullOrEmpty(service.Options.RequiredArchitecture) && !String.Equals(arch, service.Options.RequiredArchitecture, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        private static Func<IEnumerable<double>, double> Percentile(int percentile)
        {
            return list =>
            {
                var orderedList = list.OrderBy(x => x).ToArray();

                var nth = (int)Math.Ceiling((double)orderedList.Length * percentile / 100);

                if (orderedList.Length > nth)
                {
                    return orderedList[nth];
                }
                else
                {
                    return 0;
                }
            };
        }

        /// <summary>
        /// Converts any "json" serialized value (format: json) to an object
        /// </summary>
        private static void NormalizeResults(IEnumerable<JobConnection> jobs)
        {
            if (jobs == null || !jobs.Any())
            {
                return;
            }

            // For each job, compute the operation on each measurement
            foreach (var job in jobs)
            {
                // Group by name for easy lookup
                var measurements = job.Job.Measurements.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());

                foreach (var metadata in job.Job.Metadata)
                {
                    if (!measurements.ContainsKey(metadata.Name))
                    {
                        continue;
                    }

                    if (metadata.Format == "json")
                    {
                        foreach (var measurement in measurements[metadata.Name])
                        {
                            measurement.Value = JsonConvert.DeserializeObject(measurement.Value.ToString());
                        }

                        metadata.Format = "object";
                    }
                }
            }
        }

        private static async Task<JobResults> CreateJobResultsAsync(Configuration configuration, string[] dependencies, Dictionary<string, List<JobConnection>> jobsByDependency)
        {            
            var jobResults = new JobResults();

            // Initializes the JS engine to compute results

            var engine =  new Engine();
            
            engine.SetValue("benchmarks", jobResults);
            engine.SetValue("console", _scriptConsole);
            engine.SetValue("require", new Action<string> (ImportScript));

            void ImportScript(string s)
            {
                if (!configuration.Scripts.ContainsKey(s))
                {
                    var availablescripts = String.Join("', '", configuration.Scripts.Keys);
                    throw new ControllerException($"Could not find a script named '{s}'. Possible values: '{availablescripts}'");
                }

                engine.Execute(configuration.Scripts[s]);
            }                

            // Import default scripts sections
            foreach (var script in configuration.OnResultsCreating)
            {
                if (!String.IsNullOrWhiteSpace(script))
                {
                    engine.Execute(script);
                }
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (configuration.DefaultScripts != null && configuration.DefaultScripts.Any())
            {
                Log.WriteWarning($"WARNING: 'defaultScripts' has been deprecated, in the future please use 'onResultsCreating'.");

                foreach (var script in configuration.OnResultsCreating)
                {
                    if (!String.IsNullOrWhiteSpace(script))
                    {
                        engine.Execute(script);
                    }
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var jobName in dependencies)
            {
                if (configuration.Jobs[jobName].Options.DiscardResults)
                {
                    continue;
                }

                // Skip failed jobs
                if (!jobsByDependency.ContainsKey(jobName))
                {
                    continue;
                }

                var jobResult = jobResults.Jobs[jobName] = new JobResult();
                var jobConnections = jobsByDependency[jobName];

                // Extract dependencies from the first job
                jobResult.Dependencies = jobConnections.First().Job.Dependencies.ToArray();

                // Calculate results from configuration and job metadata

                var resultDefinitions = jobConnections.SelectMany(j => j.Job.Metadata.Select(x => 
                    new Result { 
                        Measurement = x .Name, 
                        Name = x.Name,
                        Description = x.ShortDescription,
                        Format = x.Format,
                        Aggregate = x.Aggregate.ToString().ToLowerInvariant(),
                        Reduce = x.Reduce.ToString().ToLowerInvariant()
                        }
                    )).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.Last());

                var jobOptions = jobConnections.First().Job.Options;

                // Update any result definition with the ones in the configuration
                foreach (var result in configuration.Results)
                {
                    // If the result is from a dotnet counter, only add it if the counter was requested
                    if (IsUnusedDotnetCounter(result.Name, jobOptions))
                    {
                        continue;
                    }

                    resultDefinitions.TryGetValue(result.Name, out var existing);

                    if (existing == null)
                    {
                        // Add the result if it is not already defined by a metadata from the job
                        resultDefinitions[result.Name] = result;
                    }
                    else 
                    {
                        if (result.Excluded)
                        {
                            existing.Excluded = true;
                        }

                        if (!String.IsNullOrWhiteSpace(result.Aggregate))
                        {
                            existing.Aggregate = result.Aggregate;
                        }

                        if (!String.IsNullOrWhiteSpace(result.Reduce))
                        {
                            existing.Reduce = result.Reduce;
                        }

                        if (!String.IsNullOrWhiteSpace(result.Format))
                        {
                            existing.Format = result.Format;
                        }

                        if (!String.IsNullOrWhiteSpace(result.Name))
                        {
                            existing.Name = result.Name;
                        }

                        if (!String.IsNullOrWhiteSpace(result.Description))
                        {
                            existing.Description = result.Description;
                        }
                    }
                }

                // Update job's metadata with custom results
                jobResult.Metadata = resultDefinitions.Values.Select(x => 
                    new ResultMetadata 
                    {
                        Name = x .Name,
                        Description = x.Description,
                        Format = x.Format
                    })
                    .ToArray();                
                
                jobResult.Results = AggregateAndReduceResults(jobConnections, engine, resultDefinitions.Values.ToList());
                
                foreach (var jobConnection in jobConnections)
                {
                    jobResult.Measurements.Add(jobConnection.Job.Measurements.ToArray());
                }

                jobResult.Environment = await jobConnections.First().GetInfoAsync();

                bool IsUnusedDotnetCounter(string name, Models.Options jobOptions)
                {
                    // Properties from dotnet counters should not be added if they are not part of the included providers

                    var isCounter = false;
                    var isImportedCounter = false;

                    foreach (var counters in configuration.Counters)
                    {
                        // is the result is from a counter ?
                        if (counters.Values.Any(y => y.Measurement == name))
                        {
                            isCounter = true;

                            // yes, then check if it is part of the ones tracked, or skip this result
                            isImportedCounter = jobOptions.CounterProviders.Contains(counters.Provider);
                            break;
                        }
                    }

                    return isCounter && !isImportedCounter;
                }
            }

            // Duplicate measurements with multiple keys
            DuplicateMeasurementKeys(jobResults);


            // Apply scripts

            // When scripts are executed, the metadata and measurements are still available.
            // The metadata is taken from the first job connection, while the measurements
            // of any job connection (multi endpoint job) are taken.
            // The "measurements" property is an array of arrays of measurements.
            // The "results" property contains all measures that are already aggregated and reduced.
            // The "benchmarks" property contains all jobs by name

            // Run scripts for OnResultsCreated
            foreach (var script in configuration.OnResultsCreated)
            {
                if (!String.IsNullOrWhiteSpace(script))
                {
                    engine.Execute(script);
                }
            }

            // Run custom scripts after the results are computed
            foreach (var scriptName in _scriptOption.Values)
            {
                var scriptContent = configuration.Scripts[scriptName];

                engine.Execute(scriptContent);
            }

            return jobResults;
        }

        private static void DuplicateMeasurementKeys(JobResults jobResults)
        {
            // Remove metadata
            foreach (var jobResult in jobResults.Jobs.Values)
            {
                // Duplicate multi key values (if the key contains ';') then it means the key is currently migrated 
                // and it wants both keys to have the result for backward compatibility
                foreach (var result in jobResult.Results.ToArray())
                {
                    if (result.Key.Contains(";"))
                    {
                        var keys = result.Key.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var key in keys)
                        {
                            jobResult.Results[key] = result.Value;
                        }

                        jobResult.Results.Remove(result.Key);
                    }
                }

                // Keep only one metadata such that results are not duplicated in the output
                for (var i = 0; i< jobResult.Metadata.Length; i++)
                {
                    var metadata = jobResult.Metadata[i];

                    if (metadata.Name.Contains(";"))
                    {
                        var keys = metadata.Name.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        metadata.Name = keys.Last();
                    }
                }

                // Duplicate measurements
                var measurementSets = jobResult.Measurements.ToArray();
                jobResult.Measurements.Clear();

                foreach (var measurementSet in measurementSets)
                {
                    jobResult.Measurements.Add(measurementSet.SelectMany(x =>
                    {
                        if (!x.Name.Contains(";"))
                        {
                            return new[] { x };
                        }
                        else
                        {
                            return x.Name.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(
                                key => new Measurement { Name = key, Timestamp = x.Timestamp, Value = x.Value }
                                );
                        }
                    }).ToArray());
                }
            }
        }

        private static void CleanMeasurements(JobResults jobResults)
        {
            // Remove metadata
            foreach (var jobResult in jobResults.Jobs.Values)
            {
                // Exclude metadata
                if (_excludeMetadataOption.HasValue())
                {
                    jobResult.Metadata = Array.Empty<ResultMetadata>();
                }

                // Exclude measurements
                if (_excludeMeasurementsOption.HasValue())
                {
                    jobResult.Measurements.Clear();
                }
            }
        }

        /// <summary>
        /// Given a list of all the JobConnection for a named job (load for instance) aggregates all the measures of the same measurement for a job,
        /// then reduces these values across jobs.
        /// </summary>
        private static Dictionary<string, object> AggregateAndReduceResults(IEnumerable<JobConnection> jobs, Engine engine, List<Result> resultDefinitions)
        {
            // resulDefinitions contains the list of all rules to apply to any measurement, e.g.:
            //   {
            //     "Measurement": "runtime-counter/time-in-gc",
            //     "Name": "runtime-counter/time-in-gc",
            //     "Description": "Max Time in GC (%)",
            //     "Format": "n2",
            //     "Aggregate": "max",
            //     "Reduce": "max",
            //     "Excluded": false
            //   }

            if (jobs == null || !jobs.Any())
            {
                return new Dictionary<string, object>();
            }

            // For each job, compute the operation on each measurement
            var groups = jobs.Select(job =>
            {
                // Group by name for easy lookup
                var measurements = job.Job.Measurements.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());

                var summaries = new Dictionary<string, object>();

                foreach (var name in measurements.Keys)
                {
                    foreach (var resultDefinition in resultDefinitions.Where(x => x.Measurement == name))
                    {
                        object aggregated = null;

                        try
                        {
                            aggregated = engine.Invoke(resultDefinition.Aggregate, arguments: new object [] { measurements[name].Select(x => x.Value).ToArray() }).ToObject();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not aggregate: {name} with {resultDefinition.Aggregate} for job {job.Job.Id} error {ex.Message}");
                            continue;
                        }

                        try
                        {
                            summaries[resultDefinition.Name] = Convert.ToDouble(aggregated);
                        }
                        catch
                        {
                            // If the value can't be converted to double, just keep it
                            // e.g., bombardier/raw
                            summaries[resultDefinition.Name] = aggregated;
                        }
                    }
                }

                return summaries;
            }).ToArray();

            // Single job, no reduce operation is necessary
            if (groups.Length == 1)
            {
                return groups[0];
            }

            var reduced = new Dictionary<string, object>();

            foreach (var resultDefinition in resultDefinitions)
            {
                var values = groups.SelectMany(x => x).Where(x => x.Key == resultDefinition.Name).ToArray();

                // No measurement for this result
                if (values.Length == 0)
                {
                    continue;
                }

                object reducedValue = null;

                try
                {
                    reducedValue = engine.Invoke(resultDefinition.Reduce, arguments: new object [] { values.Select(x => x.Value).ToArray() }).ToObject();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not reduce: {resultDefinition.Name} with {resultDefinition.Reduce} error {ex.Message}");
                    continue;
                }

                reduced[resultDefinition.Name] = reducedValue;
            }

            return reduced;
        }

        private static void WriteResults(string jobName, JobResult jobResult)
        {
            var renderChart = _renderChartOption.HasValue();

            // 1 column per jobConnection
            var table = new ResultTable(2); 

            // Add a chart column?
            if (renderChart)
            {
                table = new ResultTable(table.Columns + 1); 
            }

            table.Headers.Add(jobName);
            table.Headers.Add("");

            if (renderChart)
            {
                table.Headers.Add("");
            }

            foreach (var metadata in jobResult.Metadata)
            {
                if (!jobResult.Results.ContainsKey(metadata.Name) || metadata.Format == "object")
                {
                    continue;
                }

                var row = table.AddRow();

                var cell = new Cell();
                cell.Elements.Add(new CellElement() { Text = metadata.Description, Alignment = CellTextAlignment.Left });
                row.Add(cell);

                cell = new Cell();
                row.Add(cell);

                var value = jobResult.Results[metadata.Name];

                if (value is string s)
                {
                    cell.Elements.Add(new CellElement(s, CellTextAlignment.Left));
                }
                else if (value is double d)
                {
                    if (!String.IsNullOrEmpty(metadata.Format))
                    {
                        cell.Elements.Add(new CellElement(d.ToString(metadata.Format), CellTextAlignment.Left));
                    }
                    else
                    {
                        cell.Elements.Add(new CellElement(d.ToString("n2"), CellTextAlignment.Left));
                    }
                }
                
                if (renderChart)
                {
                    try
                    {
                        var autoScale = _chartScaleOption.HasValue() && _chartScaleOption.Value() == "auto";

                        Console.OutputEncoding = Encoding.UTF8;

                        var chars = _chartTypeOption.HasValue() && _chartTypeOption.Value() == "hex" 
                            ? hexChartChars
                            : barChartChars
                            ;

                        var values = jobResult.Measurements[0].Where(x => x.Name.Equals(metadata.Name, StringComparison.OrdinalIgnoreCase)).Select(x => Convert.ToDouble(x.Value)).ToArray();
                        
                        // Exclude zeros from min value so we can use a blank space for exact zeros
                        var min = values.Where(x => x != 0).Min();
                        var max = values.Max();
                        var delta = autoScale ? max - min : max;
                        var step = delta / (chars.Length - 1);

                        var normalizedValues = values.Select(x => x == 0 ? 0 : (int) Math.Round((x - (autoScale ? min : 0)) / step));

                        if (step != 0 && values.Length > 1)
                        {
                            if (normalizedValues.All(x => x >= 0 && x < chars.Length))
                            {
                                var chart = new String(normalizedValues.Select(x => chars[x]).ToArray());
                                row.Add(new Cell(new CellElement(chart, CellTextAlignment.Left)));
                            }
                            else
                            {
                                row.Add(new Cell());    
                            }                            
                        }
                        else
                        {
                            row.Add(new Cell());
                        }
                    }
                    catch
                    {
                        row.Add(new Cell());
                    }
                }
            }

            table.Render(Console.Out);
            Console.WriteLine();
        }

        private static void WriteMeasuresTable(string jobName, IList<JobConnection> jobConnections)
        {
            // 1 column per jobConnection
            var table = new ResultTable(1 + jobConnections.Count()); 

            if (_renderChartOption.HasValue())
            {
                table = new ResultTable(table.Columns + 1); 
            }

            table.Headers.Add(jobName);

            foreach (var jobConnection in jobConnections)
            {
                if (jobConnections.Count > 1)
                {
                    table.Headers.Add($"#{jobConnections.IndexOf(jobConnection) + 1}");

                    if (_renderChartOption.HasValue())
                    {
                        table.Headers.Add(""); // chart
                    }                    
                }
                else
                {
                    table.Headers.Add("");

                    if (_renderChartOption.HasValue())
                    {
                        table.Headers.Add(""); // chart
                    }
                }
            }
           
            foreach (var jobConnection in jobConnections)
            {
                var jobIndex = jobConnections.IndexOf(jobConnection);
                var isFirstJobConnection = jobIndex == 0;

                // Group by name for easy lookup
                var measurements = jobConnection.Job.Measurements.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());

                foreach (var metadata in jobConnection.Job.Metadata)
                {
                    var row = table.AddRow();

                    if (isFirstJobConnection)
                    {
                        var cell = new Cell();

                        cell.Elements.Add(new CellElement() { Text = metadata.ShortDescription, Alignment = CellTextAlignment.Left });

                        row.Add(cell);
                    }

                    if (!measurements.ContainsKey(metadata.Name))
                    {
                        // Add empty cell
                        row.Add(new Cell());
                        
                        if (_renderChartOption.HasValue())
                        {
                            row.Add(new Cell()); // chart
                        }
                        
                        continue;
                    }

                    object result = 0;

                    switch (metadata.Aggregate)
                    {
                        case Operation.All:
                            result = measurements[metadata.Name].Select(x => x.Value).ToArray();
                            break;

                        case Operation.First:
                            result = measurements[metadata.Name].First().Value;
                            break;

                        case Operation.Last:
                            result = measurements[metadata.Name].Last().Value;
                            break;

                        case Operation.Avg:
                            result = measurements[metadata.Name].Average(x => Convert.ToDouble(x.Value));
                            break;

                        case Operation.Count:
                            result = measurements[metadata.Name].Count();
                            break;

                        case Operation.Max:
                            result = measurements[metadata.Name].Max(x => Convert.ToDouble(x.Value));
                            break;

                        case Operation.Median:
                            result = Percentile(50)(measurements[metadata.Name].Select(x => Convert.ToDouble(x.Value)));
                            break;

                        case Operation.Min:
                            result = measurements[metadata.Name].Min(x => Convert.ToDouble(x.Value));
                            break;

                        case Operation.Sum:
                            result = measurements[metadata.Name].Sum(x => Convert.ToDouble(x.Value));
                            break;

                        case Operation.Delta:
                            result = measurements[metadata.Name].Max(x => Convert.ToDouble(x.Value)) - measurements[metadata.Name].Min(x => Convert.ToDouble(x.Value));
                            break;

                        default:
                            result = measurements[metadata.Name].First().Value;
                            break;
                    }

                    // We don't render the result if it's a raw object
                    if (metadata.Format != "object")
                    {
                        var cell = new Cell();
                        row.Add(cell);

                        if (!String.IsNullOrEmpty(metadata.Format))
                        {
                            cell.Elements.Add(new CellElement(Convert.ToDouble(result).ToString(metadata.Format), CellTextAlignment.Right));
                        }
                        else
                        {
                            var maxLength = 30;
                            var stringValue = result.ToString();
                            if (stringValue.Length > maxLength)
                            {
                                stringValue = stringValue.Substring(0, maxLength - 3) + "...";
                            }

                            cell.Elements.Add(new CellElement(stringValue, CellTextAlignment.Left));
                        }

                        // Render charts
                        if (_renderChartOption.HasValue())
                        {
                            try
                            {
                                var autoScale = _chartScaleOption.HasValue() && _chartScaleOption.Value() == "auto";

                                Console.OutputEncoding = Encoding.UTF8;

                                var chars = _chartTypeOption.HasValue() && _chartTypeOption.Value() == "hex" 
                                    ? hexChartChars
                                    : barChartChars
                                    ;

                                var values = measurements[metadata.Name].Select(x => Convert.ToDouble(x.Value)).ToArray();
                                
                                // Exclude zeros from min value so we can use a blank space for exact zeros
                                var min = values.Where(x => x != 0).Min();
                                var max = values.Max();
                                var delta = autoScale ? max - min : max;
                                var step = delta / (chars.Length - 1);

                                var normalizedValues = values.Select(x => x == 0 ? 0 : (int) Math.Round((x - (autoScale ? min : 0)) / step));

                                if (step != 0 && values.Length > 1)
                                {
                                    if (normalizedValues.All(x => x >= 0 && x < chars.Length))
                                    {
                                        var chart = new String(normalizedValues.Select(x => chars[x]).ToArray());
                                        row.Add(new Cell(new CellElement(chart, CellTextAlignment.Left)));
                                    }
                                    else
                                    {
                                        row.Add(new Cell());    
                                    }                            
                                }
                                else
                                {
                                    row.Add(new Cell());
                                }
                            }
                            catch
                            {
                                row.Add(new Cell());
                            }
                        }
                    }
                    else
                    {
                        row.Add(new Cell());

                        if (_renderChartOption.HasValue())
                        {
                            row.Add(new Cell());
                        }
                    }            
                }
            }
            
            table.RemoveEmptyRows(1);
            table.Render(Console.Out);
        }

        private static void WriteExecutionResults(ExecutionResult executionResult)
        {
            foreach (var job in executionResult.JobResults.Jobs)
            {
                var jobName = job.Key;
                var jobResult = job.Value;

                WriteResults(jobName, jobResult);
            }            
        }

        private static ExecutionResult ComputeAverages(IEnumerable<ExecutionResult> executionResults)
        {
            var jobResults = new JobResults();
            var executionResult = new ExecutionResult 
            {
                ReturnCode = executionResults.Any(x => x.ReturnCode != 0) ? 1 : 0,
                JobResults = jobResults 
            };

            foreach (var job in executionResults.First().JobResults.Jobs)
            {
                var jobName = job.Key;
                var metadata = job.Value.Metadata;

                // When computing averages, we lose all measurements
                var jobResult = new JobResult { Metadata = metadata, Dependencies = job.Value.Dependencies };
                
                jobResults.Jobs[jobName] = jobResult;

                foreach (var result in job.Value.Results)
                {
                    var allValues = executionResults
                        .Select(x => x.JobResults.Jobs[jobName])
                        .Select(x => x.Results.ContainsKey(result.Key) ? x.Results[result.Key] : null)
                        .Where(x => x != null)
                        .ToArray()
                        ;

                    if (allValues.Any())
                    {
                        if (allValues.First() is string)
                        {
                            jobResult.Results[result.Key] = allValues.First();
                        }
                        else
                        {
                            try
                            {
                                var average = allValues.Select(x => Convert.ToDouble(x)).Average();
                                
                                jobResult.Results[result.Key] = average;
                            }
                            catch
                            {
                                // If the value can't be converted to double, just skip it
                                // e.g., bombardier/raw
                            }
                        }
                    }                     
                }

                jobResults.Jobs[jobName] = jobResult;
            }

            return executionResult;
        }

        private static async Task<string> GetRelayTokenAsync(Uri endpointUri)
        {
            var connectionString = GetAzureRelayConnectionString();

            if (String.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            var rcsb = new RelayConnectionStringBuilder(connectionString);
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(rcsb.SharedAccessKeyName, rcsb.SharedAccessKey);
            return (await tokenProvider.GetTokenAsync(rcsb.Endpoint.ToString(), TimeSpan.FromHours(1))).TokenString;

            string GetAzureRelayConnectionString()
            {
                // 1- Use argument if provided
                // 2- Look for an ENV with the name of the hybrid connection
                // 3- Look for an ENV with the name of the relay

                if (_relayConnectionStringOption.HasValue())
                {
                    var relayConnectionString = _relayConnectionStringOption.Value();

                    if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(relayConnectionString)))
                    {
                        return Environment.GetEnvironmentVariable(relayConnectionString);
                    }
                    else
                    {
                        return relayConnectionString;
                    }
                }

                if (!endpointUri.Authority.EndsWith("servicebus.windows.net", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var rootVariable = $"{endpointUri.Authority.Replace(".", "_")}"; // aspnetperf_servicebus_windows_net
                var entityVariable = $"{rootVariable}__{endpointUri.LocalPath}"; // aspnetperf_servicebus_windows_net__local

                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(entityVariable)))
                {
                    return Environment.GetEnvironmentVariable(entityVariable);
                }

                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(rootVariable)))
                {
                    return Environment.GetEnvironmentVariable(entityVariable);
                }

                return null;
            }
        }
    }
}
