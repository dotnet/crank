// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Crank.Models;
using Microsoft.Crank.Controller.Ignore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Microsoft.Crank.Controller
{
    public class JobConnection
    {
        readonly HttpClient _httpClient;
        readonly HttpClientHandler _httpClientHandler;

        static List<string> _temporaryFolders = new List<string>();

        private static string _filecache = null;

        // The uri of the server
        private readonly Uri _serverUri;

        private string _serverJobUri;
        private bool _keepAlive;
        private DateTime? _runningUtc;
        private string _jobName;
        private bool _traceCollected;

        private int _outputCursor;
        private int _buildCursor;
        private Dictionary<string, string> _benchmarkDotNetResults = new Dictionary<string, string>();

        public JobConnection(Job definition, Uri serverUri)
        {
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;

            Job = definition;
            _serverUri = serverUri;
        }

        public void ConfigureRelay(string token)
        {
            _httpClient.DefaultRequestHeaders.Add("ServiceBusAuthorization", token);
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using (var cts = new CancellationTokenSource(10000))
                {
                    var response = await _httpClient.GetAsync("", cts.Token);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public Job Job { get; private set; }

        public string ServerJobUri => _serverJobUri;

        public async Task<string> StartAsync(string jobName)
        {
            _jobName = jobName;

            var content = JsonConvert.SerializeObject(Job);

            Log.Write($"Starting job '{_jobName}' ...");

            Log.Verbose($"POST {Combine(_serverUri, "/jobs")} {content}...");

            var response = await _httpClient.PostAsync(Combine(_serverUri, "/jobs"), new StringContent(content, Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            _serverJobUri = Combine(_serverUri, response.Headers.Location.ToString()).ToString();

            Log.Write($"Submitted job: {_serverJobUri}");

            // When a job is submitted it has the state New
            // Waiting for the job to be selected (Initializing), then upload custom files and send the start

            while (true)
            {
                var state = await GetStateAsync();

                if (state == JobState.Failed)
                {
                    throw new Exception("Error while queuing job");
                }

                if (state == JobState.Deleted)
                {
                    throw new Exception("Job was canceled by the agent");
                }
                
                if (state == JobState.Initializing)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            while (true)
            {
                // Once the job is in Initializing (previous loop) we need to download the full state
                // since the source properties might have been changed to dismiss source upload.
                
                Job = await GetJobAsync();

                #region Ensure the job is valid

                if (Job.ServerVersion < 4)
                {
                    throw new Exception($"Invalid server version ({Job.ServerVersion}), please update your server to match this driver version.");
                }

                if (!Job.OperatingSystem.HasValue)
                {
                    throw new InvalidOperationException("Server is required to set ServerJob.OperatingSystem.");
                }

                #endregion

                if (Job.State == JobState.Initializing)
                {
                    Log.Write($"'{_jobName}' has been selected by the server ...");

                    // Start the keep-alive loop before uploading any file as slow networks could otherwise
                    // trigger driver timeouts
                    StartKeepAlive();

                    // Uploading source code
                    if (!String.IsNullOrEmpty(Job.Source.LocalFolder))
                    {
                        // Zipping the folder
                        var tempFilename = Path.GetTempFileName();
                        File.Delete(tempFilename);

                        Log.Write($"Using local folder: \"{Job.Source.LocalFolder}\"");

                        var sourceDir = Job.Source.LocalFolder;

                        if (!File.Exists(Path.Combine(sourceDir, ".gitignore")))
                        {
                            ZipFile.CreateFromDirectory(sourceDir, tempFilename);
                        }
                        else
                        {
                            Log.Verbose(".gitignore file found");
                            DoCreateFromDirectory(sourceDir, tempFilename);
                        }

                        var result = await UploadFileAsync(tempFilename, Combine(_serverJobUri, "/source"));

                        File.Delete(tempFilename);

                        if (result != 0)
                        {
                            throw new Exception("Error while uploading source files");
                        }
                    }

                    // Upload custom package contents
                    foreach (var outputArchiveValue in Job.Options.OutputArchives)
                    {
                        var outputFileSegments = outputArchiveValue.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);

                        string localArchiveFilename = outputFileSegments[0];

                        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                        if (Directory.Exists(tempFolder))
                        {
                            Directory.Delete(tempFolder, true);
                        }

                        Directory.CreateDirectory(tempFolder);

                        _temporaryFolders.Add(tempFolder);

                        // Download the archive, while pinging the server to keep the job alive
                        if (outputArchiveValue.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            localArchiveFilename = await DownloadTemporaryFileAsync(localArchiveFilename);
                        }

                        ZipFile.ExtractToDirectory(localArchiveFilename, tempFolder);

                        if (outputFileSegments.Length > 1)
                        {
                            Job.Options.OutputFiles.Add(Path.Combine(tempFolder, "*.*") + ";" + outputFileSegments[1]);
                        }
                        else
                        {
                            Job.Options.OutputFiles.Add(Path.Combine(tempFolder, "*.*"));
                        }
                    }
                

                    // Upload custom build package contents
                    foreach (var buildArchiveValue in Job.Options.BuildArchives)
                    {
                        var buildFileSegments = buildArchiveValue.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);

                        string localArchiveFilename = buildFileSegments[0];

                        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                        if (Directory.Exists(tempFolder))
                        {
                            Directory.Delete(tempFolder, true);
                        }

                        Directory.CreateDirectory(tempFolder);

                        _temporaryFolders.Add(tempFolder);

                        // Download the archive, while pinging the server to keep the job alive
                        if (buildArchiveValue.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            localArchiveFilename = await DownloadTemporaryFileAsync(localArchiveFilename);
                        }

                        ZipFile.ExtractToDirectory(localArchiveFilename, tempFolder);

                        if (buildFileSegments.Length > 1)
                        {
                            Job.Options.BuildFiles.Add(Path.Combine(tempFolder, "*.*") + ";" + buildFileSegments[1]);
                        }
                        else
                        {
                            Job.Options.BuildFiles.Add(Path.Combine(tempFolder, "*.*"));
                        }
                    }

                    // Uploading build files
                    if (Job.Options.BuildFiles.Any())
                    {
                        foreach (var buildFileValue in Job.Options.BuildFiles)
                        {
                            var buildFileSegments = buildFileValue.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);

                            var shouldSearchRecursively = buildFileSegments[0].Contains("**");
                            buildFileSegments[0] = buildFileSegments[0].Replace("**\\", "").Replace("**/", "");

                            foreach (var resolvedFile in Directory.GetFiles(Path.GetDirectoryName(buildFileSegments[0]), Path.GetFileName(buildFileSegments[0]), shouldSearchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                            {                                var resolvedFileWithDestination = resolvedFile;

                                if (buildFileSegments.Length > 1)
                                {
                                    resolvedFileWithDestination += ";" + buildFileSegments[1] + Path.GetDirectoryName(resolvedFile).Substring(Path.GetDirectoryName(buildFileSegments[0]).Length) + "/" + Path.GetFileName(resolvedFileWithDestination);
                                }

                                var result = await UploadFileAsync(resolvedFileWithDestination, _serverJobUri + "/build");

                                if (result != 0)
                                {
                                    throw new Exception("Error while uploading build files");
                                }
                            }
                        }
                    }

                    // Uploading attachments
                    if (Job.Options.OutputFiles.Any())
                    {
                        foreach (var outputFileValue in Job.Options.OutputFiles)
                        {
                            var outputFileSegments = outputFileValue.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);

                            var shouldSearchRecursively = outputFileSegments[0].Contains("**");
                            outputFileSegments[0] = outputFileSegments[0].Replace("**\\", "").Replace("**/", "");

                            var someFilesWereUploaded = false;

                            // If the argument doesn't contain a folder, it can fail the next statement, so assume it's using the local folder
                            if (Path.GetDirectoryName(outputFileSegments[0]) == String.Empty)
                            {
                                outputFileSegments[0] = Path.Combine(".", outputFileSegments[0]);
                            }

                            foreach (var resolvedFile in Directory.GetFiles(Path.GetDirectoryName(outputFileSegments[0]), Path.GetFileName(outputFileSegments[0]), shouldSearchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                            {
                                var resolvedFileWithDestination = resolvedFile;

                                if (outputFileSegments.Length > 1)
                                {
                                    resolvedFileWithDestination += ";" + outputFileSegments[1] + Path.GetDirectoryName(resolvedFile).Substring(Path.GetDirectoryName(outputFileSegments[0]).Length) + "/" + Path.GetFileName(resolvedFileWithDestination);
                                }

                                var result = await UploadFileAsync(resolvedFileWithDestination, Combine(_serverJobUri, "/attachment"));

                                if (result != 0)
                                {
                                    throw new Exception("Error while uploading output files");
                                }
                                
                                someFilesWereUploaded = true;
                            }

                            if (!someFilesWereUploaded)
                            {
                                Log.WriteWarning($"The argument '{outputFileSegments[0]}' didn't match any existing file.");
                            }
                        }
                    }

                    response = await _httpClient.PostAsync(Combine(_serverJobUri, "/start"), new StringContent(""));
                    responseContent = await response.Content.ReadAsStringAsync();
                    Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");
                    response.EnsureSuccessStatusCode();

                    Log.Write($"'{_jobName}' is now building ... {_serverJobUri}/buildlog");

                    break;
                }
                else
                {
                    await Task.Delay(1000);
                }
            }

            // Tracking the job until it stops

            // TODO: Add a step on the server before build and start, such that start can be as fast as possible
            // "start" => "build"
            // + new start call

            var currentState = await GetStateAsync();

            while (true)
            {
                var previouState = currentState;
                currentState = await GetStateAsync();

                if (currentState == JobState.Running)
                {
                    if (previouState != JobState.Running)
                    {
                        Log.Write($"'{_jobName}' is running ... {_serverJobUri}/output");
                        _runningUtc = DateTime.UtcNow;
                    }

                    return _serverJobUri;
                }
                else if (currentState == JobState.Failed)
                {
                    Log.Write($"'{_jobName}' failed on agent, stopping...");

                    // Refreshing Job state to display the error
                    Job = await GetJobAsync();

                    Log.WriteError(Job.Error, notime: true);

                    // Returning will also send a Delete message to the server
                    return null;
                }
                else if (currentState == JobState.NotSupported)
                {
                    Log.Write("Server does not support this job configuration.");
                    return null;
                }
                else if (currentState == JobState.Stopped)
                {
                    Log.Write($"'{_jobName}' finished");

                    // If there is no ReadyStateText defined, the server will never be in Running state
                    // and we'll reach the Stopped state eventually, but that's a normal behavior.
                    if (Job.WaitForExit)
                    {
                        return Job.Url;
                    }

                    throw new Exception("Job finished unexpectedly");
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Stops the job on the server without deleting it.
        /// </summary>
        public async Task StopAsync()
        {
            StopKeepAlive();

            Log.Write($"Stopping job '{_jobName}' ...");

            var response = await _httpClient.PostAsync(Combine(_serverJobUri, "/stop"), new StringContent(""));
            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");
            var jobStoppedUtc = DateTime.UtcNow;

            // Wait for Stop state
            while (true)
            {
                await Task.Delay(1000);

                var state = await GetStateAsync();

                if (state == JobState.Stopped || state == JobState.Failed)
                {
                    break;
                }

                if (DateTime.UtcNow - jobStoppedUtc > TimeSpan.FromSeconds(30))
                {
                    // The job needs to be deleted
                    Log.Write($"Server didn't stop the job in the expected time, deleting it ...");

                    break;
                }
            }
        }

        /// <summary>
        /// Deletes the job on the server.
        /// </summary>
        public async Task DeleteAsync()
        {
            Log.Write($"Deleting job '{_jobName}' ...");

            Log.Verbose($"DELETE {_serverJobUri}...");
            var response = await _httpClient.DeleteAsync(_serverJobUri);
            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Write($@"Server job was not found, it must have been aborted. Possible cause:
                            - Issue while cloning the repository (GitHub unresponsive)
                            - Issue while restoring (MyGet/NuGet unresponsive)
                            - Issue while building
                            - Issue while running (Timeout)"
                );
            }

            response.EnsureSuccessStatusCode();
        }


        /// <summary>
        /// Downloads the whole job including the measurements.
        /// </summary>
        public async Task<bool> TryUpdateJobAsync()
        {
            Log.Verbose($"GET {_serverJobUri}...");
            var response = await _httpClient.GetAsync(_serverJobUri);
            var responseContent = await response.Content.ReadAsStringAsync();

            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

            if(response.StatusCode == HttpStatusCode.NotFound || String.IsNullOrEmpty(responseContent))
            {
                return false;
            }
            else
            {
                try
                {
                    Job = JsonConvert.DeserializeObject<Job>(responseContent);
                }
                catch
                {
                    Log.Write($"ERROR while deserializing state on {_serverJobUri}");
                    return false;
                }

                return true;
            }
        }

        public async Task ClearMeasurements()
        {
            Log.Verbose($"POST {_serverJobUri}/resetstats...");
            var response = await _httpClient.PostAsync(Combine(_serverJobUri, "/resetstats"), new StringContent(""));
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();
        }

        public async Task FlushMeasurements()
        {
            Log.Verbose($"POST {_serverJobUri}/measurements/flush...");
            var response = await _httpClient.PostAsync(Combine(_serverJobUri, "/measurements/flush"), new StringContent(""));
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode}");

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Returns the current state of the job.
        /// </summary>
        public async Task<JobState> GetStateAsync()
        {
            var delays = new[] { 100, 500, 1000 };

            for (var i = 0; i < delays.Length; i++)
            {
                HttpResponseMessage response;
                string responseContent;

                try
                {
                    Log.Verbose($"GET {_serverJobUri}/state...");
                    response = await _httpClient.GetAsync(Combine(_serverJobUri, "/state"));
                    responseContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    Log.Verbose($"Retrying ({i+1})");
                    await Task.Delay(delays[i]);
                    continue;
                }

                Log.Verbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

                if (response.StatusCode == HttpStatusCode.NotFound || String.IsNullOrEmpty(responseContent))
                {
                    return JobState.Failed;
                }
                else
                {
                    try
                    {
                        return Enum.Parse<JobState>(responseContent);
                    }
                    catch
                    {
                        Log.Write($"ERROR while reading state on {_serverJobUri}");
                        return JobState.Failed;
                    }
                }
            }

            Log.Write($"ERROR while getting state on {_serverJobUri}");
            return JobState.Failed;
        }

        /// <summary>
        /// Starts a thread that keeps the job alive on the server.
        /// </summary>
        public void StartKeepAlive()
        {
            if (_keepAlive)
            {
                return;
            }

            _keepAlive = true;

            Task.Run(async () =>
            {
                while (_keepAlive)
                {
                    try
                    {
                        // Ping server job to keep it alive
                        Log.Verbose($"GET {_serverJobUri}/touch...");
                        var response = await _httpClient.GetAsync(Combine(_serverJobUri, "/touch"), new CancellationTokenSource(2000).Token);

                        // Detect if the job has timed out. This doesn't account for any other service
                        if (Job.Timeout > 0 && _runningUtc != null && DateTime.UtcNow - _runningUtc > TimeSpan.FromSeconds(Job.Timeout))
                        {
                            Log.Write($"'{_jobName}' has timed out, stopping...");
                            await StopAsync();
                        }

                        await Task.Delay(2000);
                    }
                    catch (Exception e)
                    {
                        Log.Write($"Could not ping the agent on '{_serverUri.Host}:{_serverUri.Port}', retrying ...");
                        Log.Verbose(e.ToString());

                        await Task.Delay(100);
                    }
                }
            });

            if (Job.Options.DisplayBuild)
            {
                Task.Run(async () =>
                {
                    while (_keepAlive)
                    {
                        try
                        {
                            Log.DisplayOutput(await StreamBuildLogAsync());
                        }
                        finally
                        {
                            await Task.Delay(500);
                        }
                    }
                });
            }

            if (Job.Options.DisplayOutput)
            {
                Task.Run(async () =>
                {
                    while (_keepAlive)
                    {
                        try
                        {
                            Log.DisplayOutput(await StreamOutputAsync());
                        }
                        finally
                        {
                            await Task.Delay(500);
                        }
                    }
                });
            }
        }

        public void StopKeepAlive()
        {
            _keepAlive = false;
        }

        public async Task DownloadTraceAsync()
        {
            if (!Job.DotNetTrace && !Job.Collect)
            {
                return;
            }

            // We can only download the trace for a job that has been stopped
            if (await GetStateAsync() != JobState.Stopped)
            {
                return;
            }

            var info = await GetInfoAsync();
            var os = Enum.Parse<Models.OperatingSystem>(info["os"]?.ToString() ?? "linux", ignoreCase: true);

            var traceExtension = ".nettrace";

            if (Job.Collect)
            {
                traceExtension = os == Models.OperatingSystem.Windows
                    ? ".etl.zip"
                    : ".trace.zip"
                    ;
            }

            try
            {
                var traceDestination = Job.Options.TraceOutput;

                if (String.IsNullOrWhiteSpace(traceDestination))
                {
                    traceDestination = Job.Service;
                }

                if (!traceDestination.EndsWith(traceExtension, StringComparison.OrdinalIgnoreCase))
                {
                    traceDestination = traceDestination + "." + DateTime.Now.ToString("MM-dd-HH-mm-ss") + traceExtension;
                }

                await CollectTracesAsync();

                Log.Write($"Downloading trace file {traceDestination} ...");

                try
                {
                    StartKeepAlive();
                    var uri = Combine(_serverJobUri, "/trace");
                    await _httpClient.DownloadFileWithProgressAsync(uri, _serverJobUri, traceDestination);
                }
                catch (Exception e)
                {
                    Log.Write($"The trace was not captured on the server: " + e.ToString());
                }
                finally
                {
                    StopKeepAlive();
                }


                _traceCollected = true;
            }
            catch (Exception e)
            {
                Log.WriteWarning($"Error while fetching trace for '{Job.Service}'");
                Log.Verbose(e.Message);
            }
        }

        private async Task CollectTracesAsync()
        {
            if (_traceCollected)
            {
                return;
            }

            Log.Write($"Server is collecting trace file, this can take up to 1 minute");

            var uri = Combine(_serverJobUri, "/trace");
            var response = await _httpClient.PostAsync(uri, new StringContent(""));
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var state = await GetStateAsync();

                if (state == JobState.TraceCollecting)
                {
                    Console.Write(".");
                }
                else
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine();

            _traceCollected = true;
        }

        public async Task DownloadDumpAsync()
        {
            if (!Job.DumpProcess)
            {
                return;
            }

            // We can only download the dump for a job that has been stopped
            if (await GetStateAsync() != JobState.Stopped)
            {
                return;
            }

            try
            {
                var dumpExtension = ".dmp";

                var dumpDestination = Job.Options.DumpOutput;

                if (String.IsNullOrWhiteSpace(dumpDestination))
                {
                    dumpDestination = Job.Service;
                }

                if (!dumpDestination.EndsWith(dumpExtension, StringComparison.OrdinalIgnoreCase))
                {
                    dumpDestination = dumpDestination + "." + DateTime.Now.ToString("MM-dd-HH-mm-ss") + dumpExtension;
                }

                Log.Write($"Downloading dump file {dumpDestination} ...");

                try
                {
                    StartKeepAlive();
                    var uri = Combine(_serverJobUri, "/dump");
                    await _httpClient.DownloadFileWithProgressAsync(uri, _serverJobUri, dumpDestination);
                }
                catch (Exception e)
                {
                    Log.Write($"The dump was not captured on the server: " + e.ToString());
                }
                finally
                {
                    StopKeepAlive();
                }
            }
            catch (Exception e)
            {
                Log.WriteWarning($"Error while fetching dump for '{Job.Service}'");
                Log.Verbose(e.Message);
            }
        }

        public async Task DownloadAssetsAsync(string dependency)
        {
            // Fetch published folder
            if (Job.Options.Fetch)
            {
                try
                {
                    var fetchDestination = Job.Options.FetchOutput;

                    if (String.IsNullOrEmpty(fetchDestination) || !fetchDestination.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        // If it does not end with a *.zip then we add a DATE.zip to it
                        if (String.IsNullOrEmpty(fetchDestination))
                        {
                            fetchDestination = dependency;
                        }

                        fetchDestination = fetchDestination + "." + DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".zip";
                    }

                    Log.Write($"Creating published assets '{fetchDestination}' ...");

                    try
                    {
                        StartKeepAlive();
                        await _httpClient.DownloadFileWithProgressAsync(Combine(_serverJobUri, "/fetch"), _serverJobUri, fetchDestination);
                    }
                    finally
                    {
                        StopKeepAlive();
                    }
                }
                catch (Exception e)
                {
                    Log.Write($"Error while fetching published assets for '{dependency}'");
                    Log.Verbose(e.Message);
                }
            }

            // Download individual files
            if (Job.Options.DownloadFiles != null && Job.Options.DownloadFiles.Any())
            {
                foreach (var file in Job.Options.DownloadFiles)
                {
                    Log.Write($"Downloading file '{file}' for '{dependency}'");

                    try
                    {
                        StartKeepAlive();
                        await DownloadFileAsync(file, Job.Options.DownloadFilesOutput);
                    }
                    catch (Exception e)
                    {
                        Log.WriteError($"Error while downloading file {file}, skipping ...");
                        Log.Verbose(e.Message);
                        continue;
                    }
                    finally
                    {
                        StopKeepAlive();
                    }
                }
            }
        }

        public async Task DownloadBenchmarkDotNetResultsAsync()
        {
            // Download results files

            foreach (var pattern in new [] { "brief.json", "default.md" })
            {
                foreach (var path in await ListFilesAsync("BenchmarkDotNet.Artifacts/results/*-" + pattern))
                {
                    Log.Verbose($"Downloading {Path.GetFileName(path)}");
                    _benchmarkDotNetResults[path] = await DownloadFileContentAsync(path);
                }
            }
        }

        public IEnumerable<JObject> GetBenchmarkDotNetBenchmarks()
        {
            foreach (var jsonResults in _benchmarkDotNetResults.Where(x => x.Key.EndsWith("brief.json")))
            {
                var brief = JObject.Parse(jsonResults.Value);
                var benchmarks = brief.Property("Benchmarks").Value as JArray;

                foreach (var benchmark in benchmarks)
                {
                    yield return benchmark as JObject;
                }
            }
        }

        public void DisplayBenchmarkDotNetResults()
        {
            foreach (var markdownResult in _benchmarkDotNetResults.Where(x => x.Key.EndsWith("default.md")))
            {
                // Remove default markdown formatting
                Console.WriteLine(markdownResult.Value.Replace("**", ""));
            }
        }
        private static void DoCreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            // We ensure the name ends with '\' or '/'
            if (!sourceDirectoryName.EndsWith(Path.AltDirectorySeparatorChar))
            {
                sourceDirectoryName += Path.AltDirectorySeparatorChar;
            }

            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            var di = new DirectoryInfo(sourceDirectoryName);

            using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
            {
                var basePath = di.FullName;

                var ignoreFile = IgnoreFile.Parse(Path.Combine(sourceDirectoryName, ".gitignore"), includeParentDirectories: true);

                foreach (var gitFile in ignoreFile.ListDirectory(sourceDirectoryName))
                {
                    var localPath = gitFile.Path.Substring(sourceDirectoryName.Length);
                    Log.Verbose($"Adding {localPath}");
                    var entry = archive.CreateEntryFromFile(gitFile.Path, localPath);
                }
            }
        }

        private async Task<int> UploadFileAsync(string filename, string uri)
        {
            try
            {
                var outputFileSegments = filename.Split(';');
                var uploadFilename = outputFileSegments[0];

                if (!File.Exists(uploadFilename))
                {
                    Console.WriteLine($"File '{uploadFilename}' could not be loaded.");
                    return 8;
                }

                Log.Write($"Uploading {filename} ({(new FileInfo(uploadFilename).Length / 1024):n0}KB)");

                var destinationFilename = outputFileSegments.Length > 1
                    ? outputFileSegments[1]
                    : Path.GetFileName(uploadFilename);

                using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    var fileContent = uploadFilename.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? new StreamContent(await _httpClient.GetStreamAsync(uploadFilename))
                        : new StreamContent(new FileStream(uploadFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.Asynchronous | FileOptions.SequentialScan));

                    using (fileContent)
                    {
                        request.Content = fileContent;
                        request.Headers.Add("id", Job.Id.ToString());
                        request.Headers.Add("destinationFilename", destinationFilename);

                        await _httpClient.SendAsync(request);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"An error occurred while uploading a file.", e);
            }

            return 0;
        }

        private async Task<string> DownloadTemporaryFileAsync(string uri)
        {
            if (_filecache == null)
            {
                _filecache = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }

            Directory.CreateDirectory(_filecache);

            _temporaryFolders.Add(_filecache);

            var filehashname = Path.Combine(_filecache, uri.GetHashCode().ToString());

            if (!File.Exists(filehashname))
            {
                await _httpClient.DownloadFileAsync(uri, _serverJobUri, filehashname);
            }

            return filehashname;
        }

        private static void CleanTemporaryFiles()
        {
            foreach (var temporaryFolder in _temporaryFolders)
            {
                if (temporaryFolder != null && Directory.Exists(temporaryFolder))
                {
                    Directory.Delete(temporaryFolder, true);
                }
            }
        }

        public async Task<string> DownloadBuildLog()
        {
            var uri = Combine(_serverJobUri, "/buildlog");

            return await _httpClient.GetStringAsync(uri);
        }

        public async Task<string> StreamOutputAsync()
        {
            var uri = Combine(_serverJobUri, "/output/" + _outputCursor);

            var jsonLines = await _httpClient.GetStringAsync(uri);
            var lines = JsonConvert.DeserializeObject<string[]>(jsonLines);

            _outputCursor += lines.Length;

            using (var sw = new StringWriter())
            {
                foreach (var line in lines)
                {
                    sw.WriteLine($"[{_jobName}] {line}");
                }

                return sw.ToString();
            }
        }

        public async Task<string> StreamBuildLogAsync()
        {
            var uri = Combine(_serverJobUri, "/buildlog/" + _buildCursor);

            var jsonLines = await _httpClient.GetStringAsync(uri);
            var lines = JsonConvert.DeserializeObject<string[]>(jsonLines);

            _buildCursor += lines.Length;

            using (var sw = new StringWriter())
            {
                foreach (var line in lines)
                {
                    sw.WriteLine($"[{_jobName}] {line}");
                }

                return sw.ToString();
            }
        }

        public async Task<string> DownloadOutput()
        {
            var uri = Combine(_serverJobUri, "/output");

            return await _httpClient.GetStringAsync(uri);
        }

        public async Task DownloadFileAsync(string file, string output)
        {
            // Is it a globing pattern or a single file?
            // Don't process a filename without * with ListFilesAsync as is could match multiple files
            if (file.Contains('*'))
            {
                var files = await ListFilesAsync(file);

                foreach (var f in files)
                {
                    var uri = Combine(_serverJobUri, "/download?path=" + HttpUtility.UrlEncode(f));
                    Log.Verbose("GET " + uri);

                    var filename = Path.Combine(output ?? "", Path.GetDirectoryName(file), f);
                    filename = Path.GetFullPath(filename);

                    if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    }

                    Log.Write($"Downloading '{filename}'");

                    await _httpClient.DownloadFileAsync(uri, _serverJobUri, filename);
                }
            }
            else
            {
                var uri = Combine(_serverJobUri, "/download?path=" + HttpUtility.UrlEncode(file));
                Log.Verbose("GET " + uri);

                var filename = Path.Combine(output ?? "", file);
                filename = Path.GetFullPath(filename);

                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }

                Log.Write($"Downloading '{filename}'");

                await _httpClient.DownloadFileAsync(uri, _serverJobUri, filename);
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string pattern)
        {
            var uri = Combine(_serverJobUri, "/list?path=" + HttpUtility.UrlEncode(pattern));
            Log.Verbose("GET " + uri);

            var response = await _httpClient.GetStringAsync(uri);
            var fileNames = JArray.Parse(response).ToObject<string[]>();

            return fileNames;
        }

        public async Task<string> DownloadFileContentAsync(string file)
        {
            var uri = Combine(_serverJobUri, "/download?path=" + HttpUtility.UrlEncode(file));
            Log.Verbose("GET " + uri);

            return await _httpClient.DownloadFileContentAsync(uri);
        }

        public async Task<Dictionary<string, object>> GetInfoAsync()
        {
            var uri = Combine(_serverUri, "/info");
            var response = await _httpClient.GetStringAsync(uri);

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
        }

        /// <summary>
        /// Returns the list of active jobs.
        /// </summary>
        public async Task<IEnumerable<JobView>> GetQueueAsync()
        {
            Log.Verbose($"GET {Combine(_serverUri, "/jobs")} ...");
            var response = await _httpClient.GetAsync(Combine(_serverUri, "/jobs"));
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            Log.Verbose($"{(int)response.StatusCode} {response.StatusCode} {responseContent}");

            return JsonConvert.DeserializeObject<JobView[]>(responseContent);
        }      
        
        public async Task<Job> GetJobAsync()
        {
            Log.Verbose($"GET {_serverJobUri}...");
            
            var response = await _httpClient.GetAsync(_serverJobUri);
            var responseContent = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<Job>(responseContent);
        }

        private static string Combine(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return String.Concat(uri1, "/", uri2);
        }

        private static string Combine(Uri uri1, string uri2)
        {
            return Combine(uri1.ToString(), uri2);
        }
    }
}
