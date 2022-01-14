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
using System.Threading;
using System.Threading.Tasks;
using Manatee.Json;
using Manatee.Json.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using YamlDotNet.Serialization;

namespace Microsoft.Crank.PullRequestBot
{
    public class Program
    {
        private static GitHubClient _githubClient;
        private static Configuration _configuration;
        private static BotOptions _options;
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        // Any comment made by this bot should contain the thumbprint to detect benchmark commands have already been processed 
        private const string Thumbprint = "<!-- pullrequestbotbreadcrumb -->";

        private const string BenchmarkCommand = "/benchmark";

        private static readonly DateTime CommentCutoffDate = DateTime.Now.AddHours(-24);
        private static readonly TimeSpan BenchmarkTimeout = TimeSpan.FromMinutes(30);

        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);
        }

        static async Task<int> Main(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("env:", StringComparison.OrdinalIgnoreCase))
                {
                    args[i] = Environment.GetEnvironmentVariable(args[i].Substring(4));
                }
            }

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
                    "The GitHub account username. e.g., 'pr-benchmarks[bot]'"){ IsRequired = true },
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
                    "--config",
                    "The path to a configuration file.") { IsRequired = true },
                new Option<bool>(
                    "--verbose",
                    "When used, detailed logs are displayed."),
                new Option<bool>(
                    "--read-only",
                    "When used, nothing is written on GitHub."),
            };

            rootCommand.Description = "Crank Pull Requests Bot";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<BotOptions>(Controller);

            // Parse the incoming args and invoke the handler
            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Controller(BotOptions options)
        {
            _options = options;

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

            // Load configuration files

            _configuration = await LoadConfigurationAsync(_options.Config);

            await RunBenchmark(new Command { Environment = "aspnet-perf-lin", Scenario = "plaintext" });

            return 0;

            Console.WriteLine($"Scanning for benchmark requests in {_configuration.Organization}/{_configuration.Repository}.");

            await foreach (var command in GetPullRequestCommands())
            {
                var pr = command.PullRequest;

                try
                {
                    Console.WriteLine($"Requesting {command.Scenario} benchmark for PR #{pr.Number}.");

                    await _githubClient.Issue.Comment.Create(_configuration.Organization, _configuration.Repository, pr.Number, ApplyThumbprint($"Benchmark started for __{command.Scenario}__ on __{command.Environment}__"));

                    var result = await RunBenchmark(command);

                    await _githubClient.Issue.Comment.Create(_configuration.Organization, _configuration.Repository, pr.Number, ApplyThumbprint(result));
                }
                catch (Exception ex)
                {
                    var errorCommentText = $"Failed to benchmark PR #{pr.Number}. Skipping... Details:\n```\n{ex}\n```";
                    Console.WriteLine($"Benchmark error comment: {errorCommentText}");
                    await _githubClient.Issue.Comment.Create(_configuration.Organization, _configuration.Repository, pr.Number, ApplyThumbprint(errorCommentText));
                }
            }

            Console.WriteLine($"Done scanning for benchmark requests.");

            return 0;
        }

        private static string ApplyThumbprint(string text)
        {
            return text + "\n\n" + Thumbprint;
        }

        private static async IAsyncEnumerable<Command> GetPullRequestCommands()
        {
            var prRequest = new PullRequestRequest()
            {
                State = ItemStateFilter.Open,
                SortDirection = SortDirection.Descending,
                SortProperty = PullRequestSort.Updated,
            };

            var prs = await _githubClient.PullRequest.GetAllForRepository(_configuration.Organization, _configuration.Repository, prRequest);

            foreach (var pr in prs)
            {
                if (pr.UpdatedAt < CommentCutoffDate)
                {
                    break;
                }

                var comments = await _githubClient.Issue.Comment.GetAllForIssue(_configuration.Organization, _configuration.Repository, pr.Number);

                for (var i = comments.Count - 1; i >= 0; i--)
                {
                    var comment = comments[i];

                    if (comment.CreatedAt < CommentCutoffDate)
                    {
                        break;
                    }

                    if (comment.Body.StartsWith(BenchmarkCommand) && await _githubClient.Organization.Member.CheckMember(_configuration.Organization, comment.User.Login))
                    {
                        var arguments = comment.Body.Substring(BenchmarkCommand.Length).Trim()
                            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var scenarioName = arguments.Length > 0 ? arguments[0] : null;
                        var environmentName = arguments.Length > 1 ? arguments[1] : null;

                        if ((scenarioName != null && _configuration.Benchmarks.FirstOrDefault(x => x.Name == scenarioName) == null)
                            || (environmentName != null && _configuration.Environments.FirstOrDefault(x => x.Name == environmentName) == null))
                        {
                            // Render help

                            var text =
                                "Crank - Pull Request Bot\n" +
                                "\n" +
                                "/benchmark [<scenario> [<environment>]]\n"
                                ;

                            text += $"\nScenarios: \n";
                            foreach (var entry in _configuration.Benchmarks)
                            {
                                text += $"`{entry.Name}`: {entry.Description}\n";
                            }

                            text += $"\nEnvironments: \n";
                            foreach (var entry in _configuration.Environments)
                            {
                                text += $"`{entry.Name}`: {entry.Description}\n";
                            }

                            await _githubClient.Issue.Comment.Create(_configuration.Organization, _configuration.Repository, pr.Number, ApplyThumbprint(text));

                            yield break;
                        }

                        yield return new Command
                        {
                            Scenario = scenarioName ?? _configuration.Benchmarks.First().Name,
                            Environment = environmentName ?? _configuration.Environments.First().Name,
                            PullRequest = pr,
                        };
                    }
                    else if (comment.Body.Contains(Thumbprint))
                    {
                        // The bot has already commented with results for the most recent benchmark request.
                        break;
                    }
                    else
                    {
                        // Ignore comment
                    }
                }
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
                    throw new PullRequestBotException($"Configuration '{configurationFilenameOrUrl}' could not be loaded.");
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
                            throw new PullRequestBotException($"Error while parsing '{configurationFilenameOrUrl}'\n{e.Message}");
                        }

                        var serializer = new SerializerBuilder()
                            .JsonCompatible()
                            .Build();

                        var json = serializer.Serialize(yamlObject);

                        // Format json in case the schema validation fails and we need to render error line numbers
                        localconfiguration = JObject.Parse(json);

                        var schemaJson = File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "pullrequestbot.schema.json"));
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

                            throw new PullRequestBotException(errorBuilder.ToString());
                        }

                        break;
                    default:
                        throw new PullRequestBotException($"Unsupported configuration format: {configurationExtension}");
                }

                return localconfiguration.ToObject<Configuration>();
            }
            else
            {
                throw new PullRequestBotException($"Invalid file path or url: '{configurationFilenameOrUrl}'");
            }
        }

        private static void RunCommand(string command, TimeSpan timeout)
        {
            Console.WriteLine($"Running command: '{command}'");

            var outputBuilder = new StringBuilder();

            var splitCommand = command.Split(' ', 2);
            var fileName = splitCommand[0];
            var arguments = splitCommand.Length == 2 ? splitCommand[1] : string.Empty;

            using var process = new System.Diagnostics.Process()
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };


            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine($"stdout: {e.Data}");
                    Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine($"stderr: {e.Data}");
                    Console.Error.WriteLine(e.Data);
                }
            };


            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Process '{fileName} {arguments}' exited with exit code '{process.ExitCode}' and the following output:\n\n{outputBuilder}");
            }
        }

        private static async Task<string> RunBenchmark(Command command)
        {
            _configuration.Build = "echo fake build";

            var WORKSPACE = "/temp";

            var scenario = _configuration.Benchmarks.First(x => x.Name == command.Scenario);
            var environment = _configuration.Environments.First(x => x.Name == command.Environment);

            var cloneUrl = "https://github.com/pranavkm/aspnetcore.git"; // command.PullRequest.Head.Repository.CloneUrl
            var folder = $"aspnetcore"; // command.PullRequest.Base.Repository.Name
            var baseBranch = "main"; // command.PullRequest.Base.Ref
            var prid = 39463; // command.PullRequest.Id

            await ProcessUtil.RunAsync("cmd.exe", "/c ");

            var script = $@"
dotnet tool install Microsoft.Crank.Controller --version ""0.2.0-*"" --global

cd {WORKSPACE}

rmdir /s /q {folder}

git clone --recursive {cloneUrl} {WORKSPACE}/{folder}

cd {WORKSPACE}/{folder}
git checkout {baseBranch}
{_configuration.Build}
crank {_configuration.Defaults} {scenario.Value} {environment.Value} --json ""{WORKSPACE}/base.json""

cd {WORKSPACE}/{folder}
git fetch origin pull/{prid}/head
git config --global user.name ""user""
git config --global user.email ""user@company.com""
git merge FETCH_HEAD
{ _configuration.Build}
crank {_configuration.Defaults} {scenario.Value} {environment.Value} --json ""{WORKSPACE}/head.json""

crank compare base.json head.json >> results.txt
";

            File.WriteAllText("script.cmd", script);

            await ProcessUtil.RunAsync("cmd.exe", "/c script.cmd", log: true);

            var result = File.ReadAllText($"{WORKSPACE}/results.txt");

            return result;
        }
    }
}
