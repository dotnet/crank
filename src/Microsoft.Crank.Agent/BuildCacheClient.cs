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
        /// ASP.NET Core (dotnet/aspnetcore) variant of <see cref="PlatformToBcsConfig"/>. Maps the
        /// agent's platform (RID) to the aspnetcore BCS configuration key and artifact filename.
        /// The stored artifact is the verbatim runtime-pack nupkg, so the archive's internal layout
        /// is the nupkg's own (<c>runtimes/{rid}/lib/net{X}.0/...</c> at the root). This and the
        /// configKey / artifact tokens are a load-bearing contract produced by dotnet/performance's
        /// stage-bcs-nupkg-aspnetcore.ps1.
        /// v1 has no musl/osx/arm32 entries.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (string configKey, string artifactFile, string rid)> PlatformToBcsConfigAspNetCore =
            new Dictionary<string, (string configKey, string artifactFile, string rid)>(StringComparer.OrdinalIgnoreCase)
            {
                ["linux-x64"] = ("aspnetcore_x64_linux", "BuildArtifacts_linux_x64_Release_aspnetcore.nupkg", "linux-x64"),
                ["linux-arm64"] = ("aspnetcore_arm64_linux", "BuildArtifacts_linux_arm64_Release_aspnetcore.nupkg", "linux-arm64"),
                ["win-x64"] = ("aspnetcore_x64_windows", "BuildArtifacts_windows_x64_Release_aspnetcore.nupkg", "win-x64"),
                ["win-arm64"] = ("aspnetcore_arm64_windows", "BuildArtifacts_windows_arm64_Release_aspnetcore.nupkg", "win-arm64"),
                ["win-x86"] = ("aspnetcore_x86_windows", "BuildArtifacts_windows_x86_Release_aspnetcore.nupkg", "win-x86"),
            };

        /// <summary>
        /// Which BCS repository a ci-channel resolution targets. Selects the platform→config
        /// map, the latestBuilds.json / artifact path RepoName, and the overlay target inside the
        /// dotnet home (Runtime → Microsoft.NETCore.App; AspNetCore → Microsoft.AspNetCore.App).
        /// </summary>
        internal enum BuildCacheFlavor
        {
            Runtime,
            AspNetCore,
        }

        /// <summary>BCS RepoName for each flavour (segment in BCS blob paths).</summary>
        internal const string RepoNameRuntime = "runtime";
        internal const string RepoNameAspNetCore = "aspnetcore";

        /// <summary>
        /// Maps a BCS RepoName (<c>runtime</c> / <c>aspnetcore</c> blob path segment) to a flavour. Anything
        /// other than "aspnetcore" (including empty / "runtime") resolves to <see cref="BuildCacheFlavor.Runtime"/>,
        /// preserving the proven runtime default.
        /// </summary>
        internal static BuildCacheFlavor ParseFlavor(string repoName)
        {
            return string.Equals(repoName, RepoNameAspNetCore, StringComparison.OrdinalIgnoreCase)
                ? BuildCacheFlavor.AspNetCore
                : BuildCacheFlavor.Runtime;
        }

        /// <summary>Selects the platform→config map for a flavour.</summary>
        private static IReadOnlyDictionary<string, (string configKey, string artifactFile, string rid)> GetConfigMap(BuildCacheFlavor flavor)
        {
            return flavor == BuildCacheFlavor.AspNetCore ? PlatformToBcsConfigAspNetCore : PlatformToBcsConfig;
        }

        /// <summary>
        /// All config entries across every flavour. configKeys are globally unique (coreclr_* vs
        /// aspnetcore_*), so config→artifact and config→RID lookups can search this union without
        /// the caller needing to know the flavour.
        /// </summary>
        private static IEnumerable<(string configKey, string artifactFile, string rid)> AllConfigs()
        {
            return PlatformToBcsConfig.Values.Concat(PlatformToBcsConfigAspNetCore.Values);
        }

        /// <summary>
        /// Sentinel thrown for HTTP responses that are definitively not retryable (e.g. 404).
        /// Distinguishes "the build doesn't exist" from "transient network blip".
        /// </summary>
        public class BuildCacheNotFoundException : InvalidOperationException
        {
            public BuildCacheNotFoundException(string message) : base(message) { }
        }

        /// <summary>
        /// Thrown when a BCS archive is present but does not contain a complete framework
        /// (missing managed assemblies, deps.json/runtimeconfig.json). Used by the aspnetcore
        /// direct-placement path, which deliberately fails loudly rather than producing a partial
        /// framework — for perf runs, silently running an incomplete framework is worse than erroring.
        /// </summary>
        public class BuildCacheIncompleteException : InvalidOperationException
        {
            public BuildCacheIncompleteException(string message) : base(message) { }
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
        /// True when <paramref name="value"/> looks like a commit SHA (8-40 hex chars). A .NET feed version
        /// string always contains non-hex characters (dots/dashes/letters beyond f), so it never collides.
        /// </summary>
        internal static bool IsCommitSha(string value)
            => !string.IsNullOrEmpty(value) && _shaRegex.IsMatch(value);

        /// <summary>
        /// Interprets a "ci"-channel version argument (runtimeVersion / aspNetCoreVersion). On the ci channel
        /// these arguments carry a Build Cache commit SHA — or empty / "latest" meaning the latest build on
        /// the branch — rather than a feed version. The base framework FOLDER is always the latest feed
        /// version and the BCS bits overlay onto it, so an actual version string cannot be honored here.
        /// Returns true and sets <paramref name="commitPin"/> (empty => resolve latest) when the value is a
        /// SHA or empty/"latest"; returns false with an explanatory <paramref name="error"/> when the value
        /// is a version string.
        /// </summary>
        internal static bool TryResolveCiVersionPin(string value, string argName, out string commitPin, out string error)
        {
            commitPin = "";
            error = null;

            if (string.IsNullOrEmpty(value) || string.Equals(value, "latest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsCommitSha(value))
            {
                commitPin = value;
                return true;
            }

            error =
                $"Build Cache: '{argName}' value '{value}' is not valid on the 'ci' channel. Pass a commit SHA " +
                "(8-40 hex characters) to pin a specific build, or leave it empty to use the latest build on the " +
                "branch. Version-string pinning is not supported on the 'ci' channel.";
            return false;
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
            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig, ParseFlavor(repoName));

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

            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig, ParseFlavor(repoName));
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
        /// Builds a per-job dotnet home that overlays BOTH frameworks from BCS. The global dotnet home
        /// is NOT modified, so concurrent jobs and subsequent non-buildcache jobs are unaffected.
        ///
        /// The home mirrors the feed host/fxr + base runtime (Microsoft.NETCore.App) subtrees. When a
        /// runtime BCS extract is supplied it OVERLAYS BCS bits onto the base runtime + host: the runtime
        /// archive is raw build output and does not carry the framework's deps.json/runtimeconfig.json, so
        /// overlay-onto-feed is required (it reuses the feed metadata).
        ///
        /// When an aspnetcore BCS extract is supplied the ASP.NET Core shared framework
        /// (Microsoft.AspNetCore.App) is PLACED DIRECTLY from the BCS pack — the aspnetcore archive is the
        /// runtime-pack nupkg stored verbatim, which carries the managed assemblies AND the host-resolvable
        /// deps.json/runtimeconfig.json, so the framework folder is built entirely from BCS (no feed
        /// contribution) and fails loud if incomplete. If an aspnetcore extract is NOT supplied the feed
        /// copy is cloned instead.
        ///
        /// On the ci channel both extracts are supplied (both frameworks overridden); the per-side
        /// parameters remain nullable so a single framework can be overlaid in isolation (e.g. unit tests).
        /// </summary>
        /// <returns>Absolute path to the per-job dotnet home root. Caller owns it.</returns>
        public static string CreateBuildCacheDotnetHome(
            string globalDotnetHome,
            string runtimeVersion,
            string aspNetCoreVersion,
            string runtimeExtractDir,
            string runtimeCommitSha,
            string runtimeConfig,
            string aspNetCoreExtractDir,
            string aspNetCoreCommitSha,
            string aspNetCoreConfig)
        {
            if (string.IsNullOrEmpty(runtimeVersion))
            {
                throw new ArgumentException("runtimeVersion must be provided.", nameof(runtimeVersion));
            }

            if (!string.IsNullOrEmpty(aspNetCoreExtractDir) && string.IsNullOrEmpty(aspNetCoreVersion))
            {
                throw new ArgumentException(
                    "aspNetCoreVersion must be provided when an aspnetcore BCS extract is supplied (it is the placement target).",
                    nameof(aspNetCoreVersion));
            }

            // Short sha tag for the home dir name — prefer whichever override is present.
            var homeTagSha = ShortSha(runtimeCommitSha);
            if (string.IsNullOrEmpty(homeTagSha))
            {
                homeTagSha = ShortSha(aspNetCoreCommitSha);
            }

            // Per-job, never reused across jobs to avoid pollution. (Persistent, reuse-aware homes go
            // through EnsureBuildCacheDotnetHome instead; this overload keeps the historical throwaway
            // semantics for direct callers and unit tests.)
            var bcsHomeRoot = Path.Combine(
                Path.GetTempPath(),
                "crank-buildcache",
                $"home-{homeTagSha}-{Guid.NewGuid():N}");

            BuildDotnetHomeInto(
                bcsHomeRoot, globalDotnetHome, runtimeVersion, aspNetCoreVersion,
                runtimeExtractDir, runtimeCommitSha, runtimeConfig,
                aspNetCoreExtractDir, aspNetCoreCommitSha, aspNetCoreConfig);

            Log.Info(
                $"Build Cache: Per-job dotnet home built at {bcsHomeRoot} " +
                $"(runtime {(string.IsNullOrEmpty(runtimeCommitSha) ? "feed" : ShortSha(runtimeCommitSha))}, " +
                $"aspnetcore {(string.IsNullOrEmpty(aspNetCoreCommitSha) ? "feed" : ShortSha(aspNetCoreCommitSha))})");
            return bcsHomeRoot;
        }

        // Root for persistent, SHA-keyed dotnet homes. Unlike the per-job throwaway home above, these
        // survive job cleanup and are reused across reuseBuild runs so a framework-dependent reused build
        // reliably runs the exact BCS bits it resolved (rather than silently falling back to the feed
        // runtime once the per-job home is deleted). Homes are keyed by the CONCRETE resolved commit shas
        // (never the literal "latest"), so when "latest" advances a reused job re-resolves to the new sha,
        // gets a new key, and materializes the newer bits instead of freezing the first build.
        private static string HomesRoot => Path.Combine(Path.GetTempPath(), "crank-buildcache", "homes");

        private const string HomeMarkerFileName = "bcs-home.json";

        // How many persistent homes to keep before evicting the least-recently-used. Homes are a few
        // hundred MB each; 8 covers a handful of concurrently-bisected commits without unbounded growth.
        private const int MaxPersistentHomes = 8;

        /// <summary>
        /// Computes a stable cache key for a persistent dotnet home from the CONCRETE resolved inputs.
        /// Empty shas collapse to "feed" so a pure-feed home (no BCS override on that side) is still keyed
        /// deterministically. The literal string "latest" never appears in the key or path.
        /// </summary>
        internal static string ComputeHomeCacheKey(
            string runtimeCommitSha,
            string aspNetCoreCommitSha,
            string runtimeVersion,
            string aspNetCoreVersion,
            string rid)
        {
            string ShaOrFeed(string s) => string.IsNullOrEmpty(s) ? "feed" : ShortSha(s);

            return string.Join("-", new[]
            {
                "rt", ShaOrFeed(runtimeCommitSha),
                "asp", ShaOrFeed(aspNetCoreCommitSha),
                "rtv", SanitizeForPath(runtimeVersion),
                "aspv", SanitizeForPath(aspNetCoreVersion),
                SanitizeForPath(rid),
            });
        }

        /// <summary>
        /// Returns true and the home path if a valid persistent home already exists for <paramref name="key"/>.
        /// Touches the directory so least-recently-used eviction keeps warm homes.
        /// </summary>
        internal static bool TryGetCachedDotnetHome(string key, out string homePath)
        {
            homePath = Path.Combine(HomesRoot, key);
            var marker = Path.Combine(homePath, HomeMarkerFileName);
            if (Directory.Exists(homePath) && File.Exists(marker))
            {
                TouchDirectory(homePath);
                return true;
            }

            homePath = null;
            return false;
        }

        /// <summary>
        /// Returns a persistent, SHA-keyed dotnet home for the given resolved inputs, materializing it on a
        /// cache miss. Materialization is atomic (build into a temp dir, then <see cref="Directory.Move"/> into
        /// place) and idempotent under concurrent jobs. Pass <paramref name="baseDotnetHome"/> = the global feed
        /// home for a fresh build, or a previously-materialized home (e.g. the first build's home) when only one
        /// framework drifted on reuse — the non-drifted side is cloned from that base while the drifted side
        /// (whose extract dir is supplied) is overlaid/placed from its new BCS bits.
        /// </summary>
        public static string EnsureBuildCacheDotnetHome(
            string baseDotnetHome,
            string runtimeVersion,
            string aspNetCoreVersion,
            string runtimeExtractDir,
            string runtimeCommitSha,
            string runtimeConfig,
            string aspNetCoreExtractDir,
            string aspNetCoreCommitSha,
            string aspNetCoreConfig,
            string rid)
        {
            var key = ComputeHomeCacheKey(runtimeCommitSha, aspNetCoreCommitSha, runtimeVersion, aspNetCoreVersion, rid);

            Directory.CreateDirectory(HomesRoot);

            if (TryGetCachedDotnetHome(key, out var cached))
            {
                Log.Info($"Build Cache: Reusing persistent dotnet home {cached} (key '{key}').");
                return cached;
            }

            var finalPath = Path.Combine(HomesRoot, key);
            var tmpPath = Path.Combine(HomesRoot, $"{key}.tmp-{Guid.NewGuid():N}");

            try
            {
                BuildDotnetHomeInto(
                    tmpPath, baseDotnetHome, runtimeVersion, aspNetCoreVersion,
                    runtimeExtractDir, runtimeCommitSha, runtimeConfig,
                    aspNetCoreExtractDir, aspNetCoreCommitSha, aspNetCoreConfig);

                WriteHomeMarker(tmpPath, key, runtimeVersion, aspNetCoreVersion, runtimeCommitSha, aspNetCoreCommitSha);

                try
                {
                    Directory.Move(tmpPath, finalPath);
                }
                catch (IOException)
                {
                    // Another job materialized the same key concurrently. Prefer the existing home and
                    // discard our temp copy rather than failing the build.
                    if (Directory.Exists(finalPath))
                    {
                        TryDeleteDirectory(tmpPath);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (Directory.Exists(finalPath))
                    {
                        TryDeleteDirectory(tmpPath);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch
            {
                TryDeleteDirectory(tmpPath);
                throw;
            }

            TouchDirectory(finalPath);
            EvictOldHomes(MaxPersistentHomes, keep: finalPath);

            Log.Info(
                $"Build Cache: Persistent dotnet home materialized at {finalPath} " +
                $"(runtime {(string.IsNullOrEmpty(runtimeCommitSha) ? "feed" : ShortSha(runtimeCommitSha))}, " +
                $"aspnetcore {(string.IsNullOrEmpty(aspNetCoreCommitSha) ? "feed" : ShortSha(aspNetCoreCommitSha))}, key '{key}').");
            return finalPath;
        }

        private static void WriteHomeMarker(
            string homePath, string key, string runtimeVersion, string aspNetCoreVersion,
            string runtimeCommitSha, string aspNetCoreCommitSha)
        {
            var marker = new HomeMarker
            {
                Key = key,
                RuntimeVersion = runtimeVersion,
                AspNetCoreVersion = aspNetCoreVersion,
                RuntimeCommitSha = runtimeCommitSha ?? string.Empty,
                AspNetCoreCommitSha = aspNetCoreCommitSha ?? string.Empty,
                CreatedUtc = DateTime.UtcNow,
            };

            File.WriteAllText(
                Path.Combine(homePath, HomeMarkerFileName),
                JsonSerializer.Serialize(marker));
        }

        /// <summary>
        /// Evicts the least-recently-used persistent homes, keeping at most <paramref name="max"/>. The
        /// directory named by <paramref name="keep"/> (the one just materialized/attached) is never evicted.
        /// Best-effort: a home currently in use by another job that resists deletion is simply skipped.
        /// </summary>
        private static void EvictOldHomes(int max, string keep)
        {
            try
            {
                var root = new DirectoryInfo(HomesRoot);
                if (!root.Exists)
                {
                    return;
                }

                var keepFull = keep == null ? null : Path.GetFullPath(keep);

                var homes = root.GetDirectories()
                    // Ignore in-flight temp dirs from concurrent materializations.
                    .Where(d => !d.Name.Contains(".tmp-"))
                    .OrderByDescending(d => d.LastWriteTimeUtc)
                    .ToList();

                foreach (var dir in homes.Skip(max))
                {
                    if (keepFull != null && string.Equals(Path.GetFullPath(dir.FullName), keepFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryDeleteDirectory(dir.FullName))
                    {
                        Log.Info($"Build Cache: Evicted persistent dotnet home {dir.FullName} (LRU, keeping {max}).");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Build Cache: persistent-home eviction skipped: {ex.Message}");
            }
        }

        private static void TouchDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                }
            }
            catch
            {
                // Touch is a best-effort LRU hint; never fail a build over it.
            }
        }

        private static bool TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Persisted next to every reusable build folder (keyed by Job.BuildKey). On a reuseBuild cache hit
        // the agent reads this to re-resolve the BCS commits, recompute the home key, and re-attach (or
        // re-materialize on "latest" drift) the correct BCS bits without republishing the app.
        public static bool TryReadBuildMeta(string path, out BuildCacheBuildMeta meta)
        {
            meta = null;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                meta = JsonSerializer.Deserialize<BuildCacheBuildMeta>(File.ReadAllText(path));
                return meta != null;
            }
            catch
            {
                meta = null;
                return false;
            }
        }

        public static void WriteBuildMeta(string path, BuildCacheBuildMeta meta)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(meta));
        }

        private class HomeMarker
        {
            public string Key { get; set; }
            public string RuntimeVersion { get; set; }
            public string AspNetCoreVersion { get; set; }
            public string RuntimeCommitSha { get; set; }
            public string AspNetCoreCommitSha { get; set; }
            public DateTime CreatedUtc { get; set; }
        }

        /// <summary>
        /// Reuse metadata persisted next to a reusable build folder (Job.BuildKey). Captures the concrete
        /// feed-resolved framework versions, the target RID, whether the publish was self-contained, and the
        /// BCS commit shas resolved at first build — everything needed to re-attach or refresh BCS bits on a
        /// reuseBuild cache hit without re-publishing the app.
        /// </summary>
        public class BuildCacheBuildMeta
        {
            public string RuntimeVersion { get; set; }
            public string AspNetCoreVersion { get; set; }
            public string Rid { get; set; }
            public bool SelfContained { get; set; }
            public string RuntimeCommitSha { get; set; }
            public string AspNetCoreCommitSha { get; set; }
        }

        private static void BuildDotnetHomeInto(
            string bcsHomeRoot,
            string globalDotnetHome,
            string runtimeVersion,
            string aspNetCoreVersion,
            string runtimeExtractDir,
            string runtimeCommitSha,
            string runtimeConfig,
            string aspNetCoreExtractDir,
            string aspNetCoreCommitSha,
            string aspNetCoreConfig)
        {
            Directory.CreateDirectory(bcsHomeRoot);

            try
            {
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

                // 3. Mirror shared/Microsoft.NETCore.App/{runtimeVersion} (feed clone; BCS runtime bits
                //    are overlaid on top below when a runtime extract is supplied).
                var srcNetCoreApp = Path.Combine(globalDotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
                var dstNetCoreApp = Path.Combine(bcsHomeRoot, "shared", "Microsoft.NETCore.App", runtimeVersion);
                if (Directory.Exists(srcNetCoreApp))
                {
                    CopyDirectory(srcNetCoreApp, dstNetCoreApp);
                }

                // 4. Overlay the BCS base runtime (Microsoft.NETCore.App) + host onto the feed clone.
                if (!string.IsNullOrEmpty(runtimeExtractDir))
                {
                    OverlayRuntimeIntoHome(
                        runtimeExtractDir, bcsHomeRoot, dstNetCoreApp, dstHostFxr, dotnetExeName,
                        runtimeVersion, runtimeCommitSha, runtimeConfig);
                }

                // 5. Place / clone the ASP.NET Core shared framework (Microsoft.AspNetCore.App).
                if (!string.IsNullOrEmpty(aspNetCoreVersion))
                {
                    var dstAspNet = Path.Combine(bcsHomeRoot, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);

                    if (!string.IsNullOrEmpty(aspNetCoreExtractDir))
                    {
                        // Place DIRECTLY from the BCS pack (no feed clone) so no feed assembly can leak
                        // into the framework that actually runs. Fails loud if the pack is incomplete.
                        var aspNetConfigResolved = ResolveBuildCacheConfig(aspNetCoreConfig, BuildCacheFlavor.AspNetCore);
                        var aspNetRid = GetRidForConfig(aspNetConfigResolved);
                        PlaceAspNetFrameworkFromPack(aspNetCoreExtractDir, dstAspNet, aspNetRid, aspNetCoreVersion, aspNetCoreCommitSha);
                    }
                    else
                    {
                        // Non-overridden framework: clone the feed copy.
                        var srcAspNet = Path.Combine(globalDotnetHome, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);
                        if (Directory.Exists(srcAspNet))
                        {
                            CopyDirectory(srcAspNet, dstAspNet);
                        }
                    }
                }
            }
            catch
            {
                try { Directory.Delete(bcsHomeRoot, recursive: true); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Overlays the BCS base runtime (Microsoft.NETCore.App) managed + native binaries and host
        /// (hostpolicy/hostfxr/dotnet) onto an already-feed-cloned per-job home. Rewrites the overlaid
        /// runtime's .version to carry the BCS commit. Throws (loud) if the overlay copies 0 files.
        /// </summary>
        private static void OverlayRuntimeIntoHome(
            string extractDir, string bcsHomeRoot, string dstNetCoreApp, string dstHostFxr,
            string dotnetExeName, string runtimeVersion, string commitSha, string buildCacheConfig)
        {
            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig, BuildCacheFlavor.Runtime);
            var rid = GetRidForConfig(buildCacheConfig);

            // Track each category of the runtime overlay separately. Checking only an aggregate count
            // lets a partial/corrupt archive (e.g. host layout discovered but managed/native missing)
            // pass and rewrite .version, silently running a mix of BCS host + feed managed/native bits.
            // The managed assemblies and native runtime libraries are the load-bearing shared-framework
            // payload, so require BOTH before we stamp .version.
            int managedOverlaid = 0;
            int nativeOverlaid = 0;
            int hostOverlaid = 0;

            // Overlay BCS managed + native into the per-job NETCore.App.
            var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            if (nugetPackageDir != null)
            {
                var runtimesDir = Path.Combine(nugetPackageDir, "Release", "runtimes", rid);
                if (Directory.Exists(runtimesDir))
                {
                    managedOverlaid += CopyManaged(runtimesDir, dstNetCoreApp);
                    nativeOverlaid += CopyNative(runtimesDir, dstNetCoreApp);
                }
            }

            // Overlay BCS host binaries. Host is best-effort: the per-job home was cloned from the feed
            // install (steps 1-2 of the home build), so hostfxr/hostpolicy/muxer already exist there; the
            // BCS host overlay only refines them when the archive ships a corehost dir.
            var corehostDir = FindCorehostDirectory(extractDir, rid);
            if (corehostDir != null)
            {
                hostOverlaid += CopyHostBinaryIfPresent(corehostDir, dstNetCoreApp, GetNativeLibName("hostpolicy"));

                if (Directory.Exists(dstHostFxr))
                {
                    hostOverlaid += CopyHostBinaryIfPresent(corehostDir, dstHostFxr, GetNativeLibName("hostfxr"));
                }

                var dstDotnetHost = Path.Combine(bcsHomeRoot, dotnetExeName);
                var copied = CopyHostBinaryIfPresent(corehostDir, bcsHomeRoot, dotnetExeName);
                if (copied > 0)
                {
                    EnsureExecutable(dstDotnetHost);
                }
                hostOverlaid += copied;
            }

            if (managedOverlaid == 0 || nativeOverlaid == 0)
            {
                throw new InvalidOperationException(
                    $"Build Cache: runtime overlay is incomplete for commit {ShortSha(commitSha)} (config '{buildCacheConfig}', rid '{rid}'): " +
                    $"copied {managedOverlaid} managed assemblies, {nativeOverlaid} native libraries, {hostOverlaid} host binaries. " +
                    "Both managed and native runtime bits are required. The archive layout may have changed or the platform is not supported.");
            }

            if (hostOverlaid == 0)
            {
                Log.Info($"Build Cache: runtime overlay for commit {ShortSha(commitSha)} copied no host binaries (rid '{rid}'); using the feed-cloned host.");
            }

            // Rewrite the overlaid runtime's .version so any consumer (the agent's own version
            // measurement, GetDependencies, etc.) reports the BCS commit.
            File.WriteAllText(
                Path.Combine(dstNetCoreApp, ".version"),
                $"{commitSha}\n{runtimeVersion}\n");
        }

        /// <summary>
        /// Resolves the <c>runtimes/{rid}</c> directory inside an extracted aspnetcore archive. The
        /// aspnetcore archive is the runtime-pack nupkg stored verbatim, so the directory lives at the
        /// archive root (<c>runtimes/{rid}</c>). Falls back to the wrapped build-output layout
        /// (<c>microsoft.aspnetcore.app.runtime.{rid}/Release/runtimes/{rid}</c>) for resilience.
        /// </summary>
        private static string ResolveAspNetRuntimesDir(string extractDir, string rid)
        {
            var bare = Path.Combine(extractDir, "runtimes", rid);
            if (Directory.Exists(bare))
            {
                return bare;
            }

            var pkg = FindDirectory(extractDir, $"microsoft.aspnetcore.app.runtime.{rid}");
            if (pkg != null)
            {
                var wrapped = Path.Combine(pkg, "Release", "runtimes", rid);
                if (Directory.Exists(wrapped))
                {
                    return wrapped;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds the ASP.NET Core shared-framework folder directly from the verbatim runtime-pack
        /// nupkg's <c>runtimes/{rid}/lib/net{X}.0/</c> + <c>runtimes/{rid}/native/</c>. Copies the
        /// WHOLE managed set (assemblies + Microsoft.AspNetCore.App.deps.json + runtimeconfig.json,
        /// skipping only debug symbols), then synthesizes <c>.version</c> carrying the BCS commit.
        /// Throws <see cref="BuildCacheIncompleteException"/> if the result is not a complete,
        /// host-resolvable framework — no feed fallback.
        /// </summary>
        private static void PlaceAspNetFrameworkFromPack(
            string extractDir, string destFrameworkDir, string rid, string version, string commitSha)
        {
            var runtimesDir = ResolveAspNetRuntimesDir(extractDir, rid);
            if (runtimesDir == null)
            {
                throw new BuildCacheIncompleteException(
                    $"Build Cache: aspnetcore runtime pack (runtimes/{rid}) not found in archive for commit {ShortSha(commitSha)}. " +
                    "The archive may not be the expected verbatim Microsoft.AspNetCore.App.Runtime.{rid} nupkg.");
            }

            var managedDir = SelectHighestManagedDir(Path.Combine(runtimesDir, "lib"));
            if (managedDir == null)
            {
                throw new BuildCacheIncompleteException(
                    $"Build Cache: no managed lib/net*.0 directory in aspnetcore pack for commit {ShortSha(commitSha)} (rid '{rid}').");
            }

            Directory.CreateDirectory(destFrameworkDir);

            // Copy the ENTIRE managed set (assemblies + deps.json + runtimeconfig.json). This is the
            // load-bearing difference from an overlay: the verbatim nupkg carries the host-resolvable
            // metadata, so the placed folder is a complete shared framework with no feed contribution.
            foreach (var file in Directory.GetFiles(managedDir))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dest = Path.Combine(destFrameworkDir, name);
                File.Copy(file, dest, overwrite: true);
                EnsureExecutable(dest);
            }

            // Native (e.g. aspnetcorev2_inprocess on Windows) — optional; 0 if absent.
            CopyNative(runtimesDir, destFrameworkDir);

            // Synthesize the host-resolvable .version carrying the BCS aspnetcore commit.
            File.WriteAllText(Path.Combine(destFrameworkDir, ".version"), $"{commitSha}\n{version}\n");

            // Fail-loud: a complete framework needs managed assemblies AND the host-resolvable metadata.
            void Require(bool condition, string what)
            {
                if (!condition)
                {
                    throw new BuildCacheIncompleteException(
                        $"Build Cache: directly-placed Microsoft.AspNetCore.App for commit {ShortSha(commitSha)} is incomplete — missing {what}. " +
                        "The BCS archive must be the complete runtime-pack nupkg (managed assemblies + deps.json + runtimeconfig.json). " +
                        "Failing the job rather than running an incomplete framework.");
                }
            }

            Require(Directory.GetFiles(destFrameworkDir, "*.dll").Length > 0, "managed assemblies (*.dll)");
            Require(File.Exists(Path.Combine(destFrameworkDir, "Microsoft.AspNetCore.App.deps.json")), "Microsoft.AspNetCore.App.deps.json");
            Require(File.Exists(Path.Combine(destFrameworkDir, "Microsoft.AspNetCore.App.runtimeconfig.json")), "Microsoft.AspNetCore.App.runtimeconfig.json");
        }

        /// <summary>
        /// Overlays BCS runtime binaries into a self-contained published output directory. For SCD
        /// the runtime ships next to the app, so this is the only way to make the benchmark
        /// actually run BCS bits.
        ///
        /// For <see cref="BuildCacheFlavor.Runtime"/> the base runtime managed + native binaries
        /// and host (hostpolicy) are overlaid. For <see cref="BuildCacheFlavor.AspNetCore"/> only
        /// the managed (+ optional native) Microsoft.AspNetCore.*.dll set is overlaid — the
        /// aspnetcore pack ships no host binaries and the base runtime stays the published one.
        /// The SDK-bound apphost is never replaced (see note below).
        /// </summary>
        /// <returns>Number of files overlaid.</returns>
        public static int OverlayPublishedOutput(
            string extractDir,
            string outputFolder,
            string buildCacheConfig,
            string assemblyName,
            BuildCacheFlavor flavor = BuildCacheFlavor.Runtime)
        {
            buildCacheConfig = ResolveBuildCacheConfig(buildCacheConfig, flavor);
            var rid = GetRidForConfig(buildCacheConfig);
            int filesCopied = 0;

            // The aspnetcore artifact is the raw runtime-pack nupkg (runtimes/{rid} at the archive
            // root); the runtime artifact wraps it in microsoft.netcore.app.runtime.{rid}/Release.
            string runtimesDir;
            if (flavor == BuildCacheFlavor.AspNetCore)
            {
                runtimesDir = ResolveAspNetRuntimesDir(extractDir, rid);
            }
            else
            {
                var nugetPackageDir = FindDirectory(extractDir, $"microsoft.netcore.app.runtime.{rid}");
                runtimesDir = nugetPackageDir != null ? Path.Combine(nugetPackageDir, "Release", "runtimes", rid) : null;
            }

            if (runtimesDir != null && Directory.Exists(runtimesDir))
            {
                // SCD co-mingles the framework with the app in one folder governed by the app's own
                // .deps.json, so overlay only the managed *.dll (+ native) — CopyManaged copies *.dll
                // only, so the framework's deps.json/runtimeconfig.json are intentionally NOT copied.
                filesCopied += CopyManaged(runtimesDir, outputFolder);
                filesCopied += CopyNative(runtimesDir, outputFolder);
            }

            // The aspnetcore pack ships no host binaries, so host overlay only applies to runtime.
            var corehostDir = flavor == BuildCacheFlavor.AspNetCore ? null : FindCorehostDirectory(extractDir, rid);
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

            if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                archivePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
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

        private static string ResolveBuildCacheConfig(string buildCacheConfig, BuildCacheFlavor flavor = BuildCacheFlavor.Runtime)
        {
            if (!string.IsNullOrEmpty(buildCacheConfig))
            {
                return buildCacheConfig;
            }

            var rid = GetPlatformMoniker();
            if (GetConfigMap(flavor).TryGetValue(rid, out var mapped))
            {
                return mapped.configKey;
            }

            throw new InvalidOperationException(
                $"No Build Cache configuration mapping for platform '{rid}' (repo '{flavor}'). Specify buildCacheConfig explicitly.");
        }

        private static string GetArtifactFile(string buildCacheConfig)
        {
            var match = AllConfigs().FirstOrDefault(v =>
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
        /// archive instead of falling back to the host's detected RID. Searches every flavour's
        /// map since config keys are globally unique.
        /// </summary>
        internal static string GetRidForConfig(string buildCacheConfig)
        {
            var match = AllConfigs().FirstOrDefault(v =>
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
                    // Map x86 processes to win-x64 to match Startup.GetPlatformMoniker (the publish RID).
                    // A single shared RID keeps BCS archive selection aligned with the published output so
                    // we never overlay win-x86 BCS bits onto a win-x64 publish (or vice versa).
                    Architecture.X86 => "win-x64",
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
