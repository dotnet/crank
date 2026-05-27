// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent
{
    /// <summary>
    /// Lightweight client for the Build Caching Service (BCS) in dotnet-performance-infra.
    /// Downloads pre-built runtime artifacts from public Azure Blob Storage and assembles a
    /// per-job dotnet home (or overlays a self-contained published app) so the benchmark runs
    /// against the BCS runtime instead of the feed-installed one.
    /// </summary>
    internal static class BuildCacheClient
    {
        private const int DownloadRetryCount = 3;
        private static readonly TimeSpan _httpTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan _latestBuildsCacheDuration = TimeSpan.FromHours(1);

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = _httpTimeout };

        // Cache latestBuilds.json responses to avoid repeated downloads (keyed by baseUrl|repo|branch).
        private static readonly ConcurrentDictionary<string, (DateTimeOffset fetchedAt, LatestBuildsResponse data)> _latestBuildsCache = new();

        // Per-(commit,config) async locks so concurrent jobs serialize their downloads/extracts.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _extractLocks = new();

        // Hex SHA-1 (full or short), 8-40 chars. BCS commits are runtime repo commits.
        private static readonly Regex _shaRegex = new("^[0-9a-fA-F]{8,40}$", RegexOptions.Compiled);

        /// <summary>
        /// Maps the agent's platform (RID) to the BCS configuration key and artifact filename.
        /// The reverse direction (config → RID) is used to decide which RID-shaped subtree to
        /// look for inside the archive when the user supplies an explicit config override.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (string configKey, string artifactFile, string rid)> PlatformToBcsConfig =
            new Dictionary<string, (string configKey, string artifactFile, string rid)>(StringComparer.OrdinalIgnoreCase)
            {
                ["linux-x64"] = ("coreclr_x64_linux", "BuildArtifacts_linux_x64_Release_coreclr.tar.gz", "linux-x64"),
                ["linux-arm64"] = ("coreclr_arm64_linux", "BuildArtifacts_linux_arm64_Release_coreclr.tar.gz", "linux-arm64"),
                ["linux-musl-x64"] = ("coreclr_muslx64_linux", "BuildArtifacts_linux_musl_x64_Release_coreclr.tar.gz", "linux-musl-x64"),
                ["win-x64"] = ("coreclr_x64_windows", "BuildArtifacts_windows_x64_Release_coreclr.zip", "win-x64"),
                ["win-arm64"] = ("coreclr_arm64_windows", "BuildArtifacts_windows_arm64_Release_coreclr.zip", "win-arm64"),
                ["win-x86"] = ("coreclr_x86_windows", "BuildArtifacts_windows_x86_Release_coreclr.zip", "win-x86"),
            };

        /// <summary>
        /// Sentinel thrown for HTTP responses that are definitively not retryable (e.g. 404).
        /// Distinguishes "the build doesn't exist" from "transient network blip".
        /// </summary>
        public class BuildCacheNotFoundException : InvalidOperationException
        {
            public BuildCacheNotFoundException(string message) : base(message) { }
        }

        /// <summary>
        /// Validates a user-supplied commit SHA. Accepts 8-40 lowercase/uppercase hex chars.
        /// </summary>
        internal static void ValidateCommitSha(string commitSha)
        {
            if (string.IsNullOrEmpty(commitSha))
            {
                return;
            }

            if (!_shaRegex.IsMatch(commitSha))
            {
                throw new ArgumentException(
                    $"'{commitSha}' is not a valid commit SHA. Expected 8-40 hex characters.",
                    nameof(commitSha));
            }
        }

        /// <summary>
        /// Resolves the commit SHA to use from BCS. If a specific commit is provided, returns it
        /// (after platform-config inference). Otherwise queries latestBuilds.json for the latest
        /// commit on the branch.
        /// </summary>
        public static async Task<(string commitSha, string buildCacheConfig)> ResolveCommitAsync(
            string baseUrl,
            string repoName,
            string branch,
            string commitSha,
            string buildCacheConfig,
            CancellationToken cancellationToken = default)
        {
            ValidateCommitSha(commitSha);
            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig);

            if (string.IsNullOrEmpty(commitSha))
            {
                var latestBuilds = await GetLatestBuildsAsync(baseUrl, repoName, branch, cancellationToken);

                if (latestBuilds.Entries.TryGetValue(buildCacheConfig, out var configEntry) && !string.IsNullOrEmpty(configEntry.CommitSha))
                {
                    commitSha = configEntry.CommitSha;
                    Log.Info($"Build Cache: Using latest commit {ShortSha(commitSha)} for config '{buildCacheConfig}' on branch '{branch}' (committed {configEntry.CommitTime})");
                }
                else if (latestBuilds.Entries.TryGetValue("all", out var allEntry) && !string.IsNullOrEmpty(allEntry.CommitSha))
                {
                    commitSha = allEntry.CommitSha;
                    Log.Info($"Build Cache: Using latest commit {ShortSha(commitSha)} for all configs on branch '{branch}' (committed {allEntry.CommitTime})");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Build Cache: No latest build found for branch '{branch}' (config '{buildCacheConfig}'). Check that BCS has builds for this branch.");
                }
            }
            else
            {
                Log.Info($"Build Cache: Using specified commit {ShortSha(commitSha)}");
            }

            return (commitSha, buildCacheConfig);
        }

        /// <summary>
        /// Downloads and extracts BCS runtime artifacts to a per-call temp directory. The caller
        /// owns the returned directory and is responsible for deleting it when done.
        /// </summary>
        public static async Task<string> DownloadAndExtractAsync(
            string baseUrl,
            string repoName,
            string commitSha,
            string buildCacheConfig,
            CancellationToken cancellationToken = default)
        {
            ValidateCommitSha(commitSha);
            if (string.IsNullOrEmpty(commitSha))
            {
                throw new ArgumentException("commitSha must be provided.", nameof(commitSha));
            }

            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig);
            var artifactFile = GetArtifactFile(buildCacheConfig);
            var normalizedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');

            var artifactUrl =
                $"{normalizedBaseUrl}/builds/{Uri.EscapeDataString(repoName)}/buildArtifacts/" +
                $"{Uri.EscapeDataString(commitSha)}/{Uri.EscapeDataString(buildCacheConfig)}/{Uri.EscapeDataString(artifactFile)}";

            var rootCacheDir = Path.Combine(Path.GetTempPath(), "crank-buildcache");
            Directory.CreateDirectory(rootCacheDir);

            var safeConfig = SanitizeForPath(buildCacheConfig);

            // Per-(commit,config) lock so two concurrent jobs don't race on the same archive download.
            var lockKey = $"{commitSha}|{safeConfig}";
            var gate = _extractLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                var commitDir = Path.Combine(rootCacheDir, commitSha);
                Directory.CreateDirectory(commitDir);

                var archivePath = Path.Combine(commitDir, $"{safeConfig}-{artifactFile}");

                if (!File.Exists(archivePath))
                {
                    Log.Info($"Build Cache: Downloading {artifactFile} from {artifactUrl}");
                    await DownloadWithRetryAsync(artifactUrl, archivePath, cancellationToken);
                    Log.Info($"Build Cache: Downloaded {new FileInfo(archivePath).Length / (1024 * 1024)} MB");
                }
                else
                {
                    Log.Info($"Build Cache: Using cached archive at {archivePath}");
                }

                // Per-call unique extract dir so two concurrent jobs for the same (commit,config)
                // never delete each other's working tree.
                var extractDir = Path.Combine(commitDir, $"extracted-{safeConfig}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(extractDir);

                Log.Info($"Build Cache: Extracting archive to {extractDir} ...");
                await ExtractArchiveAsync(archivePath, extractDir, cancellationToken);

                return extractDir;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Deletes a previously-extracted directory. Safe to call multiple times. Archives in the
        /// parent commit dir are intentionally NOT deleted so subsequent jobs for the same commit
        /// can reuse the download.
        /// </summary>
        public static void CleanupExtractDir(string extractDir)
        {
            if (string.IsNullOrEmpty(extractDir))
            {
                return;
            }

            try
            {
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Build Cache: Failed to clean up extracted dir '{extractDir}': {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a per-job dotnet home that mirrors the relevant subtrees of the global dotnet
        /// home (runtime, asp.net, host) and overlays BCS bits on top. The global dotnet home is
        /// NOT modified, so concurrent jobs and subsequent non-buildcache jobs are unaffected.
        /// </summary>
        /// <returns>Absolute path to the per-job dotnet home root. Caller owns it.</returns>
        public static string CreateBuildCacheDotnetHome(
            string globalDotnetHome,
            string extractDir,
            string runtimeVersion,
            string aspNetCoreVersion,
            string commitSha,
            string buildCacheConfig)
        {
            if (string.IsNullOrEmpty(runtimeVersion))
            {
                throw new ArgumentException("runtimeVersion must be provided.", nameof(runtimeVersion));
            }

            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig);
            var rid = GetRidForConfig(buildCacheConfig);

            // Per-job, never reused across jobs to avoid pollution.
            var bcsHomeRoot = Path.Combine(
                Path.GetTempPath(),
                "crank-buildcache",
                $"home-{ShortSha(commitSha)}-{SanitizeForPath(buildCacheConfig)}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(bcsHomeRoot);

            // 1. Copy the dotnet host binary.
            var dotnetExeName = GetDotnetExecutableName();
            var srcDotnet = Path.Combine(globalDotnetHome, dotnetExeName);
            if (File.Exists(srcDotnet))
            {
                var dstDotnet = Path.Combine(bcsHomeRoot, dotnetExeName);
                File.Copy(srcDotnet, dstDotnet, overwrite: true);
                EnsureExecutable(dstDotnet);
            }

            // 2. Mirror host/fxr/{runtimeVersion} (small dir, ~1 file).
            var srcHostFxr = Path.Combine(globalDotnetHome, "host", "fxr", runtimeVersion);
            var dstHostFxr = Path.Combine(bcsHomeRoot, "host", "fxr", runtimeVersion);
            if (Directory.Exists(srcHostFxr))
            {
                CopyDirectory(srcHostFxr, dstHostFxr);
            }

            // 3. Mirror shared/Microsoft.NETCore.App/{runtimeVersion}.
            var srcNetCoreApp = Path.Combine(globalDotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            var dstNetCoreApp = Path.Combine(bcsHomeRoot, "shared", "Microsoft.NETCore.App", runtimeVersion);
            if (Directory.Exists(srcNetCoreApp))
            {
                CopyDirectory(srcNetCoreApp, dstNetCoreApp);
            }

            // 4. Mirror shared/Microsoft.AspNetCore.App/{aspNetCoreVersion}.
            if (!string.IsNullOrEmpty(aspNetCoreVersion))
            {
                var srcAspNet = Path.Combine(globalDotnetHome, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);
                var dstAspNet = Path.Combine(bcsHomeRoot, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);
                if (Directory.Exists(srcAspNet))
                {
                    CopyDirectory(srcAspNet, dstAspNet);
                }
            }

            // 5. Overlay BCS managed + native into the per-job NETCore.App.
            int filesOverlaid = 0;
            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", rid);
                if (Directory.Exists(runtimesDir))
                {
                    filesOverlaid += CopyManaged(runtimesDir, dstNetCoreApp);
                    filesOverlaid += CopyNative(runtimesDir, dstNetCoreApp);
                }
            }

            // 6. Overlay BCS host binaries.
            var corehostDir = FindCorehostDirectory(extractDir, rid);
            if (corehostDir != null)
            {
                filesOverlaid += CopyHostBinaryIfPresent(corehostDir, dstNetCoreApp, GetNativeLibName("hostpolicy"));

                if (Directory.Exists(dstHostFxr))
                {
                    filesOverlaid += CopyHostBinaryIfPresent(corehostDir, dstHostFxr, GetNativeLibName("hostfxr"));
                }

                var dstDotnetHost = Path.Combine(bcsHomeRoot, dotnetExeName);
                var copied = CopyHostBinaryIfPresent(corehostDir, bcsHomeRoot, dotnetExeName);
                if (copied > 0)
                {
                    EnsureExecutable(dstDotnetHost);
                }
                filesOverlaid += copied;
            }

            if (filesOverlaid == 0)
            {
                // The per-job home would be just a copy of the feed runtime with no BCS bits.
                // Tear it down and let the caller fail the job loudly.
                try { Directory.Delete(bcsHomeRoot, recursive: true); } catch { }
                throw new InvalidOperationException(
                    $"Build Cache: overlay copied 0 files for commit {ShortSha(commitSha)} (config '{buildCacheConfig}', rid '{rid}'). " +
                    "The archive layout may have changed or the platform is not supported.");
            }

            // 7. Rewrite .version so any consumer (the agent's own BenchmarksNetCoreAppVersion
            //    measurement, GetDependencies, etc.) reports the BCS commit.
            File.WriteAllText(
                Path.Combine(dstNetCoreApp, ".version"),
                $"{commitSha}\n{runtimeVersion}\n");

            Log.Info($"Build Cache: Per-job dotnet home built at {bcsHomeRoot} ({filesOverlaid} BCS files overlaid)");
            return bcsHomeRoot;
        }

        /// <summary>
        /// Overlays BCS runtime binaries (managed + native + apphost) into a self-contained
        /// published output directory. For SCD the runtime ships next to the app, so this is the
        /// only way to make the benchmark actually run BCS bits. The BCS apphost is renamed to
        /// match the published app's executable name (the SDK renames apphost → AssemblyName).
        /// </summary>
        /// <returns>Number of files overlaid.</returns>
        public static int OverlayPublishedOutput(
            string extractDir,
            string outputFolder,
            string buildCacheConfig,
            string assemblyName)
        {
            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig);
            var rid = GetRidForConfig(buildCacheConfig);
            int filesCopied = 0;

            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", rid);
                if (Directory.Exists(runtimesDir))
                {
                    filesCopied += CopyManaged(runtimesDir, outputFolder);
                    filesCopied += CopyNative(runtimesDir, outputFolder);
                }
            }

            var corehostDir = FindCorehostDirectory(extractDir, rid);
            if (corehostDir != null)
            {
                filesCopied += CopyHostBinaryIfPresent(corehostDir, outputFolder, GetNativeLibName("hostpolicy"));

                // Intentionally NOT replacing the SDK-bound apphost. The BCS archive ships the raw,
                // unbound apphost (the binary has a placeholder SHA-256 hash where the managed DLL
                // path is encoded). The SDK's publish step normally invokes HostWriter.CreateAppHost
                // to bake the managed entry-point path into that binary. Overlaying the raw BCS
                // apphost on top of the SDK-bound one leaves the executable unable to locate its
                // managed DLL and the app fails to start with:
                //
                //     "This executable is not bound to a managed DLL to execute. The binding value
                //      is: '<sha256-of-apphost>'"
                //
                // The perf-relevant runtime code (CoreCLR JIT, GC, managed BCL, hostfxr, hostpolicy)
                // is still overlaid above. To overlay apphost as well, BCS would need to ship a
                // pre-bound apphost per project, or the agent would need to invoke the apphost
                // binder against the BCS apphost using the published app's binding metadata.
            }

            return filesCopied;
        }

        // --- HTTP / latestBuilds.json -------------------------------------------------

        private static async Task<LatestBuildsResponse> GetLatestBuildsAsync(
            string baseUrl, string repoName, string branch, CancellationToken cancellationToken)
        {
            var normalizedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
            var cacheKey = $"{normalizedBaseUrl}|{repoName}|{branch}";

            if (_latestBuildsCache.TryGetValue(cacheKey, out var cached) &&
                DateTimeOffset.UtcNow - cached.fetchedAt < _latestBuildsCacheDuration)
            {
                return cached.data;
            }

            // Branch may contain slashes (e.g., "release/10.0"). Escape each segment but keep
            // the slash semantics so the URL still resolves correctly on the server.
            var escapedBranch = string.Join("/", branch.Split('/').Select(Uri.EscapeDataString));
            var url = $"{normalizedBaseUrl}/builds/{Uri.EscapeDataString(repoName)}/latest/{escapedBranch}/latestBuilds.json";

            Log.Info($"Build Cache: Fetching latest builds from {url}");

            string json = null;

            // 404s are not transient; pre-check before entering the retry loop.
            await RetryTransientAsync(DownloadRetryCount, async () =>
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new BuildCacheNotFoundException(
                        $"Build Cache: No latest builds found for branch '{branch}' in repo '{repoName}'. URL: {url}");
                }

                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync(cancellationToken);
            });

            var latestBuilds = ParseLatestBuilds(json);
            _latestBuildsCache[cacheKey] = (DateTimeOffset.UtcNow, latestBuilds);
            return latestBuilds;
        }

        private static async Task DownloadWithRetryAsync(string url, string destination, CancellationToken cancellationToken)
        {
            var partial = destination + ".partial";

            await RetryTransientAsync(DownloadRetryCount, async () =>
            {
                if (File.Exists(partial))
                {
                    File.Delete(partial);
                }

                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Definitively not transient; do not retry.
                        throw new BuildCacheNotFoundException(
                            $"Build Cache: Artifact not found at {url}. The build may not exist in the cache.");
                    }

                    response.EnsureSuccessStatusCode();

                    var expectedLength = response.Content.Headers.ContentLength;

                    using (var fileStream = File.Create(partial))
                    {
                        await response.Content.CopyToAsync(fileStream, cancellationToken);
                    }

                    if (expectedLength.HasValue)
                    {
                        var actual = new FileInfo(partial).Length;
                        if (actual != expectedLength.Value)
                        {
                            throw new InvalidOperationException(
                                $"Build Cache: Download size mismatch (expected {expectedLength.Value}, got {actual}). URL: {url}");
                        }
                    }
                }

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(partial, destination);
            });
        }

        /// <summary>
        /// Like <see cref="ProcessUtil.RetryOnExceptionAsync(int, Func{Task}, CancellationToken)"/>
        /// but rethrows <see cref="BuildCacheNotFoundException"/> immediately without retrying.
        /// </summary>
        private static async Task RetryTransientAsync(int retries, Func<Task> operation)
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    attempts++;
                    await operation();
                    return;
                }
                catch (BuildCacheNotFoundException)
                {
                    // Non-retryable: fail fast.
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempts > retries)
                    {
                        throw;
                    }

                    Log.Info($"Build Cache: Attempt {attempts} failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Parses the latestBuilds.json format from BCS. The JSON has dynamic keys for each
        /// build configuration plus a "branch_name" / "BranchName" string property.
        /// </summary>
        internal static LatestBuildsResponse ParseLatestBuilds(string json)
        {
            var result = new LatestBuildsResponse();

            using var doc = JsonDocument.Parse(json);

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("branch_name", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("BranchName", StringComparison.Ordinal))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        result.BranchName = property.Value.GetString();
                    }
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var entry = new LatestBuildEntry
                    {
                        CommitSha = TryGetStringPropertyAnyCase(property.Value, "CommitSha", "commit_sha"),
                        CommitTime = TryGetStringPropertyAnyCase(property.Value, "CommitTime", "commit_time"),
                    };

                    result.Entries[property.Name] = entry;
                }
            }

            return result;
        }

        private static string TryGetStringPropertyAnyCase(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }

        // --- Extraction ---------------------------------------------------------------

        private static Task ExtractArchiveAsync(string archivePath, string outputDir, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(outputDir);

            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractTarGzAsync(archivePath, outputDir, cancellationToken);
            }

            if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return Task.Run(() => ZipFile.ExtractToDirectory(archivePath, outputDir, overwriteFiles: true), cancellationToken);
            }

            throw new InvalidOperationException($"Unsupported archive format: {archivePath}");
        }

        private static async Task ExtractTarGzAsync(string archivePath, string outputDir, CancellationToken cancellationToken)
        {
            await using var fs = File.OpenRead(archivePath);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gz, outputDir, overwriteFiles: true, cancellationToken: cancellationToken);
        }

        // --- Overlay helpers ----------------------------------------------------------

        /// <summary>
        /// Selects the highest net{X}.0 directory under lib/ using a numeric comparison.
        /// Lexicographic ordering puts "net9.0" above "net10.0", which would silently overlay
        /// the wrong managed assemblies if BCS ever ships multiple TFMs.
        /// </summary>
        internal static string SelectHighestManagedDir(string libDir)
        {
            if (!Directory.Exists(libDir))
            {
                return null;
            }

            (int major, int minor, string path) Parse(string dir)
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = name.Substring(3);
                    var dot = rest.IndexOf('.');
                    if (dot > 0 &&
                        int.TryParse(rest.Substring(0, dot), out var maj) &&
                        int.TryParse(rest.Substring(dot + 1), out var min))
                    {
                        return (maj, min, dir);
                    }
                }

                return (-1, -1, dir);
            }

            return Directory.GetDirectories(libDir)
                .Select(Parse)
                .OrderByDescending(t => t.major)
                .ThenByDescending(t => t.minor)
                .Select(t => t.path)
                .FirstOrDefault();
        }

        private static int CopyManaged(string runtimesDir, string destinationDir)
        {
            int copied = 0;
            var libDir = Path.Combine(runtimesDir, "lib");
            var managedDir = SelectHighestManagedDir(libDir);

            if (managedDir == null)
            {
                return 0;
            }

            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(managedDir, "*.dll"))
            {
                var dest = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
                copied++;
            }

            return copied;
        }

        private static int CopyNative(string runtimesDir, string destinationDir)
        {
            int copied = 0;
            var nativeDir = Path.Combine(runtimesDir, "native");
            if (!Directory.Exists(nativeDir))
            {
                return 0;
            }

            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(nativeDir))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dest = Path.Combine(destinationDir, fileName);
                File.Copy(file, dest, overwrite: true);
                EnsureExecutable(dest);
                copied++;
            }

            return copied;
        }

        private static int CopyHostBinaryIfPresent(string sourceDir, string destDir, string fileName)
        {
            var sourcePath = Path.Combine(sourceDir, fileName);
            if (!File.Exists(sourcePath))
            {
                return 0;
            }

            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, fileName);
            File.Copy(sourcePath, destPath, overwrite: true);
            EnsureExecutable(destPath);
            return 1;
        }

        /// <summary>
        /// Recursively copies a directory tree. Used to materialize the per-job dotnet home
        /// from the global feed-installed one. Native files and the dotnet host are chmod'd
        /// executable on Unix-like systems.
        /// </summary>
        internal static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.EnumerateFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
                EnsureExecutable(destFile);
            }

            foreach (var subDir in Directory.EnumerateDirectories(sourceDir))
            {
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
            }
        }

        /// <summary>
        /// On Unix-like systems, ensures the destination file has the user-execute bit set.
        /// Native libs don't strictly require +x, but the dotnet host and apphost do, and the
        /// File.Copy + overwrite path can drop the bit if the destination didn't have it.
        /// </summary>
        private static void EnsureExecutable(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                var current = File.GetUnixFileMode(path);
                var withExec = current
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupExecute
                    | UnixFileMode.OtherExecute;
                if (current != withExec)
                {
                    File.SetUnixFileMode(path, withExec);
                }
            }
            catch
            {
                // Best-effort; some filesystems (FAT, network shares) don't support mode bits.
            }
        }

        private static string FindDirectory(string root, string directoryName)
        {
            if (!Directory.Exists(root))
            {
                return null;
            }

            foreach (var dir in Directory.GetDirectories(root))
            {
                if (Path.GetFileName(dir).Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
            }

            return null;
        }

        private static string FindCorehostDirectory(string extractDir, string rid)
        {
            var primary = Path.Combine(extractDir, $"{rid}.Release", "corehost");
            if (Directory.Exists(primary))
            {
                return primary;
            }

            var alternate = Path.Combine(extractDir, "corehost");
            if (Directory.Exists(alternate))
            {
                return alternate;
            }

            return null;
        }

        // --- Platform / RID mapping ---------------------------------------------------

        private static string ResolveBuildCacheConfig(string buildCacheConfig)
        {
            if (!string.IsNullOrEmpty(buildCacheConfig))
            {
                return buildCacheConfig;
            }

            var rid = GetPlatformMoniker();
            if (PlatformToBcsConfig.TryGetValue(rid, out var mapped))
            {
                return mapped.configKey;
            }

            throw new InvalidOperationException(
                $"No Build Cache configuration mapping for platform '{rid}'. Specify buildCacheConfig explicitly.");
        }

        private static string GetArtifactFile(string buildCacheConfig)
        {
            var match = PlatformToBcsConfig.Values.FirstOrDefault(v =>
                string.Equals(v.configKey, buildCacheConfig, StringComparison.OrdinalIgnoreCase));

            if (match.artifactFile == null)
            {
                throw new InvalidOperationException(
                    $"Unknown Build Cache configuration key: '{buildCacheConfig}'.");
            }

            return match.artifactFile;
        }

        /// <summary>
        /// Maps a BCS config key back to its RID. Use this for overlay path discovery so an
        /// explicit musl/cross-arch override actually finds the right runtime pack inside the
        /// archive instead of falling back to the host's detected RID.
        /// </summary>
        internal static string GetRidForConfig(string buildCacheConfig)
        {
            var match = PlatformToBcsConfig.Values.FirstOrDefault(v =>
                string.Equals(v.configKey, buildCacheConfig, StringComparison.OrdinalIgnoreCase));

            if (match.rid == null)
            {
                throw new InvalidOperationException(
                    $"Unknown Build Cache configuration key: '{buildCacheConfig}'.");
            }

            return match.rid;
        }

        internal static string GetNativeLibName(string baseName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{baseName}.dll";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"lib{baseName}.dylib";
            }

            return $"lib{baseName}.so";
        }

        private static string GetDotnetExecutableName()
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        internal static string GetPlatformMoniker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.Arm64 => "win-arm64",
                    Architecture.X86 => "win-x86",
                    _ => "win-x64",
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
            }

            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }

        private static string SanitizeForPath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "default";
            }

            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(value.Select(c => invalid.Contains(c) ? '_' : c));
        }

        internal static string ShortSha(string commitSha)
            => string.IsNullOrEmpty(commitSha)
                ? string.Empty
                : commitSha.Substring(0, Math.Min(8, commitSha.Length));

        // --- DTOs ---------------------------------------------------------------------

        internal class LatestBuildsResponse
        {
            public string BranchName { get; set; }
            public Dictionary<string, LatestBuildEntry> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        internal class LatestBuildEntry
        {
            public string CommitSha { get; set; }
            public string CommitTime { get; set; }
        }
    }
}
