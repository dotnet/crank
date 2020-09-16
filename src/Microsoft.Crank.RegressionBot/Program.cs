// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Fluid;
using Fluid.Values;
using Manatee.Json.Schema;
using Manatee.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Octokit;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.RegressionBot
{
    class Program
    {
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        static readonly TimeSpan RecentIssuesTimeSpan = TimeSpan.FromDays(8);

        static BotOptions _options;
        static Credentials _credentials;

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
                
                var regressions = await FindRegression(s).ToListAsync();

                if (!regressions.Any())
                {
                    continue;
                }

                Console.WriteLine("Excluding the ones already reported...");

                var newRegressions = await RemoveReportedRegressions(regressions, false, r => r.CurrentResult.DateTimeUtc.ToString("u"));

                if (newRegressions.Any())
                {
                    Console.WriteLine("Reporting new regressions...");

                    await CreateRegressionIssue(newRegressions, templates[s.Regressions.Template]);
                }
                else
                {
                    Console.WriteLine("No new regressions where found.");
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

        private static async Task CreateRegressionIssue(IEnumerable<Regression> regressions, string template)
        {
            if (regressions == null || !regressions.Any())
            {
                return;
            }

            // var client = new GitHubClient(_productHeaderValue);
            // client.Credentials = _credentials;

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

                return;
            }

            var context = new TemplateContext { Model = report };

            var result = await fluidTemplate.RenderAsync(context);
            
            // foreach (var r in regressions.OrderBy(x => x.Result.Scenario).ThenBy(x => x.Result.DateTimeUtc))
            // {
            //     body.AppendLine(r.Result.Scenario);
            //     body.AppendLine(r.Result.DateTimeUtc.ToString());

            //     // body.AppendLine();
            //     // body.AppendLine();
            //     // body.AppendLine("| Scenario | Environment | Date | Old RPS | New RPS | Change | Deviation |");
            //     // body.AppendLine("| -------- | ----------- | ---- | ------- | ------- | ------ | --------- |");

            //     // var prevRPS = r.Values.Skip(2).First();
            //     // var rps = r.Values.Last();
            //     // var change = Math.Round((double)(rps - prevRPS) / prevRPS * 100, 2);
            //     // var deviation = Math.Round((double)(rps - prevRPS) / r.Stdev, 2);

            //     // body.AppendLine($"| {r.Scenario} | {r.OperatingSystem}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} | {prevRPS.ToString("n0")} | {rps.ToString("n0")} | {change} % | {deviation} σ |");


            //     // body.AppendLine();
            //     // body.AppendLine("Before versions:");

            //     // body.AppendLine($"ASP.NET Core __{r.PreviousAspNetCoreVersion}__");
            //     // body.AppendLine($".NET Core __{r.PreviousDotnetCoreVersion}__");

            //     // body.AppendLine();
            //     // body.AppendLine("After versions:");

            //     // body.AppendLine($"ASP.NET Core __{r.CurrentAspNetCoreVersion}__");
            //     // body.AppendLine($".NET Core __{r.CurrentDotnetCoreVersion}__");

            //     // var aspNetChanged = r.PreviousAspNetCoreVersion != r.CurrentAspNetCoreVersion;
            //     // var runtimeChanged = r.PreviousDotnetCoreVersion != r.CurrentDotnetCoreVersion;

            //     // if (aspNetChanged || runtimeChanged)
            //     // {
            //     //     body.AppendLine();
            //     //     body.AppendLine("Commits:");

            //     //     if (aspNetChanged)
            //     //     {
            //     //         if (r.AspNetCoreHashes != null && r.AspNetCoreHashes.Length == 2 && r.AspNetCoreHashes[0] != null && r.AspNetCoreHashes[1] != null)
            //     //         {
            //     //             body.AppendLine();
            //     //             body.AppendLine("__ASP.NET Core__");
            //     //             body.AppendLine($"https://github.com/dotnet/aspnetcore/compare/{r.AspNetCoreHashes[0]}...{r.AspNetCoreHashes[1]}");
            //     //         }
            //     //     }

            //     //     if (runtimeChanged)
            //     //     {
            //     //         if (r.DotnetCoreHashes != null && r.DotnetCoreHashes.Length == 2 && r.DotnetCoreHashes[0] != null && r.DotnetCoreHashes[1] != null)
            //     //         {
            //     //             body.AppendLine();
            //     //             body.AppendLine("__.NET Core__");
            //     //             body.AppendLine($"https://github.com/dotnet/runtime/compare/{r.DotnetCoreHashes[0]}...{r.DotnetCoreHashes[1]}");
            //     //         }
            //     //     }
            //     // }
            // }


            // body
            //     .AppendLine()
            //     .AppendLine("[Logs](https://dev.azure.com/dnceng/internal/_build?definitionId=825&_a=summary)")
            //     ;

            // var title = "Performance regression: " + String.Join(", ", regressions.Select(x => x.Scenario).Take(5));

            // if (regressions.Count() > 5)
            // {
            //     title += " ...";
            // }

            // var createIssue = new NewIssue(title)
            // {
            //     Body = body.ToString()
            // };

            // createIssue.Labels.Add("Perf");
            // createIssue.Labels.Add("perf-regression");

            // AssignTags(createIssue, regressions.Select(x => x.Scenario));

            // Console.WriteLine(createIssue.Body);
            // Console.WriteLine(String.Join(", ", createIssue.Labels));

            // var issue = await client.Issue.Create(_repositoryId, createIssue);

            if (_options.Debug || _options.Verbose)
            {
                Console.WriteLine(result.ToString());
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
        /// Filters out scenarios that have already been reported.
        /// </summary>
        /// <param name="regressions">The regressions to find in existing issues.</param>
        /// <param name="ignoreClosedIssues">True to report a scenario even if it's in an existing issue that is closed.</param>
        /// <param name="textToFind">The formatted text to find in an issue.</param>
        /// <returns></returns>
        private static async Task<IEnumerable<Regression>> RemoveReportedRegressions(IEnumerable<Regression> regressions, bool ignoreClosedIssues, Func<Regression, string> textToFind)
        {
            if (!regressions.Any())
            {
                return regressions;
            }

            await Task.Delay(0);

            return regressions;

            // var issues = await GetRecentIssues();

            // // The list of regressions that will actually be reported
            // var filtered = new List<Regression>();

            // // Look for the same timestamp in all reported issues
            // foreach (var r in regressions)
            // {
            //     // When filter is false the regression is kept
            //     var filter = false;

            //     foreach (var issue in issues)
            //     {
            //         // If ignoreClosedIssues is true, we don't remove scenarios from closed issues.
            //         // It means that if an issue is already reported in a closed issue, it won't be filtered, hence it will be reported,
            //         // and closing an issue allows the bot to repeat itself and reopen the scenario

            //         if (ignoreClosedIssues && issue.State == ItemState.Closed)
            //         {
            //             continue;
            //         }

            //         if (issue.Body != null && issue.Body.Contains(textToFind(r)))
            //         {
            //             filter = true;
            //             break;
            //         }

            //         if (_ignoredScenarios.Contains(r.Scenario))
            //         {
            //             filter = true;
            //             break;
            //         }
            //     }

            //     if (!filter)
            //     {
            //         filtered.Add(r);
            //     }
            // }

            // return filtered;
        }

        // private static void AssignTags(NewIssue issue, IEnumerable<string> scenarios)
        // {
        //     // Use hashsets to handle duplicates
        //     var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //     var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        //     foreach (var scenario in scenarios)
        //     {
        //         foreach (var tag in Tags.Match(scenario))
        //         {
        //             foreach (var label in tag.Labels)
        //             {
        //                 if (!String.IsNullOrWhiteSpace(label))
        //                 {
        //                     labels.Add(label);
        //                 }
        //             }

        //             foreach (var owner in tag.Owners)
        //             {
        //                 owners.Add(owner);
        //             }
        //         }
        //     }

        //     foreach (var label in labels)
        //     {
        //         issue.Labels.Add(label);
        //     }

        //     if (owners.Any())
        //     {
        //         issue.Body += $"\n\n";
        //     }

        //     foreach (var owner in owners)
        //     {
        //         issue.Body += $"@{owner}\n";
        //     }
        // }

        
    }
}
