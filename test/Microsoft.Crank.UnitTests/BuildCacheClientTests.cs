// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Crank.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Crank.UnitTests
{
    public class BuildCacheClientTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testDir;

        public BuildCacheClientTests(ITestOutputHelper output)
        {
            _output = output;
            _testDir = Path.Combine(Path.GetTempPath(), "crank_buildcache_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // -------------------------------------------------------------------
        // ParseLatestBuilds
        // -------------------------------------------------------------------

        [Fact]
        public void ParseLatestBuilds_PascalCase_ParsesCommitShaAndTime()
        {
            const string json = """
                {
                    "BranchName": "main",
                    "coreclr_x64_linux": {
                        "CommitSha": "abc123def456",
                        "CommitTime": "2025-01-01T00:00:00Z"
                    }
                }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.Equal("main", result.BranchName);
            Assert.Equal("abc123def456", result.Entries["coreclr_x64_linux"].CommitSha);
        }

        [Fact]
        public void ParseLatestBuilds_SnakeCase_ParsesCommitShaAndTime()
        {
            const string json = """
                {
                    "branch_name": "release/10.0",
                    "coreclr_arm64_linux": {
                        "commit_sha": "deadbeef",
                        "commit_time": "2025-02-02T00:00:00Z"
                    }
                }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.Equal("release/10.0", result.BranchName);
            Assert.Equal("deadbeef", result.Entries["coreclr_arm64_linux"].CommitSha);
        }

        [Fact]
        public void ParseLatestBuilds_MixedCasing_ParsesAllConfigs()
        {
            const string json = """
                {
                    "branch_name": "main",
                    "coreclr_x64_windows": { "CommitSha": "win123", "CommitTime": "2025-03-03" },
                    "coreclr_x64_linux":   { "commit_sha": "lnx456", "commit_time": "2025-04-04" }
                }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.Equal(2, result.Entries.Count);
            Assert.Equal("win123", result.Entries["coreclr_x64_windows"].CommitSha);
            Assert.Equal("lnx456", result.Entries["coreclr_x64_linux"].CommitSha);
        }

        [Fact]
        public void ParseLatestBuilds_NonObjectValues_AreSkipped()
        {
            const string json = """
                {
                    "branch_name": "main",
                    "schemaVersion": 2,
                    "lastUpdated": "2025-01-01",
                    "coreclr_x64_linux": { "CommitSha": "abc" }
                }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.Single(result.Entries);
            Assert.True(result.Entries.ContainsKey("coreclr_x64_linux"));
        }

        // -------------------------------------------------------------------
        // ValidateCommitSha
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("abcdef12")] // min length
        [InlineData("ABCDEF12")] // upper hex
        [InlineData("603403d9cb49d3d1c35b56bcff024ce99a8c5c3a")] // full 40
        public void ValidateCommitSha_AcceptsValid(string sha)
        {
            BuildCacheClient.ValidateCommitSha(sha);
        }

        [Theory]
        [InlineData("abc")] // too short
        [InlineData("ghijklmn")] // non-hex
        [InlineData("abcd 1234")] // contains space
        [InlineData("../../../etc/passwd")] // path traversal attempt
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 41 chars, too long
        public void ValidateCommitSha_RejectsInvalid(string sha)
        {
            Assert.Throws<ArgumentException>(() => BuildCacheClient.ValidateCommitSha(sha));
        }

        // -------------------------------------------------------------------
        // ShortSha
        // -------------------------------------------------------------------

        [Fact]
        public void ShortSha_LongInput_ReturnsFirstEight()
        {
            Assert.Equal("abcdef12", BuildCacheClient.ShortSha("abcdef1234567890"));
        }

        [Fact]
        public void ShortSha_ShortInput_ReturnsAsIs()
        {
            Assert.Equal("abc", BuildCacheClient.ShortSha("abc"));
        }

        [Fact]
        public void ShortSha_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, BuildCacheClient.ShortSha(null));
            Assert.Equal(string.Empty, BuildCacheClient.ShortSha(""));
        }

        // -------------------------------------------------------------------
        // Platform / RID mapping
        // -------------------------------------------------------------------

        [Fact]
        public void GetPlatformMoniker_ReturnsKnownRid()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();

            var validRids = new[]
            {
                "linux-x64", "linux-arm64",
                "win-x64", "win-arm64", "win-x86",
                "osx-x64", "osx-arm64",
            };

            Assert.Contains(rid, validRids);
        }

        [Theory]
        [InlineData("coreclr_x64_linux", "linux-x64")]
        [InlineData("coreclr_arm64_linux", "linux-arm64")]
        [InlineData("coreclr_muslx64_linux", "linux-musl-x64")]
        [InlineData("coreclr_x64_windows", "win-x64")]
        [InlineData("coreclr_arm64_windows", "win-arm64")]
        [InlineData("coreclr_x86_windows", "win-x86")]
        public void GetRidForConfig_ReturnsMatchingRid(string configKey, string expectedRid)
        {
            Assert.Equal(expectedRid, BuildCacheClient.GetRidForConfig(configKey));
        }

        [Fact]
        public void GetRidForConfig_UnknownConfig_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => BuildCacheClient.GetRidForConfig("totally_unknown"));
        }

        // -------------------------------------------------------------------
        // ASP.NET Core config map (locked external contract — see
        // dotnet/performance pack-bcs-archives.ps1). Pins configKey / RID /
        // artifact filename so an accidental token change is caught.
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("linux-x64", "aspnetcore_x64_linux", "BuildArtifacts_linux_x64_Release_aspnetcore.tar.gz")]
        [InlineData("linux-arm64", "aspnetcore_arm64_linux", "BuildArtifacts_linux_arm64_Release_aspnetcore.tar.gz")]
        [InlineData("win-x64", "aspnetcore_x64_windows", "BuildArtifacts_windows_x64_Release_aspnetcore.zip")]
        [InlineData("win-arm64", "aspnetcore_arm64_windows", "BuildArtifacts_windows_arm64_Release_aspnetcore.zip")]
        [InlineData("win-x86", "aspnetcore_x86_windows", "BuildArtifacts_windows_x86_Release_aspnetcore.zip")]
        public void PlatformToBcsConfigAspNetCore_MatchesLockedContract(string rid, string expectedConfigKey, string expectedArtifact)
        {
            Assert.True(BuildCacheClient.PlatformToBcsConfigAspNetCore.TryGetValue(rid, out var entry), $"Missing aspnetcore entry for '{rid}'.");
            Assert.Equal(expectedConfigKey, entry.configKey);
            Assert.Equal(expectedArtifact, entry.artifactFile);
            Assert.Equal(rid, entry.rid);
        }

        [Fact]
        public void PlatformToBcsConfigAspNetCore_HasExactlyTheFiveV1Platforms()
        {
            var rids = BuildCacheClient.PlatformToBcsConfigAspNetCore.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
            Assert.Equal(
                new[] { "linux-arm64", "linux-x64", "win-arm64", "win-x64", "win-x86" },
                rids);

            // v1 explicitly excludes musl / osx / arm32.
            Assert.DoesNotContain(BuildCacheClient.PlatformToBcsConfigAspNetCore.Keys, k => k.Contains("musl", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(BuildCacheClient.PlatformToBcsConfigAspNetCore.Keys, k => k.StartsWith("osx", StringComparison.OrdinalIgnoreCase));
        }

        // -------------------------------------------------------------------
        // SelectHighestManagedDir (numeric-aware)
        // -------------------------------------------------------------------

        [Fact]
        public void SelectHighestManagedDir_NumericOrderNotLexicographic()
        {
            var libDir = Path.Combine(_testDir, "lib");
            Directory.CreateDirectory(Path.Combine(libDir, "net8.0"));
            Directory.CreateDirectory(Path.Combine(libDir, "net9.0"));
            Directory.CreateDirectory(Path.Combine(libDir, "net10.0"));
            Directory.CreateDirectory(Path.Combine(libDir, "net11.0"));

            // Lexicographic: net9.0 > net8.0 > net11.0 > net10.0 (wrong).
            // Numeric:       net11.0 > net10.0 > net9.0 > net8.0 (correct).
            var selected = BuildCacheClient.SelectHighestManagedDir(libDir);

            Assert.Equal("net11.0", Path.GetFileName(selected));
        }

        [Fact]
        public void SelectHighestManagedDir_NoDirs_ReturnsNull()
        {
            var libDir = Path.Combine(_testDir, "empty-lib");
            Directory.CreateDirectory(libDir);

            Assert.Null(BuildCacheClient.SelectHighestManagedDir(libDir));
        }

        [Fact]
        public void SelectHighestManagedDir_MissingDir_ReturnsNull()
        {
            Assert.Null(BuildCacheClient.SelectHighestManagedDir(Path.Combine(_testDir, "does-not-exist")));
        }

        // -------------------------------------------------------------------
        // OverlayPublishedOutput
        // -------------------------------------------------------------------

        [Fact]
        public void OverlayPublishedOutput_CopiesRuntimeFilesAndHostpolicyButNotApphost()
        {
            // The BCS archive ships an unbound apphost (the SDK normally binds the published
            // managed DLL path into the executable during publish). Overlaying the raw BCS apphost
            // on top of the SDK-bound one breaks the published app, so we deliberately skip it.
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = ConfigKeyForRid(rid);
            var (extractDir, _, managed, native) = BuildFakeBcsArchive(rid, includeHost: true, includeApphost: true);

            var outputFolder = Path.Combine(_testDir, "published");
            Directory.CreateDirectory(outputFolder);

            // Pre-existing SDK-bound apphost that must NOT be overwritten.
            var apphostName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "MyApp.exe" : "MyApp";
            File.WriteAllText(Path.Combine(outputFolder, apphostName), "SDK_BOUND_APPHOST");

            var copied = BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder, configKey, "MyApp");

            // managed + native + hostpolicy (no apphost contribution)
            Assert.True(copied >= managed.Count + native.Count + 1);

            foreach (var dll in managed)
            {
                Assert.True(File.Exists(Path.Combine(outputFolder, dll)), $"Missing managed file {dll}");
            }
            foreach (var n in native)
            {
                Assert.True(File.Exists(Path.Combine(outputFolder, n)), $"Missing native file {n}");
            }

            Assert.True(File.Exists(Path.Combine(outputFolder, BuildCacheClient.GetNativeLibName("hostpolicy"))));

            // SDK-bound apphost preserved.
            Assert.Equal("SDK_BOUND_APPHOST", File.ReadAllText(Path.Combine(outputFolder, apphostName)));
        }

        [Fact]
        public void OverlayPublishedOutput_EmptyExtract_ReturnsZero()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = ConfigKeyForRid(rid);

            var extractDir = Path.Combine(_testDir, "empty");
            Directory.CreateDirectory(extractDir);

            var outputFolder = Path.Combine(_testDir, "output");
            Directory.CreateDirectory(outputFolder);

            var copied = BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder, configKey, "MyApp");
            Assert.Equal(0, copied);
        }

        [Fact]
        public void OverlayPublishedOutput_SkipsPdbAndDbg()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = ConfigKeyForRid(rid);
            var (extractDir, runtimesDir, _, _) = BuildFakeBcsArchive(rid, includeHost: false, includeApphost: false);

            var nativeDir = Path.Combine(runtimesDir, "native");
            File.WriteAllText(Path.Combine(nativeDir, "coreclr.pdb"), "pdb");
            File.WriteAllText(Path.Combine(nativeDir, "libcoreclr.dbg"), "dbg");

            var outputFolder = Path.Combine(_testDir, "published-pdb");
            Directory.CreateDirectory(outputFolder);

            BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder, configKey, "MyApp");

            Assert.False(File.Exists(Path.Combine(outputFolder, "coreclr.pdb")));
            Assert.False(File.Exists(Path.Combine(outputFolder, "libcoreclr.dbg")));
        }

        // -------------------------------------------------------------------
        // CreateBuildCacheDotnetHome — the heart of round 3
        // -------------------------------------------------------------------

        [Fact]
        public void CreateBuildCacheDotnetHome_MirrorsGlobalAndOverlaysBcs()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = ConfigKeyForRid(rid);
            var (extractDir, _, managed, native) = BuildFakeBcsArchive(rid, includeHost: true, includeApphost: false);

            const string runtimeVersion = "11.0.0-preview.5.26256.117";
            const string aspNetCoreVersion = "11.0.0-preview.5.26256.117";
            var globalHome = BuildFakeGlobalDotnetHome(runtimeVersion, aspNetCoreVersion);
            var commitSha = "603403d9cb49d3d1c35b56bcff024ce99a8c5c3a";

            var bcsHome = BuildCacheClient.CreateBuildCacheDotnetHome(
                globalHome, extractDir, runtimeVersion, aspNetCoreVersion, commitSha, configKey);

            try
            {
                // 1. Global dotnet home must NOT be touched (no cross-job pollution).
                var globalVersion = File.ReadAllText(Path.Combine(globalHome, "shared", "Microsoft.NETCore.App", runtimeVersion, ".version"));
                Assert.Contains("FEED_COMMIT", globalVersion);
                Assert.DoesNotContain(commitSha, globalVersion);

                // 2. Per-job home exists with BCS overlay applied.
                Assert.True(Directory.Exists(bcsHome));
                var bcsNetCoreApp = Path.Combine(bcsHome, "shared", "Microsoft.NETCore.App", runtimeVersion);

                foreach (var dll in managed)
                {
                    Assert.True(File.Exists(Path.Combine(bcsNetCoreApp, dll)), $"Missing BCS managed {dll}");
                }
                foreach (var n in native)
                {
                    Assert.True(File.Exists(Path.Combine(bcsNetCoreApp, n)), $"Missing BCS native {n}");
                }

                // 3. .version was rewritten with BCS commit.
                var bcsVersion = File.ReadAllText(Path.Combine(bcsNetCoreApp, ".version"));
                Assert.Contains(commitSha, bcsVersion);

                // 4. ASP.NET Core dir was mirrored (from global, not overlaid).
                Assert.True(Directory.Exists(Path.Combine(bcsHome, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion)));

                // 5. dotnet host binary is present.
                var dotnetExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
                Assert.True(File.Exists(Path.Combine(bcsHome, dotnetExeName)));

                // 6. host/fxr was mirrored AND overlaid.
                var hostFxrFile = Path.Combine(bcsHome, "host", "fxr", runtimeVersion, BuildCacheClient.GetNativeLibName("hostfxr"));
                Assert.True(File.Exists(hostFxrFile));
            }
            finally
            {
                try { Directory.Delete(bcsHome, recursive: true); } catch { }
            }
        }

        [Fact]
        public void CreateBuildCacheDotnetHome_NoBcsBitsForPlatform_Throws()
        {
            // Build a BCS archive layout for an RID that doesn't match the host RID, so the
            // overlay finds nothing.
            var hostRid = BuildCacheClient.GetPlatformMoniker();
            var wrongRid = hostRid == "linux-x64" ? "win-x64" : "linux-x64";
            var (extractDir, _, _, _) = BuildFakeBcsArchive(wrongRid, includeHost: false, includeApphost: false);

            const string runtimeVersion = "11.0.0-preview.5";
            const string aspNetCoreVersion = "11.0.0-preview.5";
            var globalHome = BuildFakeGlobalDotnetHome(runtimeVersion, aspNetCoreVersion);

            // Will resolve config from host RID and search for hostRid-shaped subtree → 0 files.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                BuildCacheClient.CreateBuildCacheDotnetHome(
                    globalHome, extractDir, runtimeVersion, aspNetCoreVersion,
                    "abcdef0123456789", buildCacheConfig: null));

            Assert.Contains("0 files", ex.Message);
        }

        [Fact]
        public void CreateBuildCacheDotnetHome_TwoConcurrentJobs_AreIsolated()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = ConfigKeyForRid(rid);
            var (extractDir1, _, _, _) = BuildFakeBcsArchive(rid, includeHost: true, includeApphost: false);
            var (extractDir2, _, _, _) = BuildFakeBcsArchive(rid, includeHost: true, includeApphost: false);

            const string runtimeVersion = "11.0.0-preview.5";
            var globalHome = BuildFakeGlobalDotnetHome(runtimeVersion, runtimeVersion);
            var sha1 = "1111aaaa2222bbbb3333cccc4444dddd55556666";
            var sha2 = "6666eeee7777ffff8888aaaa9999bbbbccccdddd";

            var home1 = BuildCacheClient.CreateBuildCacheDotnetHome(
                globalHome, extractDir1, runtimeVersion, runtimeVersion, sha1, configKey);
            var home2 = BuildCacheClient.CreateBuildCacheDotnetHome(
                globalHome, extractDir2, runtimeVersion, runtimeVersion, sha2, configKey);

            try
            {
                Assert.NotEqual(home1, home2);

                var v1 = File.ReadAllText(Path.Combine(home1, "shared", "Microsoft.NETCore.App", runtimeVersion, ".version"));
                var v2 = File.ReadAllText(Path.Combine(home2, "shared", "Microsoft.NETCore.App", runtimeVersion, ".version"));

                Assert.Contains(sha1, v1);
                Assert.DoesNotContain(sha2, v1);
                Assert.Contains(sha2, v2);
                Assert.DoesNotContain(sha1, v2);

                // Global home untouched.
                var globalV = File.ReadAllText(Path.Combine(globalHome, "shared", "Microsoft.NETCore.App", runtimeVersion, ".version"));
                Assert.DoesNotContain(sha1, globalV);
                Assert.DoesNotContain(sha2, globalV);
            }
            finally
            {
                try { Directory.Delete(home1, recursive: true); } catch { }
                try { Directory.Delete(home2, recursive: true); } catch { }
            }
        }

        // -------------------------------------------------------------------
        // ASP.NET Core flavour: ParseFlavor + config→RID + overlay targeting
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("aspnetcore", true)]
        [InlineData("ASPNETCORE", true)]
        [InlineData("runtime", false)]
        [InlineData("RUNTIME", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("something-else", false)]
        public void ParseFlavor_MapsRepoNameToFlavor(string repoName, bool expectedAspNetCore)
        {
            var expected = expectedAspNetCore
                ? BuildCacheClient.BuildCacheFlavor.AspNetCore
                : BuildCacheClient.BuildCacheFlavor.Runtime;
            Assert.Equal(expected, BuildCacheClient.ParseFlavor(repoName));
        }

        [Theory]
        [InlineData("aspnetcore_x64_linux", "linux-x64")]
        [InlineData("aspnetcore_arm64_linux", "linux-arm64")]
        [InlineData("aspnetcore_x64_windows", "win-x64")]
        [InlineData("aspnetcore_arm64_windows", "win-arm64")]
        [InlineData("aspnetcore_x86_windows", "win-x86")]
        public void GetRidForConfig_AspNetCoreConfig_ReturnsMatchingRid(string configKey, string expectedRid)
        {
            // GetRidForConfig searches the union of both flavour maps; aspnetcore keys must resolve.
            Assert.Equal(expectedRid, BuildCacheClient.GetRidForConfig(configKey));
        }

        [Fact]
        public void OverlayPublishedOutput_AspNetCore_CopiesManagedAspNetDllsAndNoHost()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = AspNetConfigKeyForRid(rid);
            var (extractDir, managed) = BuildFakeAspNetCoreBcsArchive(rid);

            var outputFolder = Path.Combine(_testDir, "published-aspnet");
            Directory.CreateDirectory(outputFolder);

            var copied = BuildCacheClient.OverlayPublishedOutput(
                extractDir, outputFolder, configKey, "MyApp", BuildCacheClient.BuildCacheFlavor.AspNetCore);

            // Managed-only pack: every Microsoft.AspNetCore.*.dll is overlaid; no host binaries.
            Assert.Equal(managed.Count, copied);
            foreach (var dll in managed)
            {
                Assert.True(File.Exists(Path.Combine(outputFolder, dll)), $"Missing aspnetcore managed file {dll}");
            }
            Assert.False(File.Exists(Path.Combine(outputFolder, BuildCacheClient.GetNativeLibName("hostpolicy"))));
        }

        [Fact]
        public void CreateBuildCacheDotnetHome_AspNetCore_OverlaysAspNetCoreAppNotRuntime()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = AspNetConfigKeyForRid(rid);
            var (extractDir, managed) = BuildFakeAspNetCoreBcsArchive(rid);

            const string runtimeVersion = "11.0.0-preview.5.26256.117";
            const string aspNetCoreVersion = "11.0.0-preview.5.26256.117";
            var globalHome = BuildFakeGlobalDotnetHome(runtimeVersion, aspNetCoreVersion);
            var commitSha = "aaaabbbbccccddddeeeeffff0000111122223333";

            var bcsHome = BuildCacheClient.CreateBuildCacheDotnetHome(
                globalHome, extractDir, runtimeVersion, aspNetCoreVersion, commitSha, configKey,
                BuildCacheClient.BuildCacheFlavor.AspNetCore);

            try
            {
                // 1. BCS managed Microsoft.AspNetCore.*.dll overlaid into the per-job AspNetCore.App.
                var bcsAspNet = Path.Combine(bcsHome, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);
                foreach (var dll in managed)
                {
                    Assert.True(File.Exists(Path.Combine(bcsAspNet, dll)), $"Missing BCS aspnetcore managed {dll}");
                }

                // 2. AspNetCore.App/.version rewritten with the BCS (aspnetcore) commit.
                var aspNetVersion = File.ReadAllText(Path.Combine(bcsAspNet, ".version"));
                Assert.Contains(commitSha, aspNetVersion);
                Assert.Contains(aspNetCoreVersion, aspNetVersion);

                // 3. The base runtime (NETCore.App) is mirrored from the feed and its .version is
                //    NOT rewritten — the aspnetcore flavour overrides only the asp.net framework.
                var netCoreVersion = File.ReadAllText(Path.Combine(bcsHome, "shared", "Microsoft.NETCore.App", runtimeVersion, ".version"));
                Assert.Contains("FEED_COMMIT", netCoreVersion);
                Assert.DoesNotContain(commitSha, netCoreVersion);

                // 4. Global home untouched.
                var globalAspNet = File.ReadAllText(Path.Combine(globalHome, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion, ".version"));
                Assert.DoesNotContain(commitSha, globalAspNet);
            }
            finally
            {
                try { Directory.Delete(bcsHome, recursive: true); } catch { }
            }
        }

        [Fact]
        public void CreateBuildCacheDotnetHome_AspNetCore_MissingAspNetVersion_Throws()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var configKey = AspNetConfigKeyForRid(rid);
            var (extractDir, _) = BuildFakeAspNetCoreBcsArchive(rid);

            const string runtimeVersion = "11.0.0-preview.5";
            var globalHome = BuildFakeGlobalDotnetHome(runtimeVersion, runtimeVersion);

            Assert.Throws<ArgumentException>(() =>
                BuildCacheClient.CreateBuildCacheDotnetHome(
                    globalHome, extractDir, runtimeVersion, aspNetCoreVersion: "",
                    "abcdef0123456789", configKey, BuildCacheClient.BuildCacheFlavor.AspNetCore));
        }

        // -------------------------------------------------------------------
        // CleanupExtractDir
        // -------------------------------------------------------------------

        [Fact]
        public void CleanupExtractDir_DeletesDirectory()
        {
            var dir = Path.Combine(_testDir, "cleanup-target");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "x.txt"), "hi");

            BuildCacheClient.CleanupExtractDir(dir);

            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void CleanupExtractDir_MissingDir_DoesNotThrow()
        {
            BuildCacheClient.CleanupExtractDir(Path.Combine(_testDir, "never-existed"));
            BuildCacheClient.CleanupExtractDir(null);
            BuildCacheClient.CleanupExtractDir("");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static string ConfigKeyForRid(string rid)
            => BuildCacheClient.PlatformToBcsConfig.TryGetValue(rid, out var v) ? v.configKey : null;

        /// <summary>
        /// Builds a fake "global" dotnet home with .version files containing a FEED commit so
        /// tests can detect whether the .version was overwritten with the BCS commit.
        /// </summary>
        private string BuildFakeGlobalDotnetHome(string runtimeVersion, string aspNetCoreVersion)
        {
            var home = Path.Combine(_testDir, "global-home-" + Guid.NewGuid().ToString("N"));
            var netCoreApp = Path.Combine(home, "shared", "Microsoft.NETCore.App", runtimeVersion);
            var aspNetCoreApp = Path.Combine(home, "shared", "Microsoft.AspNetCore.App", aspNetCoreVersion);
            var hostFxr = Path.Combine(home, "host", "fxr", runtimeVersion);

            Directory.CreateDirectory(netCoreApp);
            Directory.CreateDirectory(aspNetCoreApp);
            Directory.CreateDirectory(hostFxr);

            File.WriteAllText(Path.Combine(netCoreApp, ".version"), "FEED_COMMIT_DO_NOT_TOUCH\n" + runtimeVersion + "\n");
            File.WriteAllText(Path.Combine(netCoreApp, "System.Private.CoreLib.dll"), "feed managed");
            File.WriteAllText(Path.Combine(netCoreApp, BuildCacheClient.GetNativeLibName("hostpolicy")), "feed hostpolicy");

            File.WriteAllText(Path.Combine(aspNetCoreApp, ".version"), "FEED_ASPNET\n" + aspNetCoreVersion + "\n");
            File.WriteAllText(Path.Combine(aspNetCoreApp, "Microsoft.AspNetCore.dll"), "feed aspnet");

            File.WriteAllText(Path.Combine(hostFxr, BuildCacheClient.GetNativeLibName("hostfxr")), "feed hostfxr");

            var dotnetExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
            File.WriteAllText(Path.Combine(home, dotnetExeName), "feed dotnet host");

            return home;
        }

        /// <summary>
        /// Builds a fake BCS extraction at <c>microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/</c>
        /// + corehost layout. <paramref name="includeApphost"/> adds the renamed-by-SDK apphost binary.
        /// </summary>
        private (string extractDir, string runtimesDir, List<string> managed, List<string> native)
            BuildFakeBcsArchive(string rid, bool includeHost, bool includeApphost)
        {
            var extractDir = Path.Combine(_testDir, "extracted-" + Guid.NewGuid().ToString("N"));
            var nugetPkg = Path.Combine(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            var runtimesDir = Path.Combine(nugetPkg, "Release", "runtimes", rid);
            var libDir = Path.Combine(runtimesDir, "lib", "net11.0");
            var nativeDir = Path.Combine(runtimesDir, "native");
            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(nativeDir);

            var managed = new List<string>
            {
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "System.Console.dll",
            };
            foreach (var dll in managed)
            {
                File.WriteAllText(Path.Combine(libDir, dll), "BCS managed " + dll);
            }

            List<string> native;
            if (rid.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
            {
                native = new List<string> { "coreclr.dll", "clrjit.dll" };
            }
            else if (rid.StartsWith("osx-", StringComparison.OrdinalIgnoreCase))
            {
                native = new List<string> { "libcoreclr.dylib", "libclrjit.dylib" };
            }
            else
            {
                native = new List<string> { "libcoreclr.so", "libclrjit.so" };
            }
            foreach (var n in native)
            {
                File.WriteAllText(Path.Combine(nativeDir, n), "BCS native " + n);
            }

            if (includeHost)
            {
                var hostDir = Path.Combine(extractDir, $"{rid}.Release", "corehost");
                Directory.CreateDirectory(hostDir);
                File.WriteAllText(Path.Combine(hostDir, NativeLibForRid(rid, "hostpolicy")), "BCS hostpolicy");
                File.WriteAllText(Path.Combine(hostDir, NativeLibForRid(rid, "hostfxr")), "BCS hostfxr");
                File.WriteAllText(Path.Combine(hostDir, rid.StartsWith("win-") ? "dotnet.exe" : "dotnet"), "BCS dotnet host");

                if (includeApphost)
                {
                    File.WriteAllText(Path.Combine(hostDir, rid.StartsWith("win-") ? "apphost.exe" : "apphost"), "BCS apphost");
                }
            }

            return (extractDir, runtimesDir, managed, native);
        }

        private static string NativeLibForRid(string rid, string baseName)
        {
            if (rid.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseName}.dll";
            }
            if (rid.StartsWith("osx-", StringComparison.OrdinalIgnoreCase))
            {
                return $"lib{baseName}.dylib";
            }
            return $"lib{baseName}.so";
        }

        private static string AspNetConfigKeyForRid(string rid)
            => BuildCacheClient.PlatformToBcsConfigAspNetCore.TryGetValue(rid, out var v) ? v.configKey : null;

        /// <summary>
        /// Builds a fake aspnetcore BCS extraction at
        /// <c>microsoft.aspnetcore.app.runtime.{rid}/Release/runtimes/{rid}/lib/net11.0/</c> with
        /// managed Microsoft.AspNetCore.*.dll files. The aspnetcore runtime pack is managed-only:
        /// no native dir, no corehost (mirrors dotnet/performance#5243's pack-bcs-archives.ps1).
        /// </summary>
        private (string extractDir, List<string> managed) BuildFakeAspNetCoreBcsArchive(string rid)
        {
            var extractDir = Path.Combine(_testDir, "extracted-aspnet-" + Guid.NewGuid().ToString("N"));
            var nugetPkg = Path.Combine(extractDir, $"microsoft.aspnetcore.app.runtime.{rid}");
            var libDir = Path.Combine(nugetPkg, "Release", "runtimes", rid, "lib", "net11.0");
            Directory.CreateDirectory(libDir);

            var managed = new List<string>
            {
                "Microsoft.AspNetCore.dll",
                "Microsoft.AspNetCore.Mvc.Core.dll",
                "Microsoft.AspNetCore.Routing.dll",
            };
            foreach (var dll in managed)
            {
                File.WriteAllText(Path.Combine(libDir, dll), "BCS aspnetcore managed " + dll);
            }

            return (extractDir, managed);
        }
    }
}
