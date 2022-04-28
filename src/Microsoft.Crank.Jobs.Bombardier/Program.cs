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
using Microsoft.Crank.EventSources;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Jobs.Bombardier
{
    static class Program
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
            TryGetArgumentValue("-o", argsList, out string outputFormat);
            TryGetArgumentValue("-f", argsList, out string bodyFile);

            if (duration == 0 && requests == 0)
            {
                Console.WriteLine("Couldn't find valid -d and -n arguments (integers)");
                return -1;
            }

            if (string.IsNullOrEmpty(outputFormat))
            {
                outputFormat = "json";
            }
            else if ((outputFormat != "json") && (outputFormat != "plain-text"))
            {
                Console.WriteLine("Value value for -o is json or plain-text");
                return -1;
            }

            args = argsList.ToArray();

            string bombardierUrl = null;
            string bombardierVersion = "1.2.5";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-windows-amd64.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.Arm64 : bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-linux-arm64"; break;
                    case Architecture.Arm : bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-linux-arm"; break;
                    default: bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-linux-amd64"; break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-darwin-amd64";
            }
            else
            {
                Console.WriteLine("Unsupported platform");
                return -1;
            }

            var bombardierFileName = Path.Combine(Path.GetTempPath(), ".crank", bombardierVersion, Path.GetFileName(bombardierUrl));

            if (!File.Exists(bombardierFileName))
            {            
                Directory.CreateDirectory(Path.GetDirectoryName(bombardierFileName));

                Console.WriteLine($"Downloading bombardier from {bombardierUrl} to {bombardierFileName}");

                using (var downloadStream = await _httpClient.GetStreamAsync(bombardierUrl))
                using (var fileStream = File.Create(bombardierFileName))
                {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                    await downloadStream.FlushAsync();
                }
            }
            
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

            if (!String.IsNullOrEmpty(bodyFile))
            {
                if (bodyFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Downloading body file {bodyFile}");
                    var tempFile = Path.GetTempFileName();

                    using (var downloadStream = await _httpClient.GetStreamAsync(bodyFile))
                    using (var fileStream = File.Create(tempFile))
                    {
                        await downloadStream.CopyToAsync(fileStream);
                    }

                    bodyFile = tempFile;
                }

                argsList.Add("-f");
                argsList.Add(bodyFile);
            }

            args = argsList.Select(Quote).ToArray();

            var baseArguments = String.Join(' ', args) + $" --print r --format {outputFormat}";

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

                await StartProcessWithRetriesAsync(process);
                
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Failed to run bombardier.");
                    return process.ExitCode;
                }
            }
            else
            {
                Console.WriteLine("Warmup skipped");
            }

            lock (stringBuilder)
            {
                stringBuilder.Clear();
            }

            if (duration > 0)
            {
                process.StartInfo.Arguments =
                    requests > 0
                        ? $" -n {requests} {baseArguments}"
                        : $" -d {duration}s {baseArguments}";

                Console.WriteLine("> bombardier " + process.StartInfo.Arguments);
                
                await StartProcessWithRetriesAsync(process);

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

                if (outputFormat != "json")
                {
                    return 0;
                }

                var document = JObject.Parse(output);

                BenchmarksEventSource.Register("bombardier/requests;http/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
                BenchmarksEventSource.Register("bombardier/badresponses;http/requests/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");

                BenchmarksEventSource.Register("bombardier/latency/50;http/latency/50", Operations.Max, Operations.Max, "Latency 50th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Register("bombardier/latency/75;http/latency/75", Operations.Max, Operations.Max, "Latency 75th (ms)", "Latency 75th (ms)", "n2");
                BenchmarksEventSource.Register("bombardier/latency/90;http/latency/90", Operations.Max, Operations.Max, "Latency 90th (ms)", "Latency 90th (ms)", "n2");
                BenchmarksEventSource.Register("bombardier/latency/95;http/latency/95", Operations.Max, Operations.Max, "Latency 95th (ms)", "Latency 95th (ms)", "n2");
                BenchmarksEventSource.Register("bombardier/latency/99;http/latency/99", Operations.Max, Operations.Max, "Latency 99th (ms)", "Latency 99th (ms)", "n2");

                BenchmarksEventSource.Register("bombardier/latency/mean;http/latency/mean", Operations.Max, Operations.Avg, "Mean latency (ms)", "Mean latency (ms)", "n2");
                BenchmarksEventSource.Register("bombardier/latency/max;http/latency/max", Operations.Max, Operations.Max, "Max latency (ms)", "Max latency (ms)", "n2");

                BenchmarksEventSource.Register("bombardier/rps/mean;http/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
                BenchmarksEventSource.Register("bombardier/rps/max;http/rps/max", Operations.Max, Operations.Sum, "Requests/sec (max)", "Max requests per second", "n0");
                BenchmarksEventSource.Register("bombardier/throughput;http/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");

                BenchmarksEventSource.Register("bombardier/raw", Operations.All, Operations.All, "Raw results", "Raw results", "json");

                var total =
                    document["result"]["req1xx"].Value<long>()
                    + document["result"]["req2xx"].Value<long>()
                    + document["result"]["req3xx"].Value<long>()
                    + document["result"]["req4xx"].Value<long>()
                    + document["result"]["req5xx"].Value<long>()
                    + document["result"]["others"].Value<long>();

                var success = document["result"]["req2xx"].Value<long>() + document["result"]["req3xx"].Value<long>();

                BenchmarksEventSource.Measure("bombardier/requests;http/requests", total);
                BenchmarksEventSource.Measure("bombardier/badresponses;http/requests/badresponses", total - success);

                BenchmarksEventSource.Measure("bombardier/latency/50;http/latency/50", document["result"]["latency"]["percentiles"]["50"].Value<double>().ToMilliseconds());
                BenchmarksEventSource.Measure("bombardier/latency/75;http/latency/75", document["result"]["latency"]["percentiles"]["75"].Value<double>().ToMilliseconds());
                BenchmarksEventSource.Measure("bombardier/latency/90;http/latency/90", document["result"]["latency"]["percentiles"]["90"].Value<double>().ToMilliseconds());
                BenchmarksEventSource.Measure("bombardier/latency/95;http/latency/95", document["result"]["latency"]["percentiles"]["95"].Value<double>().ToMilliseconds());
                BenchmarksEventSource.Measure("bombardier/latency/99;http/latency/99", document["result"]["latency"]["percentiles"]["99"].Value<double>().ToMilliseconds());

                BenchmarksEventSource.Measure("bombardier/latency/mean;http/latency/mean", document["result"]["latency"]["mean"].Value<double>().ToMilliseconds());
                BenchmarksEventSource.Measure("bombardier/latency/max;http/latency/max", document["result"]["latency"]["max"].Value<double>().ToMilliseconds());

                BenchmarksEventSource.Measure("bombardier/rps/max;http/rps/max", document["result"]["rps"]["max"].Value<double>());
                BenchmarksEventSource.Measure("bombardier/rps/mean;http/rps/mean", document["result"]["rps"]["mean"].Value<double>());

                BenchmarksEventSource.Measure("bombardier/raw", output);

                var bytesPerSecond = document["result"]["bytesRead"].Value<long>() / document["result"]["timeTakenSeconds"].Value<double>();

                // B/s to MB/s
                BenchmarksEventSource.Measure("bombardier/throughput;http/throughput", bytesPerSecond / 1024 / 1024);
            }
            else
            {
                Console.WriteLine("Benchmark skipped");
            }

            // Clean temporary files
            try
            {
                File.Delete(bodyFile);
            }
            catch
            {
            }

            return 0;
        }

        private static async Task StartProcessWithRetriesAsync(Process process)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    process.Start();
                    break;
                }
                catch (Exception e)
                {
                    // The process might fail with a permissions exception during unit tests
                    await Task.Delay(500);
                    Console.WriteLine("Error, retrying: " + e.Message);
                }
            }
        }

        private static bool TryGetArgumentValue<T>(string argName, List<string> argsList, out T value)
        {
            var argumentIndex = argsList.FindIndex(arg => string.Equals(arg, argName, StringComparison.OrdinalIgnoreCase));
            if (argumentIndex >= 0)
            {
                string copy = argsList[argumentIndex + 1];
                argsList.RemoveAt(argumentIndex);
                argsList.RemoveAt(argumentIndex);

                try
                {
                    value = (T) Convert.ChangeType(copy, typeof(T));
                    return true;
                }
                catch
                {
                }
            }

            value = default(T);
            return false;
        }

        public static async Task MeasureFirstRequest(string[] args)
        {
            var url = args.FirstOrDefault(arg => arg.StartsWith("http", StringComparison.OrdinalIgnoreCase));

            if (url == null)
            {
                Console.WriteLine("URL not found, skipping first request");
                return;
            }

            var cts = new CancellationTokenSource(30000);
            var httpMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                using (var response = await _httpClient.SendAsync(httpMessage, cts.Token))
                {
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"{elapsed} ms");

                    BenchmarksEventSource.Register("http/firstrequest", Operations.Max, Operations.Max, "First Request (ms)", "Time to first request in ms", "n0");
                    BenchmarksEventSource.Measure("http/firstrequest", elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("A timeout occurred while measuring the first request");
            }
        }

        private static string Quote(string s)
        {
            // Wraps a string in double-quotes if it contains a space

            if (s.Contains(' '))
            {
                return "\"" + s + "\"";
            }

            return s;
        }

        private static double ToMilliseconds(this double microseconds)
        {
            if (microseconds <= 0)
            {
                return microseconds;
            }

            return microseconds / 1000;
        }
    }
}
