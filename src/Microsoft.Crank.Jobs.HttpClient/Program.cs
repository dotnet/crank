// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Crank.EventSources;

namespace Microsoft.Crank.Jobs.HttpClient
{
    class Program
    {
        private static object _synLock = new object();
        private static HttpMessageInvoker _httpMessageInvoker;
        private static bool _running;
        private static bool _measuring;

        public static string ServerUrl { get; set; }
        public static int WarmupTimeSeconds { get; set; }
        public static int ExecutionTimeSeconds { get; set; }
        public static int Connections { get; set; }
        public static List<string> Headers { get; set; }
        public static Version Version { get; set; }
        public static string CertPath { get; set; }
        public static string CertPassword { get; set; }
        public static X509Certificate2 Certificate { get; set; }

        static async Task Main(string[] args)
        {

            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var optionUrl = app.Option("-u|--url <URL>", "The server url to request", CommandOptionType.SingleValue).IsRequired();
            var optionConnections = app.Option<int>("-c|--connections <N>", "Total number of HTTP connections to open. Default is 10.", CommandOptionType.SingleValue);
            var optionWarmup = app.Option<int>("-w|--warmup <N>", "Duration of the warmup in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionDuration = app.Option<int>("-d|--duration <N>", "Duration of the test in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionHeaders = app.Option("-H|--header <HEADER>", "HTTP header to add to request, e.g. \"User-Agent: edge\"", CommandOptionType.MultipleValue);
            var optionVersion = app.Option("-v|--version <1.0,1.1,2.0>", "HTTP version, e.g. \"2.0\". Default is 1.1", CommandOptionType.SingleValue);
            var optionCertPath = app.Option("-t|--cert <filepath>", "The path to a cert pfx file.", CommandOptionType.SingleValue);
            var optionCertPwd = app.Option("-p|--certpwd <password>", "The password for the cert pfx file.", CommandOptionType.SingleValue);

            app.OnExecuteAsync(cancellationToken =>
            {
                Console.WriteLine("Http Client");
                Console.WriteLine($"args: {string.Join(" ", args)}");

                ServerUrl = optionUrl.Value();

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
                            Console.WriteLine("Unkown HTTP version: {0}", optionVersion.Value());
                            break;
                    }
                }

                CertPath = optionCertPath.Value();
                Console.WriteLine("CerPath: " + CertPath);
                if (CertPath != null && !string.Equals(CertPath, "none", StringComparison.OrdinalIgnoreCase))
                {
                    CertPassword = optionCertPwd.Value();
                    if (CertPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Downloading cert from: {CertPath}");
                        var httpClientHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };

                        var httpClient = new System.Net.Http.HttpClient(httpClientHandler);
                        var bytes = await httpClient.GetByteArrayAsync(CertPath);
                        Certificate = new X509Certificate2(bytes, CertPassword);
                        Console.WriteLine("Cert Thumb: " + Certificate.Thumbprint);
                    }
                    else
                    {
                        Console.WriteLine($"Using cert from: {CertPath}");
                        Certificate = new X509Certificate2(CertPath, CertPassword);
                    }
                }

                return RunAsync();
            });

            await app.ExecuteAsync(args);
        }

        public static async Task RunAsync()
        {
            Console.WriteLine($"Running {ExecutionTimeSeconds}s test @ {ServerUrl}");

            DateTime startTime = default, stopTime = default;

            IEnumerable<Task> CreateTasks()
            {
                // Statistics thread
                yield return Task.Run(
                    async () =>
                    {
                        if (WarmupTimeSeconds > 0)
                        {
                            Console.WriteLine($"Warming up for {WarmupTimeSeconds}s...");
                            await Task.Delay(TimeSpan.FromSeconds(WarmupTimeSeconds));
                        }

                        Console.WriteLine($"Running for {ExecutionTimeSeconds}s...");

                        _measuring = true;

                        startTime = DateTime.UtcNow;

                        do
                        {
                            await Task.Delay(1000);

                            Console.Write(".");

                        } while (_running);

                        Console.WriteLine();
                    });

                // Shutdown everything
                yield return Task.Run(
                   async () =>
                   {
                       await Task.Delay(TimeSpan.FromSeconds(WarmupTimeSeconds + ExecutionTimeSeconds));

                       _running = false;

                       Console.WriteLine($"Stopping...");

                       stopTime = DateTime.UtcNow;
                   });
            }

            _running = true;

            var workerTasks = Enumerable
                .Range(0, Connections)
                .Select(_ => Task.Run(DoWorkAsync))
                .ToList();

            await Task.WhenAll(CreateTasks());

            await Task.WhenAll(workerTasks);

            Console.WriteLine($"Stopped...");

            var result = new WorkerResult
            {
                Status1xx = workerTasks.Select(x => x.Result.Status1xx).Sum(),
                Status2xx = workerTasks.Select(x => x.Result.Status2xx).Sum(),
                Status3xx = workerTasks.Select(x => x.Result.Status3xx).Sum(),
                Status4xx = workerTasks.Select(x => x.Result.Status4xx).Sum(),
                Status5xx = workerTasks.Select(x => x.Result.Status5xx).Sum(),
                SocketErrors = workerTasks.Select(x => x.Result.SocketErrors).Sum()
            };

            var totalTps = (int)((result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx ) / (stopTime - startTime).TotalSeconds);

            Console.WriteLine($"Average RPS:     {totalTps:N0}");
            Console.WriteLine($"1xx:             {result.Status1xx:N0}");
            Console.WriteLine($"2xx:             {result.Status2xx:N0}");
            Console.WriteLine($"3xx:             {result.Status3xx:N0}");
            Console.WriteLine($"4xx:             {result.Status4xx:N0}");
            Console.WriteLine($"5xx:             {result.Status5xx:N0}");
            Console.WriteLine($"Socket Errors:   {result.SocketErrors:N0}");

            // If multiple samples are provided, take the max RPS, then sum the result from all clients
            BenchmarksEventSource.Register("httpclient/connections", Operations.Max, Operations.Sum, "Connections", "Number of active connections", "n0");
            BenchmarksEventSource.Register("httpclient/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");
            BenchmarksEventSource.Register("httpclient/latency/mean", Operations.Max, Operations.Sum, "Mean latency (us)", "Mean latency (us)", "n0");
            BenchmarksEventSource.Register("httpclient/latency/max", Operations.Max, Operations.Sum, "Max latency (us)", "Max latency (us)", "n0");
            BenchmarksEventSource.Register("httpclient/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Register("httpclient/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Register("httpclient/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");
            BenchmarksEventSource.Register("httpclient/errors", Operations.Sum, Operations.Sum, "Socket Errors", "Socket Errors", "n0");

            BenchmarksEventSource.Measure("httpclient/rps/mean", totalTps);
            BenchmarksEventSource.Measure("httpclient/connections", Connections);
            BenchmarksEventSource.Measure("httpclient/requests", result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx + result.SocketErrors);
            BenchmarksEventSource.Measure("httpclient/badresponses", result.Status1xx + result.Status4xx + result.Status5xx);
            BenchmarksEventSource.Measure("httpclient/errors", result.SocketErrors);

            BenchmarksEventSource.Measure("httpclient/latency/mean", 0);
            BenchmarksEventSource.Measure("httpclient/latency/max", 0);
            BenchmarksEventSource.Measure("httpclient/throughput", 0);
        }

        private static HttpMessageInvoker CreateHttpMessageInvoker()
        {
            if (_httpMessageInvoker == null)
            {
                lock (_synLock)
                {
                    var httpHandler = new SocketsHttpHandler();

                    // There should be only as many connections as Tasks concurrently, so there is no need
                    // to limit the max connections per server 
                    // httpHandler.MaxConnectionsPerServer = Connections;
                    httpHandler.AllowAutoRedirect = false;
                    httpHandler.UseProxy = false;
                    httpHandler.AutomaticDecompression = System.Net.DecompressionMethods.None;
                    // Accept any SSL certificate
                    httpHandler.SslOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

                    if (Certificate != null)
                    {
                        Console.WriteLine($"Using Cert");
                        httpHandler.SslOptions.ClientCertificates = new X509CertificateCollection();
                        httpHandler.SslOptions.ClientCertificates.Add(Certificate);
                    }
                    else
                    {
                        Console.WriteLine($"No cert specified.");
                    }

                    _httpMessageInvoker = new HttpMessageInvoker(httpHandler);
                }
            }

            return _httpMessageInvoker;
        }

        public static async Task<WorkerResult> DoWorkAsync()
        {
            var httpMessageInvoker = CreateHttpMessageInvoker();
            
            var requestMessage = new HttpRequestMessage();
            requestMessage.Method = HttpMethod.Get;

            // Copy the request headers
            foreach (var header in Headers)
            {
                var headerNameValue = header.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
                requestMessage.Headers.TryAddWithoutValidation(headerNameValue[0], headerNameValue[1]);
            }

            var uri = new Uri(ServerUrl);

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Version = Version;

            // Counters local to this worker
            var counters = new int[5];
            var socketErrors = 0;

            // var sw = new Stopwatch();

            while (_running)
            {
                try
                {
                    // sw.Start();

                    using var responseMessage = await httpMessageInvoker.SendAsync(requestMessage, CancellationToken.None);

                    // sw.Stop();
                    // Add the latency divided by the pipeline depth

                    if (_measuring)
                    {
                        var status = (int)responseMessage.StatusCode;

                        if (status < 100 && status >= 600)
                        {
                            socketErrors++;
                        }

                        counters[status / 100 - 1]++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (_measuring)
                    {
                        socketErrors++;
                    }
                }
            }

            return new WorkerResult
            {
                Status1xx = counters[0],
                Status2xx = counters[1],
                Status3xx = counters[2],
                Status4xx = counters[3],
                Status5xx = counters[4],
                SocketErrors = socketErrors
            };
        }
    }
}
