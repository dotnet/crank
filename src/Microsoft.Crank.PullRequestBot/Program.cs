// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
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
        private static readonly FluidParser FluidParser = new FluidParser();

        // Any comment made by this bot should contain the thumbprint to detect benchmark commands have already been processed 
        private static string Thumbprint = "<!-- pullrequestthumbprint {0} -->";

        private const string BenchmarkCommand = "/benchmark";
        private const string BaseFilename = "base.json";
        private const string PrFilename = "pr.json";
        private const int CrankMaxRetries = 2; // retry once after an unsuccessful run

        private static DateTime CommentCutoffDate;

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
                    args[i] = Environment.GetEnvironmentVariable(args[i][4..]);
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
                new Option<Uri>(
                    "--github-base-url",
                    "The GitHub base URL if using GitHub Enterprise, e.g., https://github.local"),
                new Option<string>(
                    "--arguments",
                    "Any additional arguments to pass through to crank."),
                new Option<string>(
                    "--config",
                    "The path to a configuration file.") { IsRequired = true },
                new Option<int>(
                    "--age",
                    "The age of the most recent comment to look for in minutes. Default is 60."),
                new Option<Uri>(
                    "--external-log-uri",
                    "Link to the logs (e.g. in AzDO) to publish in the start comment."),
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

            CommentCutoffDate =  DateTime.Now.Subtract(TimeSpan.FromMinutes(_options.Age));

            if (_options.GitHubBaseUrl != null)
            {
                // GitHub Enterprise may require the client to be authenticated,
                // rather than just use a different base address for the API.
                await UpgradeAuthenticatedClient();
            }
            else if (!string.IsNullOrEmpty(_options.AccessToken) || !string.IsNullOrEmpty(_options.AppId) || !string.IsNullOrEmpty(_options.AppKey))
            {
                // If authentication information is provided pre-authenticate since it will prevent some rate limiting exception for read-only API calls.
                await UpgradeAuthenticatedClient();
            }

            // Load configuration files

            _configuration = await LoadConfigurationAsync(_options.Config);

            // Compute a thumbprint for this run before anything uses it
            CreateThumbprint();

            string host = "github.com", owner = null, name = null;

            if (options.Repository != null)
            {
                var segments = options.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    throw new ArgumentException("Invalid argument --repository");
                }

                if (segments.Length >= 4)
                {
                    host = segments[1];
                }

                name = segments[^1];
                owner = segments[^2];

                if (name.EndsWith(".git"))
                {
                    name = name[0..^4];
                }
            }

            if (options.PullRequest != null)
            {
                int value;

                // Parse parts of the pull-request URL, e.g., https://github.com/dotnet/aspnetcore/pull/39527
                var match = Regex.Match(options.PullRequest, $@".*/{host}/([\w-]+)/([\w-]+)/pull/(\d+)");

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

                // Check if the arguments are available in the configuration file

                var benchmarkNames = options.Benchmarks.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var profileNames = options.Profiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var componentNames = options.Components.Split(',', StringSplitOptions.RemoveEmptyEntries);

                benchmarkNames = benchmarkNames.Intersect(_configuration.Benchmarks.Keys).ToArray();
                profileNames = profileNames.Intersect(_configuration.Profiles.Keys).ToArray();
                componentNames = componentNames.Intersect(_configuration.Components.Keys).ToArray();

                if (!benchmarkNames.Any() || !profileNames.Any() || !componentNames.Any())
                {
                    Console.WriteLine("Arguments don't match a valid command.");
                    return 0;
                }

                var command = new Command { PullRequest = pr, Benchmarks = benchmarkNames, Profiles = profileNames, Components = componentNames };

                try
                {
                    var results = await RunBenchmark(command, options.AccessToken);

                    if (options.PublishResults)
                    {
                        var text = new StringBuilder();

                        foreach (var result in results)
                        {
                            text.AppendLine(FormatResult(result));
                        }

                        await UpgradeAuthenticatedClient();

                        var issueComment = await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(text.ToString()));

                        Console.WriteLine($"Results published at {issueComment.HtmlUrl}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to benchmark PR #{command.PullRequest.Number}. Skipping... Details:\n```\n{ex}\n```");
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

                    Console.WriteLine($"Scanning for benchmark requests in {owner}/{name}.");

                    try
                    {
                        var startComment = $"Benchmark started for __{string.Join(", ", command.Benchmarks)}__ on __{string.Join(", ", command.Profiles)}__ with __{string.Join(", ", command.Components)}__";
                        if (command.Arguments != null)
                        {
                            startComment += $" and arguments `{command.Arguments}`";
                        }
                        if (_options.ExternalLogUri != null)
                        {
                            startComment += $". Logs: [link]({_options.ExternalLogUri.OriginalString})";
                        }
                        
                        await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(startComment));

                        var results = await RunBenchmark(command, options.AccessToken);

                        if (options.PublishResults)
                        {
                            Console.WriteLine("Publishing results");

                            try
                            {
                                var text = new StringBuilder();

                                foreach (var result in results)
                                {
                                    text.AppendLine(FormatResult(result));
                                }

                                Console.WriteLine($"Creating result:\n{text}");

                                var newComment = await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint(text.ToString()));

                                if (newComment != null)
                                {
                                    Console.WriteLine($"Result published for {owner}/{name}/{command.PullRequest.Number}");
                                }
                                else
                                {
                                    Console.WriteLine($"Error while publishing a result {owner}/{name}/{command.PullRequest.Number}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"An error occurred: {ex}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Skipping publishing results.");
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorCommentText = $"Failed to benchmark PR #{command.PullRequest.Number}. Skipping... Details:\n```\n{ex}\n```";
                        Console.WriteLine($"Benchmark error comment: {errorCommentText}");
                        await _githubClient.Issue.Comment.Create(owner, name, command.PullRequest.Number, ApplyThumbprint("An error occurred, please check the logs"));
                    }
                }

                Console.WriteLine($"Done scanning for benchmark requests.");
            }

            return 0;
        }

        private static void CreateThumbprint()
        {
            // Create a unique thumbprint per configuration file such that multiple pipelines can process the same PR with different arguments (linux/windows, x64/arm64)
            var identifier = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(_options.Config)));

            Thumbprint = string.Format(Thumbprint, HtmlEncoder.Default.Encode(identifier));
        }

        private static string FormatResult(Result result)
        {
            return $"<details>\n<summary>{result.Benchmark} - {result.Profile}</summary>\n<p>\n\n{result.Output}\n</p>\n</details>";
        }

        private static async Task UpgradeAuthenticatedClient()
        {
            if (_githubClient.Credentials == Credentials.Anonymous)
            {
                _githubClient = GitHubHelper.CreateClient(await GitHubHelper.GetCredentialsAsync(_options), _options.GitHubBaseUrl);
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

            // Get all PRs by page

            var page = 0;

            while (true)
            {
                // 1-indexed value
                page++;

                var prs = await _githubClient.PullRequest.GetAllForRepository(owner, name, prRequest, new ApiOptions { PageCount = 1, PageSize = 5, StartPage = page });

                if (!prs.Any())
                {
                    break;
                }

                foreach (var pr in prs)
                {
                    // Stop processing PRs when we find one that is too old since they are ordered
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
                            await UpgradeAuthenticatedClient();

                            if (await _githubClient.Repository.Collaborator.IsCollaborator(pr.Base.Repository.Id, comment.User.Login))
                            {
                                var arguments = comment.Body[BenchmarkCommand.Length..].Trim()
                                    .Split(' ', 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                                var benchmarkNames = (arguments.Length > 0 ? arguments[0] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                                var profileNames = (arguments.Length > 1 ? arguments[1] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                                var componentNames = (arguments.Length > 2 ? arguments[2] : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                                var crankArguments = (arguments.Length > 3 ? arguments[3] : null);

                                // If some arguments are missing render the help text as a new comment
                                if (!benchmarkNames.Any() || !profileNames.Any() || !componentNames.Any())
                                {
                                    await _githubClient.Issue.Comment.Create(owner, name, pr.Number, ApplyThumbprint(GetHelp(markdown: true)));

                                    yield break;
                                }

                                // Check if the arguments are available in the configuration file
                                benchmarkNames = benchmarkNames.Intersect(_configuration.Benchmarks.Keys).ToArray();
                                profileNames = profileNames.Intersect(_configuration.Profiles.Keys).ToArray();
                                componentNames = componentNames.Intersect(_configuration.Components.Keys).ToArray();

                                if (!benchmarkNames.Any() || !profileNames.Any() || !componentNames.Any())
                                {
                                    // Skip comment
                                    yield break;
                                }


                                // Default command values
                                yield return new Command
                                {
                                    Benchmarks = benchmarkNames.Any() ? benchmarkNames : new[] { _configuration.Benchmarks.First().Key },
                                    Profiles = profileNames.Any() ? profileNames : new[] { _configuration.Profiles.First().Key },
                                    Components = componentNames.Any() ? componentNames : new[] { _configuration.Components.First().Key },
                                    PullRequest = pr,
                                    Arguments = crankArguments,
                                };
                            }
                            else
                            {
                                var message = $"The user @{comment.User.Login} is not allowed to perform this action.";

                                Console.WriteLine(message);

                                await _githubClient.Issue.Comment.Create(owner, name, pr.Number, ApplyThumbprint(message));
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
        }

        public static string GetHelp(bool markdown)
        {
            string help =
                "Crank Pull Request Bot\n" +
                "\n" +
                "`/benchmark <benchmark[,...]> <profile[,...]> <component,[...]> <arguments>`\n"
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
            foreach (var entry in _configuration.Components)
            {
                help += $"- `{entry.Key}`\n";
            }
            help += $"\nArguments: any additional arguments to pass through to crank, e.g. `--variable name=value`\n";

            if (!markdown)
            {
                help = help.Replace("`", "");
            }

            return help;
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
                        var filename = configurationFilenameOrUrl[..questionMarkIndex];
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
                        var schema = Json.Schema.JsonSchema.FromText(schemaJson);

                        var jsonToValidate = System.Text.Json.Nodes.JsonNode.Parse(json);
                        var validationResults = schema.Validate(jsonToValidate, new Json.Schema.ValidationOptions { OutputFormat = Json.Schema.OutputFormat.Detailed });

                        if (!validationResults.IsValid)
                        {
                            // Create a json debug file with the schema
                            localconfiguration.AddFirst(new JProperty("$schema", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.PullRequestBot/pullrequestbot.schema.json"));

                            var debugFilename = Path.Combine(Path.GetTempPath(), "configuration.debug.json");
                            File.WriteAllText(debugFilename, localconfiguration.ToString(Formatting.Indented));

                            var errorBuilder = new StringBuilder();

                            errorBuilder.AppendLine($"Invalid configuration file '{configurationFilenameOrUrl}' at '{validationResults.InstanceLocation}'");
                            errorBuilder.AppendLine($"{validationResults.Message}");
                            errorBuilder.AppendLine($"Debug file created at '{debugFilename}'");

                            throw new PullRequestBotException(errorBuilder.ToString());
                        }

                        break;
                    default:
                        throw new PullRequestBotException($"Unsupported configuration format: {configurationExtension}");
                }

                // Evaluate templates in variables
                var rootVariables = localconfiguration["variables"];
                foreach (JProperty property in localconfiguration["benchmarks"] ?? new JObject())
                {
                    var benchmark = property.Value;
                    var benchmarkVariables = benchmark["variables"];

                    var variables = MergeVariables(rootVariables, benchmarkVariables);
                    ApplyTemplates(variables, new TemplateContext(variables.DeepClone()));

                    benchmark["variables"] = variables;
                }

                return localconfiguration.ToObject<Configuration>();
            }
            else
            {
                throw new PullRequestBotException($"Invalid file path or url: '{configurationFilenameOrUrl}'");
            }
        }

        private static JObject MergeVariables(params object[] variableObjects)
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

        private static void ApplyTemplates(JToken node, TemplateContext templateContext)
        {
            foreach (var token in node.Children())
            {
                if (token is JValue jValue)
                {
                    if (jValue.Type == JTokenType.String)
                    {
                        jValue.Value = ApplyTemplate(jValue.ToString(), templateContext);
                    }
                }
                else
                {
                    ApplyTemplates(token, templateContext);
                }
            }
        }

        private static string ApplyTemplate(string template, TemplateContext templateContext)
        {
            if (template != null && template.Contains("{"))
            {
                if (FluidParser.TryParse(template, out var tree))
                {
                    return tree.Render(templateContext);
                }
            }
            return template;
        }

        private static async Task<IEnumerable<Result>> RunBenchmark(Command command, string accessToken)
        {
            var results = new List<Result>();

            var runs = command.Profiles.SelectMany(x => command.Benchmarks.Select(y => new Run(x, y)));

            var workspace = _options.Workspace;

            // Workspace ends with path separator
            workspace = workspace.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

            var cloneUri = new UriBuilder(command.PullRequest.Base.Repository.CloneUrl);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                cloneUri.UserName = $"x-access-token:{accessToken}";
            }

            var cloneUrl = cloneUri.Uri.ToString();
            var folder = command.PullRequest.Base.Repository.Name; // $"aspnetcore"; // 
            var baseBranch = command.PullRequest.Base.Ref; // "main"; // 
            var prNumber = command.PullRequest.Number; // 39463;

            // Compute a unique clone folder name
            var counter = 1;
            while (Directory.Exists(Path.Combine(workspace, folder))) { folder = command.PullRequest.Base.Repository.Name + counter++; }

            var cloneFolder = Path.Combine(workspace, folder);

            var buildCommands = command.Components.SelectMany(b => _configuration.Components[b].Script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            // 1- Build the base branch once
            // 2- Run all benchmarks on all profiles
            // 4- Merge HEAD
            // 5- Build the PR branch once
            // 6- Run all benchmarks on all profiles
            // 6- Clean

            try
            {
                using var buildBaseCts = new CancellationTokenSource();
                var buildBaseTask = Task.Run(async () =>
                {
                    await ProcessUtil.RunAsync("git", $"clone -c core.longpaths=true --recursive {cloneUrl} {cloneFolder} -b {baseBranch}", workingDirectory: workspace, log: true, cancellationToken: buildBaseCts.Token);

                    // Build base
                    foreach (var c in buildCommands)
                    {
                        buildBaseCts.Token.ThrowIfCancellationRequested();

                        var scriptArgs = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/c {c}" : $"-c \"{c}\"";
                        await ProcessUtil.RunAsync(ProcessUtil.GetScriptHost(), scriptArgs, workingDirectory: cloneFolder, log: true, cancellationToken: buildBaseCts.Token);
                    }
                });

                var runOptions = new RunOptions(CrankMaxRetries, captureOutput: false); // note: runOptions.Variables are modified

                // In parallel with build, pre-check crank commands -- run without copying private bits from components
                // to verify that commands are correct and agents are up
                try
                {
                    Console.WriteLine($"Pre-check started...");
                    foreach (var run in runs)
                    {
                        var benchmark = _configuration.Benchmarks[run.Benchmark];
                        var profile = _configuration.Profiles[run.Profile];

                        runOptions.Variables = benchmark.Variables;
                        RunCrank(runOptions, _configuration.Defaults, benchmark.Arguments, profile.Arguments, command.Arguments, _options.Arguments);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Pre-check failed");
                    buildBaseCts.Cancel(); // Cancel build on pre-check failure
                    try
                    {
                        await buildBaseTask; // Wait for ProcessUtil to kill the process, swallow exception
                    } catch { }

                    throw; // Re-throw original pre-check exception
                }

                Console.WriteLine($"Pre-check successful");
                await buildBaseTask; // on successful pre-check, wait until build is finished

                Console.WriteLine($"Base run started...");
                foreach (var run in runs)
                {
                    var benchmark = _configuration.Benchmarks[run.Benchmark];
                    var profile = _configuration.Profiles[run.Profile];
                    var buildArguments = string.Join(" ", command.Components.Select(b => _configuration.Components[b].Arguments));

                    // Delete existing files (if any) before the base run
                    var baseResultsFilename = $"{workspace}{run.Benchmark}.{BaseFilename}";
                    var prResultsFilename = $"{workspace}{run.Benchmark}.{PrFilename}";

                    File.Delete(baseResultsFilename);
                    File.Delete(prResultsFilename);

                    Directory.SetCurrentDirectory(cloneFolder);
                    runOptions.Variables = benchmark.Variables;
                    RunCrank(runOptions, _configuration.Defaults, benchmark.Arguments, profile.Arguments, buildArguments, $@"--json ""{baseResultsFilename}""", command.Arguments, _options.Arguments);
                }

                await ProcessUtil.RunAsync("git", $@"fetch origin pull/{prNumber}/head", workingDirectory: cloneFolder, log: true);
                await ProcessUtil.RunAsync("git", $@"config user.name ""user""", workingDirectory: cloneFolder, log: true);
                await ProcessUtil.RunAsync("git", $@"config user.email ""user@company.com""", workingDirectory: cloneFolder, log: true);
                await ProcessUtil.RunAsync("git", $@"merge FETCH_HEAD", workingDirectory: cloneFolder, log: true);

                // Build head
                foreach (var c in buildCommands)
                {
                    var scriptArgs = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/c {c}" : $"-c \"{c}\"";
                    await ProcessUtil.RunAsync(ProcessUtil.GetScriptHost(), scriptArgs, workingDirectory: cloneFolder, log: true);
                }

                Console.WriteLine($"PR run started...");
                foreach (var run in runs)
                {
                    var benchmark = _configuration.Benchmarks[run.Benchmark];
                    var profile = _configuration.Profiles[run.Profile];
                    var buildArguments = string.Join(" ", command.Components.Select(b => _configuration.Components[b].Arguments));

                    var baseResultsFilename = $"{workspace}{run.Benchmark}.{BaseFilename}";
                    var prResultsFilename = $"{workspace}{run.Benchmark}.{PrFilename}";

                    Directory.SetCurrentDirectory(cloneFolder);
                    runOptions.Variables = benchmark.Variables;
                    RunCrank(runOptions, _configuration.Defaults, benchmark.Arguments, profile.Arguments, buildArguments, $@"--json ""{prResultsFilename}""", command.Arguments, _options.Arguments);

                    // Compare benchmarks
                    var result = RunCrank($"compare", baseResultsFilename, prResultsFilename);

                    results.Add(new Result(run.Profile, run.Benchmark, result));
                }

                return results;
            }
            finally
            {
                // Stop dotnet process
                // This assumes dotnet is part of the PATH, which might not work based on the host

                await ProcessUtil.RunAsync(@"dotnet", "build-server shutdown", log: true, throwOnError: false);

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    await ProcessUtil.RunAsync("sh", "-c \"pkill dotnet || true\"", log: true, throwOnError: false);
                }
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    await ProcessUtil.RunAsync(@"taskkill", "/F /IM dotnet.exe", log: true, throwOnError: false);
                }

                // Clean git clones
                try
                {
                    Console.WriteLine($"Cleaning '{cloneFolder}'");
                    Directory.Delete(cloneFolder);
                }
                catch
                {
                    // Fail to delete the clone folder, continue
                    Console.WriteLine($"Failed to clean {cloneFolder}. Ignoring...");
                }
            }
        }

        private static string RunCrank(params string[] args)
        {
            return RunCrank(RunOptions.Default, args);
        }

        private static string RunCrank(RunOptions options, params string[] args)
        {
            IEnumerable<string> rawArgs = args;
            if (options.Variables != null)
            {
                var templateContext = new TemplateContext(options.Variables);
                rawArgs = rawArgs.Select(arg => ApplyTemplate(arg, templateContext));
            }

            args = rawArgs.SelectMany(c => CommandLineStringSplitter.Instance.Split(c)).ToArray();

            Console.WriteLine($"crank {string.Join(' ', args)}");

            for (int i = 0; i < options.MaxRetries; ++i)
            {
                int exitCode = 1;
                string result = null;

                try
                {
                    if (!options.CaptureOutput)
                    {
                        exitCode = Crank.Controller.Program.Main(args);
                    }
                    else
                    {
                        using var sw = new StringWriter();
                        var consoleOut = Console.Out;
                        try
                        {
                            Console.SetOut(sw);
                            exitCode = Crank.Controller.Program.Main(args);
                        }
                        finally
                        {
                            Console.SetOut(consoleOut);
                            Console.WriteLine(sw.ToString());
                        }
                        result = sw.ToString();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (exitCode == 0)
                {
                    // successful run
                    return result;
                }

                var errorMessage = "An error occurred during crank run";
                if (i + 1 < options.MaxRetries)
                {                
                    errorMessage += $". Retrying... ({i + 2} of {options.MaxRetries})";
                }
                Console.WriteLine(errorMessage);
            }

            throw new InvalidOperationException("Crank run failed. Max retries reached");
        }
    }

    public class Run
    {
        public Run(string profile, string benchmark)
        {
            Profile = profile;
            Benchmark = benchmark;
        }

        public string Profile { get; set; }
        public string Benchmark { get; set; }
    }

    public class Result
    {
        public Result(string profile, string benchmark, string output)
        {
            Profile = profile;
            Benchmark = benchmark;
            Output = output;
        }

        public string Profile { get; set; }
        public string Benchmark { get; set; }
        public string Output { get; set; }
    }

    public class RunOptions
    {
        public static RunOptions Default = new();

        public RunOptions(int maxRetries = 1, bool captureOutput = true, IDictionary<string, object> variables = null)
        {
            MaxRetries = maxRetries;
            CaptureOutput = captureOutput;
            Variables = variables;
        }

        public int MaxRetries { get; set; }
        public bool CaptureOutput { get; set; }
        public IDictionary<string, object> Variables { get; set; }
    }
}
