// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            Assert.True(result.Entries.ContainsKey("coreclr_x64_linux"));
            Assert.Equal("abc123def456", result.Entries["coreclr_x64_linux"].CommitSha);
            Assert.Equal("2025-01-01T00:00:00Z", result.Entries["coreclr_x64_linux"].CommitTime);
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
            Assert.True(result.Entries.ContainsKey("coreclr_arm64_linux"));
            Assert.Equal("deadbeef", result.Entries["coreclr_arm64_linux"].CommitSha);
            Assert.Equal("2025-02-02T00:00:00Z", result.Entries["coreclr_arm64_linux"].CommitTime);
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
        public void ParseLatestBuilds_MissingFields_ReturnsNullsWithoutThrowing()
        {
            const string json = """
                {
                    "branch_name": "main",
                    "coreclr_x64_linux": { "CommitSha": "abc" },
                    "empty_config": {}
                }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.Equal("abc", result.Entries["coreclr_x64_linux"].CommitSha);
            Assert.Null(result.Entries["coreclr_x64_linux"].CommitTime);
            Assert.Null(result.Entries["empty_config"].CommitSha);
            Assert.Null(result.Entries["empty_config"].CommitTime);
        }

        [Fact]
        public void ParseLatestBuilds_EntriesLookupIsCaseInsensitive()
        {
            const string json = """
                { "branch_name": "main", "CoreCLR_X64_Linux": { "CommitSha": "abc" } }
                """;

            var result = BuildCacheClient.ParseLatestBuilds(json);

            Assert.True(result.Entries.ContainsKey("coreclr_x64_linux"));
            Assert.True(result.Entries.ContainsKey("CORECLR_X64_LINUX"));
        }

        [Fact]
        public void ParseLatestBuilds_NonObjectValues_AreSkipped()
        {
            // Real-world payloads sometimes carry non-object metadata that must be ignored.
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
        // GetPlatformMoniker
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

        [Fact]
        public void PlatformToBcsConfig_ContainsAllSupportedRids()
        {
            // Sanity: agents typically run on these RIDs; ensure the table covers them.
            Assert.True(BuildCacheClient.PlatformToBcsConfig.ContainsKey("linux-x64"));
            Assert.True(BuildCacheClient.PlatformToBcsConfig.ContainsKey("linux-arm64"));
            Assert.True(BuildCacheClient.PlatformToBcsConfig.ContainsKey("win-x64"));
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
        // GetNativeLibName
        // -------------------------------------------------------------------

        [Fact]
        public void GetNativeLibName_MatchesHostPlatform()
        {
            var name = BuildCacheClient.GetNativeLibName("hostpolicy");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal("hostpolicy.dll", name);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Equal("libhostpolicy.dylib", name);
            }
            else
            {
                Assert.Equal("libhostpolicy.so", name);
            }
        }

        // -------------------------------------------------------------------
        // OverlayPublishedOutput / OverlayDotnetHome
        // -------------------------------------------------------------------

        [Fact]
        public void OverlayPublishedOutput_CopiesAllRuntimeFilesUnconditionally()
        {
            // Build a fake BCS extract layout for the host RID.
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, _, expectedManagedNames, expectedNativeNames) = BuildFakeBcsArchive(rid, includeHost: true);

            var outputFolder = Path.Combine(_testDir, "published");
            Directory.CreateDirectory(outputFolder);

            // Note: outputFolder is intentionally EMPTY — the overlay must still copy
            // managed/native runtime files (regression: earlier behavior skipped missing dest).
            var copied = BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder);

            Assert.True(copied >= expectedManagedNames.Count + expectedNativeNames.Count,
                $"Expected at least {expectedManagedNames.Count + expectedNativeNames.Count} files; got {copied}");

            foreach (var dll in expectedManagedNames)
            {
                Assert.True(File.Exists(Path.Combine(outputFolder, dll)), $"Missing managed file {dll}");
            }
            foreach (var native in expectedNativeNames)
            {
                Assert.True(File.Exists(Path.Combine(outputFolder, native)), $"Missing native file {native}");
            }

            // hostpolicy must have been copied alongside the app for self-contained.
            Assert.True(File.Exists(Path.Combine(outputFolder, BuildCacheClient.GetNativeLibName("hostpolicy"))));
        }

        [Fact]
        public void OverlayPublishedOutput_NoMatchingPlatformLayout_ReturnsZero()
        {
            // Empty extract directory ⇒ overlay finds nothing ⇒ returns 0 (caller fails the job).
            var extractDir = Path.Combine(_testDir, "empty-extract");
            Directory.CreateDirectory(extractDir);

            var outputFolder = Path.Combine(_testDir, "published");
            Directory.CreateDirectory(outputFolder);

            var copied = BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder);

            Assert.Equal(0, copied);
        }

        [Fact]
        public void OverlayPublishedOutput_SkipsPdbAndDbgFiles()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, runtimesDir, _, _) = BuildFakeBcsArchive(rid, includeHost: false);

            // Add a .pdb and .dbg in the native dir.
            var nativeDir = Path.Combine(runtimesDir, "native");
            File.WriteAllText(Path.Combine(nativeDir, "coreclr.pdb"), "pdb");
            File.WriteAllText(Path.Combine(nativeDir, "libcoreclr.dbg"), "dbg");

            var outputFolder = Path.Combine(_testDir, "published");
            Directory.CreateDirectory(outputFolder);

            BuildCacheClient.OverlayPublishedOutput(extractDir, outputFolder);

            Assert.False(File.Exists(Path.Combine(outputFolder, "coreclr.pdb")));
            Assert.False(File.Exists(Path.Combine(outputFolder, "libcoreclr.dbg")));
        }

        [Fact]
        public void OverlayDotnetHome_RequiresExistingSharedFrameworkDir()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, _, _, _) = BuildFakeBcsArchive(rid, includeHost: true);

            var dotnetHome = Path.Combine(_testDir, "dotnetHome-missing");
            Directory.CreateDirectory(dotnetHome);

            // shared/Microsoft.NETCore.App/{version} does NOT exist ⇒ should throw with a clear message.
            var ex = Assert.Throws<InvalidOperationException>(
                () => BuildCacheClient.OverlayDotnetHome(extractDir, dotnetHome, "10.0.0-preview.1"));

            Assert.Contains("shared framework", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OverlayDotnetHome_OverlaysSharedFrameworkAndHostFxr()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, _, expectedManagedNames, expectedNativeNames) = BuildFakeBcsArchive(rid, includeHost: true);

            const string runtimeVersion = "10.0.0-preview.1";
            var dotnetHome = Path.Combine(_testDir, "dotnetHome");
            var sharedFw = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            var hostFxr = Path.Combine(dotnetHome, "host", "fxr", runtimeVersion);
            Directory.CreateDirectory(sharedFw);
            Directory.CreateDirectory(hostFxr);

            var copied = BuildCacheClient.OverlayDotnetHome(extractDir, dotnetHome, runtimeVersion);

            Assert.True(copied > 0);
            foreach (var dll in expectedManagedNames)
            {
                Assert.True(File.Exists(Path.Combine(sharedFw, dll)), $"Missing managed in shared FW: {dll}");
            }
            foreach (var native in expectedNativeNames)
            {
                Assert.True(File.Exists(Path.Combine(sharedFw, native)), $"Missing native in shared FW: {native}");
            }

            Assert.True(File.Exists(Path.Combine(hostFxr, BuildCacheClient.GetNativeLibName("hostfxr"))));
        }

        [Fact]
        public void OverlayDotnetHome_WithCommitSha_RewritesVersionFile()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, _, _, _) = BuildFakeBcsArchive(rid, includeHost: true);

            const string runtimeVersion = "11.0.0-preview.5.26256.117";
            const string commitSha = "603403d9cb49d3d1c35b56bcff024ce99a8c5c3a";
            var dotnetHome = Path.Combine(_testDir, "dotnetHome-version");
            var sharedFw = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            Directory.CreateDirectory(sharedFw);

            // Simulate dotnet-install having already written a .version file with the FEED commit.
            File.WriteAllText(Path.Combine(sharedFw, ".version"), "feedfeedfeed\n" + runtimeVersion + "\n");

            BuildCacheClient.OverlayDotnetHome(extractDir, dotnetHome, runtimeVersion, commitSha);

            var versionFileContents = File.ReadAllText(Path.Combine(sharedFw, ".version"));
            var lines = versionFileContents.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(commitSha, lines[0]);
            Assert.Equal(runtimeVersion, lines[1]);
        }

        [Fact]
        public void OverlayDotnetHome_WithoutCommitSha_LeavesVersionFileUntouched()
        {
            var rid = BuildCacheClient.GetPlatformMoniker();
            var (extractDir, _, _, _) = BuildFakeBcsArchive(rid, includeHost: true);

            const string runtimeVersion = "11.0.0-preview.5.26256.117";
            var dotnetHome = Path.Combine(_testDir, "dotnetHome-noversion");
            var sharedFw = Path.Combine(dotnetHome, "shared", "Microsoft.NETCore.App", runtimeVersion);
            Directory.CreateDirectory(sharedFw);

            const string original = "feedfeedfeed\n" + "11.0.0-preview.5.26256.117\n";
            File.WriteAllText(Path.Combine(sharedFw, ".version"), original);

            BuildCacheClient.OverlayDotnetHome(extractDir, dotnetHome, runtimeVersion);

            Assert.Equal(original, File.ReadAllText(Path.Combine(sharedFw, ".version")));
        }

        // -------------------------------------------------------------------
        // Fake BCS archive helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Builds an on-disk fake of an extracted BCS archive matching the layout the agent expects:
        /// <c>microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/lib/net10.0/*.dll</c>,
        /// <c>microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/native/*</c>, and
        /// optionally <c>{rid}.Release/corehost/*</c>.
        /// </summary>
        private (string extractDir, string runtimesDir, System.Collections.Generic.List<string> managed, System.Collections.Generic.List<string> native)
            BuildFakeBcsArchive(string rid, bool includeHost)
        {
            var extractDir = Path.Combine(_testDir, "extracted-" + Guid.NewGuid().ToString("N"));
            var nugetPkg = Path.Combine(extractDir, $"microsoft.netcore.app.runtime.{rid}");
            var runtimesDir = Path.Combine(nugetPkg, "Release", "runtimes", rid);
            var libDir = Path.Combine(runtimesDir, "lib", "net10.0");
            var nativeDir = Path.Combine(runtimesDir, "native");
            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(nativeDir);

            var managed = new System.Collections.Generic.List<string>
            {
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "System.Console.dll",
            };
            foreach (var dll in managed)
            {
                File.WriteAllText(Path.Combine(libDir, dll), "fake managed " + dll);
            }

            var native = new System.Collections.Generic.List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                native.AddRange(new[] { "coreclr.dll", "clrjit.dll" });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                native.AddRange(new[] { "libcoreclr.dylib", "libclrjit.dylib" });
            }
            else
            {
                native.AddRange(new[] { "libcoreclr.so", "libclrjit.so" });
            }
            foreach (var n in native)
            {
                File.WriteAllText(Path.Combine(nativeDir, n), "fake native " + n);
            }

            if (includeHost)
            {
                var hostDir = Path.Combine(extractDir, $"{rid}.Release", "corehost");
                Directory.CreateDirectory(hostDir);
                File.WriteAllText(Path.Combine(hostDir, BuildCacheClient.GetNativeLibName("hostpolicy")), "hostpolicy");
                File.WriteAllText(Path.Combine(hostDir, BuildCacheClient.GetNativeLibName("hostfxr")), "hostfxr");
                File.WriteAllText(Path.Combine(hostDir, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet"), "dotnet");
            }

            return (extractDir, runtimesDir, managed, native);
        }
    }
}
