using System;
using Microsoft.Crank.Agent;
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
        public void ParseKernelVersion_AcceptsCommonReleaseStrings(
            string raw, int major, int minor, int build)
        {
            var actual = Startup.ParseKernelVersion(raw);

            Assert.NotNull(actual);
            Assert.Equal(major, actual.Major);
            Assert.Equal(minor, actual.Minor);
            // System.Version.Build is -1 when only Major.Minor was supplied;
            // ParseKernelVersion treats missing build as 0 for ordering.
            Assert.True(actual.Build == build || (build == 0 && actual.Build <= 0));
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
    }
}
