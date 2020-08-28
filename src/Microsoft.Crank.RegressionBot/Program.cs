// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace Microsoft.Crank.RegressionBot
{
    class Program
    {
        // Used as the GitHub client agent
        private const string AppName = "crank-bot";

        static readonly TimeSpan RecentIssuesTimeSpan = TimeSpan.FromDays(8);
        static readonly TimeSpan GitHubJwtTimeout = TimeSpan.FromMinutes(5);

        static long _repositoryId;
        static string _accessToken;
        static string _username;
        static string _githubAppId;
        static string _githubAppKey;
        static string _githubAppInstallationId;
        static Credentials _credentials;
        static string _connectionString;
        static bool _debug;

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "CRANK_REGRESSION_BOT_")
                .AddCommandLine(args)
                .Build();

            await LoadSettings(config);

            // Regressions

            Console.WriteLine("Looking for regressions...");

            foreach (var source in DefaultSources.Value.Sources)
            {
                var regressions = await FindRegression(source).ToListAsync();

                Console.WriteLine("Excluding the ones already reported...");

                var newRegressions = await RemoveReportedRegressions(regressions, false, r => r.Result.DateTimeUtc.ToString("u"));

                // await PopulateHashes(newRegressions);

                if (newRegressions.Any())
                {
                    Console.WriteLine("Reporting new regressions...");

                    await CreateRegressionIssue(newRegressions);
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
        }

        private static async Task LoadSettings(IConfiguration config)
        {
            // Tip: The repository id can be found using this endpoint: https://api.github.com/repos/dotnet/aspnetcore

            long.TryParse(config["RepositoryId"], out _repositoryId);
            _accessToken = config["AccessToken"];
            _username = config["Username"];
            _githubAppKey = config["GitHubAppKey"];
            _githubAppId = config["GitHubAppId"];
            _githubAppInstallationId = config["GitHubInstallationId"];

            _connectionString = config["ConnectionString"];
            _debug = bool.TryParse(config["Debug"], out _debug) && _debug;

            if (!_debug)
            {
                if (_repositoryId == 0)
                {
                    throw new ArgumentException("RepositoryId argument is missing or invalid");
                }

                if (String.IsNullOrEmpty(_accessToken) && String.IsNullOrEmpty(_githubAppKey))
                {
                    throw new ArgumentException("AccessToken or GitHubAppKey is required");
                }
                else if (!String.IsNullOrEmpty(_githubAppKey))
                {
                    if(String.IsNullOrEmpty(_githubAppId))
                    {
                        throw new ArgumentException("GitHubAppId argument is missing");
                    }

                    if (String.IsNullOrEmpty(_githubAppInstallationId))
                    {
                        throw new ArgumentException("GitHubInstallationId argument is missing");
                    }

                    if (!long.TryParse(_githubAppInstallationId, out var installationId))
                    {
                        throw new ArgumentException("GitHubInstallationId should be a number or is invalid");
                    }

                    _credentials = await GetCredentialsForApp(_githubAppId, _githubAppKey, installationId);
                }
                else
                {
                    if (String.IsNullOrEmpty(_username))
                    {
                        throw new ArgumentException("Username argument is missing");
                    }

                    _credentials = GetCredentialsForUser(_username, _accessToken);
                }

                if (String.IsNullOrEmpty(_connectionString))
                {
                    throw new ArgumentException("ConnectionString argument is missing");
                }
            }
        }

        private static async Task CreateRegressionIssue(IEnumerable<Regression> regressions)
        {
            if (regressions == null || !regressions.Any())
            {
                return;
            }

            // var client = new GitHubClient(_productHeaderValue);
            // client.Credentials = _credentials;

            var body = new StringBuilder();
            body.Append("A performance regression has been detected for the following scenarios:");

            foreach (var r in regressions.OrderBy(x => x.Result.Scenario).ThenBy(x => x.Result.DateTimeUtc))
            {
                body.AppendLine(r.Result.Scenario);
                body.AppendLine(r.Result.DateTimeUtc.ToString());

                // body.AppendLine();
                // body.AppendLine();
                // body.AppendLine("| Scenario | Environment | Date | Old RPS | New RPS | Change | Deviation |");
                // body.AppendLine("| -------- | ----------- | ---- | ------- | ------- | ------ | --------- |");

                // var prevRPS = r.Values.Skip(2).First();
                // var rps = r.Values.Last();
                // var change = Math.Round((double)(rps - prevRPS) / prevRPS * 100, 2);
                // var deviation = Math.Round((double)(rps - prevRPS) / r.Stdev, 2);

                // body.AppendLine($"| {r.Scenario} | {r.OperatingSystem}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} | {prevRPS.ToString("n0")} | {rps.ToString("n0")} | {change} % | {deviation} σ |");


                // body.AppendLine();
                // body.AppendLine("Before versions:");

                // body.AppendLine($"ASP.NET Core __{r.PreviousAspNetCoreVersion}__");
                // body.AppendLine($".NET Core __{r.PreviousDotnetCoreVersion}__");

                // body.AppendLine();
                // body.AppendLine("After versions:");

                // body.AppendLine($"ASP.NET Core __{r.CurrentAspNetCoreVersion}__");
                // body.AppendLine($".NET Core __{r.CurrentDotnetCoreVersion}__");

                // var aspNetChanged = r.PreviousAspNetCoreVersion != r.CurrentAspNetCoreVersion;
                // var runtimeChanged = r.PreviousDotnetCoreVersion != r.CurrentDotnetCoreVersion;

                // if (aspNetChanged || runtimeChanged)
                // {
                //     body.AppendLine();
                //     body.AppendLine("Commits:");

                //     if (aspNetChanged)
                //     {
                //         if (r.AspNetCoreHashes != null && r.AspNetCoreHashes.Length == 2 && r.AspNetCoreHashes[0] != null && r.AspNetCoreHashes[1] != null)
                //         {
                //             body.AppendLine();
                //             body.AppendLine("__ASP.NET Core__");
                //             body.AppendLine($"https://github.com/dotnet/aspnetcore/compare/{r.AspNetCoreHashes[0]}...{r.AspNetCoreHashes[1]}");
                //         }
                //     }

                //     if (runtimeChanged)
                //     {
                //         if (r.DotnetCoreHashes != null && r.DotnetCoreHashes.Length == 2 && r.DotnetCoreHashes[0] != null && r.DotnetCoreHashes[1] != null)
                //         {
                //             body.AppendLine();
                //             body.AppendLine("__.NET Core__");
                //             body.AppendLine($"https://github.com/dotnet/runtime/compare/{r.DotnetCoreHashes[0]}...{r.DotnetCoreHashes[1]}");
                //         }
                //     }
                // }
            }


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

            Console.WriteLine(body.ToString());
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
            var detectionDateTimeUtc = DateTime.UtcNow.AddDays(0 - source.DaysToAnalyze);
            Console.WriteLine(detectionDateTimeUtc);
            
            var allResults = new List<BenchmarksResult>();

            // Load latest records

            using (var connection = new SqlConnection(_connectionString))
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

                var rules = source.Match(descriptor);

                if (!rules.Any())
                {
                    if (_debug)
                    {
                        Console.WriteLine($"No matching rules, descriptor skipped: {descriptor}");
                    }

                    continue;
                }

                if (_debug)
                {
                    Console.WriteLine($"Found matching rules for {descriptor}");
                }

                // Should regressions be ignored for this descriptor?
                var lastIgnoreRegressionRule = rules.LastOrDefault(x => x.IgnoreRegressions != null);

                if (lastIgnoreRegressionRule != null && lastIgnoreRegressionRule.IgnoreRegressions.Value)
                {
                    if (_debug)
                    {
                        Console.WriteLine("Regressions ignored");
                    }
                
                    continue;
                }

                // Resolve path for the metric
                var results = resultsByScenario[descriptor];

                foreach (var probe in source.RegressionProbes)
                {
                    Console.WriteLine($"Evaluating probe {probe.Path} for {results.Count()} benchmarks");

                    var resultSet = results
                        .Select(x => new { Result = x, Token = x.Data.SelectTokens(probe.Path).FirstOrDefault() })
                        .Where(x => x.Token != null)
                        .Select(x => new { Result = x.Result, Value = Convert.ToDouble(x.Token)})
                        .ToArray();

                    // Find regressions

                    // Can't find a regression if there are less than 5 value
                    if (resultSet.Length < 5)
                    {
                        if (_debug)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Not enough data ({resultSet.Length})");
                            Console.ResetColor();
                        }

                        continue;
                    }

                    // Calculate standard deviation
                    var values = resultSet.Select(x => x.Value).ToArray();

                    double average = values.Average();
                    double sumOfSquaresOfDifferences = values.Sum(val => (val - average) * (val - average));
                    double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Length);

                    // Look for 2 consecutive values that are outside of the standard deviations, 
                    // subsequent to 3 consecutive values that are inside the standard deviations.  

                    for (var i = 0; i < resultSet.Length - 5; i++)
                    {
                        // Ignore results before the searched date
                        if (resultSet[i].Result.DateTimeUtc < detectionDateTimeUtc)
                        {
                            continue;
                        }

                        var value1 = Math.Abs(values[i+1] - values[i]);
                        var value2 = Math.Abs(values[i+2] - values[i]);
                        var value3 = Math.Abs(values[i+3] - values[i+2]);
                        var value4 = Math.Abs(values[i+4] - values[i+2]);

                        if (_debug)
                        {
                            Console.WriteLine($"{descriptor} {probe.Path} {resultSet[i+2].Result.DateTimeUtc} {values[i+0]} {values[i+1]} {values[i+2]} {values[i+3]} ({value3}) {values[i+4]} ({value4}) / {standardDeviation * probe.Threshold:n0}");
                        }                        

                        if (value1 < standardDeviation
                            && value2 < standardDeviation
                            && value3 > probe.Threshold * standardDeviation
                            && value4 > probe.Threshold * standardDeviation
                            && Math.Sign(value3) == Math.Sign(value4)
                            )
                        {
                            if (_debug)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Regression");
                                Console.ResetColor();
                            }

                            yield return new Regression 
                            {
                                Result = resultSet[i+2].Result,
                                Deviation = value2,
                                StandardDeviation = standardDeviation
                            };
                        }
                    }
                }
            }
        }

        // private static async Task<IEnumerable<Regression>> FindNotRunning()
        // {
        //     var regressions = new List<Regression>();

        //     using (var connection = new SqlConnection(_connectionString))
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

        //     using (var connection = new SqlConnection(_connectionString))
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

        // private static async Task<bool> DownloadFileAsync(string url, string outputPath, int maxRetries = 3, int timeout = 5)
        // {
        //     for (var i = 0; i < maxRetries; ++i)
        //     {
        //         try
        //         {
        //             var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        //             var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
        //             response.EnsureSuccessStatusCode();

        //             // This probably won't use async IO on windows since the stream
        //             // needs to created with the right flags
        //             using (var stream = File.Create(outputPath))
        //             {
        //                 // Copy the response stream directly to the file stream
        //                 await response.Content.CopyToAsync(stream);
        //             }

        //             return true;
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine($"Error while downloading {url}:");
        //             Console.WriteLine(e);
        //         }
        //     }

        //     return false;
        // }

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

        private static Credentials GetCredentialsForUser(string userName, string token)
        {
            return new Credentials(token);
        }

        private static RsaSecurityKey GetRsaSecurityKeyFromPemKey(string keyText)
        {
            using var rsa = RSA.Create();

            var keyBytes = Convert.FromBase64String(keyText);

            rsa.ImportRSAPrivateKey(keyBytes, out _);

            return new RsaSecurityKey(rsa.ExportParameters(true));
        }

        private static async Task<Credentials> GetCredentialsForApp(string appId, string keyPath, long installId)
        {
            var creds = new SigningCredentials(GetRsaSecurityKeyFromPemKey(_githubAppKey), SecurityAlgorithms.RsaSha256);

            var jwtToken = new JwtSecurityToken(
                new JwtHeader(creds),
                new JwtPayload(
                    issuer: appId,
                    issuedAt: DateTime.Now,
                    expires: DateTime.Now.Add(GitHubJwtTimeout),
                    audience: null,
                    claims: null,
                    notBefore: null));

            var jwtTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            var initClient = new GitHubClient(new ProductHeaderValue(AppName))
            {
                Credentials = new Credentials(jwtTokenString, AuthenticationType.Bearer),
            };

            var installationToken = await initClient.GitHubApps.CreateInstallationToken(installId);
            return new Credentials(installationToken.Token, AuthenticationType.Bearer);
        }
    }
}
