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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.EventSources;

namespace Microsoft.Crank.Jobs.K6
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
            Console.WriteLine("K6 Client");
            Console.WriteLine("args: " + String.Join(' ', args));

            Console.Write("Measuring first request ... ");
            await MeasureFirstRequest(args);

            // Extracting parameters
            var argsList = args.ToList();

            TryGetArgumentValue("--warmup", argsList, out int warmup);
            TryGetArgumentValue("--duration", argsList, out int duration);

            args = argsList.ToArray();

            string k6Url = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64 : k6Url = "https://aspnetbenchmarks.blob.core.windows.net/tools/k6-win-amd64.exe"; break;
                    default: Console.WriteLine("Unsupported platform"); return -1;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.Arm64: k6Url = "https://aspnetbenchmarks.blob.core.windows.net/tools/k6-linux-arm64"; break;
                    case Architecture.X64: k6Url = "https://aspnetbenchmarks.blob.core.windows.net/tools/k6-linux-amd64"; break;
                    default: Console.WriteLine("Unsupported platform"); return -1;
                }
            }
            else
            {
                Console.WriteLine("Unsupported platform");
                return -1;
            }

            var k6FileName = Path.Combine(Path.GetTempPath(), ".crank", Path.GetFileName(k6Url));

            if (!File.Exists(k6FileName))
            {            
                Directory.CreateDirectory(Path.GetDirectoryName(k6FileName));

                Console.WriteLine($"Downloading K6 from {k6Url} to {k6FileName}");

                using (var downloadStream = await _httpClient.GetStreamAsync(k6Url))
                using (var fileStream = File.Create(k6FileName))
                {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                    await downloadStream.FlushAsync();
                }
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine($"Setting execute permission on executable {k6FileName}");
                Process.Start("chmod", "+x " + k6FileName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine($"Setting execute permission on executable {k6FileName}");
                Process.Start("spctl", "--add " + k6FileName);
            }

            args = argsList.Select(Quote).ToArray();

            var scriptFilename = "./scripts/default.js";

            var baseArguments = $"run {scriptFilename}";
            var extraArguments = String.Join(' ', args);

            var process = new Process()
            {
                StartInfo = {
                    FileName = k6FileName,
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
                process.StartInfo.Arguments = $"{baseArguments} {extraArguments} --duration {warmup}s";

                Console.WriteLine("> k6 " + process.StartInfo.Arguments);

                await StartProcessWithRetriesAsync(process);
                
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Failed to run K6.");
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

            process.StartInfo.Arguments = $"{baseArguments} {extraArguments} --duration {duration}s";

            Console.WriteLine("> k6 " + process.StartInfo.Arguments);
                
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

            BenchmarksEventSource.Register("http/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
            BenchmarksEventSource.Register("http/requests/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");

            BenchmarksEventSource.Register("http/latency/50", Operations.Max, Operations.Max, "Latency 50th (ms)", "Latency 50th (ms)", "n2");
            BenchmarksEventSource.Register("http/latency/90", Operations.Max, Operations.Max, "Latency 90th (ms)", "Latency 90th (ms)", "n2");
            BenchmarksEventSource.Register("http/latency/95", Operations.Max, Operations.Max, "Latency 95th (ms)", "Latency 95th (ms)", "n2");

            BenchmarksEventSource.Register("http/latency/mean", Operations.Max, Operations.Avg, "Mean latency (ms)", "Mean latency (ms)", "n2");
            BenchmarksEventSource.Register("http/latency/max", Operations.Max, Operations.Max, "Max latency (ms)", "Max latency (ms)", "n2");

            BenchmarksEventSource.Register("http/rps/mean", Operations.Max, Operations.Sum, "Requests/sec", "Requests per second", "n0");
            BenchmarksEventSource.Register("http/throughput", Operations.Max, Operations.Sum, "Read throughput (MB/s)", "Read throughput (MB/s)", "n2");

            BenchmarksEventSource.Register("k6/raw", Operations.All, Operations.All, "Raw results", "Raw results", "json");

            var summary = await File.ReadAllTextAsync("summary.json");
            var document = JsonDocument.Parse(summary);
            var metrics = document.RootElement.GetProperty("metrics");

            var reqFailed = metrics.GetProperty("http_req_failed");

            BenchmarksEventSource.Measure("http/requests", reqFailed.GetProperty("values").GetProperty("fails").GetInt64());
            BenchmarksEventSource.Measure("http/requests/badresponses", reqFailed.GetProperty("values").GetProperty("passes").GetInt64());

            var reqDurationValues = metrics.GetProperty("http_req_duration").GetProperty("values");
            
            BenchmarksEventSource.Measure("http/latency/50", reqDurationValues.GetProperty("med").GetDouble());
            BenchmarksEventSource.Measure("http/latency/90", reqDurationValues.GetProperty("p(90)").GetDouble());
            BenchmarksEventSource.Measure("http/latency/95", reqDurationValues.GetProperty("p(95)").GetDouble());

            BenchmarksEventSource.Measure("http/latency/mean", reqDurationValues.GetProperty("avg").GetDouble());
            BenchmarksEventSource.Measure("http/latency/max", reqDurationValues.GetProperty("max").GetDouble());

            var iterations = metrics.GetProperty("iterations").GetProperty("values");

            BenchmarksEventSource.Measure("http/rps/mean", iterations.GetProperty("rate").GetDouble());

            BenchmarksEventSource.Measure("k6/raw", summary);

            var dataReceived = metrics.GetProperty("data_received").GetProperty("values");

            // B/s to MB/s
            BenchmarksEventSource.Measure("http/throughput", dataReceived.GetProperty("rate").GetDouble() / 1024 / 1024);

            return 0;
        }

        private static async Task<string> DownloadToTempFile(string url)
        {
            Console.WriteLine($"Downloading file {url}");
            var tempFile = Path.GetTempFileName();

            using (var downloadStream = await _httpClient.GetStreamAsync(url))
            using (var fileStream = File.Create(tempFile))
            {
                await downloadStream.CopyToAsync(fileStream);
            }

            return tempFile;
        }

        private static void CleanTempFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
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
            var url = args.FirstOrDefault(arg => arg.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));

            if (url == null)
            {
                Console.WriteLine("URL not found, skipping first request");
                return;
            }

            url = url.Substring(4);

            var cts = new CancellationTokenSource(30000);
            var httpMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var stopwatch = Stopwatch.StartNew();

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
            catch (HttpRequestException)
            {
                Console.WriteLine("A connection exception occurred while measuring the first request");
            }
            catch (Exception e)
            {
                Console.WriteLine("An unexpected exception occurred while measuring the first request:");
                Console.WriteLine(e.ToString());
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
