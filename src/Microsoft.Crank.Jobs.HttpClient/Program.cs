// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jint;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Crank.EventSources;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    class Program
    {
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;
        private static ScriptConsole _scriptConsole = new ScriptConsole();

        private static readonly object _synLock = new object();

        private static HttpMessageInvoker _httpMessageInvoker;
        private static SocketsHttpHandler _httpHandler;

        private static bool _running;
        private static bool _measuring;
        private static List<Worker> _workers = new List<Worker>();

        public static string ServerUrl { get; set; }
        public static int WarmupTimeSeconds { get; set; }
        public static int ExecutionTimeSeconds { get; set; }
        public static int Connections { get; set; }
        public static List<string> Headers { get; set; }
        public static Version Version { get; set; }
        public static string CertPath { get; set; }
        public static string CertPassword { get; set; }
        public static X509Certificate2 Certificate { get; set; }
        public static bool Quiet { get; set; }
        public static bool SendCookies { get; set; }
        public static string Format { get; set; }
        public static bool Local { get; set; }
        public static Timeline[] Timelines {  get; set; }
        public static string Script {  get; set; }  


        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);
        }

        static async Task Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var optionUrl = app.Option("-u|--url <URL>", "The server url to request. If --har is used, this becomes the new based url for the .HAR file.", CommandOptionType.SingleValue);
            var optionConnections = app.Option<int>("-c|--connections <N>", "Total number of HTTP connections to open. Default is 10.", CommandOptionType.SingleValue);
            var optionWarmup = app.Option<int>("-w|--warmup <N>", "Duration of the warmup in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionDuration = app.Option<int>("-d|--duration <N>", "Duration of the test in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionHeaders = app.Option("-H|--header <HEADER>", "HTTP header to add to request, e.g. \"User-Agent: edge\"", CommandOptionType.MultipleValue);
            var optionVersion = app.Option("-v|--version <1.0,1.1,2.0>", "HTTP version, e.g. \"2.0\". Default is 1.1", CommandOptionType.SingleValue);
            var optionCertPath = app.Option("-t|--cert <filepath>", "The path to a cert pfx file.", CommandOptionType.SingleValue);
            var optionCertPwd = app.Option("-p|--certpwd <password>", "The password for the cert pfx file.", CommandOptionType.SingleValue);
            var optionFormat = app.Option("-f|--format <format>", "The format of the output, e.g., text, json. Default is text.", CommandOptionType.SingleValue);
            var optionQuiet = app.Option("-q|--quiet", "When set, nothing is rendered on stsdout but the results.", CommandOptionType.NoValue);
            var optionCookies = app.Option("-c|--cookies", "When set, cookies are stored and sent back.", CommandOptionType.NoValue);
            var optionHar = app.Option("-h|--har <filename>", "A .har file representing the urls to request.", CommandOptionType.SingleValue);
            var optionScript = app.Option("-s|--script <filename>", "A .js script file altering the current client.", CommandOptionType.SingleValue);
            var optionLocal = app.Option("-l|--local", "Ignore requests outside of the main domain.", CommandOptionType.NoValue);

            app.OnValidate(ctx =>
            {
                if (!optionHar.HasValue() && !optionUrl.HasValue())
                {
                    return new ValidationResult($"The --{optionUrl.LongName} field is required.");
                }

                return ValidationResult.Success;
            });

            app.OnExecuteAsync(async cancellationToken =>
            {
                SendCookies = optionCookies.HasValue();

                Quiet = optionQuiet.HasValue();

                Local = optionLocal.HasValue();

                Log("Http Client");

                ServerUrl = optionUrl.Value();

                if (optionHar.HasValue())
                {
                    var harFilename = optionHar.Value();

                    if (harFilename.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Downloading har file {harFilename}");
                        var tempFile = Path.GetTempFileName();

                        using (var downloadStream = await _httpClient.GetStreamAsync(harFilename))
                        using (var fileStream = File.Create(tempFile))
                        {
                            await downloadStream.CopyToAsync(fileStream);
                        }

                        harFilename = tempFile;
                    }

                    if (!File.Exists(harFilename))
                    {
                        Console.WriteLine($"HAR file not found: '{Path.GetFullPath(harFilename)}'");
                        return;
                    }

                    Timelines = TimelineFactory.FromHar(harFilename);

                    var baseUri = Timelines.First().Uri;
                    var serverUri = String.IsNullOrEmpty(ServerUrl) ? baseUri : new Uri(ServerUrl);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Subsituting '{baseUri.Host}' with '{serverUri}'");
                    Console.ResetColor();

                    // Substiture the base url with the one provided

                    foreach (var timeline in Timelines)
                    {
                        if (baseUri.Host == timeline.Uri.Host || timeline.Uri.Host.EndsWith("." + baseUri.Host))
                        {
                            timeline.Uri = new UriBuilder(serverUri.Scheme, serverUri.Host, serverUri.Port, timeline.Uri.AbsolutePath, timeline.Uri.Query).Uri;
                        }
                    }

                    if (Local)
                    {
                        Timelines = Timelines.Where(x => String.Equals(x.Uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                }
                else
                {
                    Timelines = new[] { new Timeline { Method = "GET", Uri = new Uri(ServerUrl) } };
                }

                ServerUrl = optionUrl.Value();

                Format = optionFormat.HasValue() ? optionFormat.Value() : "text";

                WarmupTimeSeconds = optionWarmup.HasValue()
                    ? int.Parse(optionWarmup.Value())
                    : 5;

                ExecutionTimeSeconds = optionDuration.HasValue()
                    ? int.Parse(optionDuration.Value())
                    : 5;

                Connections = optionConnections.HasValue()
                    ? int.Parse(optionConnections.Value())
                    : 10;

                Headers = new List<string>(optionHeaders.Values);

                if (!optionVersion.HasValue())
                {
                    Version = HttpVersion.Version11;
                }
                else
                {
                    switch (optionVersion.Value())
                    {
                        case "1.0" : Version = HttpVersion.Version10; break;
                        case "1.1" : Version = HttpVersion.Version11; break;
                        case "2.0" : Version = HttpVersion.Version20; break;
                        default:
                            Log("Unkown HTTP version: {0}", optionVersion.Value());
                            break;
                    }
                }

                if (optionCertPath.HasValue())
                {
                    CertPath = optionCertPath.Value();
                    Log("CerPath: " + CertPath);
                    CertPassword = optionCertPwd.Value();
                    if (CertPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"Downloading certificate: {CertPath}");
                        var httpClientHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };

                        var httpClient = new HttpClient(httpClientHandler);
                        var bytes = await httpClient.GetByteArrayAsync(CertPath);
                        Certificate = new X509Certificate2(bytes, CertPassword);
                    }
                    else
                    {
                        Log($"Reading certificate: {CertPath}");
                        Certificate = new X509Certificate2(CertPath, CertPassword);
                    }

                    Log("Certificate Thumbprint: " + Certificate.Thumbprint);
                }

                if (optionScript.HasValue())
                {
                    var scriptFilename = optionScript.Value();

                    if (scriptFilename.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Downloading script file {scriptFilename}");
                        var tempFile = Path.GetTempFileName();

                        using (var downloadStream = await _httpClient.GetStreamAsync(scriptFilename))
                        using (var fileStream = File.Create(tempFile))
                        {
                            await downloadStream.CopyToAsync(fileStream);
                        }

                        scriptFilename = tempFile;
                    }

                    if (!File.Exists(scriptFilename))
                    {
                        Console.WriteLine($"Script file not found: '{Path.GetFullPath(scriptFilename)}'");
                        return;
                    }

                    Script = File.ReadAllText(scriptFilename);
                }

                await RunAsync();
            });

            await app.ExecuteAsync(args);
        }

        public static void Log()
        {
            Log("");
        }

        public static void Log(string message, params object[] args)
        {
            if (Quiet)
            {
                return;
            }

            Console.WriteLine(message, args);
        }

        public static async Task RunAsync()
        {
            Log($"Running {ExecutionTimeSeconds}s test @ {ServerUrl}");

            DateTime startTime = default, stopTime = default;

            IEnumerable<Task> CreateTasks()
            {
                // Statistics thread
                yield return Task.Run(
                    async () =>
                    {
                        if (WarmupTimeSeconds > 0)
                        {
                            Log($"Warming up for {WarmupTimeSeconds}s...");
                            await Task.Delay(TimeSpan.FromSeconds(WarmupTimeSeconds));
                        }
                        else
                        {
                            Log($"Warmup skipped");
                        }

                        Log($"Running for {ExecutionTimeSeconds}s...");

                        _measuring = true;

                        startTime = DateTime.UtcNow;

                        do
                        {
                            await Task.Delay(1000);

                        } while (_running);

                        Log();
                    });

                // Shutdown everything
                yield return Task.Run(
                   async () =>
                   {
                       await Task.Delay(TimeSpan.FromSeconds(WarmupTimeSeconds + ExecutionTimeSeconds));

                       _running = false;

                       Log($"Stopping...");

                       stopTime = DateTime.UtcNow;
                   });
            }

            if (ExecutionTimeSeconds <= 0)
            {
                Log($"Benchmark skipped");

                return;
            }

            _running = true;

            var workerTasks = Enumerable
                .Range(0, Connections)
                .Select(_ => Task.Run(DoWorkAsync))
                .ToList();

            await Task.WhenAll(CreateTasks());

            await Task.WhenAll(workerTasks);

            Log($"Stopped...");

            var result = new WorkerResult
            {
                Status1xx = workerTasks.Select(x => x.Result.Status1xx).Sum(),
                Status2xx = workerTasks.Select(x => x.Result.Status2xx).Sum(),
                Status3xx = workerTasks.Select(x => x.Result.Status3xx).Sum(),
                Status4xx = workerTasks.Select(x => x.Result.Status4xx).Sum(),
                Status5xx = workerTasks.Select(x => x.Result.Status5xx).Sum(),
                SocketErrors = workerTasks.Select(x => x.Result.SocketErrors).Sum(),
                Stopped = stopTime.ToLocalTime(),
                Started = startTime.ToLocalTime(),
                ThroughputBps = workerTasks.Select(x => x.Result.ThroughputBps).Sum(),
                LatencyMaxMs = Math.Round(workerTasks.Select(x => x.Result.LatencyMaxMs).Max(), 3),
                LatencyMeanMs = workerTasks.Select(x => x.Result.TotalRequests).Sum() == 0 ? 0 : Math.Round((stopTime - startTime).TotalMilliseconds / workerTasks.Select(x => x.Result.TotalRequests).Sum(), 3),
                Connections = Connections,
            };

            var totalTps = (int)((result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx ) / (stopTime - startTime).TotalSeconds);

            if (Format == "text")
            {
                Console.WriteLine($"Average RPS:       {totalTps:N0}");
                Console.WriteLine($"1xx:               {result.Status1xx:N0}");
                Console.WriteLine($"2xx:               {result.Status2xx:N0}");
                Console.WriteLine($"3xx:               {result.Status3xx:N0}");
                Console.WriteLine($"4xx:               {result.Status4xx:N0}");
                Console.WriteLine($"5xx:               {result.Status5xx:N0}");
                Console.WriteLine($"Socket Errors:     {result.SocketErrors:N0}");
                Console.WriteLine($"Total requests:    {result.TotalRequests:N0}");
                Console.WriteLine($"Latency mean (ms): {result.LatencyMeanMs:N3}");
                Console.WriteLine($"Latency max (ms):  {result.LatencyMaxMs:N3}");
                Console.WriteLine($"Throughput (MB/s): {(double)result.ThroughputBps / 1024 / 1024:N3}");
            }
            else if (Format == "json")
            {
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }

            // If multiple samples are provided, take the max RPS, then sum the result from all clients
            BenchmarksEventSource.Register("httpclient/badresponses;http/requests/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");
            BenchmarksEventSource.Register("httpclient/latency/mean;http/latency/mean", Operations.Max, Operations.Avg, "Mean latency (us)", "Mean latency (us)", "n0");
            BenchmarksEventSource.Register("httpclient/latency/max;http/latency/max", Operations.Max, Operations.Max, "Max latency (us)", "Max latency (us)", "n0");
            BenchmarksEventSource.Register("httpclient/requests;http/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Register("httpclient/rps/mean;http/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Register("httpclient/throughput;http/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");
            BenchmarksEventSource.Register("httpclient/errors;http/requests/errors", Operations.Sum, Operations.Sum, "Socket Errors", "Socket Errors", "n0");

            BenchmarksEventSource.Measure("httpclient/rps/mean;http/rps/mean", totalTps);
            BenchmarksEventSource.Measure("httpclient/requests;http/requests", result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx + result.SocketErrors);
            BenchmarksEventSource.Measure("httpclient/badresponses;http/requests/badresponses", result.Status1xx + result.Status4xx + result.Status5xx);
            BenchmarksEventSource.Measure("httpclient/errors;http/requests/errors", result.SocketErrors);

            BenchmarksEventSource.Measure("httpclient/latency/mean;http/latency/mean", result.LatencyMeanMs);
            BenchmarksEventSource.Measure("httpclient/latency/max;http/latency/max", result.LatencyMaxMs);
            BenchmarksEventSource.Measure("httpclient/throughput;http/throughput", (double)result.ThroughputBps / 1024 / 1024);
        }

        private static Worker CreateWorker()
        {
            if (_httpMessageInvoker == null)
            {
                lock (_synLock)
                {
                    if (_httpMessageInvoker == null)
                    {
                        _httpHandler = new SocketsHttpHandler
                        {
                            // There should be only as many connections as Tasks concurrently, so there is no need
                            // to limit the max connections per server 
                            // httpHandler.MaxConnectionsPerServer = Connections;
                            AllowAutoRedirect = false,
                            UseProxy = false,
                            AutomaticDecompression = DecompressionMethods.None
                        };

                        // Accept any SSL certificate
                        _httpHandler.SslOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

                        if (Certificate != null)
                        {
                            Log($"Using Cert");
                            _httpHandler.SslOptions.ClientCertificates = new X509CertificateCollection
                            {
                                Certificate
                            };
                        }
                        else
                        {
                            Log($"No cert specified.");
                        }

                        _httpMessageInvoker = new HttpMessageInvoker(_httpHandler);
                    }
                }
            }

            var worker = new Worker
            {
                Handler = _httpHandler,
                Invoker = _httpMessageInvoker,
                Script = String.IsNullOrWhiteSpace(Script) ? null : new Engine(new Options().AllowClr(typeof(HttpRequestMessage).Assembly)).Execute(Script)
            };

            if (!String.IsNullOrWhiteSpace(Script))
            {
                worker.Script.SetValue("console", _scriptConsole);
            }

            _workers.Add(worker);

            return worker;
        }

        public static async Task<WorkerResult> DoWorkAsync()
        {
            // Store received cookies across domains and requests
            var cookieContainer = new CookieContainer();

            var worker = CreateWorker();

            if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("initialize").IsUndefined())
            {
                try
                {
                    worker.Script.Invoke("initialize", ServerUrl, Connections, WarmupTimeSeconds, ExecutionTimeSeconds, Headers, Version, Quiet);
                }
                catch (Exception ex)
                {
                    Log("An error occured while running a 'initialize' script: {0}", ex.Message);
                }
            }

            // Pre-create all requests for this thread
            var requests = new List<HttpRequestMessage>();

            foreach (var timeline in Timelines)
            {
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get
                };

                foreach (var header in timeline.Headers)
                {
                    requestMessage.Headers.Remove(header.Key);
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Apply the command-line headers
                foreach (var header in Headers)
                {
                    var headerNameValue = header.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
                    requestMessage.Headers.Remove(headerNameValue[0]);
                    requestMessage.Headers.TryAddWithoutValidation(headerNameValue[0], headerNameValue[1]);
                }

                var uri = timeline.Uri;

                requestMessage.Headers.Host = uri.Authority;
                requestMessage.RequestUri = uri;
                requestMessage.Version = Version;

                requests.Add(requestMessage);
            }

            if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("start").IsUndefined())
            {
                try
                {
                    worker.Script.Invoke("start", worker.Handler, requests);
                }
                catch (Exception ex)
                {
                    Log("An error occured while running a 'start' script: {0}", ex.Message);
                }
            }

            // Counters local to this worker
            var counters = new int[5];
            var socketErrors = 0;
            var maxLatency = 0D;
            var transferred = 0L;
            var measuringStart = 0L;
            var sw = new Stopwatch();
            sw.Start();

            var requestIndex = 0;

            while (_running)
            {
                // Get next request
                requestIndex++;

                if (requestIndex > requests.Count - 1)
                {
                    requestIndex = 0;
                }

                var requestMessage = requests[requestIndex];

                try
                {
                    // Add cookies for this domain
                    if (SendCookies)
                    {
                        requestMessage.Headers.Remove("Cookie");
                        requestMessage.Headers.Add("Cookie", cookieContainer.GetCookieHeader(new Uri(requestMessage.RequestUri.AbsoluteUri)));
                    }

                    var start = sw.ElapsedTicks;

                    if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("request").IsUndefined())
                    {
                        try
                        {
                            worker.Script.Invoke("request", requestMessage, !_running);
                        }
                        catch
                        {
                        }
                    }

                    using var responseMessage = await _httpMessageInvoker.SendAsync(requestMessage, CancellationToken.None);

                    if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("response").IsUndefined())
                    {
                        try
                        {
                            worker.Script.Invoke("response", responseMessage, !_running);
                        }
                        catch
                        {
                        }                        
                    }

                    if (_measuring)
                    {
                        if (measuringStart == 0)
                        {
                            measuringStart = sw.ElapsedTicks;
                        }

                        transferred += responseMessage.Content.Headers.ContentLength ?? 0;
                        var latency = sw.ElapsedTicks - start;
                        maxLatency = Math.Max(maxLatency, latency);

                        var status = (int)responseMessage.StatusCode;

                        if (status < 100 && status >= 600)
                        {
                            socketErrors++;
                        }

                        counters[status / 100 - 1]++;
                    }

                    // Wait to the desired delay
                    if (Timelines[requestIndex].Delay > TimeSpan.Zero)
                    {
                        var delay = Timelines[requestIndex].Delay;
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    if (_measuring)
                    {
                        socketErrors++;
                    }

                    if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("error").IsUndefined())
                    {
                        try
                        {
                            worker.Script.Invoke("error", ex);
                        }
                        catch (Exception er)
                        {
                            Log("An error occured while running a 'error' script: {0}", er.Message);
                        }
                    }
                }
            }

            var throughput = transferred / ((sw.ElapsedTicks - measuringStart) / Stopwatch.Frequency);

            if (!String.IsNullOrWhiteSpace(Script) && !worker.Script.GetValue("stop").IsUndefined())
            {
                try
                {
                    worker.Script.Invoke("stop", worker.Handler);
                }
                catch (Exception ex)
                {
                    Log("An error occured while running a 'stop' script: {0}", ex.Message);
                }                
            }

            return new WorkerResult
            {
                Status1xx = counters[0],
                Status2xx = counters[1],
                Status3xx = counters[2],
                Status4xx = counters[3],
                Status5xx = counters[4],
                SocketErrors = socketErrors,
                LatencyMaxMs = maxLatency / Stopwatch.Frequency * 1000,
                ThroughputBps = throughput
            };
        }
    }
}
