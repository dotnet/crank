// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Jobs.Wrk2
{
    class Program
    {
        const string Wrk2Url = "https://aspnetbenchmarks.blob.core.windows.net/tools/wrk2";

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("WRK2 Client");
            Console.WriteLine("args: " + String.Join(' ', args));

            var wrk2Filename = await DownloadWrk2Async();

            Console.Write("Measuring first request ... ");
            await MeasureFirstRequest(args);

            // Do we need to parse latency?
            var parseLatency = args.Any(x => x == "--latency" || x == "-L");

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

            args = argsList.ToArray();

            var baseArguments = String.Join(' ', args.ToArray());

            var process = new Process()
            {
                StartInfo = {
                    FileName = wrk2Filename,
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
                process.StartInfo.Arguments = $" -d {warmup} {baseArguments}";

                Console.WriteLine("> wrk2 " + process.StartInfo.Arguments);

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

            process.StartInfo.Arguments = $" -d {duration} {baseArguments}";

            Console.WriteLine("> wrk2 " + process.StartInfo.Arguments);

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

            BenchmarksEventSource.Log.Metadata("wrk2/rps/mean", "max", "sum", "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Log.Metadata("wrk2/requests", "max", "sum", "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Log.Metadata("wrk2/latency/mean", "max", "sum", "Mean latency (ms)", "Mean latency (ms)", "n2");
            BenchmarksEventSource.Log.Metadata("wrk2/latency/max", "max", "sum", "Max latency (ms)", "Max latency (ms)", "n2");
            BenchmarksEventSource.Log.Metadata("wrk2/errors/badresponses", "max", "sum", "Bad responses", "Non-2xx or 3xx responses", "n0");
            BenchmarksEventSource.Log.Metadata("wrk2/errors/socketerrors", "max", "sum", "Socket errors", "Socket errors", "n0");

            var rpsMatch = Regex.Match(output, @"Requests/sec:\s*([\d\.]*)");
            if (rpsMatch.Success && rpsMatch.Groups.Count == 2)
            {
                BenchmarksEventSource.Measure("wrk2/rps/mean", double.Parse(rpsMatch.Groups[1].Value));
            }

            const string LatencyPattern = @"\s+{0}\s*([\d\.]+)([a-z]+)";

            var avgLatencyMatch = Regex.Match(output, String.Format(LatencyPattern, "Latency"));
            BenchmarksEventSource.Measure("wrk2/latency/mean", ReadLatency(avgLatencyMatch));

            // Max latency is 3rd number after "Latency "
            var maxLatencyMatch = Regex.Match(output, @"\s+Latency\s+[\d\.]+\w+\s+[\d\.]+\w+\s+([\d\.]+)(\w+)");
            BenchmarksEventSource.Measure("wrk2/latency/max", ReadLatency(maxLatencyMatch));

            var requestsCountMatch = Regex.Match(output, @"([\d\.]*) requests in ([\d\.]*)(\w*)");
            BenchmarksEventSource.Measure("wrk2/requests", ReadRequests(requestsCountMatch));

            var badResponsesMatch = Regex.Match(output, @"Non-2xx or 3xx responses: ([\d\.]*)");
            BenchmarksEventSource.Measure("wrk2/errors/badresponses", ReadBadReponses(badResponsesMatch));

            var socketErrorsMatch = Regex.Match(output, @"Socket errors: connect ([\d\.]*), read ([\d\.]*), write ([\d\.]*), timeout ([\d\.]*)");
            BenchmarksEventSource.Measure("wrk2/errors/socketerrors", CountSocketErrors(socketErrorsMatch));

            if (parseLatency)
            {
                BenchmarksEventSource.Log.Metadata("wrk2/latency/50", "max", "avg", "Latency 50th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/75", "max", "avg", "Latency 75th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/90", "max", "avg", "Latency 90th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/99", "max", "avg", "Latency 99th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/99.9", "max", "avg", "Latency 99.9th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/99.99", "max", "avg", "Latency 99.99th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/99.999", "max", "avg", "Latency 99.999th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/100", "max", "avg", "Latency 100th (ms)", "Latency 50th (ms)", "n2");
                BenchmarksEventSource.Log.Metadata("wrk2/latency/distribution", "all", "all", "Latency distribution", "Latency distribution", "json");

                BenchmarksEventSource.Measure("wrk2/latency/50", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "50\\.000%"))));
                BenchmarksEventSource.Measure("wrk2/latency/75", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "75\\.000%"))));
                BenchmarksEventSource.Measure("wrk2/latency/90", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "90\\.000%"))));
                BenchmarksEventSource.Measure("wrk2/latency/99", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "99\\.000%"))));
                BenchmarksEventSource.Measure("wrk2/latency/99.9", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "99\\.900%"))));
                BenchmarksEventSource.Measure("wrk2/latency/99.99", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "99\\.990%"))));
                BenchmarksEventSource.Measure("wrk2/latency/99.999", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "99\\.999%"))));
                BenchmarksEventSource.Measure("wrk2/latency/100", ReadLatency(Regex.Match(output, String.Format(LatencyPattern, "100\\.000%"))));

                using(var sr = new StringReader(output))
                {
                    var line = "";

                    do
                    {
                        line = sr.ReadLine();
                    } while (line != null && !line.Contains("Detailed Percentile spectrum:"));

                    var doc = new JArray();

                    if (line != null)
                    {
                        sr.ReadLine();
                        sr.ReadLine();

                        line = sr.ReadLine();

                        while (line != null && !line.StartsWith("#"))
                        {
                            Console.WriteLine("Analyzing: " + line);

                            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            doc.Add(
                                new JObject(
                                    new JProperty("latency_us", decimal.Parse(values[0], CultureInfo.InvariantCulture)), 
                                    new JProperty("count", decimal.Parse(values[2], CultureInfo.InvariantCulture)),
                                    new JProperty("percentile", decimal.Parse(values[1], CultureInfo.InvariantCulture))
                                    ));

                            line = sr.ReadLine();
                        }
                    }

                    BenchmarksEventSource.Measure("wrk2/latency/distribution", doc.ToString());
                }
            }

            return 0;
        }

        private static TimeSpan ReadDuration(Match responseCountMatch)
        {
            if (!responseCountMatch.Success || responseCountMatch.Groups.Count != 4)
            {
                Console.WriteLine("Failed to parse duration");
                return TimeSpan.Zero;
            }

            try
            {
                var value = double.Parse(responseCountMatch.Groups[2].Value);

                var unit = responseCountMatch.Groups[3].Value;

                switch (unit)
                {
                    case "s": return TimeSpan.FromSeconds(value);
                    case "m": return TimeSpan.FromMinutes(value);
                    case "h": return TimeSpan.FromHours(value);

                    default: throw new NotSupportedException("Failed to parse duration unit: " + unit);
                }
            }
            catch
            {
                Console.WriteLine("Failed to parse durations");
                return TimeSpan.Zero;
            }
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

                switch (unit)
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
            using(var httpClient = new HttpClient(httpClientHandler))
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

        public static async Task<string> DownloadWrk2Async()
        {
            Console.Write("Downloading wrk2 ... ");
            var wrk2Filename = Path.GetFileName(Wrk2Url);

            // Search for cached file
            var cacheFolder = Path.Combine(Path.GetTempPath(), ".benchmarks");

            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            var cacheFilename = Path.Combine(cacheFolder, wrk2Filename);

            if (!File.Exists(cacheFilename))
            {
                using (var httpClient = new HttpClient())
                using (var downloadStream = await httpClient.GetStreamAsync(Wrk2Url))
                using (var fileStream = File.Create(wrk2Filename))
                {
                    await downloadStream.CopyToAsync(fileStream);
                }
            }
            else
            {
                File.Copy(cacheFilename, wrk2Filename);
            }

            Process.Start("chmod", "+x " + wrk2Filename);

            return wrk2Filename;
        }
    }
}
