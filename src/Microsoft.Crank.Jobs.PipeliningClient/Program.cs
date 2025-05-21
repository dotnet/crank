// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Crank.EventSources;

namespace Microsoft.Crank.Jobs.PipeliningClient
{
    class Program
    {
        private static bool _running;
        private static bool _measuring;
        public static string ServerUrl { get; set; }
        public static int PipelineDepth { get; set; }
        public static int WarmupTimeSeconds { get; set; }
        public static int ExecutionTimeSeconds { get; set; }
        public static int Connections { get; set; }
        public static bool DetailedResponseStats { get; set; }
        public static List<string> Headers { get; set; }

        private static List<KeyValuePair<int, int>> _statistics = new List<KeyValuePair<int, int>>();

        static async Task Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-h|--help");
            var optionUrl = app.Option("-u|--url <URL>", "The server url to request", CommandOptionType.SingleValue).IsRequired();
            var optionConnections = app.Option<int>("-c|--connections <N>", "Total number of HTTP connections to keep open. Default is 10.", CommandOptionType.SingleValue);
            var optionWarmup = app.Option<int>("-w|--warmup <N>", "Duration of the warmup in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionDuration = app.Option<int>("-d|--duration <N>", "Duration of the test in seconds. Default is 5.", CommandOptionType.SingleValue);
            var optionHeaders = app.Option("-H|--header <HEADER>", "HTTP header to add to request, e.g. \"User-Agent: edge\"", CommandOptionType.MultipleValue);
            var optionPipeline = app.Option<int>("-p|--pipeline <N>", "The pipelining depth", CommandOptionType.SingleValue);
            var optionDetailedResponseStats = app.Option<bool>("--detailedResponseStats", "Detailed stats of responses", CommandOptionType.NoValue);

            app.OnExecuteAsync(cancellationToken =>
            {
                Console.WriteLine("Pipelining Client");

                PipelineDepth = optionPipeline.HasValue()
                    ? int.Parse(optionPipeline.Value())
                    : 1;

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

                DetailedResponseStats = optionDetailedResponseStats.HasValue();

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
                            Console.WriteLine($"Warming up for {WarmupTimeSeconds}s");
                            var warmup = Task.Delay(TimeSpan.FromSeconds(WarmupTimeSeconds));

                            do
                            {
                                await Task.Delay(1000);

                                Console.Write(".");

                            } while (!warmup.IsCompleted && !warmup.IsCanceled && !warmup.IsFaulted);

                            Console.WriteLine();
                        }

                        Console.WriteLine($"Running for {ExecutionTimeSeconds}s...");

                        _measuring = true;

                        startTime = DateTime.UtcNow;

                        var duration = Task.Delay(TimeSpan.FromSeconds(ExecutionTimeSeconds));

                        do
                        {
                            await Task.Delay(1000);

                            Console.Write(".");

                        } while (!duration.IsCompleted && !duration.IsCanceled && !duration.IsFaulted);

                        Console.WriteLine();
                       
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

            _running = false;

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

            var totalTps = (int)((result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx) / (stopTime - startTime).TotalSeconds);

            Console.WriteLine($"Average RPS:     {totalTps:N0}");
            Console.WriteLine($"1xx:             {result.Status1xx:N0}");
            Console.WriteLine($"2xx:             {result.Status2xx:N0}");
            Console.WriteLine($"3xx:             {result.Status3xx:N0}");
            Console.WriteLine($"4xx:             {result.Status4xx:N0}");
            Console.WriteLine($"5xx:             {result.Status5xx:N0}");
            Console.WriteLine($"Socket Errors:   {result.SocketErrors:N0}");

            // If multiple samples are provided, take the max RPS, then sum the result from all clients
            BenchmarksEventSource.Register("pipelineclient/connections", Operations.Max, Operations.Sum, "Connections", "Number of active connections", "n0");
            BenchmarksEventSource.Register("pipelineclient/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");
            BenchmarksEventSource.Register("pipelineclient/latency/mean", Operations.Max, Operations.Avg, "Mean latency (us)", "Mean latency (us)", "n0");
            BenchmarksEventSource.Register("pipelineclient/latency/max", Operations.Max, Operations.Max, "Max latency (us)", "Max latency (us)", "n0");
            BenchmarksEventSource.Register("pipelineclient/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Register("pipelineclient/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Register("pipelineclient/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");
            BenchmarksEventSource.Register("pipelineclient/errors", Operations.Sum, Operations.Sum, "Socket Errors", "Socket Errors", "n0");

            BenchmarksEventSource.Measure("pipelineclient/rps/mean", totalTps);
            BenchmarksEventSource.Measure("pipelineclient/connections", Connections);
            BenchmarksEventSource.Measure("pipelineclient/requests", result.Status1xx + result.Status2xx + result.Status3xx + result.Status4xx + result.Status5xx + result.SocketErrors);
            BenchmarksEventSource.Measure("pipelineclient/badresponses", result.Status1xx + result.Status4xx + result.Status5xx);
            BenchmarksEventSource.Measure("pipelineclient/errors", result.SocketErrors);

            BenchmarksEventSource.Measure("httpclient/latency/mean", 0);
            BenchmarksEventSource.Measure("httpclient/latency/max", 0);
            BenchmarksEventSource.Measure("httpclient/throughput", 0);
        }

        private static int _connectionCount = 0;
        public static async Task<WorkerResult> DoWorkAsync()
        {
            var result = new WorkerResult();
            var connectionId = Interlocked.Increment(ref _connectionCount);

            while (_running)
            {
                // Creating a new connection every time it is necessary
                using (var connection = new HttpConnection(ServerUrl, PipelineDepth, Headers))
                {
                    Console.WriteLine("Connection created ({0})", connectionId);

                    await connection.ConnectAsync();

                    try
                    {
                        // var sw = new Stopwatch();

                        while (_running)
                        {
                            // sw.Start();

                            var responses = await connection.SendRequestsAsync();

                            // sw.Stop();
                            // Add the latency divided by the pipeline depth

                            var doBreak = false;

                            if (_measuring)
                            {
                                for (var k = 0; k < responses.Length; k++)
                                {
                                    var response = responses[k];

                                    if (response.State == HttpResponseState.Completed)
                                    {
                                        switch (response.StatusCode / 100)
                                        {
                                            case 1: result.Status1xx++; break;
                                            case 2: result.Status2xx++; break;
                                            case 3: result.Status3xx++; break;
                                            case 4: result.Status4xx++; break;
                                            case 5: result.Status5xx++; break;
                                            default: result.SocketErrors++; break;
                                        }
                                    }
                                    else
                                    {
                                        result.SocketErrors++;
                                        doBreak = true;
                                    }
                                }
                            }

                            if (DetailedResponseStats)
                            {
                                Console.WriteLine("Detailed responses info: ");
                                var grouped = responses.GroupBy(r => r.StatusCode);
                                foreach (var group in grouped)
                                {
                                    Console.WriteLine($"\t Status Code: {group.Key} - Count: {group.Count()}");
                                }
                            }

                            if (doBreak)
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        result.SocketErrors++;
                    }
                }
            }

            Console.WriteLine("Connection closed {0}", connectionId);


            return result;
        }
    }
}
