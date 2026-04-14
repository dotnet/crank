// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent
{
    /// <summary>
    /// Lightweight client for the Build Caching Service (BCS) in dotnet-performance-infra.
    /// Downloads pre-built runtime artifacts from public Azure Blob Storage and overlays
    /// them into a standard dotnet installation directory.
    /// </summary>
    internal static class BuildCacheClient
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        // Cache latestBuilds.json responses to avoid repeated downloads (keyed by baseUrl+branch)
        private static readonly ConcurrentDictionary<string, (DateTimeOffset fetchedAt, LatestBuildsResponse data)> _latestBuildsCache = new();
        private static readonly TimeSpan _latestBuildsCacheDuration = TimeSpan.FromHours(1);

        // Cache of already-installed BCS commit SHAs to avoid re-extracting
        private static readonly ConcurrentDictionary<string, byte> _installedBuildCacheRuntimes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps the agent's platform (RID) to the BCS configuration key and artifact filename.
        /// </summary>
        private static readonly Dictionary<string, (string configKey, string artifactFile)> _platformToBcsConfig = new(StringComparer.OrdinalIgnoreCase)
        {
            ["linux-x64"] = ("coreclr_x64_linux", "BuildArtifacts_linux_x64_Release_coreclr.tar.gz"),
            ["linux-arm64"] = ("coreclr_arm64_linux", "BuildArtifacts_linux_arm64_Release_coreclr.tar.gz"),
            ["linux-musl-x64"] = ("coreclr_muslx64_linux", "BuildArtifacts_linux_musl_x64_Release_coreclr.tar.gz"),
            ["win-x64"] = ("coreclr_x64_windows", "BuildArtifacts_windows_x64_Release_coreclr.zip"),
            ["win-arm64"] = ("coreclr_arm64_windows", "BuildArtifacts_windows_arm64_Release_coreclr.zip"),
            ["win-x86"] = ("coreclr_x86_windows", "BuildArtifacts_windows_x86_Release_coreclr.zip"),
        };

        /// <summary>
        /// Resolves the commit SHA to use from BCS. If a specific commit is provided, validates
        /// it exists. Otherwise queries latestBuilds.json for the latest commit on the branch.
        /// Returns the commit SHA and the runtime version string.
        /// </summary>
        public static async Task<(string commitSha, string runtimeVersion)> ResolveCommitAsync(
            string baseUrl,
            string repoName,
            string branch,
            string commitSha,
            string buildCacheConfig,
            CancellationToken cancellationToken = default)
        {
            var platformMoniker = GetPlatformMoniker();

            if (!string.IsNullOrEmpty(buildCacheConfig))
            {
                // Use explicit config key
            }
            else if (_platformToBcsConfig.TryGetValue(platformMoniker, out var mapped))
            {
                buildCacheConfig = mapped.configKey;
            }
            else
            {
                throw new InvalidOperationException($"No Build Cache configuration mapping for platform '{platformMoniker}'. Specify buildCacheConfig explicitly.");
            }

            if (string.IsNullOrEmpty(commitSha))
            {
                // Query latestBuilds.json for the latest commit
                var latestBuilds = await GetLatestBuildsAsync(baseUrl, repoName, branch, cancellationToken);

                // Try to get the config-specific entry, fall back to "all"
                if (latestBuilds.Entries.TryGetValue(buildCacheConfig, out var configEntry) && !string.IsNullOrEmpty(configEntry.CommitSha))
                {
                    commitSha = configEntry.CommitSha;
                    Log.Info($"Build Cache: Using latest commit {commitSha.Substring(0, Math.Min(8, commitSha.Length))} for config '{buildCacheConfig}' on branch '{branch}' (committed {configEntry.CommitTime})");
                }
                else if (latestBuilds.Entries.TryGetValue("all", out var allEntry) && !string.IsNullOrEmpty(allEntry.CommitSha))
                {
                    commitSha = allEntry.CommitSha;
                    Log.Info($"Build Cache: Using latest commit {commitSha.Substring(0, Math.Min(8, commitSha.Length))} for all configs on branch '{branch}' (committed {allEntry.CommitTime})");
                }
                else
                {
                    throw new InvalidOperationException($"Build Cache: No latest build found for branch '{branch}'. Check that BCS has builds for this branch.");
                }
            }
            else
            {
                Log.Info($"Build Cache: Using specified commit {commitSha.Substring(0, Math.Min(8, commitSha.Length))}");
            }

            // Try to determine a runtime version from the commit. For now, we return a placeholder
            // that will be replaced after extraction by reading .version from the shared framework.
            var runtimeVersion = $"buildcache-{commitSha.Substring(0, Math.Min(12, commitSha.Length))}";

            return (commitSha, runtimeVersion);
        }

        /// <summary>
        /// Downloads and extracts BCS runtime artifacts to a temp directory without overlaying.
        /// Returns the path to the extracted directory for later overlay into published output.
        /// </summary>
        public static async Task<string> DownloadAndExtractAsync(
            string baseUrl,
            string repoName,
            string commitSha,
            string buildCacheConfig,
            string targetFramework,
            CancellationToken cancellationToken = default)
        {
            var platformMoniker = GetPlatformMoniker();

            if (string.IsNullOrEmpty(buildCacheConfig))
            {
                if (_platformToBcsConfig.TryGetValue(platformMoniker, out var mapped))
                {
                    buildCacheConfig = mapped.configKey;
                }
                else
                {
                    throw new InvalidOperationException($"No Build Cache configuration mapping for platform '{platformMoniker}'.");
                }
            }

            string artifactFile;
            if (_platformToBcsConfig.Values.Any(v => v.configKey == buildCacheConfig))
            {
                artifactFile = _platformToBcsConfig.Values.First(v => v.configKey == buildCacheConfig).artifactFile;
            }
            else
            {
                throw new InvalidOperationException($"Unknown Build Cache configuration key: '{buildCacheConfig}'.");
            }

            var artifactUrl = $"{baseUrl}/builds/{repoName}/buildArtifacts/{commitSha}/{buildCacheConfig}/{artifactFile}";

            Log.Info($"Build Cache: Downloading {artifactFile} from {artifactUrl}");

            var tempDir = Path.Combine(Path.GetTempPath(), "crank-buildcache", commitSha);
            Directory.CreateDirectory(tempDir);
            var tempArchive = Path.Combine(tempDir, artifactFile);

            if (!File.Exists(tempArchive))
            {
                using var response = await _httpClient.GetAsync(artifactUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"Build Cache: Artifact not found for commit {commitSha} with config '{buildCacheConfig}'.");
                }

                response.EnsureSuccessStatusCode();

                using var fileStream = File.Create(tempArchive);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                Log.Info($"Build Cache: Downloaded {new FileInfo(tempArchive).Length / (1024 * 1024)} MB");
            }
            else
            {
                Log.Info($"Build Cache: Using cached archive at {tempArchive}");
            }

            var extractDir = Path.Combine(tempDir, $"extracted-{buildCacheConfig}");
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, true);
            }

            Log.Info($"Build Cache: Extracting archive...");

            if (artifactFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractTarGzAsync(tempArchive, extractDir, cancellationToken);
            }
            else if (artifactFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(tempArchive, extractDir);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported archive format: {artifactFile}");
            }

            return extractDir;
        }

        /// <summary>
        /// Overlays BCS runtime binaries (managed + native) into a published output directory,
        /// replacing NuGet-sourced runtime DLLs with BCS-built ones.
        /// Returns the number of files overlaid.
        /// </summary>
        public static int OverlayPublishedOutput(string extractDir, string outputFolder)
        {
            var platformMoniker = GetPlatformMoniker();
            int filesCopied = 0;

            // Find the NuGet package directory for managed + native DLLs
            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{platformMoniker}");

            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", platformMoniker);

                if (Directory.Exists(runtimesDir))
                {
                    // Copy managed DLLs from lib/net{X}.0/
                    var libDir = Path.Combine(runtimesDir, "lib");
                    if (Directory.Exists(libDir))
                    {
                        var managedDir = Directory.GetDirectories(libDir).FirstOrDefault();
                        if (managedDir != null)
                        {
                            foreach (var file in Directory.GetFiles(managedDir, "*.dll"))
                            {
                                var destFile = Path.Combine(outputFolder, Path.GetFileName(file));
                                if (File.Exists(destFile))
                                {
                                    File.Copy(file, destFile, overwrite: true);
                                    filesCopied++;
                                }
                            }
                        }
                    }

                    // Copy native libraries from native/
                    var nativeDir = Path.Combine(runtimesDir, "native");
                    if (Directory.Exists(nativeDir))
                    {
                        foreach (var file in Directory.GetFiles(nativeDir))
                        {
                            var fileName = Path.GetFileName(file);
                            if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                                fileName.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var destFile = Path.Combine(outputFolder, fileName);
                            if (File.Exists(destFile))
                            {
                                File.Copy(file, destFile, overwrite: true);
                                filesCopied++;
                            }
                        }
                    }
                }
            }

            // Also overlay host binaries from {rid}.Release/corehost/
            var corehostDir = FindCorehostDirectory(extractDir, platformMoniker);
            if (corehostDir != null)
            {
                var hostPolicyName = GetNativeLibName("hostpolicy");
                var hostPolicySrc = Path.Combine(corehostDir, hostPolicyName);
                var hostPolicyDest = Path.Combine(outputFolder, hostPolicyName);
                if (File.Exists(hostPolicySrc) && File.Exists(hostPolicyDest))
                {
                    File.Copy(hostPolicySrc, hostPolicyDest, overwrite: true);
                    filesCopied++;
                }
            }

            return filesCopied;
        }

        /// <summary>
        /// Downloads and extracts BCS runtime artifacts into a standard dotnet installation directory.
        /// Overlays runtime binaries on top of an existing dotnet-install layout.
        /// Returns the actual runtime version string read from the extracted artifacts.
        /// </summary>
        public static async Task<string> InstallRuntimeFromBuildCacheAsync(
            string baseUrl,
            string repoName,
            string commitSha,
            string buildCacheConfig,
            string dotnetHome,
            string targetFramework,
            CancellationToken cancellationToken = default)
        {
            if (_installedBuildCacheRuntimes.ContainsKey(commitSha))
            {
                Log.Info($"Build Cache: Runtime for commit {commitSha.Substring(0, Math.Min(8, commitSha.Length))} already installed, skipping.");

                // Read the version from the already-installed runtime
                return ReadInstalledBuildCacheVersion(dotnetHome, targetFramework) ?? $"buildcache-{commitSha.Substring(0, 12)}";
            }

            var platformMoniker = GetPlatformMoniker();

            if (string.IsNullOrEmpty(buildCacheConfig))
            {
                if (_platformToBcsConfig.TryGetValue(platformMoniker, out var mapped))
                {
                    buildCacheConfig = mapped.configKey;
                }
                else
                {
                    throw new InvalidOperationException($"No Build Cache configuration mapping for platform '{platformMoniker}'.");
                }
            }

            // Determine artifact filename
            string artifactFile;
            if (_platformToBcsConfig.Values.Any(v => v.configKey == buildCacheConfig))
            {
                artifactFile = _platformToBcsConfig.Values.First(v => v.configKey == buildCacheConfig).artifactFile;
            }
            else
            {
                throw new InvalidOperationException($"Unknown Build Cache configuration key: '{buildCacheConfig}'.");
            }

            // Construct the download URL
            var artifactUrl = $"{baseUrl}/builds/{repoName}/buildArtifacts/{commitSha}/{buildCacheConfig}/{artifactFile}";

            Log.Info($"Build Cache: Downloading {artifactFile} from {artifactUrl}");

            // Download to a temp file
            var tempDir = Path.Combine(Path.GetTempPath(), "crank-buildcache", commitSha);
            Directory.CreateDirectory(tempDir);
            var tempArchive = Path.Combine(tempDir, artifactFile);

            try
            {
                if (!File.Exists(tempArchive))
                {
                    using var response = await _httpClient.GetAsync(artifactUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new InvalidOperationException($"Build Cache: Artifact not found for commit {commitSha} with config '{buildCacheConfig}'. The build may not exist in the cache.");
                    }

                    response.EnsureSuccessStatusCode();

                    using var fileStream = File.Create(tempArchive);
                    await response.Content.CopyToAsync(fileStream, cancellationToken);

                    Log.Info($"Build Cache: Downloaded {new FileInfo(tempArchive).Length / (1024 * 1024)} MB");
                }
                else
                {
                    Log.Info($"Build Cache: Using cached archive at {tempArchive}");
                }

                // Extract and overlay
                var extractDir = Path.Combine(tempDir, "extracted");
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }

                Log.Info($"Build Cache: Extracting archive...");

                if (artifactFile.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                {
                    await ExtractTarGzAsync(tempArchive, extractDir, cancellationToken);
                }
                else if (artifactFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(tempArchive, extractDir);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported archive format: {artifactFile}");
                }

                // Overlay into dotnet home
                var runtimeVersion = await OverlayRuntimeAsync(extractDir, dotnetHome, platformMoniker, targetFramework, commitSha, cancellationToken);

                _installedBuildCacheRuntimes.TryAdd(commitSha, 0);

                Log.Info($"Build Cache: Runtime {runtimeVersion} (commit {commitSha.Substring(0, Math.Min(8, commitSha.Length))}) installed successfully.");

                return runtimeVersion;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Build Cache: Failed to install runtime from commit {commitSha}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Overlays extracted BCS artifacts into the dotnet home directory structure.
        /// </summary>
        private static async Task<string> OverlayRuntimeAsync(
            string extractDir,
            string dotnetHome,
            string platformMoniker,
            string targetFramework,
            string commitSha,
            CancellationToken cancellationToken)
        {
            var versionPrefix = ExtractVersionPrefix(targetFramework);
            var rid = platformMoniker;

            // The NuGet package layout inside the archive is at:
            // microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/
            //   lib/net{X}.0/  → managed DLLs
            //   native/        → native libraries
            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            string managedDir = null;
            string nativeDir = null;

            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", rid);

                if (Directory.Exists(runtimesDir))
                {
                    // Find the lib/net{X}.0 directory
                    var libDir = Path.Combine(runtimesDir, "lib");
                    if (Directory.Exists(libDir))
                    {
                        managedDir = Directory.GetDirectories(libDir)
                            .FirstOrDefault(d => Path.GetFileName(d).StartsWith($"net{versionPrefix}", StringComparison.OrdinalIgnoreCase))
                            ?? Directory.GetDirectories(libDir).FirstOrDefault();
                    }

                    nativeDir = Path.Combine(runtimesDir, "native");
                    if (!Directory.Exists(nativeDir))
                    {
                        nativeDir = null;
                    }
                }
            }

            // Determine the runtime version from the managed DLLs directory or other metadata
            var runtimeVersion = DetermineRuntimeVersion(extractDir, versionPrefix, commitSha);

            // Create the shared framework directory
            var sharedFrameworkDir = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            Directory.CreateDirectory(sharedFrameworkDir);

            int filesCopied = 0;

            // Copy managed DLLs
            if (managedDir != null && Directory.Exists(managedDir))
            {
                foreach (var file in Directory.GetFiles(managedDir, "*.dll"))
                {
                    File.Copy(file, Path.Combine(sharedFrameworkDir, Path.GetFileName(file)), overwrite: true);
                    filesCopied++;
                }

                Log.Info($"Build Cache: Copied {filesCopied} managed assemblies to shared framework.");
            }

            // Copy native libraries
            if (nativeDir != null && Directory.Exists(nativeDir))
            {
                int nativeCount = 0;

                foreach (var file in Directory.GetFiles(nativeDir))
                {
                    var fileName = Path.GetFileName(file);

                    // Skip debug symbols during overlay (keep it lean)
                    if (fileName.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    File.Copy(file, Path.Combine(sharedFrameworkDir, fileName), overwrite: true);
                    nativeCount++;
                }

                Log.Info($"Build Cache: Copied {nativeCount} native libraries to shared framework.");
                filesCopied += nativeCount;
            }

            // Also look for host binaries in the corehost directory
            // Pattern: {rid}.Release/corehost/ or linux-arm64.Release/corehost/
            var corehostDir = FindCorehostDirectory(extractDir, platformMoniker);

            if (corehostDir != null)
            {
                // Copy libhostpolicy to shared framework
                CopyFileIfExists(corehostDir, sharedFrameworkDir, GetNativeLibName("hostpolicy"));

                // Copy libhostfxr to host/fxr/{version}/
                var hostFxrDir = Path.Combine(dotnetHome, "host", "fxr", runtimeVersion);
                Directory.CreateDirectory(hostFxrDir);
                CopyFileIfExists(corehostDir, hostFxrDir, GetNativeLibName("hostfxr"));

                Log.Info($"Build Cache: Copied host binaries.");
            }

            // Write a .version file with the commit SHA for traceability
            var versionFilePath = Path.Combine(sharedFrameworkDir, ".version");
            await File.WriteAllTextAsync(versionFilePath, $"{commitSha}\n{runtimeVersion}\n", cancellationToken);

            if (filesCopied == 0)
            {
                throw new InvalidOperationException($"Build Cache: No runtime files found to extract. The archive may not contain the expected layout for platform '{platformMoniker}'.");
            }

            return runtimeVersion;
        }

        /// <summary>
        /// Determines the runtime version from extracted artifacts.
        /// </summary>
        private static string DetermineRuntimeVersion(string extractDir, string versionPrefix, string commitSha)
        {
            // Look for .version file in the shared framework subdirectory of the archive
            var versionFiles = Directory.GetFiles(extractDir, ".version", SearchOption.AllDirectories);

            foreach (var versionFile in versionFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(versionFile);
                    // The .version file typically has: line 0 = commit hash, line 1 = version string
                    if (lines.Length >= 2 && lines[1].StartsWith(versionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return lines[1].Trim();
                    }
                }
                catch
                {
                    // Continue searching
                }
            }

            // Fallback: construct a version string from the prefix and commit
            return $"{versionPrefix}.0-buildcache.{commitSha.Substring(0, Math.Min(8, commitSha.Length))}";
        }

        /// <summary>
        /// Reads the runtime version from an already-installed BCS runtime.
        /// </summary>
        private static string ReadInstalledBuildCacheVersion(string dotnetHome, string targetFramework)
        {
            var versionPrefix = ExtractVersionPrefix(targetFramework);
            var sharedDir = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App");

            if (!Directory.Exists(sharedDir))
            {
                return null;
            }

            // Find directories matching the version prefix that have a .version file with a commit SHA
            foreach (var dir in Directory.GetDirectories(sharedDir).OrderByDescending(d => d))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.StartsWith(versionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var versionFile = Path.Combine(dir, ".version");
                    if (File.Exists(versionFile))
                    {
                        var lines = File.ReadAllLines(versionFile);
                        if (lines.Length >= 2)
                        {
                            return lines[1].Trim();
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<LatestBuildsResponse> GetLatestBuildsAsync(
            string baseUrl, string repoName, string branch, CancellationToken cancellationToken)
        {
            var cacheKey = $"{baseUrl}|{repoName}/{branch}";

            if (_latestBuildsCache.TryGetValue(cacheKey, out var cached) &&
                DateTimeOffset.UtcNow - cached.fetchedAt < _latestBuildsCacheDuration)
            {
                return cached.data;
            }

            var url = $"{baseUrl}/builds/{repoName}/latest/{branch}/latestBuilds.json";
            Log.Info($"Build Cache: Fetching latest builds from {url}");

            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Build Cache: No latest builds found for branch '{branch}' in repo '{repoName}'. URL: {url}");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var latestBuilds = ParseLatestBuilds(json);

            _latestBuildsCache[cacheKey] = (DateTimeOffset.UtcNow, latestBuilds);

            return latestBuilds;
        }

        /// <summary>
        /// Parses the latestBuilds.json format from BCS. The JSON has dynamic keys for each
        /// build configuration, with "branch_name" as a special key.
        /// </summary>
        private static LatestBuildsResponse ParseLatestBuilds(string json)
        {
            var result = new LatestBuildsResponse();

            using var doc = JsonDocument.Parse(json);

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name == "branch_name")
                {
                    result.BranchName = property.Value.GetString();
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var entry = new LatestBuildEntry
                    {
                        CommitSha = property.Value.TryGetProperty("CommitSha", out var sha) ? sha.GetString()
                                  : property.Value.TryGetProperty("commit_sha", out sha) ? sha.GetString()
                                  : null,
                        CommitTime = property.Value.TryGetProperty("CommitTime", out var time) ? time.GetString()
                                   : property.Value.TryGetProperty("commit_time", out time) ? time.GetString()
                                   : null,
                    };

                    result.Entries[property.Name] = entry;
                }
            }

            return result;
        }

        private static string FindDirectory(string root, string directoryName)
        {
            if (!Directory.Exists(root))
            {
                return null;
            }

            // Check direct children first
            foreach (var dir in Directory.GetDirectories(root))
            {
                if (Path.GetFileName(dir).Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }
            }

            return null;
        }

        private static string FindCorehostDirectory(string extractDir, string platformMoniker)
        {
            // BCS layout: {rid}.Release/corehost/ (e.g., linux-arm64.Release/corehost/)
            // Map RID to the directory name format used in BCS artifacts
            var ridDirName = $"{platformMoniker}.Release";
            var corehostPath = Path.Combine(extractDir, ridDirName, "corehost");

            if (Directory.Exists(corehostPath))
            {
                return corehostPath;
            }

            // Also try the raw format without dots
            var altCorehostPath = Path.Combine(extractDir, "corehost");
            if (Directory.Exists(altCorehostPath))
            {
                return altCorehostPath;
            }

            return null;
        }

        private static void CopyFileIfExists(string sourceDir, string destDir, string fileName)
        {
            var sourcePath = Path.Combine(sourceDir, fileName);
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, Path.Combine(destDir, fileName), overwrite: true);
            }
        }

        private static string GetNativeLibName(string baseName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{baseName}.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"lib{baseName}.dylib";
            }
            else
            {
                return $"lib{baseName}.so";
            }
        }

        private static string GetPlatformMoniker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64"
                     : RuntimeInformation.ProcessArchitecture == Architecture.X86 ? "win-x86"
                     : "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
            }
            else
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
            }
        }

        private static string ExtractVersionPrefix(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                throw new InvalidOperationException("Target framework must be specified.");
            }

            // "net10.0" → "10.0", "net9.0" → "9.0"
            if (targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
                targetFramework.Length > 3 &&
                char.IsDigit(targetFramework[3]))
            {
                return targetFramework.Substring(3);
            }

            // "netcoreapp3.1" → "3.1"
            if (targetFramework.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) &&
                targetFramework.Length > "netcoreapp".Length)
            {
                return targetFramework.Substring("netcoreapp".Length);
            }

            throw new InvalidOperationException(
                $"Unsupported target framework '{targetFramework}' for Build Cache runtime version inference.");
        }

        private static async Task ExtractTarGzAsync(string archivePath, string outputDir, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(outputDir);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, use tar (available since Windows 10 1803)
                var result = await ProcessUtil.RunAsync("tar", $"-xzf \"{archivePath}\" -C \"{outputDir}\"",
                    throwOnError: false, cancellationToken: cancellationToken);

                if (result.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to extract tar.gz: {result.StandardError}");
                }
            }
            else
            {
                var result = await ProcessUtil.RunAsync("/usr/bin/env", $"tar -xzf \"{archivePath}\" -C \"{outputDir}\"",
                    throwOnError: false, cancellationToken: cancellationToken);

                if (result.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to extract tar.gz: {result.StandardError}");
                }
            }
        }

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
