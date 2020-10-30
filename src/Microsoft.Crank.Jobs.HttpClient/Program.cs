// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        // private static HttpMessageInvoker _httpMessageInvoker;
        private static bool _running;
        private static bool _measuring;

        public static string ServerUrl { get; set; }
        public static int WarmupTimeSeconds { get; set; }
        public static int ExecutionTimeSeconds { get; set; }
        public static int Connections { get; set; }
        public static List<string> Headers { get; set; }

        static async Task Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var optionUrl = app.Option("-u|--url <URL>", "The server url to request", CommandOptionType.SingleValue);
            var optionConnections = app.Option<int>("-c|--connections <N>", "Total number of HTTP connections to keep open", CommandOptionType.SingleValue);
            var optionWarmup = app.Option<int>("-w|--warmup <N>", "Duration of the warmup in seconds", CommandOptionType.SingleValue);
            var optionDuration = app.Option<int>("-d|--duration <N>", "Duration of the test in seconds", CommandOptionType.SingleValue);
            var optionHeaders = app.Option("-H|--header <HEADER>", "HTTP header to add to request, e.g. \"User-Agent: edge\"", CommandOptionType.MultipleValue);

            app.OnExecuteAsync(cancellationToken =>
            {
                Console.WriteLine("Http Client");

                ServerUrl = optionUrl.Value();

                WarmupTimeSeconds = optionWarmup.HasValue()
                    ? int.Parse(optionWarmup.Value())
                    : 0;

                ExecutionTimeSeconds = int.Parse(optionDuration.Value());

                Connections = int.Parse(optionConnections.Value());

                Headers = new List<string>(optionHeaders.Values);

                return RunAsync();
            });

            await app.ExecuteAsync(args);
        }

        public static async Task RunAsync()
        {
            // InitializeHttpClient();

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

            BenchmarksEventSource.Measure("httpclient/rps-mean", totalTps);
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
            var httpHandler = new SocketsHttpHandler();

            httpHandler.MaxConnectionsPerServer = 1;
            httpHandler.AllowAutoRedirect = false;
            httpHandler.UseProxy = false;
            httpHandler.AutomaticDecompression = System.Net.DecompressionMethods.None;
            // Accept any SSL certificate
            httpHandler.SslOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

            return new HttpMessageInvoker(httpHandler);
        }

        public static async Task<WorkerResult> DoWorkAsync()
        {
            var httpMessageInvoker = CreateHttpMessageInvoker();

            var requestMessage = new HttpRequestMessage();
            requestMessage.Method = HttpMethod.Get;

            var result = new WorkerResult();

            // Copy the request headers
            foreach (var header in Headers)
            {
                var headerNameValue = header.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
                requestMessage.Headers.TryAddWithoutValidation(headerNameValue[0], headerNameValue[1]);
            }

            var uri = new Uri(ServerUrl);

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Version = new Version(1, 1);

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
                catch
                {
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
