using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Trace;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class TraceExtensionsTests
    {
        // ----- CLR keywords -----

        [Theory]
        // Legacy typo'd names preserved for back-compat.
        [InlineData("gcsampledobjectallcationhigh", 0x200000L)]
        [InlineData("gcsampledobjectallcationlow", 0x2000000L)]
        [InlineData("fusion", 0x4L)]
        [InlineData("gcheapcollect", 0x800000L)]
        // Corrected modern names.
        [InlineData("gcsampledobjectallocationhigh", 0x200000L)]
        [InlineData("gcsampledobjectallocationlow", 0x2000000L)]
        [InlineData("assemblyloader", 0x4L)]
        [InlineData("managedheapcollect", 0x800000L)]
        // Modern additions from dotnet/diagnostics ProviderUtils.cs.
        [InlineData("monitoring", 0x200000000L)]
        [InlineData("codesymbols", 0x400000000L)]
        [InlineData("compilation", 0x1000000000L)]
        [InlineData("waithandle", 0x40000000000L)]
        [InlineData("allocationsampling", 0x80000000000L)]
        public void ToCLREventPipeProviders_ResolvesSingleKeyword(string keyword, long expectedMask)
        {
            var providers = TraceExtensions.ToCLREventPipeProviders(keyword).ToList();

            Assert.Single(providers);
            Assert.Equal(TraceExtensions.CLREventProviderName, providers[0].Name);
            Assert.Equal(expectedMask, providers[0].Keywords);
        }

        [Fact]
        public void ToCLREventPipeProviders_TyposAndModernAliasesProduceIdenticalMasks()
        {
            // Pairs of (legacy, modern) that must resolve to the same mask.
            var pairs = new[]
            {
                ("gcsampledobjectallcationhigh", "gcsampledobjectallocationhigh"),
                ("gcsampledobjectallcationlow",  "gcsampledobjectallocationlow"),
                ("fusion",                       "assemblyloader"),
                ("gcheapcollect",                "managedheapcollect"),
            };

            foreach (var (legacy, modern) in pairs)
            {
                var legacyMask = TraceExtensions.ToCLREventPipeProviders(legacy).Single().Keywords;
                var modernMask = TraceExtensions.ToCLREventPipeProviders(modern).Single().Keywords;
                Assert.Equal(legacyMask, modernMask);
            }
        }

        [Fact]
        public void ToCLREventPipeProviders_PlusExpressionOrsKeywords()
        {
            // GC (0x1) + GCHandle (0x2) + Exception (0x8000) = 0x8003.
            var providers = TraceExtensions.ToCLREventPipeProviders("gc+gchandle+exception").ToList();

            Assert.Single(providers);
            Assert.Equal(0x8003L, providers[0].Keywords);
        }

        [Fact]
        public void ToCLREventPipeProviders_UnknownKeywordSilentlySkipped()
        {
            // Crank intentionally diverges from upstream's ProviderUtils.cs by
            // skipping unknown keywords instead of throwing, so legacy
            // configurations don't break when a user has a typo.
            var providers = TraceExtensions.ToCLREventPipeProviders("not-a-real-keyword").ToList();

            Assert.Empty(providers);
        }

        // ----- Profile lookup -----

        [Theory]
        // Legacy profile preserved for back-compat — must keep both providers.
        [InlineData("cpu-sampling", 2)]
        [InlineData("gc-verbose", 1)]
        [InlineData("gc-collect", 1)]
        // Modern profiles imported from dotnet/diagnostics ListProfilesCommandHandler.
        [InlineData("dotnet-common", 1)]
        [InlineData("dotnet-sampled-thread-time", 1)]
        [InlineData("sample-profiler", 1)]
        [InlineData("database", 2)]
        public void DotNETRuntimeProfiles_ContainsExpectedProviders(string profileName, int expectedProviderCount)
        {
            Assert.True(TraceExtensions.DotNETRuntimeProfiles.TryGetValue(profileName, out var providers),
                $"Expected profile '{profileName}' to be registered.");
            Assert.Equal(expectedProviderCount, providers.Length);
        }

        [Fact]
        public void DotNETRuntimeProfiles_CpuSamplingPreservesLegacyShape()
        {
            // PR A is strictly additive: the legacy cpu-sampling profile must
            // still resolve to (SampleProfiler, DotNETRuntime@Default-keywords)
            // so existing crank pipelines produce identical trace contents.
            var providers = TraceExtensions.DotNETRuntimeProfiles["cpu-sampling"];

            Assert.Contains(providers, p => p.Name == "Microsoft-DotNETCore-SampleProfiler");
            Assert.Contains(providers, p => p.Name == "Microsoft-Windows-DotNETRuntime");
        }

        [Fact]
        public void DotNETRuntimeProfiles_LookupIsCaseInsensitive()
        {
            Assert.True(TraceExtensions.DotNETRuntimeProfiles.TryGetValue("CPU-SAMPLING", out var _));
            Assert.True(TraceExtensions.DotNETRuntimeProfiles.TryGetValue("Dotnet-Common", out var _));
        }

        // ----- ToProvider parsing -----

        [Fact]
        public void ToProvider_ParsesProviderColonKeywordsColonLevel()
        {
            // Microsoft-Windows-DotNETRuntime:0x14C14FCCBD:4 is the canonical
            // example from dotnet-trace docs.
            var providers = TraceExtensions.ToProvider("Microsoft-Windows-DotNETRuntime:0x14C14FCCBD:4").ToList();

            Assert.Single(providers);
            Assert.Equal("Microsoft-Windows-DotNETRuntime", providers[0].Name);
            Assert.Equal(0x14C14FCCBDL, providers[0].Keywords);
            Assert.Equal(System.Diagnostics.Tracing.EventLevel.Informational, providers[0].EventLevel);
        }

        [Fact]
        public void ToProvider_NameOnlyDefaultsKeywordsToAllAndLevelToVerbose()
        {
            var providers = TraceExtensions.ToProvider("Microsoft-AspNetCore-Server-Kestrel").ToList();

            Assert.Single(providers);
            Assert.Equal("Microsoft-AspNetCore-Server-Kestrel", providers[0].Name);
            Assert.Equal(-1L, providers[0].Keywords);
        }

        // ----- Smoke test: validates the bumped Microsoft.Diagnostics.NETCore.Client surface -----

        [Fact]
        public async Task StartEventPipeSession_ThreeArgOverload_ProducesNonEmptyStream()
        {
            // PR A bumps Microsoft.Diagnostics.NETCore.Client from 0.2.621003
            // to 0.2.661903, primarily to pick up the 3-arg
            // StartEventPipeSession(providers, requestRundown, circularBufferMB)
            // overload that crank now wires through DotNetTraceRequestRundown
            // and DotNetTraceBufferSizeMB. This test attaches to the current
            // test process and exercises that overload end-to-end to make sure
            // the package bump didn't silently change behavior.
            //
            // EventPipe over IPC requires the DOTNET_ runtime config to be
            // available; on some Linux CI sandboxes the socket directory is
            // restricted and the test is skipped rather than failing.
            int pid = Process.GetCurrentProcess().Id;
            var client = new DiagnosticsClient(pid);

            var providers = TraceExtensions.DotNETRuntimeProfiles["cpu-sampling"];

            EventPipeSession session;
            try
            {
                session = client.StartEventPipeSession(providers, requestRundown: true, circularBufferMB: 64);
            }
            catch (ServerNotAvailableException)
            {
                // Diagnostic IPC unavailable in this sandbox; skip rather than fail.
                return;
            }
            catch (UnsupportedCommandException)
            {
                return;
            }

            using (session)
            {
                using var cts = new CancellationTokenSource(System.TimeSpan.FromSeconds(5));
                var buffer = new byte[1024];
                int totalRead = 0;

                try
                {
                    while (totalRead < buffer.Length && !cts.IsCancellationRequested)
                    {
                        int n = await session.EventStream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cts.Token);
                        if (n == 0)
                        {
                            break;
                        }
                        totalRead += n;
                    }
                }
                catch (System.OperationCanceledException)
                {
                    // Expected if the stream is slow to produce; the 5s cap is
                    // intentional so the test stays under a second on fast
                    // machines without flake risk on slow ones.
                }

                Assert.True(totalRead > 0, "Expected EventPipe stream to produce at least some bytes within 5s.");
            }
        }
    }
}
