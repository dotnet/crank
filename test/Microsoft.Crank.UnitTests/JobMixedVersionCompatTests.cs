using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class JobMixedVersionCompatTests
    {
        // PR A adds DotNetTraceBufferSizeMB and DotNetTraceRequestRundown to
        // Job. PR B adds DotNetTraceCollectMode and DotNetTraceStopTimeoutSec.
        // To preserve back-compat in mixed-version deployments (new agent + old
        // controller, or vice versa), the defaults must reproduce today's exact
        // behavior so a JSON payload from a controller release that predates
        // these fields deserializes into a Job whose runtime values match what
        // the legacy code path used.
        //
        // Today's behavior:
        //   * Hardcoded circular buffer = 256 MB
        //     (Startup.cs StartDotNetTrace literal pre-PR-A)
        //   * Implicit requestRundown = true
        //     (default of the 2-arg StartEventPipeSession overload)
        //   * In-process EventPipe via DiagnosticsClient (the only path)
        //     -- represented post-PR-B as DotNetTraceCollectMode = "default"
        //   * Per-mode stop timeout default applies only in CLI modes;
        //     DotNetTraceStopTimeoutSec = 0 sentinel means "use the
        //     mode-specific default".

        [Fact]
        public void NewJobInstance_DotNetTraceDefaults_MatchLegacyBehavior()
        {
            var job = new Job();

            Assert.Equal(256, job.DotNetTraceBufferSizeMB);
            Assert.True(job.DotNetTraceRequestRundown);
            Assert.Equal("default", job.DotNetTraceCollectMode);
            Assert.Equal(0, job.DotNetTraceStopTimeoutSec);
        }

        [Fact]
        public void Deserialize_PrePrAControllerPayload_FillsLegacyDefaults()
        {
            // Hand-crafted payload representative of what a pre-PR-A
            // controller would send: the new fields are absent.
            const string legacyPayload = @"{
                ""DotNetTrace"": true,
                ""DotNetTraceProviders"": ""cpu-sampling""
            }";

            var job = Newtonsoft.Json.JsonConvert.DeserializeObject<Job>(legacyPayload);

            Assert.NotNull(job);
            Assert.True(job.DotNetTrace);
            Assert.Equal("cpu-sampling", job.DotNetTraceProviders);
            // Defaults filled in by the new agent must match today's behavior.
            Assert.Equal(256, job.DotNetTraceBufferSizeMB);
            Assert.True(job.DotNetTraceRequestRundown);
            Assert.Equal("default", job.DotNetTraceCollectMode);
            Assert.Equal(0, job.DotNetTraceStopTimeoutSec);
        }

        [Fact]
        public void Deserialize_PrePrBControllerPayload_FillsPrBDefaults()
        {
            // Hand-crafted payload representative of what a PR-A-era controller
            // (which knows the buffer / rundown knobs but not the CLI-mode
            // knobs) would send to a PR-B-era agent.
            const string prAPayload = @"{
                ""DotNetTrace"": true,
                ""DotNetTraceProviders"": ""gc-collect"",
                ""DotNetTraceBufferSizeMB"": 512,
                ""DotNetTraceRequestRundown"": false
            }";

            var job = Newtonsoft.Json.JsonConvert.DeserializeObject<Job>(prAPayload);

            Assert.NotNull(job);
            Assert.Equal(512, job.DotNetTraceBufferSizeMB);
            Assert.False(job.DotNetTraceRequestRundown);
            // PR B knobs default to today's behavior: in-process mode, per-mode timeout.
            Assert.Equal("default", job.DotNetTraceCollectMode);
            Assert.Equal(0, job.DotNetTraceStopTimeoutSec);
        }

        [Fact]
        public void Serialize_NewJob_RoundTripsNewKnobs()
        {
            // New controller + new agent: explicit values round-trip cleanly.
            var original = new Job
            {
                DotNetTrace = true,
                DotNetTraceProviders = "gc-collect",
                DotNetTraceBufferSizeMB = 512,
                DotNetTraceRequestRundown = false,
                DotNetTraceCollectMode = "collect-linux",
                DotNetTraceStopTimeoutSec = 240,
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
            var roundTripped = Newtonsoft.Json.JsonConvert.DeserializeObject<Job>(json);

            Assert.Equal(512, roundTripped.DotNetTraceBufferSizeMB);
            Assert.False(roundTripped.DotNetTraceRequestRundown);
            Assert.Equal("collect-linux", roundTripped.DotNetTraceCollectMode);
            Assert.Equal(240, roundTripped.DotNetTraceStopTimeoutSec);
        }
    }
}
