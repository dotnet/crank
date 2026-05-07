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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent
{
    /// <summary>
    /// Lightweight client for the Build Caching Service (BCS) in dotnet-performance-infra.
    /// Downloads pre-built runtime artifacts from public Azure Blob Storage and overlays
    /// them onto the agent's installed shared framework and/or the published app output so
    /// benchmarks run against the BCS runtime instead of the feed-installed one.
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

        /// <summary>
        /// Maps the agent's platform (RID) to the BCS configuration key and artifact filename.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (string configKey, string artifactFile)> PlatformToBcsConfig =
            new Dictionary<string, (string configKey, string artifactFile)>(StringComparer.OrdinalIgnoreCase)
            {
                ["linux-x64"] = ("coreclr_x64_linux", "BuildArtifacts_linux_x64_Release_coreclr.tar.gz"),
                ["linux-arm64"] = ("coreclr_arm64_linux", "BuildArtifacts_linux_arm64_Release_coreclr.tar.gz"),
                ["linux-musl-x64"] = ("coreclr_muslx64_linux", "BuildArtifacts_linux_musl_x64_Release_coreclr.tar.gz"),
                ["win-x64"] = ("coreclr_x64_windows", "BuildArtifacts_windows_x64_Release_coreclr.zip"),
                ["win-arm64"] = ("coreclr_arm64_windows", "BuildArtifacts_windows_arm64_Release_coreclr.zip"),
                ["win-x86"] = ("coreclr_x86_windows", "BuildArtifacts_windows_x86_Release_coreclr.zip"),
            };

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
        /// Downloads and extracts BCS runtime artifacts to a per-job temp directory. The caller
        /// is responsible for invoking the overlay methods on the returned directory.
        /// </summary>
        public static async Task<string> DownloadAndExtractAsync(
            string baseUrl,
            string repoName,
            string commitSha,
            string buildCacheConfig,
            CancellationToken cancellationToken = default)
        {
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

            // Per-(commit,config) lock so two concurrent jobs don't race on the same directory.
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
        /// Overlays BCS runtime binaries (managed + native + host binaries) into a published
        /// output directory. Used for self-contained publishes where the runtime is bundled in
        /// the publish output.
        /// </summary>
        /// <returns>Number of files overlaid.</returns>
        public static int OverlayPublishedOutput(string extractDir, string outputFolder)
        {
            var rid = GetPlatformMoniker();
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
                // For self-contained, all three host binaries live alongside the app.
                filesCopied += CopyHostBinaryIfPresent(corehostDir, outputFolder, GetNativeLibName("hostpolicy"));
                filesCopied += CopyHostBinaryIfPresent(corehostDir, outputFolder, GetNativeLibName("hostfxr"));
                filesCopied += CopyHostBinaryIfPresent(corehostDir, outputFolder, GetDotnetExecutableName());
            }

            return filesCopied;
        }

        /// <summary>
        /// Overlays BCS runtime binaries into the agent's installed dotnet home so framework-
        /// dependent apps that load the runtime from <c>shared/Microsoft.NETCore.App/{version}/</c>
        /// get the BCS bits at runtime. Also rewrites the <c>.version</c> file so reporting code
        /// that reads it (e.g., GetDependencies) picks up the BCS commit instead of the feed commit.
        /// </summary>
        /// <returns>Number of files overlaid.</returns>
        public static int OverlayDotnetHome(string extractDir, string dotnetHome, string runtimeVersion, string commitSha = null)
        {
            if (string.IsNullOrEmpty(runtimeVersion))
            {
                throw new ArgumentException("runtimeVersion must be provided.", nameof(runtimeVersion));
            }

            var rid = GetPlatformMoniker();
            int filesCopied = 0;

            var sharedFrameworkDir = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            if (!Directory.Exists(sharedFrameworkDir))
            {
                throw new InvalidOperationException(
                    $"Build Cache: Expected shared framework directory does not exist: '{sharedFrameworkDir}'. " +
                    "The feed-resolved runtime must be installed before overlaying.");
            }

            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", rid);
                if (Directory.Exists(runtimesDir))
                {
                    filesCopied += CopyManaged(runtimesDir, sharedFrameworkDir);
                    filesCopied += CopyNative(runtimesDir, sharedFrameworkDir);
                }
            }

            var corehostDir = FindCorehostDirectory(extractDir, rid);
            if (corehostDir != null)
            {
                // hostpolicy lives in the shared framework dir.
                filesCopied += CopyHostBinaryIfPresent(corehostDir, sharedFrameworkDir, GetNativeLibName("hostpolicy"));

                // hostfxr lives at host/fxr/{version}/.
                var hostFxrDir = Path.Combine(dotnetHome, "host", "fxr", runtimeVersion);
                if (Directory.Exists(hostFxrDir))
                {
                    filesCopied += CopyHostBinaryIfPresent(corehostDir, hostFxrDir, GetNativeLibName("hostfxr"));
                }

                // The dotnet host lives at the dotnetHome root.
                filesCopied += CopyHostBinaryIfPresent(corehostDir, dotnetHome, GetDotnetExecutableName());
            }

            // Rewrite the .version file so anything that reads it (notably the agent's own
            // GetDependencies / BenchmarksNetCoreAppVersion measurement) reports the BCS commit
            // instead of the feed-installed commit. Format: "<commit-sha>\n<version>\n".
            if (!string.IsNullOrEmpty(commitSha))
            {
                var versionFile = Path.Combine(sharedFrameworkDir, ".version");
                File.WriteAllText(versionFile, $"{commitSha}\n{runtimeVersion}\n");
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
            await ProcessUtil.RetryOnExceptionAsync(DownloadRetryCount, async () =>
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException(
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

            await ProcessUtil.RetryOnExceptionAsync(DownloadRetryCount, async () =>
            {
                if (File.Exists(partial))
                {
                    File.Delete(partial);
                }

                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Don't retry 404s; they aren't transient.
                        throw new InvalidOperationException(
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

        private static int CopyManaged(string runtimesDir, string destinationDir)
        {
            int copied = 0;
            var libDir = Path.Combine(runtimesDir, "lib");
            if (!Directory.Exists(libDir))
            {
                return 0;
            }

            // Pick the highest-versioned net{X}.0 directory (the archive should only ship one).
            var managedDir = Directory.GetDirectories(libDir)
                .OrderByDescending(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

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
                copied++;
            }

            return copied;
        }

        private static int CopyHostBinaryIfPresent(string sourceDir, string destDir, string fileName)
        {
            var src = Path.Combine(sourceDir, fileName);
            if (!File.Exists(src))
            {
                return 0;
            }

            Directory.CreateDirectory(destDir);
            File.Copy(src, Path.Combine(destDir, fileName), overwrite: true);
            return 1;
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
