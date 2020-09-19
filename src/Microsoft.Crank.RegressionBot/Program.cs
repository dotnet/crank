// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Manatee.Json.Schema;
using Manatee.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.RegressionBot
{
    class Program
    {
        static ProductHeaderValue ClientHeader = new ProductHeaderValue("CrankBot");

        private static GitHubClient _githubClient;
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        static readonly TimeSpan RecentIssuesTimeSpan = TimeSpan.FromDays(8);

        static BotOptions _options;
        static Credentials _credentials;
        static IReadOnlyList<Issue> _recentIssues;
        
        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);

            TemplateContext.GlobalMemberAccessStrategy.Register<BenchmarksResult>();
            TemplateContext.GlobalMemberAccessStrategy.Register<Report>();
            TemplateContext.GlobalMemberAccessStrategy.Register<Regression>();
            TemplateContext.GlobalMemberAccessStrategy.Register<JObject, object>((obj, name) => obj[name]);
            FluidValue.SetTypeMapping<JObject>(o => new ObjectValue(o));
            FluidValue.SetTypeMapping<JValue>(o => FluidValue.Create(o.Value));
        }

        static async Task<int> Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                new Option<long>(
                    "--repository-id",
                    description: "The GitHub repository id. Tip: The repository id can be found using this endpoint: https://api.github.com/repos/dotnet/aspnetcore"),
                new Option<string>(
                    "--access-token",
                    "The GitHub account access token. (Secured)"),
                new Option<string>(
                    "--username",
                    "The GitHub account username."),
                new Option<string>(
                    "--app-key",
                    "The GitHub application key. (Secured)"),
                new Option<string>(
                    "--app-id",
                    "The GitHub application id."),
                new Option<long>(
                    "--install-id",
                    "The GitHub installation id."),
                new Option<string>(
                    "--connectionstring",
                    "The database connection string, or environment variable name containing it. (Secured)") { IsRequired = true },
                new Option<string[]>(
                    "--config",
                    "The path to a configuration file. (Can be repeated)") { IsRequired = true },
                new Option<bool>(
                    "--debug",
                    "When used, GitHub issues are not created."
                ),
                new Option<bool>(
                    "--verbose",
                    "When used, detailed logs are displayed."
                ),
                new Option<bool>(
                    "--read-only",
                    "When used, nothing is written on GitHub."
                ),
            };

            rootCommand.Description = "Crank Regression Bot";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<BotOptions>(Controller);

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Controller(BotOptions options)
        {

            // Validate arguments
            try
            {
                options.Validate();
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception: " + e.ToString());

                return 1;
            }

            // Substitute with ENV value if it exists

            if (!String.IsNullOrEmpty(options.ConnectionString) && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(options.ConnectionString)))
            {
                options.ConnectionString = Environment.GetEnvironmentVariable(options.ConnectionString);
            }

            if (!String.IsNullOrEmpty(options.AccessToken) && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(options.AccessToken)))
            {
                options.AccessToken = Environment.GetEnvironmentVariable(options.AccessToken);
            }

            if (!String.IsNullOrEmpty(options.AppKey) && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(options.AppKey)))
            {
                options.AppKey = Environment.GetEnvironmentVariable(options.AppKey);
            }

            _options = options;

            // Load configuration files

            var sources = new List<Source>();
            var templates = new Dictionary<string, string>();

            foreach (var configurationFilenameOrUrl in options.Config)
            {
                try
                {
                    var configuration = await LoadConfigurationAsync(configurationFilenameOrUrl);
                    sources.AddRange(configuration.Sources);
                    
                    foreach (var template in configuration.Templates)
                    {
                        templates[template.Key] = template.Value;
                    }
                }
                catch (RegressionBotException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
            }

            if (!sources.Any())
            {
                Console.WriteLine("No source could be found.");
                return 1;
            }

            // Creating GitHub credentials

            if (!options.Debug)
            {
                if (!String.IsNullOrEmpty(options.AccessToken))
                {
                    _credentials = CredentialsHelper.GetCredentialsForUser(options);
                }
                else
                {
                    _credentials = await CredentialsHelper.GetCredentialsForAppAsync(options);
                }
            }

            // Regressions

            Console.WriteLine("Looking for regressions...");

            foreach (var s in sources)
            {
                if (!String.IsNullOrEmpty(s.Name))
                {
                    Console.WriteLine($"Processing source '{s.Name}'");
                }

                var template = templates[s.Regressions.Template];
                
                var regressions = await FindRegression(s).ToListAsync();

                if (!regressions.Any())
                {
                    continue;
                }

                Console.WriteLine($"Found {regressions.Count()} regressions");
                
                Console.WriteLine("Updating existing issues...");

                var newRegressions = await UpdateIssues(regressions, s, template);

                if (newRegressions.Any())
                {
                    Console.WriteLine("Reporting new regressions...");

                    await CreateRegressionIssue(newRegressions, template);
                }
                else
                {
                    Console.WriteLine("No new regressions were found.");
                }
            }

            // // Not running

            // Console.WriteLine("Looking for scenarios that are not running...");

            // var notRunning = await FindNotRunning();

            // Console.WriteLine("Excluding the ones already reported...");

            // // If the LastRun date doesn't match either it's because it was fixed then broke again since last reported issue
            // notRunning = await RemoveReportedRegressions(notRunning, true, r => $"| {r.Scenario} | {r.OperatingSystem}, {r.Hardware}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} |");

            // if (notRunning.Any())
            // {
            //     Console.WriteLine("Reporting new scenarios...");

            //     await CreateNotRunningIssue(notRunning);
            // }
            // else
            // {
            //     Console.WriteLine("All scenarios are running correctly.");
            // }

            // // Bad responses

            // Console.WriteLine("Looking for scenarios that have errors...");

            // var badResponses = await FindErrors();

            // Console.WriteLine("Excluding the ones already reported...");

            // badResponses = await RemoveReportedRegressions(badResponses, true, r => $"| {r.Scenario} | {r.OperatingSystem}, {r.Hardware}, {r.Scheme}, {r.WebHost} |");

            // if (badResponses.Any())
            // {
            //     Console.WriteLine("Reporting new scenarios...");

            //     await CreateErrorsIssue(badResponses);
            // }
            // else
            // {
            //     Console.WriteLine("All scenarios are running correctly.");
            // }

            return 0;
        }

        private static async Task<string> CreateIssueBody(IEnumerable<Regression> regressions, string template)
        {
            var report = new Report
            {
                Regressions = regressions.OrderBy(x => x.CurrentResult.Scenario).ThenBy(x => x.CurrentResult.DateTimeUtc).ToList()
            };

            if (!FluidTemplate.TryParse(template, out var fluidTemplate, out var errors))
            {   
                Console.WriteLine("Error parsing the template:");
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

                return "";
            }

            var context = new TemplateContext { Model = report };

            var body = await fluidTemplate.RenderAsync(context);

            body = AddOwners(body, regressions);

            body += CreateRegressionsBlock(regressions);

            return body;
        }

        private static async Task CreateRegressionIssue(IEnumerable<Regression> regressions, string template)
        {
            if (regressions == null || !regressions.Any())
            {
                return;
            }

            var body = await CreateIssueBody(regressions, template);            
            
            var title = "Performance regression: " + String.Join(", ", regressions.Select(x => x.CurrentResult.Scenario).Take(5));

            if (regressions.Count() > 5)
            {
                title += " ...";
            }

            if (!_options.Debug)
            {
                var createIssue = new NewIssue(title)
                {
                    Body = body
                };

                TagIssue(createIssue, regressions);

                if (!_options.ReadOnly) 
                {
                    await GetClient().Issue.Create(_options.RepositoryId, createIssue);
                }
            }

            if (_options.Debug || _options.Verbose)
            {
                Console.WriteLine(body.ToString());
            }
        }

        public static async Task<Configuration> LoadConfigurationAsync(string configurationFilenameOrUrl)
        {
            JObject localconfiguration = null;

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
                    throw new RegressionBotException($"Configuration '{configurationFilenameOrUrl}' could not be loaded.");
                }

                // Detect file extension
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

                        object yamlObject;

                        try
                        {
                            yamlObject = deserializer.Deserialize(new StringReader(configurationContent));
                        }
                        catch (YamlDotNet.Core.SyntaxErrorException e)
                        {
                            throw new RegressionBotException($"Error while parsing '{configurationFilenameOrUrl}'\n{e.Message}");
                        }

                        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

                        var json = serializer.Serialize(yamlObject);
                        
                        // Format json in case the schema validation fails and we need to render error line numbers
                        localconfiguration = JObject.Parse(json);

                        var schemaJson = File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "regressionbot.schema.json"));
                        var schema = new Manatee.Json.Serialization.JsonSerializer().Deserialize<JsonSchema>(JsonValue.Parse(schemaJson));

                        var jsonToValidate = JsonValue.Parse(json);
                        var validationResults = schema.Validate(jsonToValidate, new JsonSchemaOptions { OutputFormat = SchemaValidationOutputFormat.Detailed });

                        if (!validationResults.IsValid)
                        {
                            // Create a json debug file with the schema
                            localconfiguration.AddFirst(new JProperty("$schema", "https://raw.githubusercontent.com/dotnet/crank/master/src/Microsoft.Crank.RegressionBot/regressionbot.schema.json"));

                            var debugFilename = Path.Combine(Path.GetTempPath(), "configuration.debug.json");
                            File.WriteAllText(debugFilename, localconfiguration.ToString(Formatting.Indented));

                            var errorBuilder = new StringBuilder();

                            errorBuilder.AppendLine($"Invalid configuration file '{configurationFilenameOrUrl}' at '{validationResults.InstanceLocation}'");
                            errorBuilder.AppendLine($"{validationResults.ErrorMessage}");
                            errorBuilder.AppendLine($"Debug file created at '{debugFilename}'");

                            throw new RegressionBotException(errorBuilder.ToString());
                        }

                        break;
                    default:
                        throw new RegressionBotException($"Unsupported configuration format: {configurationExtension}");
                }

                return localconfiguration.ToObject<Configuration>();
            }
            else
            {
                throw new RegressionBotException($"Invalid file path or url: '{configurationFilenameOrUrl}'");
            }
        }

        /// <summary>
        /// This method finds regressions for a give source.
        /// Steps:
        /// - Query the table for the latest rows specified in the source
        /// - Group records by Scenario + Description (descriptor)
        /// - For each unique descriptor
        ///   - Find matching rules from the source
        ///   - Evaluate the source's probes for each record
        ///   - Calculates the std deviation
        ///   - Look for 2 consecutive deviations
        /// </summary>
        private static async IAsyncEnumerable<Regression> FindRegression(Source source)
        {
            if (source.Regressions == null)
            {
                yield break;
            }

            var detectionMinDateTimeUtc = DateTime.UtcNow.AddDays(0 - source.DaysToAnalyze);
            var detectionMaxDateTimeUtc = DateTime.UtcNow.AddDays(0 - source.DaysToSkip);
            
            var allResults = new List<BenchmarksResult>();

            // Load latest records

            Console.Write("Loading records... ");

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                using (var command = new SqlCommand(String.Format(Queries.Latest, source.Table), connection))
                {
                    // Load 14 days or data, to measure 7 days of standard deviation prior to detection
                    command.Parameters.AddWithValue("@startDate", DateTime.UtcNow.AddDays(0 - source.DaysToLoad));
                    
                    await connection.OpenAsync();

                    var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        allResults.Add(new BenchmarksResult
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Excluded = Convert.ToBoolean(reader["Excluded"]),
                            DateTimeUtc = (DateTimeOffset)reader["DateTimeUtc"],
                            Session = Convert.ToString(reader["Session"]),
                            Scenario = Convert.ToString(reader["Scenario"]),
                            Description = Convert.ToString(reader["Description"]),
                            Document = Convert.ToString(reader["Document"]),
                        });
                    }
                }
            }

            Console.WriteLine($"{allResults.Count} found");
            
            // Reorder results chronologically

            allResults.Reverse();

            // Compute standard deviation

            var resultsByScenario = allResults
                .GroupBy(x => x.Scenario + ":" + x.Description)
                .ToDictionary(x => x.Key, x => x.ToArray())
                ;

            foreach (var descriptor in resultsByScenario.Keys)
            {
                // Does the descriptor match a rule?

                if (!source.Include(descriptor))
                {
                    continue;
                }

                var rules = source.Match(descriptor);

                // Should regressions be ignored for this descriptor?
                var lastIgnoreRegressionRule = rules.LastOrDefault(x => x.IgnoreRegressions != null);

                if (lastIgnoreRegressionRule != null && lastIgnoreRegressionRule.IgnoreRegressions.Value)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine("Regressions ignored");
                    }

                    continue;
                }

                // Resolve path for the metric
                var results = resultsByScenario[descriptor];

                foreach (var probe in source.Regressions.Probes)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Evaluating probe {probe.Path} for {results.Count()} benchmarks");
                    }
                    
                    var resultSet = results
                        .Select(x => new { Result = x, Token = x.Data.SelectTokens(probe.Path).FirstOrDefault() })
                        .Where(x => x.Token != null)
                        .Select(x => new { Result = x.Result, Value = Convert.ToDouble(x.Token)})
                        .ToArray();

                    // Find regressions

                    // Can't find a regression if there are less than 5 value
                    if (resultSet.Length < 5)
                    {
                        if (_options.Verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Not enough data ({resultSet.Length})");
                            Console.ResetColor();
                        }

                        continue;
                    }

                    // Calculate standard deviation
                    var values = resultSet.Select(x => x.Value).ToArray();

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Values: [{String.Join(",", values)}]");
                    }

                    double average = values.Average();
                    double sumOfSquaresOfDifferences = values.Sum(val => (val - average) * (val - average));
                    double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Length);

                    // Look for 2 consecutive values that are outside of the threshold, 
                    // subsequent to 3 consecutive values that are inside the threshold.  

                    for (var i = 0; i < resultSet.Length - 5; i++)
                    {
                        // Ignore results before the searched date and after the skipped dates
                        if (resultSet[i].Result.DateTimeUtc < detectionMinDateTimeUtc
                        || resultSet[i].Result.DateTimeUtc >= detectionMaxDateTimeUtc)
                        {
                            continue;
                        }

                        var value1 = Math.Abs(values[i+1] - values[i]);
                        var value2 = Math.Abs(values[i+2] - values[i]);
                        var value3 = Math.Abs(values[i+3] - values[i+2]);
                        var value4 = Math.Abs(values[i+4] - values[i+2]);

                        if (_options.Verbose)
                        {
                            Console.WriteLine($"{descriptor} {probe.Path} {resultSet[i+2].Result.DateTimeUtc} {values[i+0]} {values[i+1]} {values[i+2]} {values[i+3]} ({value3}) {values[i+4]} ({value4}) / {standardDeviation * probe.Threshold:n0}");
                        }                        

                        var hasRegressed = false;

                        switch (probe.Unit)
                        {
                            case ThresholdUnits.StDev:
                                // factor of standard deviation
                                hasRegressed = value1 < standardDeviation
                                    && value2 < standardDeviation
                                    && value3 >= probe.Threshold * standardDeviation
                                    && value4 >= probe.Threshold * standardDeviation
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;
                            case ThresholdUnits.Percent:
                                // percentage of the average of values
                                hasRegressed = value1 < average * (probe.Threshold / 100)
                                    && value2 < average * (probe.Threshold / 100)
                                    && value3 >= average * (probe.Threshold / 100)
                                    && value4 >= average * (probe.Threshold / 100)
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;                            
                            case ThresholdUnits.Absolute:
                                // absolute deviation
                                hasRegressed = value1 < probe.Threshold
                                    && value2 < probe.Threshold
                                    && value3 >= probe.Threshold
                                    && value4 >= probe.Threshold
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;
                            default:
                                break;
                        }

                        if (hasRegressed)
                        {
                            if (_options.Verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Regression");
                                Console.ResetColor();
                            }

                            
                            var regression = new Regression 
                            {
                                PreviousResult = resultSet[i+2].Result,
                                CurrentResult = resultSet[i+3].Result,
                                Change = value3,
                                StandardDeviation = standardDeviation,
                                Average = average
                            };

                            foreach (var rule in rules)
                            {
                                foreach (var label in rule.Labels)
                                {
                                    regression.Labels.Add(label);
                                }

                                foreach(var owner in rule.Owners)
                                {
                                    regression.Owners.Add(owner);
                                }
                            }

                            foreach (var label in source.Regressions.Labels)
                            {
                                regression.Labels.Add(label);
                            }

                            foreach(var owner in source.Regressions.Owners)
                            {
                                regression.Owners.Add(owner);
                            }

                            // If there are subsequent measurements, detect if the benchmark has 
                            // recovered by search for a value in the limits
                            
                            for (var j = i + 5; j < resultSet.Length; j++)
                            {
                                var nextValue = Math.Abs(values[j] - values[i+2]);

                                var hasRecovered = false;

                                switch (probe.Unit)
                                {
                                    case ThresholdUnits.StDev:
                                        // factor of standard deviation
                                        hasRecovered = nextValue < probe.Threshold * standardDeviation
                                            && Math.Sign(nextValue) == Math.Sign(value4);

                                        break;
                                    case ThresholdUnits.Percent:
                                        // percentage of the average of values
                                        hasRecovered = nextValue < average * (probe.Threshold / 100)
                                            && Math.Sign(nextValue) == Math.Sign(value4);

                                        break;                            
                                    case ThresholdUnits.Absolute:
                                        // absolute deviation
                                        hasRecovered = nextValue < probe.Threshold
                                            && Math.Sign(nextValue) == Math.Sign(value4);

                                        break;
                                    default:
                                        break;
                                } 

                                if (hasRecovered)
                                {
                                    regression.RecoveredResult = resultSet[j].Result;
                                    
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"Recovered on {regression.RecoveredResult.DateTimeUtc}");
                                    Console.ResetColor();
                                    
                                    break;
                                }
                            }

                            yield return regression; 
                        }
                    }
                }
            }
        }

        // private static async Task<IEnumerable<Regression>> FindNotRunning()
        // {
        //     var regressions = new List<Regression>();

        //     using (var connection = new SqlConnection(_options.ConnectionString))
        //     {
        //         using (var command = new SqlCommand(Queries.NotRunning.Replace("@table", _tableName), connection))
        //         {
        //             await connection.OpenAsync();

        //             var reader = await command.ExecuteReaderAsync();

        //             while (await reader.ReadAsync())
        //             {
        //                 regressions.Add(new Regression
        //                 {
        //                     Scenario = Convert.ToString(reader["Scenario"]),
        //                     Hardware = Convert.ToString(reader["Hardware"]),
        //                     OperatingSystem = Convert.ToString(reader["OperatingSystem"]),
        //                     Scheme = Convert.ToString(reader["Scheme"]),
        //                     WebHost = Convert.ToString(reader["WebHost"]),
        //                     DateTimeUtc = (DateTimeOffset)(reader["LastDateTime"]),
        //                 });
        //             }
        //         }
        //     }

        //     return regressions;
        // }

        // private static async Task CreateNotRunningIssue(IEnumerable<Regression> regressions)
        // {
        //     if (regressions == null || !regressions.Any())
        //     {
        //         return;
        //     }

        //     var client = new GitHubClient(_productHeaderValue);
        //     client.Credentials = _credentials;

        //     var body = new StringBuilder();
        //     body.Append("Some scenarios have stopped running:");

        //     body.AppendLine();
        //     body.AppendLine();
        //     body.AppendLine("| Scenario | Environment | Last Run |");
        //     body.AppendLine("| -------- | ----------- | -------- |");

        //     foreach (var r in regressions.OrderBy(x => x.Scenario).ThenBy(x => x.DateTimeUtc))
        //     {
        //         body.AppendLine($"| {r.Scenario} | {r.OperatingSystem}, {r.Hardware}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} |");
        //     }

        //     body
        //         .AppendLine()
        //         .AppendLine("[Logs](https://dev.azure.com/dnceng/internal/_build?definitionId=825&_a=summary)")
        //         ;

        //     var title = "Scenarios are not running: " + String.Join(", ", regressions.Select(x => x.Scenario).Take(5));

        //     if (regressions.Count() > 5)
        //     {
        //         title += " ...";
        //     }

        //     var createIssue = new NewIssue(title)
        //     {
        //         Body = body.ToString()
        //     };

        //     createIssue.Labels.Add("Perf");
        //     createIssue.Labels.Add("perf-not-running");

        //     AssignTags(createIssue, regressions.Select(x => x.Scenario));

        //     Console.WriteLine(createIssue.Body);
        //     Console.WriteLine(String.Join(", ", createIssue.Labels));

        //     var issue = await client.Issue.Create(_repositoryId, createIssue);
        // }

        // private static async Task<IEnumerable<Regression>> FindErrors()
        // {
        //     var regressions = new List<Regression>();

        //     using (var connection = new SqlConnection(_options.ConnectionString))
        //     {
        //         using (var command = new SqlCommand(Queries.Error.Replace("@table", _tableName), connection))
        //         {
        //             await connection.OpenAsync();

        //             var reader = await command.ExecuteReaderAsync();

        //             while (await reader.ReadAsync())
        //             {
        //                 regressions.Add(new Regression
        //                 {
        //                     Scenario = Convert.ToString(reader["Scenario"]),
        //                     Hardware = Convert.ToString(reader["Hardware"]),
        //                     OperatingSystem = Convert.ToString(reader["OperatingSystem"]),
        //                     Scheme = Convert.ToString(reader["Scheme"]),
        //                     WebHost = Convert.ToString(reader["WebHost"]),
        //                     DateTimeUtc = (DateTimeOffset)(reader["LastDateTime"]),
        //                     Errors = Convert.ToInt32(reader["Errors"]),
        //                 });
        //             }
        //         }
        //     }

        //     return regressions;
        // }

        // private static async Task CreateErrorsIssue(IEnumerable<Regression> regressions)
        // {
        //     if (regressions == null || !regressions.Any())
        //     {
        //         return;
        //     }

        //     var client = new GitHubClient(_productHeaderValue);
        //     client.Credentials = _credentials;

        //     var body = new StringBuilder();
        //     body.Append("Some scenarios return errors:");

        //     body.AppendLine();
        //     body.AppendLine();
        //     body.AppendLine("| Scenario | Environment | Last Run | Errors |");
        //     body.AppendLine("| -------- | ----------- | -------- | ------ |");

        //     foreach (var r in regressions.OrderBy(x => x.Scenario).ThenBy(x => x.DateTimeUtc))
        //     {
        //         body.AppendLine($"| {r.Scenario} | {r.OperatingSystem}, {r.Hardware}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} | {r.Errors} |");
        //     }

        //     body
        //         .AppendLine()
        //         .AppendLine("[Logs](https://dev.azure.com/dnceng/internal/_build?definitionId=825&_a=summary)")
        //         ;

        //     var title = "Bad responses: " + String.Join(", ", regressions.Select(x => x.Scenario).Take(5));

        //     if (regressions.Count() > 5)
        //     {
        //         title += " ...";
        //     }

        //     var createIssue = new NewIssue(title)
        //     {
        //         Body = body.ToString()
        //     };

        //     createIssue.Labels.Add("Perf");
        //     createIssue.Labels.Add("perf-bad-response");

        //     AssignTags(createIssue, regressions.Select(x => x.Scenario));

        //     Console.WriteLine(createIssue.Body);
        //     Console.WriteLine(String.Join(", ", createIssue.Labels));

        //     var issue = await client.Issue.Create(_repositoryId, createIssue);
        // }

        /// <summary>
        /// Returns the issues from the past <see cref="RecentIssuesTimeSpan"/>
        /// </summary>
        private static async Task<IReadOnlyList<Issue>> GetRecentIssues(Source source)
        {
            if (_recentIssues != null)
            {
                return _recentIssues;
            }

            if (_options.Debug)
            {
                return Enumerable.Empty<Issue>().ToArray();
            }

            var recently = new RepositoryIssueRequest
            {
                Filter = IssueFilter.Created,
                State = ItemStateFilter.All,
                Since = DateTimeOffset.Now.AddDays(0 - source.DaysOfRecentIssues)
            };

            var issues = await GetClient().Issue.GetAllForRepository(_options.RepositoryId, recently);

            return _recentIssues = issues;
        }

        /// <summary>
        /// Updates and closes existing issues based on regressions found.
        /// </summary>
        /// <returns>
        /// The remaining issues that haven't been reported yet.
        /// </returns>
        private static async Task<IEnumerable<Regression>> UpdateIssues(IEnumerable<Regression> regressions, Source source, string template)
        {
            if (!regressions.Any())
            {
                return regressions;
            }

            if (_options.Debug)
            {
                return regressions;
            }

            var issues = await GetRecentIssues(source);

            Console.WriteLine($"Downloaded {issues.Count()} issues");
            
            // The list of regressions that remain to be reported
            var regressionsToReport = new List<Regression>(regressions);

            foreach (var issue in issues)
            {
                if (String.IsNullOrWhiteSpace(issue.Body))
                {
                    continue;
                }

                // For each issue, extract the regression and update their status (fixed).
                // If all regressions are fixed, close the issue.

                var regressionSummaries = ExtractRegressionsBlock(issue.Body)?.ToDictionary(x => x.Identifier, x => x);

                if (regressionSummaries == null)
                {
                    continue;
                }

                Console.WriteLine($"Checking issue {issue.Url}");

                // Find all regressions that are reported in this issue.

                var allRegressionsInIssue = regressions
                    .Where(x => regressionSummaries.Keys.Contains(x.Identifier))
                    .ToList();                

                // Remove all regressions that were found so they are not reported again.

                foreach (var r in allRegressionsInIssue)
                {
                    regressionsToReport.Remove(r);
                }

                if (allRegressionsInIssue.Count() == regressionSummaries.Count())
                {
                    // We could find all the regressions from this issue.
                    // We can attempt to update it.

                    var issueNeedsUpdate = allRegressionsInIssue.Any(x => x.HasRecovered != regressionSummaries[x.Identifier].HasRecovered);

                    if (issueNeedsUpdate)
                    {
                        Console.WriteLine("Updating issue...");
                        var update = issue.ToUpdate();

                        update.Body = await CreateIssueBody(regressions, template);

                        // If all regressions have recovered, close it
                        if (allRegressionsInIssue.All(x => x.HasRecovered))
                        {
                            Console.WriteLine("All regression have recovered, closing the issue");
                            update.State = ItemState.Closed;
                        }

                        if (!_options.ReadOnly)
                        {
                            await GetClient().Issue.Update(_options.RepositoryId, issue.Number, update);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Issue doesn't need to be updated");
                    }
                }
            }

            // Exclude all un-recovered issues to be reported on new issues
            regressionsToReport = regressionsToReport.Where(x => !x.HasRecovered).ToList();

            return regressionsToReport;
        }

        private static GitHubClient GetClient()
        {
            if (_githubClient == null)
            {
                _githubClient = new GitHubClient(ClientHeader);
                _githubClient.Credentials = _credentials;
            }

            return _githubClient;
        }

        private static string AddOwners(string body, IEnumerable<Regression> regressions)
        {
            // Use hashsets to handle duplicates
            var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var regression in regressions)
            {
                foreach (var owner in regression.Owners)
                {
                    if (!String.IsNullOrWhiteSpace(owner))
                    {
                        owners.Add(owner);
                    }
                }
            }

            if (owners.Any())
            {
                body += $"\n\n";
            }

            foreach (var owner in owners)
            {
                body += $"@{owner}\n";
            }

            return body;
        }

        private static void TagIssue(NewIssue issue, IEnumerable<Regression> regressions)
        {
            // Use hashsets to handle duplicates
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var regression in regressions)
            {
                foreach (var label in regression.Labels)
                {
                    if (!String.IsNullOrWhiteSpace(label))
                    {
                        labels.Add(label);
                    }
                }
            }

            foreach (var label in labels)
            {
                issue.Labels.Add(label);
            }
        }
        
        private static string RegressionsPrefix = "{Regressions}";
        private static string RegressionsSuffix = "{/Regressions}";
        
        private static string CreateRegressionsBlock(IEnumerable<Regression> regressions)
        {
            // A custom Base64 payload is injected as a comment in the issue such that we
            // can come back on the issue to update its content

            var summaries = regressions.Select(x => new RegressionSummary { Identifier = x.Identifier, HasRecovered = x.HasRecovered });
            var json = JsonConvert.SerializeObject(summaries, Formatting.None);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return $"<!-- {RegressionsPrefix}{base64}{RegressionsSuffix} -->";
        }

        private static IEnumerable<RegressionSummary> ExtractRegressionsBlock(string body)
        {
            var start = body.IndexOf(RegressionsPrefix);

            if (start == -1)
            {
                return null;
            }

            start = start + RegressionsPrefix.Length + 1;

            var end = body.IndexOf(RegressionsSuffix, start);

            if (end == -1)
            {
                return null;
            }

            var base64 = body.Substring(start, end - start);

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return JsonConvert.DeserializeObject<RegressionSummary[]>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
