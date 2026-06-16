using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    // Unit tests for the PR B dotnet-trace CLI argv builder and the legacy
    // CLR-keyword alias normalizer. These live entirely on
    // pure-function helpers so they don't need a running agent or any
    // process; the helpers are exposed via InternalsVisibleTo.
    public class DotNetTraceCliNormalizationTests
    {
        // ----- BuildDotnetTraceCliArgs: empty-providers defaults -----

        [Fact]
        public void BuildArgs_EmptyProviders_CollectMode_DefaultsToModernPair()
        {
            var argv = BuildArgvFor(verb: "collect", providers: null);

            // Modern recommended pair when nothing was specified.
            AssertProfileFlag(argv, "dotnet-sampled-thread-time,dotnet-common");
            AssertNotPresent(argv, "--providers");
            AssertNotPresent(argv, "--clrevents");
        }

        [Fact]
        public void BuildArgs_EmptyProviders_CollectLinuxMode_DefaultsToCpuSampling()
        {
            var argv = BuildArgvFor(verb: "collect-linux", providers: "");

            // cpu-sampling is the collect-linux-exclusive kernel-sampling profile.
            AssertProfileFlag(argv, "cpu-sampling");
            AssertNotPresent(argv, "--providers");
            AssertNotPresent(argv, "--clrevents");
        }

        // ----- BuildDotnetTraceCliArgs: cpu-sampling rewrite per mode -----

        [Fact]
        public void BuildArgs_CpuSampling_CollectMode_IsRewritten()
        {
            var (argv, notes) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("cpu-sampling"), "collect", FakeOutput());

            AssertProfileFlag(argv, "dotnet-sampled-thread-time");
            Assert.Contains(notes, n => n.Contains("cpu-sampling") && n.Contains("dotnet-sampled-thread-time"));
        }

        [Fact]
        public void BuildArgs_CpuSampling_CollectLinuxMode_PassesThrough()
        {
            var (argv, notes) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("cpu-sampling"), "collect-linux", FakeOutput());

            AssertProfileFlag(argv, "cpu-sampling");
            // Native to collect-linux; no rewrite note expected.
            Assert.DoesNotContain(notes, n => n.Contains("dotnet-sampled-thread-time"));
        }

        // ----- BuildDotnetTraceCliArgs: token classification -----

        [Fact]
        public void BuildArgs_KeywordExpression_GoesToClrEventsFlag()
        {
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("gc+jit"), "collect-linux", FakeOutput());

            AssertClrEventsFlag(argv, "gc+jit");
            AssertNotPresent(argv, "--profile");
            AssertNotPresent(argv, "--providers");
        }

        [Fact]
        public void BuildArgs_ProviderSpec_GoesToProvidersFlag()
        {
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("Microsoft-Windows-DotNETRuntime:0x14C14FCCBD:5"),
                "collect", FakeOutput());

            AssertProvidersFlag(argv, "Microsoft-Windows-DotNETRuntime:0x14C14FCCBD:5");
            AssertNotPresent(argv, "--profile");
            AssertNotPresent(argv, "--clrevents");
        }

        [Fact]
        public void BuildArgs_MixedTokens_AreClassifiedSeparately()
        {
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("gc-collect, Microsoft-DotNETCore-SampleProfiler:0:4, gc+jit"),
                "collect-linux", FakeOutput());

            AssertProfileFlag(argv, "gc-collect");
            AssertProvidersFlag(argv, "Microsoft-DotNETCore-SampleProfiler:0:4");
            AssertClrEventsFlag(argv, "gc+jit");
        }

        [Fact]
        public void BuildArgs_BareProviderName_GoesToProvidersFlag()
        {
            // A name-only provider token (no ':' and not a profile / CLR keyword)
            // must be routed to --providers. The dotnet-trace CLI accepts a bare
            // provider name there; passing it through --clrevents would be rejected.
            // Regression test for PR #3 review comment routing
            // `Microsoft-DotNETCore-SampleProfiler` to --clrevents.
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("Microsoft-DotNETCore-SampleProfiler"),
                "collect-linux", FakeOutput());

            AssertProvidersFlag(argv, "Microsoft-DotNETCore-SampleProfiler");
            AssertNotPresent(argv, "--profile");
            AssertNotPresent(argv, "--clrevents");
        }

        [Fact]
        public void BuildArgs_PlusJoinedWithUnknownPart_RoutesToProviders()
        {
            // When at least one '+'-part isn't a known CLR keyword, the whole
            // token can't be a CLR keyword expression -- the CLI's --clrevents
            // would reject it. Route to --providers as a bare provider name.
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("gc+Microsoft-DotNETCore-SampleProfiler"),
                "collect-linux", FakeOutput());

            AssertProvidersFlag(argv, "gc+Microsoft-DotNETCore-SampleProfiler");
            AssertNotPresent(argv, "--clrevents");
        }

        [Fact]
        public void BuildArgs_SpaceSeparatedTokens_AreTokenizedSeparately()
        {
            // Backcompat with legacy Collect() which splits DotNetTraceProviders
            // on both ',' and ' '. Regression test for PR #3 review comment.
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("gc-collect gc+jit"),
                "collect-linux", FakeOutput());

            AssertProfileFlag(argv, "gc-collect");
            AssertClrEventsFlag(argv, "gc+jit");
        }

        // ----- BuildDotnetTraceCliArgs: alias rewrites and grouping -----

        [Fact]
        public void BuildArgs_LegacyAliasesAreRewrittenAndRecorded()
        {
            var (argv, notes) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("fusion+gcheapcollect"), "collect-linux", FakeOutput());

            AssertClrEventsFlag(argv, "assemblyloader+managedheapcollect");
            Assert.Contains(notes, n => n == "fusion\u2192assemblyloader");
            Assert.Contains(notes, n => n == "gcheapcollect\u2192managedheapcollect");
        }

        [Fact]
        public void BuildArgs_TypoAliasIsCorrected()
        {
            var (argv, notes) = Startup.BuildDotnetTraceCliArgs(
                MakeJob("gcsampledobjectallcationhigh"), "collect-linux", FakeOutput());

            AssertClrEventsFlag(argv, "gcsampledobjectallocationhigh");
            Assert.Contains(notes, n => n == "gcsampledobjectallcationhigh\u2192gcsampledobjectallocationhigh");
        }

        // ----- BuildDotnetTraceCliArgs: --buffersize gating -----

        [Fact]
        public void BuildArgs_CollectLinuxNeverEmitsBufferSize()
        {
            // collect-linux's CLI parser rejects --buffersize as an unknown
            // token; the helper must strip it regardless of the Job knob.
            var job = MakeJob("gc-collect");
            job.DotNetTraceBufferSizeMB = 1024;

            var (argv, _) = Startup.BuildDotnetTraceCliArgs(job, "collect-linux", FakeOutput());

            AssertNotPresent(argv, "--buffersize");
        }

        [Fact]
        public void BuildArgs_CollectEmitsBufferSizeOnlyWhenNonDefault()
        {
            var defaultJob = MakeJob("gc-collect");
            defaultJob.DotNetTraceBufferSizeMB = 256;
            var (defaultArgv, _) = Startup.BuildDotnetTraceCliArgs(defaultJob, "collect", FakeOutput());
            AssertNotPresent(defaultArgv, "--buffersize");

            var bigJob = MakeJob("gc-collect");
            bigJob.DotNetTraceBufferSizeMB = 1024;
            var (bigArgv, _) = Startup.BuildDotnetTraceCliArgs(bigJob, "collect", FakeOutput());
            var idx = bigArgv.IndexOf("--buffersize");
            Assert.True(idx >= 0, "expected --buffersize in argv");
            Assert.Equal("1024", bigArgv[idx + 1]);
        }

        // ----- NormalizeClrEventExpression directly -----

        [Theory]
        [InlineData("fusion", "assemblyloader", "fusion\u2192assemblyloader")]
        [InlineData("gcheapcollect", "managedheapcollect", "gcheapcollect\u2192managedheapcollect")]
        [InlineData("gcsampledobjectallcationhigh", "gcsampledobjectallocationhigh", "gcsampledobjectallcationhigh\u2192gcsampledobjectallocationhigh")]
        [InlineData("gcsampledobjectallcationlow", "gcsampledobjectallocationlow", "gcsampledobjectallcationlow\u2192gcsampledobjectallocationlow")]
        public void Normalize_RewritesBareAlias(string input, string expectedOutput, string expectedNote)
        {
            var actual = Startup.NormalizeClrEventExpression(input, out var notes);

            Assert.Equal(expectedOutput, actual);
            Assert.NotNull(notes);
            Assert.Single(notes);
            Assert.Equal(expectedNote, notes[0]);
        }

        [Fact]
        public void Normalize_RewritesAliasesInsidePlusJoinedExpression()
        {
            var actual = Startup.NormalizeClrEventExpression("gc+fusion+jit", out var notes);

            Assert.Equal("gc+assemblyloader+jit", actual);
            Assert.NotNull(notes);
            Assert.Single(notes);
            Assert.Equal("fusion\u2192assemblyloader", notes[0]);
        }

        [Fact]
        public void Normalize_LeavesProviderSpecsAlone()
        {
            // Provider specs (containing ':') are not keyword expressions.
            var input = "Microsoft-Windows-DotNETRuntime:0x4:5";
            var actual = Startup.NormalizeClrEventExpression(input, out var notes);

            Assert.Equal(input, actual);
            Assert.Null(notes);
        }

        [Fact]
        public void Normalize_LeavesNonAliasedKeywordExpressionAlone()
        {
            // "gc+jit" is a valid CLR keyword expression but uses no aliases
            // we rewrite, so the normalizer should pass it through verbatim.
            var actual = Startup.NormalizeClrEventExpression("gc+jit", out var notes);

            Assert.Equal("gc+jit", actual);
            Assert.Null(notes);
        }

        [Fact]
        public void Normalize_HandlesEmptyAndNullInputs()
        {
            Assert.Null(Startup.NormalizeClrEventExpression(null, out var nullNotes));
            Assert.Null(nullNotes);

            Assert.Equal("", Startup.NormalizeClrEventExpression("", out var emptyNotes));
            Assert.Null(emptyNotes);
        }

        // ----- helpers -----

        private static Job MakeJob(string providers)
        {
            return new Job
            {
                ProcessId = 4242,
                DotNetTrace = true,
                DotNetTraceProviders = providers,
            };
        }

        private static FileInfo FakeOutput() =>
            new FileInfo(Path.Combine(Path.GetTempPath(), "crank-test-trace.nettrace"));

        private static List<string> BuildArgvFor(string verb, string providers)
        {
            var (argv, _) = Startup.BuildDotnetTraceCliArgs(MakeJob(providers), verb, FakeOutput());
            return argv;
        }

        private static void AssertProfileFlag(List<string> argv, string expectedValue)
            => AssertFlagValue(argv, "--profile", expectedValue);

        private static void AssertProvidersFlag(List<string> argv, string expectedValue)
            => AssertFlagValue(argv, "--providers", expectedValue);

        private static void AssertClrEventsFlag(List<string> argv, string expectedValue)
            => AssertFlagValue(argv, "--clrevents", expectedValue);

        private static void AssertFlagValue(List<string> argv, string flag, string expectedValue)
        {
            var idx = argv.IndexOf(flag);
            Assert.True(idx >= 0, $"expected flag '{flag}' in argv: {string.Join(' ', argv)}");
            Assert.True(idx + 1 < argv.Count, $"flag '{flag}' missing value in argv");
            Assert.Equal(expectedValue, argv[idx + 1]);
        }

        private static void AssertNotPresent(List<string> argv, string flag)
            => Assert.False(argv.Contains(flag), $"did not expect '{flag}' in argv: {string.Join(' ', argv)}");
    }
}
