// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benchmarks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Jobs.Bombardier
{
    class Program
    {
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _httpClientHandler;

        static Program()
        {
            // Configuring the http client to trust the self-signed certificate
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);
        }

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Bombardier Client");
            Console.WriteLine("args: " + String.Join(' ', args));

            Console.Write("Measuring first request ... ");
            await MeasureFirstRequest(args);

            // Extracting parameters
            var argsList = args.ToList();

            TryGetArgumentValue("-w", argsList, out int warmup);
            TryGetArgumentValue("-d", argsList, out int duration);
            TryGetArgumentValue("-n", argsList, out int requests);

            if (duration == 0 && requests == 0)
            {
                Console.WriteLine("Couldn't find valid -d and -n arguments (integers)");
                return -1;
            }

            TryGetArgumentValue("-w", argsList, out warmup);

            args = argsList.ToArray();

            string bombardierUrl = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.4/bombardier-windows-amd64.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.4/bombardier-linux-amd64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.4/bombardier-darwin-amd64";
            }
            else
            {
                Console.WriteLine("Unsupported platform");
                return -1;
            }

            var bombardierFileName = Path.GetFileName(bombardierUrl);

            Console.WriteLine($"Downloading bombardier from {bombardierUrl} to {bombardierFileName}");
            using (var downloadStream = await _httpClient.GetStreamAsync(bombardierUrl))
            using (var fileStream = File.Create(bombardierFileName))
            {
                await downloadStream.CopyToAsync(fileStream);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Console.WriteLine($"Setting execute permission on executable {bombardierFileName}");
                    Process.Start("chmod", "+x " + bombardierFileName);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Console.WriteLine($"Allow running bombardier");
                    Process.Start("spctl", "--add " + bombardierFileName);
                }
            }

            var baseArguments = String.Join(' ', args.ToArray()) + " --print r --format json";

            var process = new Process()
            {
                StartInfo = {
                    FileName = bombardierFileName,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true
            };

            var stringBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e != null && e.Data != null)
                {
                    Console.WriteLine(e.Data);

                    lock (stringBuilder)
                    {
                        stringBuilder.AppendLine(e.Data);
                    }
                }
            };

            // Warmup

            if (warmup > 0)
            {
                process.StartInfo.Arguments = $" -d {warmup}s {baseArguments}";

                Console.WriteLine("> bombardier " + process.StartInfo.Arguments);

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return process.ExitCode;
                }
            }

            lock (stringBuilder)
            {
                stringBuilder.Clear();
            }

            process.StartInfo.Arguments = 
                requests > 0
                    ? $" -n {requests} {baseArguments}"
                    : $" -d {duration}s {baseArguments}";

            Console.WriteLine("> bombardier " + process.StartInfo.Arguments);

            process.Start();
            
            BenchmarksEventSource.SetChildProcessId(process.Id);

            process.BeginOutputReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return process.ExitCode;
            }

            string output;

            lock (stringBuilder)
            {
                output = stringBuilder.ToString();
            }

            var document = JObject.Parse(output);

            BenchmarksEventSource.Log.Metadata("bombardier/requests", "max", "sum", "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Log.Metadata("bombardier/badresponses", "max", "sum", "Bad responses", "Non-2xx or 3xx responses", "n0");

            BenchmarksEventSource.Log.Metadata("bombardier/latency/mean", "max", "sum", "Mean latency (us)", "Mean latency (us)", "n0");
            BenchmarksEventSource.Log.Metadata("bombardier/latency/max", "max", "sum", "Max latency (us)", "Max latency (us)", "n0");

            BenchmarksEventSource.Log.Metadata("bombardier/rps/mean", "max", "sum", "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Log.Metadata("bombardier/rps/max", "max", "sum", "Requests/sec (max)", "Max requests per second", "n0");

            BenchmarksEventSource.Log.Metadata("bombardier/raw", "all", "all", "Raw results", "Raw results", "json");

            var total = 
                document["result"]["req1xx"].Value<int>()
                + document["result"]["req2xx"].Value<int>()
                + document["result"]["req3xx"].Value<int>()
                + document["result"]["req3xx"].Value<int>()
                + document["result"]["req4xx"].Value<int>()
                + document["result"]["req5xx"].Value<int>()
                + document["result"]["others"].Value<int>();

            var success = document["result"]["req2xx"].Value<int>() + document["result"]["req3xx"].Value<int>();

            BenchmarksEventSource.Measure("bombardier/requests", total);
            BenchmarksEventSource.Measure("bombardier/badresponses", total - success);

            BenchmarksEventSource.Measure("bombardier/latency/mean", document["result"]["latency"]["mean"].Value<double>());
            BenchmarksEventSource.Measure("bombardier/latency/max", document["result"]["latency"]["max"].Value<double>());

            BenchmarksEventSource.Measure("bombardier/rps/max", document["result"]["rps"]["max"].Value<double>());
            BenchmarksEventSource.Measure("bombardier/rps/mean", document["result"]["rps"]["mean"].Value<double>());

            BenchmarksEventSource.Measure("bombardier/raw", output);

            return 0;
        }

        private static bool TryGetArgumentValue(string argName, List<string> argsList, out int value)
        {
            var argumentIndex = argsList.FindIndex(arg => string.Equals(arg, argName, StringComparison.OrdinalIgnoreCase));
            if (argumentIndex >= 0)
            {
                string copy = argsList[argumentIndex + 1];
                argsList.RemoveAt(argumentIndex);
                argsList.RemoveAt(argumentIndex);

                return int.TryParse(copy, out value) && value > 0;
            }
            else
            {
                value = default;

                return false;
            }
        }

        public static async Task MeasureFirstRequest(string[] args)
        {
            var url = args.FirstOrDefault(arg => arg.StartsWith("http", StringComparison.OrdinalIgnoreCase));

            if (url == null)
            {
                Console.WriteLine("URL not found, skipping first request");
                return;
            }

            var cts = new CancellationTokenSource(5000);
            var httpMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using (var response = await _httpClient.SendAsync(httpMessage, cts.Token))
                {
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"{elapsed} ms");

                    BenchmarksEventSource.Log.Metadata("http/firstrequest", "max", "max", "First Request (ms)", "Time to first request in ms", "n0");
                    BenchmarksEventSource.Measure("http/firstrequest", elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("A timeout occurred while measuring the first request");
            }
        }

    }
}
