using System;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    // Cross-platform tests for the PR B collect-linux preflight helpers.
    // ValidateCollectLinuxPrerequisites itself is OS-specific (and even on
    // Linux requires reading /proc), so the only thing we can unit-test
    // portably is the kernel-version parser, plus the contract that the
    // error-message template stays greppable.
    public class DotNetTraceCollectLinuxPreflightTests
    {
        [Theory]
        [InlineData("6.8.0-1018-azure", 6, 8, 0)]
        [InlineData("6.4", 6, 4, 0)]
        [InlineData("5.15.0-78-generic", 5, 15, 0)]
        [InlineData("6.4.0", 6, 4, 0)]
        [InlineData("6.10.5", 6, 10, 5)]
        [InlineData("6.8.0+", 6, 8, 0)]
        [InlineData("6.8.0~rc1", 6, 8, 0)]
        [InlineData("6.8.0 generic", 6, 8, 0)]
        [InlineData("6.8", 6, 8, 0)]
        public void ParseKernelVersion_AcceptsCommonReleaseStrings(
            string raw, int major, int minor, int build)
        {
            var actual = Startup.ParseKernelVersion(raw);

            Assert.NotNull(actual);
            Assert.Equal(major, actual.Major);
            Assert.Equal(minor, actual.Minor);
            // ParseKernelVersion always constructs new Version(major, minor, patch)
            // with missing patch normalized to 0, so Build is always >= 0 here.
            Assert.Equal(build, actual.Build);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("garbage")]
        [InlineData("not.a.version")]
        public void ParseKernelVersion_ReturnsNullForJunk(string raw)
        {
            Assert.Null(Startup.ParseKernelVersion(raw));
        }

        [Fact]
        public void ParseKernelVersion_OrderingIsCorrectAcross6Dot4Boundary()
        {
            var below = Startup.ParseKernelVersion("6.3.99");
            var threshold = Startup.ParseKernelVersion("6.4");
            var above = Startup.ParseKernelVersion("6.8.0-1018-azure");

            Assert.NotNull(below);
            Assert.NotNull(threshold);
            Assert.NotNull(above);
            Assert.True(below < threshold);
            Assert.True(above >= threshold);
        }

        [Fact]
        public void ValidateCollectLinuxPrerequisites_OnNonLinux_ReportsExpectedErrorShape()
        {
            // On any non-Linux OS the helper must fail the static check with
            // the documented, greppable error template. (On Linux we can't
            // assert success or failure here because it depends on whether
            // the test host actually runs as root with a recent kernel; the
            // string contract is what unit tests own.)
            if (System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return;
            }

            var (ok, err) = Startup.ValidateCollectLinuxPrerequisites();

            Assert.False(ok);
            Assert.NotNull(err);
            Assert.Contains("DotNetTraceCollectMode=collect-linux requires Linux", err);
            Assert.Contains("effective UID 0", err);
            Assert.Contains("kernel >= 6.4", err);
            Assert.Contains("Set DotNetTraceCollectMode=collect", err);
        }

        [Fact]
        public void ValidateDotNetTraceOptions_WhenTracingNotRequested_IsOk()
        {
            // No dotnet-trace collection requested -> nothing to validate, even
            // if a bogus mode is set, because the mode is never consulted.
            var job = new Job { DotNetTrace = false, DotNetTraceCollectMode = "nonsense" };

            var (ok, err) = Startup.ValidateDotNetTraceOptions(job);

            Assert.True(ok);
            Assert.Null(err);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("default")]
        [InlineData("Default")]
        [InlineData(" collect ")]
        [InlineData("COLLECT")]
        public void ValidateDotNetTraceOptions_AcceptsKnownNonLinuxModes(string mode)
        {
            // default/collect are host-agnostic, so they validate on any OS.
            var job = new Job { DotNetTrace = true, DotNetTraceCollectMode = mode };

            var (ok, err) = Startup.ValidateDotNetTraceOptions(job);

            Assert.True(ok);
            Assert.Null(err);
        }

        [Theory]
        [InlineData("nonsense")]
        [InlineData("collectlinux")]
        [InlineData("perf")]
        public void ValidateDotNetTraceOptions_RejectsUnknownMode(string mode)
        {
            var job = new Job { DotNetTrace = true, DotNetTraceCollectMode = mode };

            var (ok, err) = Startup.ValidateDotNetTraceOptions(job);

            Assert.False(ok);
            Assert.NotNull(err);
            Assert.Contains("Unknown DotNetTraceCollectMode", err);
            Assert.Contains("default, collect, collect-linux", err);
        }

        [Fact]
        public void ValidateDotNetTraceOptions_CollectLinux_DelegatesToPrereqCheck()
        {
            // collect-linux routes through the host prerequisite check. We can
            // only assert the failure contract portably (on non-Linux); on Linux
            // the result depends on the host's root/kernel state.
            if (System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return;
            }

            var job = new Job { DotNetTrace = true, DotNetTraceCollectMode = "collect-linux" };

            var (ok, err) = Startup.ValidateDotNetTraceOptions(job);

            Assert.False(ok);
            Assert.NotNull(err);
            Assert.Contains("DotNetTraceCollectMode=collect-linux requires Linux", err);
        }

        [Fact]
        public void ValidateDotNetTraceOptions_HonorsProfileTypeGate()
        {
            // Tracing can also be requested via Profile + ProfileType=dotnet-trace
            // (not just DotNetTrace=true); an unknown mode must still be rejected.
            var job = new Job
            {
                DotNetTrace = false,
                Profile = true,
                ProfileType = Job.DotnetTraceProfileType,
                DotNetTraceCollectMode = "nonsense",
            };

            var (ok, err) = Startup.ValidateDotNetTraceOptions(job);

            Assert.False(ok);
            Assert.Contains("Unknown DotNetTraceCollectMode", err);
        }
    }
}
