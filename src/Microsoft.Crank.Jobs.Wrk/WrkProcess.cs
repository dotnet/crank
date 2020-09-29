// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.EventSources;

namespace Microsoft.Crank.Wrk
{
    static class WrkProcess
    {
        static string _wrkFilename;

        const string WrkUrl = "https://aspnetbenchmarks.blob.core.windows.net/tools/wrk";

        public static async Task MeasureFirstRequest(string[] args)
        {
            var url = args.FirstOrDefault(arg => arg.StartsWith("http", StringComparison.OrdinalIgnoreCase));

            if (url == null)
            {
                Console.WriteLine("URL not found, skipping first request");
                return;
            }

            // Configuring the http client to trust the self-signed certificate
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            httpClientHandler.MaxConnectionsPerServer = 1;

            using (var httpClient = new HttpClient(httpClientHandler))
            {
                var cts = new CancellationTokenSource(5000);
                var httpMessage = new HttpRequestMessage(HttpMethod.Get, url);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    using (var response = await httpClient.SendAsync(httpMessage, cts.Token))
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
        }

        public static async Task<int> RunAsync(string[] args)
        {
            // Do we need to parse latency?
            var parseLatency = args.Any(x => x == "--latency" || x == "-L");
            var tempScriptFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                var wrkFilename = _wrkFilename;
                await ProcessScriptFile(args, tempScriptFile);
                return RunCore(wrkFilename, args, parseLatency);
            }
            catch
            {
                return -1;
            }
            finally
            {
                if (File.Exists(tempScriptFile))
                {
                    File.Delete(tempScriptFile);
                }
            }
        }

        public static async Task DownloadWrkAsync()
        {
            var cacheFolder = Path.Combine(Path.GetTempPath(), ".crank");

            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            _wrkFilename = Path.Combine(cacheFolder, Path.GetFileName(WrkUrl));

            Console.WriteLine($"Downloading wrk from {WrkUrl} to {_wrkFilename}");
            
            using (var httpClient = new HttpClient())
            using (var downloadStream = await httpClient.GetStreamAsync(WrkUrl))
            using (var fileStream = File.Create(_wrkFilename))
            {
                await downloadStream.CopyToAsync(fileStream);
            }

            Process.Start("chmod", "+x " + _wrkFilename);
        }

        static async Task ProcessScriptFile(string[] args, string tempScriptFile)
        {
            using var httpClient = new HttpClient();

            for (var i = 0; i < args.Length - 1; i++)
            {
                // wrk does not support loading scripts from the network. We'll shim it in this client.
                if ((args[i] == "-s" || args[i] == "--script") &&
                    Uri.TryCreate(args[i + 1], UriKind.Absolute, out var uri) &&
                    (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    using var response = await httpClient.GetStreamAsync(uri);

                    using var fileStream = File.Create(tempScriptFile);
                    await response.CopyToAsync(fileStream);

                    args[i + 1] = tempScriptFile;
                }
            }
        }

        static int RunCore(string fileName, string[] args, bool parseLatency)
        {
            // Extracting duration parameters
            string warmup = "";
            string duration = "";

            var argsList = args.ToList();

            var durationIndex = argsList.FindIndex(x => String.Equals(x, "-d", StringComparison.OrdinalIgnoreCase));
            if (durationIndex >= 0)
            {
                duration = argsList[durationIndex + 1];
                argsList.RemoveAt(durationIndex);
                argsList.RemoveAt(durationIndex);
            }
            else
            {
                Console.WriteLine("Couldn't find -d argument");
                return -1;
            }

            var warmupIndex = argsList.FindIndex(x => String.Equals(x, "-w", StringComparison.OrdinalIgnoreCase));
            if (warmupIndex >= 0)
            {
                warmup = argsList[warmupIndex + 1];
                argsList.RemoveAt(warmupIndex);
                argsList.RemoveAt(warmupIndex);
            }

            args = argsList.Select(Quote).ToArray();

            var baseArguments = String.Join(' ', args);

            var process = new Process()
            {
                StartInfo = 
                {
                    FileName = fileName,
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

            if (!String.IsNullOrEmpty(warmup) && warmup != "0s")
            {
                process.StartInfo.Arguments = $"-d {warmup} {baseArguments}";

                Console.WriteLine("> wrk " + process.StartInfo.Arguments);

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

            process.StartInfo.Arguments = $"-d {duration} {baseArguments}";

            Console.WriteLine("> wrk " + process.StartInfo.Arguments);

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

            BenchmarksEventSource.Register("wrk/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Register("wrk/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Register("wrk/latency/mean", Operations.Max, Operations.Sum, "Mean latency (ms)", "Mean latency (ms)", "n2");
            BenchmarksEventSource.Register("wrk/latency/max", Operations.Max, Operations.Sum, "Max latency (ms)", "Max latency (ms)", "n2");
            BenchmarksEventSource.Register("wrk/errors/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");
            BenchmarksEventSource.Register("wrk/errors/socketerrors", Operations.Max, Operations.Sum, "Socket errors", "Socket errors", "n0");
            BenchmarksEventSource.Register("wrk/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");

            const string LatencyPattern = @"\s+{0}\s+([\d\.]+)(\w+)";

            var latencyMatch = Regex.Match(output, String.Format(LatencyPattern, "Latency"));
            BenchmarksEventSource.Measure("wrk/latency/mean", ReadLatency(latencyMatch));

            var rpsMatch = Regex.Match(output, @"Requests/sec:\s*([\d\.]*)");
            if (rpsMatch.Success && rpsMatch.Groups.Count == 2)
            {
                BenchmarksEventSource.Measure("wrk/rps/mean", double.Parse(rpsMatch.Groups[1].Value));
            }

            var throughputMatch = Regex.Match(output, @"Transfer/sec:\s+([\d\.]+)(\w+)");
            BenchmarksEventSource.Measure("wrk/throughput", ReadThroughput(throughputMatch));

            // Max latency is 3rd number after "Latency "
            var maxLatencyMatch = Regex.Match(output, @"\s+Latency\s+[\d\.]+\w+\s+[\d\.]+\w+\s+([\d\.]+)(\w+)");
            BenchmarksEventSource.Measure("wrk/latency/max", ReadLatency(maxLatencyMatch));

            var requestsCountMatch = Regex.Match(output, @"([\d\.]*) requests in ([\d\.]*)(\w*)");
            BenchmarksEventSource.Measure("wrk/requests", ReadRequests(requestsCountMatch));

            var badResponsesMatch = Regex.Match(output, @"Non-2xx or 3xx responses: ([\d\.]*)");
            BenchmarksEventSource.Measure("wrk/errors/badresponses", ReadBadReponses(badResponsesMatch));

            var socketErrorsMatch = Regex.Match(output, @"Socket errors: connect ([\d\.]*), read ([\d\.]*), write ([\d\.]*), timeout ([\d\.]*)");
            BenchmarksEventSource.Measure("wrk/errors/socketerrors", CountSocketErrors(socketErrorsMatch));

            if (parseLatency)
            {
                BenchmarksEventSource.Register("wrk/latency/50", Operations.Max, Operations.Avg, "Latency 50th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Register("wrk/latency/75", Operations.Max, Operations.Avg, "Latency 75th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Register("wrk/latency/90", Operations.Max, Operations.Avg, "Latency 90th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Register("wrk/latency/99", Operations.Max, Operations.Avg, "Latency 99th (ms)", "Latency 50th (ms)", "n2");

                BenchmarksEventSource.Measure("wrk/latency/50", ReadLatency(Regex.Match(output, string.Format(LatencyPattern, "50%"))));
                BenchmarksEventSource.Measure("wrk/latency/75", ReadLatency(Regex.Match(output, string.Format(LatencyPattern, "75%"))));
                BenchmarksEventSource.Measure("wrk/latency/90", ReadLatency(Regex.Match(output, string.Format(LatencyPattern, "90%"))));
                BenchmarksEventSource.Measure("wrk/latency/99", ReadLatency(Regex.Match(output, string.Format(LatencyPattern, "99%"))));
            }

            return 0;
        }

        private static int ReadRequests(Match responseCountMatch)
        {
            if (!responseCountMatch.Success || responseCountMatch.Groups.Count != 4)
            {
                Console.WriteLine("Failed to parse requests");
                return -1;
            }

            try
            {
                return int.Parse(responseCountMatch.Groups[1].Value);
            }
            catch
            {
                Console.WriteLine("Failed to parse requests");
                return -1;
            }
        }

        private static int ReadBadReponses(Match badResponsesMatch)
        {
            if (!badResponsesMatch.Success)
            {
                // wrk does not display the expected line when no bad responses occur
                return 0;
            }

            if (!badResponsesMatch.Success || badResponsesMatch.Groups.Count != 2)
            {
                Console.WriteLine("Failed to parse bad responses");
                return 0;
            }

            try
            {
                return int.Parse(badResponsesMatch.Groups[1].Value);
            }
            catch
            {
                Console.WriteLine("Failed to parse bad responses");
                return 0;
            }
        }

        private static int CountSocketErrors(Match socketErrorsMatch)
        {
            if (!socketErrorsMatch.Success)
            {
                // wrk does not display the expected line when no errors occur
                return 0;
            }

            if (socketErrorsMatch.Groups.Count != 5)
            {
                Console.WriteLine("Failed to parse socket errors");
                return 0;
            }

            try
            {
                return
                    int.Parse(socketErrorsMatch.Groups[1].Value) +
                    int.Parse(socketErrorsMatch.Groups[2].Value) +
                    int.Parse(socketErrorsMatch.Groups[3].Value) +
                    int.Parse(socketErrorsMatch.Groups[4].Value)
                    ;

            }
            catch
            {
                Console.WriteLine("Failed to parse socket errors");
                return 0;
            }

        }

        private static double ReadLatency(Match match)
        {
            if (!match.Success || match.Groups.Count != 3)
            {
                Console.WriteLine("Failed to parse latency");
                return -1;
            }

            try
            {
                var value = double.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value;

                switch (unit.ToLowerInvariant())
                {
                    case "s": return value * 1000;
                    case "ms": return value;
                    case "us": return value / 1000;

                    default:
                        Console.WriteLine("Failed to parse latency unit: " + unit);
                        return -1;
                }
            }
            catch
            {
                Console.WriteLine("Failed to parse latency");
                return -1;
            }
        }

        private static double ReadThroughput(Match match)
        {
            if (!match.Success || match.Groups.Count != 3)
            {
                Console.WriteLine("Failed to parse throughput");
                return -1;
            }

            try
            {
                var value = double.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value;

                switch (unit.ToLowerInvariant())
                {
                    case "b": return value / 1024 / 1024;
                    case "kb": return value / 1024;
                    case "mb": return value;
                    case "gb": return value * 1024;

                    default:
                        Console.WriteLine("Failed to parse throughput unit: " + unit);
                        return -1;
                }
            }
            catch
            {
                Console.WriteLine("Failed to parse throughput");
                return -1;
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
    }
}
