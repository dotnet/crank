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
using System.Text.RegularExpressions;
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
        private static GitHubClient _githubClient = GitHubHelper.CreateClient(Credentials.Anonymous);

        private static Configuration _configuration;
        private static BotOptions _options;
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        // Any comment made by this bot should contain the thumbprint to detect benchmark commands have already been processed 
        private const string Thumbprint = "<!-- pullrequestthumbprint -->";

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
                new Option<string>(
                    "--workspace",
                    "The folder used to clone the repository. Defaut is temp folder."),
                new Option<string>(
                    "--benchmarks",
                    "The benchmarks to run."),
                new Option<string>(
                    "--profiles",
                    "The profiles to run the benchmarks one."),
                new Option<string>(
                    "--components",
                    "The components to build."),
                new Option<int>(
                    "--limit",
                    "The maximum number of commands to execute. 0 for unlimited."),
                new Option<string>(
                    "--repository",
                    "The repository for which pull-request comments should be scanned, e.g., https://github.com/dotnet/aspnetcore, dotnet/aspnetcore"),
                new Option<string>(
                    "--pull-request",
                    "The Pull Request url or id to benchmark, e.g., https://github.com/dotnet/aspnetcore/pull/39527, 39527"),
                new Option<string>(
                    "--publish-results",
                    "Publishes the results on the original PR."),
                new Option<string>(
                    "--access-token",
                    "The GitHub account access token. (Secured)"),
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

            string owner = null, name = null;

            if (options.Repository != null)
            {
                var segments = options.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    throw new ArgumentException("Invalid argument --repository");
                }

                name = segments[segments.Length - 1];
                owner = segments[segments.Length - 2];

                if (name.EndsWith(".git"))
                {
                    name = name.Substring(0, name.Length - 4);
                }
            }

            if (options.PullRequest != null)
            {
                int value;

                // https://github.com/dotnet/aspnetcore/pull/39527
                var match = Regex.Match(options.PullRequest, @".*/github.com/([\w-]+)/([\w-]+)/pull/(\d+)");

                if (match.Success)
                {
                    owner = match.Groups[1].Value;
                    name = match.Groups[2].Value;

                    value = int.Parse(match.Groups[3].Value);
                }
                else if (!int.TryParse(options.PullRequest, out value))
                {
                    Console.WriteLine("Invalid pull-request argument");

                    return 1;
                }

                if (owner == null || name == null)
                {
                    throw new ArgumentException("Missing repository information");
                }

                PullRequest pr;

                try
                {
                    pr = await _githubClient.PullRequest.Get(owner, name, value);
                }
                catch (NotFoundException)
                {
                    Console.WriteLine("Pull request not found");

                    return -1;
                }

                var benchmarkNames = options.Benchmarks.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var profileNames = options.Profiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var buildNames = options.Components.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (!ArgumentsValid(benchmarkNames, profileNames, buildNames, markdown:false, out var help))
                {
                    Console.WriteLine(ApplyThumbprint(help));

                    return -1;
                }

                var command = new Command { PullRequest = pr, Benchmarks = benchmarkNames, Profiles = profileNames, Builds = buildNames };

                var results = await RunBenchmark(command);

                if (options.PublishResults)
                {
                    var text = new StringBuilder();

                    foreach (var result in results)
                    {
                        text.AppendLine(FormatResult(result));
                    }

                    await UpdateAuthenticatedClient();

                    var issueComment = await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(text.ToString()));

                    Console.WriteLine($"Results published at {issueComment.HtmlUrl}");
                }
            }
            else
            {
                Console.WriteLine($"Scanning for benchmark requests in {owner}/{name}.");

                var count = 0;

                await foreach (var command in GetPullRequestCommands(owner, name))
                {
                    if (options.Limit > 0 && count > options.Limit)
                    {
                        break;
                    }

                    count++;

                    var pr = command.PullRequest;

                    try
                    {
                        await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint($"Benchmark started for __{String.Join(", ", command.Benchmarks)}__ on __{String.Join(", ", command.Profiles)}__ with __{String.Join(", ", command.Builds)}__"));

                        var results = await RunBenchmark(command);

                        if (options.PublishResults)
                        {
                            var text = new StringBuilder();

                            foreach (var result in results)
                            {
                                text.AppendLine(FormatResult(result));
                            }

                            await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(text.ToString()));
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorCommentText = $"Failed to benchmark PR #{command.PullRequest.Number}. Skipping... Details:\n```\n{ex}\n```";
                        Console.WriteLine($"Benchmark error comment: {errorCommentText}");
                        await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(errorCommentText));
                    }
                }

                Console.WriteLine($"Done scanning for benchmark requests.");
            }

            return 0;
        }

        private static string FormatResult(Result result)
        {
            return $"<details>\n<summary>{result.Benchmark} - {result.Profile}</summary>\n<p>\n\n{result.Output}\n</p>\n</details>";
        }

        private static async Task UpdateAuthenticatedClient()
        {
            if (_githubClient.Credentials == Credentials.Anonymous)
            {
                _githubClient = GitHubHelper.CreateClient(await GitHubHelper.GetCredentialsFromStore());
            }
        }

        private static string ApplyThumbprint(string text)
        {
            return text + "\n\n" + Thumbprint;
        }

        private static async IAsyncEnumerable<Command> GetPullRequestCommands(string owner, string name)
        {
            var prRequest = new PullRequestRequest()
            {
                State = ItemStateFilter.Open,
                SortDirection = SortDirection.Descending,
                SortProperty = PullRequestSort.Updated,
            };

            var prs = await _githubClient.PullRequest.GetAllForRepository(owner, name, prRequest);

            foreach (var pr in prs)
            {
                if (pr.UpdatedAt < CommentCutoffDate)
                {
                    break;
                }

                var comments = await _githubClient.Issue.Comment.GetAllForIssue(owner, name, pr.Number);

                for (var i = comments.Count - 1; i >= 0; i--)
                {
                    var comment = comments[i];

                    if (comment.CreatedAt < CommentCutoffDate)
                    {
                        break;
                    }

                    if (comment.Body.StartsWith(BenchmarkCommand))
                    {
                        if (await _githubClient.Repository.Collaborator.IsCollaborator(pr.Base.Repository.Id, comment.User.Login))
                        {
                            var arguments = comment.Body.Substring(BenchmarkCommand.Length).Trim()
                                .Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            var benchmarkNames = (arguments.Length > 0 ? arguments[0] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                            var profileNames = (arguments.Length > 1 ? arguments[1] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                            var buildNames = (arguments.Length > 2 ? arguments[2] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);

                            if (!ArgumentsValid(benchmarkNames, profileNames, buildNames, markdown: true, out var help))
                            {
                                await UpdateAuthenticatedClient();

                                await _githubClient.Issue.Comment.Create(owner, name, pr.Number, ApplyThumbprint(help));

                                yield break;
                            }

                            // Default command values
                            yield return new Command
                            {
                                Benchmarks = benchmarkNames.Any() ? benchmarkNames : new[] { _configuration.Benchmarks.First().Key },
                                Profiles = profileNames.Any() ? profileNames : new[] { _configuration.Profiles.First().Key },
                                Builds = buildNames.Any() ? buildNames : new[] { _configuration.Builds.First().Key },
                                PullRequest = pr,
                            };
                        }
                        else
                        {
                            Console.WriteLine($"The user '{comment.User.Login}' is not allowed to perform this action.");
                        }
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

        public static bool ArgumentsValid(string[] benchmarkNames, string[] profileNames, string[] buildNames, bool markdown, out string help)
        {
            if ((!benchmarkNames.Any() || benchmarkNames.Any(x => !_configuration.Benchmarks.ContainsKey(x)))
                    || (!profileNames.Any() || profileNames.Any(x => !_configuration.Profiles.ContainsKey(x)))
                    || (!buildNames.Any() || buildNames.Any(x => !_configuration.Builds.ContainsKey(x))))
            {
                // Render help

                help =
                    "Crank - Pull Request Bot\n" +
                    "\n" +
                    "`/benchmark <benchmarks[,...]> <profiles[,...]> <components,[...]>`\n"
                    ;

                help += $"\nBenchmarks: \n";
                foreach (var entry in _configuration.Benchmarks)
                {
                    help += $"- `{entry.Key}`: {entry.Value.Description}\n";
                }

                help += $"\nProfiles: \n";
                foreach (var entry in _configuration.Profiles)
                {
                    help += $"- `{entry.Key}`: {entry.Value.Description}\n";
                }

                help += $"\nComponents: \n";
                foreach (var entry in _configuration.Builds)
                {
                    help += $"- `{entry.Key}`\n";
                }

                if (!markdown)
                {
                    help = help.Replace("`", "");
                }

                return false;
            }

            help = "";
            return true;
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

        private static async Task<IEnumerable<Result>> RunBenchmark(Command command)
        {
            var results = new List<Result>();

            var runs = command.Profiles.SelectMany(x => command.Benchmarks.Select(y => new Run(x, y)));

            var workspace = _options.Workspace;

            foreach (var run in runs)
            {
                File.Delete($"{workspace}/base.json");
                File.Delete($"{workspace}/head.json");

                var benchmark = _configuration.Benchmarks[run.Benchmark];
                var profile = _configuration.Profiles[run.Profile];
                var buildScript = String.Join(Environment.NewLine, command.Builds.Select(b => _configuration.Builds[b].Script));
                var buildArguments = String.Join(" ", command.Builds.Select(b => _configuration.Builds[b].Arguments));
                var cloneUrl = command.PullRequest.Base.Repository.CloneUrl; // "https://github.com/dotnet/aspnetcore.git";
                var folder = command.PullRequest.Base.Repository.Name; // $"aspnetcore"; // 
                var baseBranch = command.PullRequest.Base.Ref; // "main"; // 
                var prNumber = command.PullRequest.Number; // 39463;

                // Compute a unique clone folder name
                var counter = 1;
                while (File.Exists(Path.Combine(workspace, folder))) { folder = command.PullRequest.Base.Repository.Name + counter++; }

                var cloneFolder = Path.Combine(workspace, folder);

                await ProcessUtil.RunAsync(ProcessUtil.GetScriptHost(), "/c ");

                var script = $@"
dotnet tool install Microsoft.Crank.Controller --version ""0.2.0-*"" --global

cd {workspace}

git clone --recursive {cloneUrl} {cloneFolder}

cd {cloneFolder}
git checkout {baseBranch}
{buildScript}

crank {_configuration.Defaults} {benchmark.Arguments} {profile.Arguments} {buildArguments} --json ""{workspace}/base.json""

cd {cloneFolder}
git fetch origin pull/{prNumber}/head
git config --global user.name ""user""
git config --global user.email ""user@company.com""
git merge FETCH_HEAD
{buildScript}

crank {_configuration.Defaults} {benchmark.Arguments} {profile.Arguments} {buildArguments} --json ""{workspace}/head.json""

";
                var scriptFilename = Path.Combine(Path.GetTempPath(), "benchmark" + ProcessUtil.GetEnvironmentCommand(".cmd", ".sh"));

                File.WriteAllText(scriptFilename, script);

                try
                {
                    await ProcessUtil.RunAsync(ProcessUtil.GetScriptHost(), $"/c {scriptFilename}", log: true);

                    var result = await ProcessUtil.RunAsync("crank", $"compare {workspace}/base.json {workspace}/head.json", log: true, captureOutput: true);

                    File.Delete(scriptFilename);

                    results.Add(new Result(run.Profile, run.Benchmark, result.StandardOutput));
                }
                finally
                {
                    // Clean git clones
                    try
                    {
                        Directory.Delete(cloneFolder);
                    }
                    catch
                    {
                        // Fail to delete the clone folder, continue
                        Console.WriteLine($"Failed to clean {cloneFolder}. Ignoring...");
                    }
                }                
            }

            return results;
        }
    }

    public record class Run(string Profile, string Benchmark);
    public record class Result(string Profile, string Benchmark, string Output);
}
