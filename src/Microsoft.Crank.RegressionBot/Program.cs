// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Manatee.Json;
using Manatee.Json.Schema;
using MessagePack;
using Microsoft.Crank.RegressionBot.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.RegressionBot
{
    class Program
    {

        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        private static readonly MessagePackSerializerOptions UncompressedSerializationOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options;
        private static readonly MessagePackSerializerOptions CompressedSerializationOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);

        static BotOptions _options;
        static IReadOnlyList<Issue> _recentIssues;

        static TemplateOptions _templateOptions = new TemplateOptions();
        static FluidParser _fluidParser = new FluidParser();
        
        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);

            _templateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;

            // When a property of a JObject value is accessed, try to look into its properties
            _templateOptions.MemberAccessStrategy.Register<JObject, object>((source, name) => source[name]);

            // Convert JToken to FluidValue
            _templateOptions.ValueConverters.Add(x => x is JObject o ? new ObjectValue(o) : null);
            _templateOptions.ValueConverters.Add(x => x is JValue v ? v.Value : null);
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
                    "The GitHub account username. e.g., 'pr-benchmarks[bot]'"),
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
                    GitHubHelper.GetCredentialsForUser(options);
                }
                else
                {
                    await GitHubHelper.GetCredentialsForAppAsync(options);
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

                // The result of updating issues is the list of regressions that are not yet reported
                var newRegressions = await UpdateIssues(regressions, s, template);

                Console.WriteLine($"{newRegressions.Count()} of them were not reported");

                // Exclude all regressions that have recovered, and have not been reported
                newRegressions = newRegressions.Where(x => !x.HasRecovered).ToArray();

                Console.WriteLine($"{newRegressions.Count()} of them have not recovered");

                // Group issues by trend, such that all improvements are in the same issue
                var positiveRegressions = newRegressions.Where(x => x.Change >= 0).ToArray();
                var negativeRegressions = newRegressions.Where(x => x.Change < 0).ToArray();

                if (newRegressions.Any())
                {
                    Console.WriteLine("Reporting new regressions...");

                    foreach (var regressionSet in new[] { positiveRegressions, negativeRegressions })
                    {
                        if (_options.Verbose)
                        {
                            Console.WriteLine(JsonConvert.SerializeObject(regressionSet, Formatting.None));
                        }

                        var skip = 0;
                        var pageSize = 3; // Create issues with 3 regressions per issue max, due to Issue body size limitations (65KB)

                        while (true)
                        {                            
                            var page = regressionSet.Skip(skip).Take(pageSize);

                            if (!page.Any()) break;

                            await CreateRegressionIssue(page, s.Regressions.Title, template);
                            skip += pageSize;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No new regressions were found.");
                }
            }

            return 0;
        }

        private static async Task<string> CreateIssueBody(IEnumerable<Regression> regressions, string template)
        {
            var report = new Report
            {
                Regressions = regressions.OrderBy(x => x.CurrentResult.Scenario).ThenBy(x => x.CurrentResult.DateTimeUtc).ToList()
            };

            // The base64 encoded MessagePack-serialized model
            var regressionBlock = CreateRegressionsBlock(regressions);

            if (!_fluidParser.TryParse(template, out var fluidTemplate, out var errors))
            {   
                Console.WriteLine("Error parsing the template:");
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

                return "";
            }

            var context = new TemplateContext(report, _templateOptions);

            try
            {
                var body = await fluidTemplate.RenderAsync(context);

                body = AddOwners(body, regressions);

                body += regressionBlock;

                if (body.Length > 65536)
                {
                    throw new Exception($"Body too long ({body.Length} > 65536 chars)");
                }

                return body;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred while rendering an issue: {e}");
                Console.WriteLine("[DEBUG] Model used:");
                Console.WriteLine(regressionBlock);

                throw;
            }
        }

        private static async Task<string> CreateIssueTitle(IEnumerable<Regression> regressions, string template)
        {
            var report = new Report
            {
                Regressions = regressions.OrderBy(x => x.CurrentResult.Scenario).ThenBy(x => x.CurrentResult.DateTimeUtc).ToList()
            };

            var title = "";

            if (String.IsNullOrWhiteSpace(template))
            {
                title = "Performance difference: " + String.Join(", ", regressions.Select(x => x.CurrentResult.Scenario).Take(5));

                if (regressions.Count() > 5)
                {
                    title += " ...";
                }
            }
            else
            {
                if (!_fluidParser.TryParse(template, out var fluidTemplate, out var errors))
                {
                    Console.WriteLine("Error parsing the template:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error);
                    }

                    return "";
                }

                var context = new TemplateContext(report, _templateOptions);

                title = await fluidTemplate.RenderAsync(context);
            }

            return title.Trim();
        }

        private static async Task CreateRegressionIssue(IEnumerable<Regression> regressions, string titleTemplate, string bodyTemplate)
        {
            if (regressions == null || !regressions.Any())
            {
                return;
            }

            var body = await CreateIssueBody(regressions, bodyTemplate);

            var title = await CreateIssueTitle(regressions, titleTemplate);

            var createIssue = new NewIssue(title)
            {
                Body = body
            };

            TagIssue(createIssue, regressions);

            if (!_options.ReadOnly) 
            {
                await GitHubHelper.GetClient().Issue.Create(_options.RepositoryId, createIssue);
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
                            localconfiguration.AddFirst(new JProperty("$schema", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.RegressionBot/regressionbot.schema.json"));

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

            if (!source.Table.All(char.IsLetterOrDigit))
            {
                Console.Write("Invalid table name should only contain alphanumeric characters.");

                yield break;
            }

            var loadStartDateTimeUtc = DateTime.UtcNow.AddDays(0 - source.DaysToLoad);
            var detectionMaxDateTimeUtc = DateTime.UtcNow.AddDays(0 - source.DaysToSkip);
            
            var allResults = new List<BenchmarksResult>();

            // Load latest records

            Console.Write("Loading records... ");

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                using (var command = new SqlCommand(string.Format(Queries.Latest, source.Table), connection))
                {
                    command.Parameters.AddWithValue("@startDate", loadStartDateTimeUtc);
                    
                    await connection.OpenAsync();

                    var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var result = new BenchmarksResult
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Excluded = reader["Excluded"] as bool? ?? false, // Handle DBNull values
                            DateTimeUtc = (DateTimeOffset)reader["DateTimeUtc"],
                            Session = Convert.ToString(reader["Session"]),
                            Scenario = Convert.ToString(reader["Scenario"]),
                            Description = Convert.ToString(reader["Description"]),
                            Document = Convert.ToString(reader["Document"]),
                        };

                        if (!result.Excluded)
                        {
                            allResults.Add(result);
                        }                        
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
                        Console.WriteLine();
                        Console.WriteLine($"Evaluating probe {descriptor} {probe.Path} with {results.Count()} results");
                        Console.WriteLine("=============================================================================================");
                        Console.WriteLine();
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

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Values: {JsonConvert.SerializeObject(resultSet.Select(x => x.Value).ToArray())}");
                    }

                    var values = resultSet.Select(x => x.Value).ToArray();

                    // Look for 2 consecutive values that are outside of the threshold, 
                    // subsequent to 3 consecutive values that are inside the threshold.  

                    // 5 is the number of data points necessary to detect a threshold

                    for (var i = 0; i < resultSet.Length - 5; i++)
                    {
                        // Skip the measurement if it's too recent
                        if (resultSet[i].Result.DateTimeUtc >= detectionMaxDateTimeUtc)
                        {
                            continue;
                        }

                        if (_options.Verbose)
                        {
                            Console.WriteLine($"Checking {resultSet[i + 3].Value} at {resultSet[i + 3].Result.DateTimeUtc} with values {JsonConvert.SerializeObject(values.Skip(i).Take(5).ToArray())}");
                        }

                        // Measure stdev by picking the StdevCount results before the currently checked one
                        var stdevs = values.Take(i + 1).TakeLast(source.StdevCount).ToArray();

                        if (stdevs.Length < source.StdevCount && probe.Unit == ThresholdUnits.StDev)
                        {
                            Console.WriteLine($"Not enough values to build a standard deviation: {JsonConvert.SerializeObject(stdevs)}");
                            continue;
                        }

                        // Calculate the stdev from all values up to the verified window
                        double average = stdevs.Average();
                        double sumOfSquaresOfDifferences = stdevs.Sum(val => (val - average) * (val - average));
                        double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / stdevs.Length);
                        
                        if (_options.Verbose)
                        {
                            Console.WriteLine($"Building stdev ({standardDeviation}) from last {source.StdevCount} values {JsonConvert.SerializeObject(stdevs)}");
                        }

                        /*                      checked value (included in stdev)
                         *                      ^                          ______/i+3---------i+4---------
                         *  (stdev results) ----i---------i+1---------i+2/
                         *         
                         *                      <- value1 ->            
                         *                      <------- value2 ------->
                         *                      <--------------- value3 --------->  
                         *                      <------------------------ value4 ------------->
                         */

                        if (standardDeviation == 0)
                        {
                            // We skip measurement with stdev of zero since it could induce divisions by zero, and any change will trigger
                            // a regression
                            Console.WriteLine($"Ignoring measurement with stdev = 0");
                            continue;
                        }
                        
                        var value1 = values[i+1] - values[i];
                        var value2 = values[i+2] - values[i];
                        var value3 = values[i+3] - values[i];
                        var value4 = values[i+4] - values[i];
                        
                        if (_options.Verbose)
                        {
                            Console.WriteLine($"Next values: {values[i + 0]} {values[i + 1]} {values[i + 2]} {values[i + 3]} {values[i + 4]}");
                            Console.WriteLine($"Deviations: {value1:n0} {value2:n0} {value3:n0} {value4:n0} Allowed deviation: {standardDeviation * probe.Threshold:n0}");
                        }

                        var hasRegressed = false;

                        switch (probe.Unit)
                        {
                            case ThresholdUnits.StDev:
                                // factor of standard deviation
                                hasRegressed = Math.Abs(value1) < probe.Threshold * standardDeviation
                                    && Math.Abs(value2) < probe.Threshold * standardDeviation
                                    && Math.Abs(value3) >= probe.Threshold * standardDeviation
                                    && Math.Abs(value4) >= probe.Threshold * standardDeviation
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;
                            case ThresholdUnits.Percent:
                                // percentage of the average of values
                                hasRegressed = Math.Abs(value1) < average * (probe.Threshold / 100)
                                    && Math.Abs(value2) < average * (probe.Threshold / 100)
                                    && Math.Abs(value3) >= average * (probe.Threshold / 100)
                                    && Math.Abs(value4) >= average * (probe.Threshold / 100)
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;                            
                            case ThresholdUnits.Absolute:
                                // absolute deviation
                                hasRegressed = Math.Abs(value1) < probe.Threshold
                                    && Math.Abs(value2) < probe.Threshold
                                    && Math.Abs(value3) >= probe.Threshold
                                    && Math.Abs(value4) >= probe.Threshold
                                    && Math.Sign(value3) == Math.Sign(value4);

                                break;
                            default:
                                break;
                        }
                        
                        var currentValue = values[i + 3];
                        var baseValue = values[i];
                        
                        if (hasRegressed)
                        {
                            var regression = new Regression 
                            {
                                PreviousResult = resultSet[i].Result,
                                CurrentResult = resultSet[i+3].Result,
                                Change = currentValue - baseValue,
                                StandardDeviation = standardDeviation,
                                Average = average
                            };

                            if (_options.Verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Regression detected: {baseValue:n0} to {currentValue:n0} for {regression.Identifier}");
                                Console.ResetColor();
                            }

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
                            // recovered
                            // - if the delta is inside the threshold limits
                            // - the delta is outside the threshold limits but in the opposite sign
                            
                            for (var j = i + 5; j < resultSet.Length; j++)
                            {
                                var nextValue = values[j] - values[i];

                                var hasRecovered = false;

                                // It has recovered if the difference between the first measurement and the current one 
                                // are within the threshold boundaries, or if the value is better (opposite sign).

                                switch (probe.Unit)
                                {
                                    case ThresholdUnits.StDev:
                                        // factor of standard deviation
                                        hasRecovered = Math.Abs(nextValue) < probe.Threshold * standardDeviation || Math.Sign(nextValue) != Math.Sign(value4);

                                        break;
                                    case ThresholdUnits.Percent:
                                        // percentage of the average of values
                                        hasRecovered = Math.Abs(nextValue) < average * (probe.Threshold / 100) || Math.Sign(nextValue) != Math.Sign(value4);
                                        ;

                                        break;                            
                                    case ThresholdUnits.Absolute:
                                        // absolute deviation
                                        hasRecovered = Math.Abs(nextValue) < probe.Threshold || Math.Sign(nextValue) != Math.Sign(value4);

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

                            regression.ComputeChanges();

                            yield return regression;
                        }
                    }
                }

                if (source.Regressions.HealthCheck)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Checking health");
                        Console.WriteLine("=============================================================================================");
                        Console.WriteLine();
                    }

                    var resultSet = results
                        .Select(x => new { Result = x, DateTimeUtc = x.DateTimeUtc })
                        .ToList();

                    // Add NOW as the last value
                    resultSet.Add(new { Result = default(BenchmarksResult), DateTimeUtc = DateTimeOffset.UtcNow });

                    // Find regressions

                    // Can't find a regression if there are less than StdevCount values
                    if (resultSet.Count < source.StdevCount)
                    {
                        if (_options.Verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Not enough data ({resultSet.Count})");
                            Console.ResetColor();
                        }

                        continue;
                    }

                    if (_options.Verbose)
                    {
                        Console.WriteLine($"Values: {JsonConvert.SerializeObject(resultSet.Select(x => x.DateTimeUtc.ToString("G")).ToArray())}");
                    }

                    var values = resultSet.Select(x => x.DateTimeUtc).ToArray();
                    var stdevCount = source.StdevCount;

                    // Calculate average time between runs and standard deviation
                    // i represents the current value to analyze
                    for (var i = stdevCount; i < values.Length; i++)
                    {
                        var intervalsInSeconds = new List<long>();

                        var currentValue = values[i];
                        var previousValue = values[i - 1];

                        Console.WriteLine($"Testing value {currentValue}");

                        // Calculate stdev from previous values to the current one
                        for (var k = i - stdevCount; k < i - 1; k++)
                        {
                            var differenceInSeconds = (values[k + 1].Ticks - values[k].Ticks) / TimeSpan.TicksPerSecond;

                            intervalsInSeconds.Add(differenceInSeconds);
                        }

                        // Calculate the stdev from all values up to the verified window
                        var averageInSeconds = (long)intervalsInSeconds.Average();
                        var sumOfSquaresOfDifferences = intervalsInSeconds.Sum(val => (val - averageInSeconds) * (val - averageInSeconds));
                        var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / stdevCount);

                        if (_options.Verbose)
                        {
                            Console.WriteLine($"Value: {currentValue}, benchmark runs on average every {(int)TimeSpan.FromSeconds(averageInSeconds).TotalMinutes} minutes with a stdev of {(int)TimeSpan.FromSeconds(standardDeviation).TotalMinutes} minutes. Intervals were: {String.Join(',', intervalsInSeconds)}");
                        }

                        // We assume the benchmark is not running if it wasn't triggered for twice the expected delay.
                        // The standard deviation could also be ignore here but since it's available let's take it into account.
                        var changeInSeconds = (currentValue.Ticks - previousValue.Ticks) / TimeSpan.TicksPerSecond;
                        var acceptedChange = 2 * (averageInSeconds + standardDeviation);

                        var hasRegressed = changeInSeconds > acceptedChange;

                        if (hasRegressed)
                        {
                            var regression = new Regression
                            {
                                PreviousResult = results[i - 1],
                                CurrentResult = results[i - 1],
                                Change = (currentValue - previousValue).TotalSeconds,
                                StandardDeviation = standardDeviation,
                                Average = averageInSeconds
                            };

                            if (_options.Verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Downtime detected: {previousValue} to {currentValue} for {regression.Identifier}");
                                Console.ResetColor();
                            }

                            foreach (var rule in rules)
                            {
                                foreach (var label in rule.Labels)
                                {
                                    regression.Labels.Add(label);
                                }

                                foreach (var owner in rule.Owners)
                                {
                                    regression.Owners.Add(owner);
                                }
                            }

                            foreach (var label in source.Regressions.Labels)
                            {
                                regression.Labels.Add(label);
                            }

                            foreach (var owner in source.Regressions.Owners)
                            {
                                regression.Owners.Add(owner);
                            }

                            // If there are subsequent measurements, the benchmark has recovered

                            var hasRecovered = resultSet.Count > i + 1;

                            if (hasRecovered)
                            {
                                regression.RecoveredResult = resultSet[i + 1].Result;

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Recovered on {regression.RecoveredResult.DateTimeUtc}");
                                Console.ResetColor();
                            }

                            regression.ComputeChanges();

                            yield return regression;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the issues from the past
        /// </summary>
        private static async Task<IReadOnlyList<Issue>> GetRecentIssues(Source source)
        {
            if (_recentIssues != null)
            {
                return _recentIssues;
            }

            var recently = new RepositoryIssueRequest
            {
                Creator = _options.Username,
                Filter = IssueFilter.Created,
                State = ItemStateFilter.All,
                Since = DateTimeOffset.Now.AddDays(0 - source.DaysToLoad)
            };

            var issues = await GitHubHelper.GetClient().Issue.GetAllForRepository(_options.RepositoryId, recently);

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

            var issues = await GetRecentIssues(source);

            Console.WriteLine($"Downloaded {issues.Count()} issues");

            // The list of regressions that remain to be reported
            var regressionsToReport = new List<Regression>(regressions).ToDictionary(x => x.Identifier, x => x);

            foreach (var issue in issues)
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"Checking issue: {issue.HtmlUrl}");
                }

                if (String.IsNullOrWhiteSpace(issue.Body))
                {
                    continue;
                }

                // For each issue, extract the regressions and update their status (recovered).
                // If all regressions are recovered, close the issue.

                var existingRegressions = ExtractRegressionsBlock(issue.Body)?.ToDictionary(x => x.Identifier, x => x);

                if (existingRegressions == null)
                {
                    continue;
                }

                // Find all regressions that are reported in this issue, and check if they have recovered

                var issueNeedsUpdate = false;

                // Update local regressions that have recovered

                foreach (var r in regressions)
                {
                    if (existingRegressions.TryGetValue(r.Identifier, out var localRegression))
                    {
                        // If the issue has been reported, exclude it
                        if (regressionsToReport.Remove(r.Identifier))
                        {
                            Console.WriteLine($"Issue already reported {r.CurrentResult.Description} at {r.CurrentResult.DateTimeUtc}");
                        }

                        if (!localRegression.HasRecovered && r.HasRecovered)
                        {
                            Console.WriteLine($"Found update for {r.Identifier}");
                            existingRegressions.Remove(r.Identifier);
                            existingRegressions.Add(r.Identifier, r);
                            issueNeedsUpdate = true;
                        }
                    }
                }

                if (issueNeedsUpdate)
                {
                    Console.WriteLine("Updating issue...");
                    var update = issue.ToUpdate();

                    update.Body = await CreateIssueBody(existingRegressions.Values, template);

                    // If all regressions have recovered, close it
                    if (existingRegressions.Values.All(x => x.HasRecovered))
                    {
                        Console.WriteLine("All regressions have recovered, closing the issue");
                        update.State = ItemState.Closed;
                    }

                    if (!_options.ReadOnly)
                    {
                        await GitHubHelper.GetClient().Issue.Update(_options.RepositoryId, issue.Number, update);
                    }
                }
                else
                {
                    Console.WriteLine("Issue doesn't need to be updated");
                }                
            }

            return regressionsToReport.Values;
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
        
        private static string RegressionsPrefix = "[MSGPACK]";
        private static string RegressionsSuffix = "[/MSGPACK]";
        
        private static string CreateRegressionsBlock(IEnumerable<Regression> regressions)
        {
            // A custom Base64 payload is injected as a comment in the issue such that we
            // can come back on the issue to update its content

            var data = MessagePackSerializer.Serialize(regressions, CompressedSerializationOptions);
            var base64 = Convert.ToBase64String(data);
            return $"<!-- {RegressionsPrefix}{base64}{RegressionsSuffix} -->";
        }

        private static IEnumerable<Regression> ExtractRegressionsBlock(string body)
        {
            var start = body.IndexOf(RegressionsPrefix);

            if (start == -1)
            {
                return null;
            }

            start = start + RegressionsPrefix.Length;

            var end = body.IndexOf(RegressionsSuffix, start);

            if (end == -1)
            {
                return null;
            }

            var base64 = body.Substring(start, end - start);

            try
            {
                var data = Convert.FromBase64String(base64);
                
                Regression[] results;

                // Try deserialing the issues with compression enabled (default). If it fails it might be an old issue that was not compressed
                // This can be removed once old issues are closed.

                try
                {
                    results = MessagePackSerializer.Deserialize<Regression[]>(data, CompressedSerializationOptions);
                }
                catch
                {
                    results = MessagePackSerializer.Deserialize<Regression[]>(data, UncompressedSerializationOptions);
                }

                Console.WriteLine($"Loaded {results.Length} regressions");
                return results;
            }
            catch (Exception e)
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"Error while parsing regressions: {e}");
                }

                return Array.Empty<Regression>();
            }
        }
    }
}
