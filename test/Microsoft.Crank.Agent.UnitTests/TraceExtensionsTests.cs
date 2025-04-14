using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Trace;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.Diagnostics.Tools.Trace.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TraceExtensions"/> class.
    /// </summary>
    public class TraceExtensionsTests
    {
        /// <summary>
        /// Tests that ToCLREventPipeProviders returns an empty enumerable when passed a null or empty string.
        /// </summary>
        /// <param name="input">The input string representing CLR events.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ToCLREventPipeProviders_NullOrEmptyInput_ReturnsEmptyEnumerable(string input)
        {
            // Act
            var result = TraceExtensions.ToCLREventPipeProviders(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that ToCLREventPipeProviders returns an empty enumerable when no valid event keywords are provided.
        /// </summary>
        [Fact]
        public void ToCLREventPipeProviders_InvalidKeywords_ReturnsEmptyEnumerable()
        {
            // Arrange
            string input = "invalid1+invalid2";

            // Act
            var result = TraceExtensions.ToCLREventPipeProviders(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that ToCLREventPipeProviders aggregates valid CLR event keywords and returns an appropriate EventPipeProvider.
        /// </summary>
        [Fact]
        public void ToCLREventPipeProviders_ValidKeywords_ReturnsEventPipeProvider()
        {
            // Arrange
            string input = "gc+jit"; // valid keywords: "gc" (0x1) and "jit" (0x10), aggregate = 0x11
            long expectedKeywords = 0x1 | 0x10;
            string expectedProviderName = TraceExtensions.CLREventProviderName;
            EventLevel expectedEventLevel = EventLevel.Verbose;

            // Act
            var result = TraceExtensions.ToCLREventPipeProviders(input).ToArray();

            // Assert
            Assert.Single(result);
            var provider = result[0];
            Assert.Equal(expectedProviderName, provider.Name);
            Assert.Equal(expectedEventLevel, provider.EventLevel);
            Assert.Equal(expectedKeywords, provider.Keywords);
        }

        /// <summary>
        /// Tests that ToProvider returns an empty enumerable when passed a null, empty, or whitespace string.
        /// </summary>
        /// <param name="input">The provider string input.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToProvider_NullOrWhitespaceInput_ReturnsEmptyEnumerable(string input)
        {
            // Act
            var result = TraceExtensions.ToProvider(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that ToProvider returns an empty enumerable when the provider name is a GUID.
        /// </summary>
        [Fact]
        public void ToProvider_ProviderNameIsGuid_ReturnsEmptyEnumerable()
        {
            // Arrange
            string input = Guid.NewGuid().ToString();

            // Act
            var result = TraceExtensions.ToProvider(input);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that ToProvider parses a provider string with only the provider name and returns default values.
        /// </summary>
        [Fact]
        public void ToProvider_OnlyProviderName_ReturnsProviderWithDefaultValues()
        {
            // Arrange
            string providerName = "TestProvider";
            string input = providerName;  // Only provider name provided; other tokens missing.

            // Act
            var result = TraceExtensions.ToProvider(input).ToArray();

            // Assert
            Assert.Single(result);
            var provider = result[0];
            Assert.Equal(providerName, provider.Name);
            Assert.Equal(EventLevel.Verbose, provider.EventLevel);
            Assert.Equal(-1, provider.Keywords);
            Assert.Null(provider.Arguments);
        }

        /// <summary>
        /// Tests that ToProvider parses a fully specified provider string including keywords, event level, and filter data.
        /// </summary>
        [Fact]
        public void ToProvider_FullProviderString_ReturnsProviderWithParsedValues()
        {
            // Arrange
            string providerName = "TestProvider";
            string keywordsHex = "FF"; // Hex string representing 255.
            string eventLevelToken = "warning"; // Should map to EventLevel.Warning.
            string filterData = "key1=value1;key2=\"value2\"";
            string input = $"{providerName}:{keywordsHex}:{eventLevelToken}:{filterData}";

            // Act
            var result = TraceExtensions.ToProvider(input).ToArray();

            // Assert
            Assert.Single(result);
            var provider = result[0];
            Assert.Equal(providerName, provider.Name);
            Assert.Equal(255, provider.Keywords);
            Assert.Equal(EventLevel.Warning, provider.EventLevel);
            Assert.NotNull(provider.Arguments);
            Assert.Equal(2, provider.Arguments.Count);
            Assert.Equal("value1", provider.Arguments["key1"]);
            Assert.Equal("value2", provider.Arguments["key2"]);
        }

        /// <summary>
        /// Tests that ToProvider correctly interprets numeric event level tokens.
        /// </summary>
        /// <param name="eventLevelToken">The numeric token as string.</param>
        /// <param name="expectedLevel">The expected EventLevel enumeration value.</param>
        [Theory]
        [InlineData("2", EventLevel.Error)]
        [InlineData("5", EventLevel.Verbose)]
        [InlineData("10", EventLevel.Verbose)] // Numeric token above Verbose should return Verbose.
        public void ToProvider_NumericEventLevelToken_ReturnsCorrectEventLevel(string eventLevelToken, EventLevel expectedLevel)
        {
            // Arrange
            string providerName = "TestProvider";
            string keywordsHex = "0";
            string input = $"{providerName}:{keywordsHex}:{eventLevelToken}:";

            // Act
            var result = TraceExtensions.ToProvider(input).ToArray();

            // Assert
            Assert.Single(result);
            var provider = result[0];
            Assert.Equal(expectedLevel, provider.EventLevel);
        }

        /// <summary>
        /// Tests that ToProvider throws an ArgumentException when an unknown event level token is provided.
        /// </summary>
        [Fact]
        public void ToProvider_UnknownEventLevelToken_ThrowsArgumentException()
        {
            // Arrange
            string input = "TestProvider:0:unknown:";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => TraceExtensions.ToProvider(input).ToArray());
            Assert.Contains("Unknown EventLevel", exception.Message);
        }

        /// <summary>
        /// Tests that Merge returns an empty collection when both input collections are empty.
        /// </summary>
        [Fact]
        public void Merge_BothEmptyCollections_ReturnsEmptyCollection()
        {
            // Arrange
            var collection1 = Enumerable.Empty<EventPipeProvider>();
            var collection2 = Enumerable.Empty<EventPipeProvider>();

            // Act
            var result = TraceExtensions.Merge(collection1, collection2);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that Merge correctly combines two disjoint provider collections.
        /// </summary>
        [Fact]
        public void Merge_DisjointCollections_ReturnsCombinedCollection()
        {
            // Arrange
            var provider1 = new EventPipeProvider("Provider1", EventLevel.Informational, 0x01, null);
            var provider2 = new EventPipeProvider("Provider2", EventLevel.Warning, 0x02, null);
            var collection1 = new List<EventPipeProvider> { provider1 };
            var collection2 = new List<EventPipeProvider> { provider2 };

            // Act
            var result = TraceExtensions.Merge(collection1, collection2).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => string.Equals(p.Name, "Provider1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result, p => string.Equals(p.Name, "Provider2", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tests that Merge overrides duplicate providers from the first collection with those from the second collection.
        /// </summary>
        [Fact]
        public void Merge_DuplicateProviders_SecondOverridesFirst()
        {
            // Arrange
            var provider1 = new EventPipeProvider("Provider", EventLevel.Informational, 0x01, null);
            var provider2 = new EventPipeProvider("Provider", EventLevel.Critical, 0xFF, null);
            var collection1 = new List<EventPipeProvider> { provider1 };
            var collection2 = new List<EventPipeProvider> { provider2 };

            // Act
            var result = TraceExtensions.Merge(collection1, collection2).ToList();

            // Assert
            Assert.Single(result);
            var provider = result[0];
            Assert.Equal("Provider", provider.Name);
            Assert.Equal(EventLevel.Critical, provider.EventLevel);
            Assert.Equal(0xFF, provider.Keywords);
        }

        /// <summary>
        /// Tests that the DotNETRuntimeProfiles property contains the expected profiles and provider configurations.
        /// </summary>
        [Fact]
        public void DotNETRuntimeProfiles_ContainsExpectedProfiles()
        {
            // Act
            var profiles = TraceExtensions.DotNETRuntimeProfiles;

            // Assert
            Assert.NotNull(profiles);
            Assert.True(profiles.ContainsKey("cpu-sampling"), "Expected profile 'cpu-sampling' is missing.");
            Assert.True(profiles.ContainsKey("gc-verbose"), "Expected profile 'gc-verbose' is missing.");
            Assert.True(profiles.ContainsKey("gc-collect"), "Expected profile 'gc-collect' is missing.");

            // Verify the 'cpu-sampling' profile.
            var cpuSampling = profiles["cpu-sampling"];
            Assert.Equal(2, cpuSampling.Length);
            var sampleProfiler = cpuSampling.FirstOrDefault(x => x.Name == "Microsoft-DotNETCore-SampleProfiler");
            Assert.NotNull(sampleProfiler);
            Assert.Equal(EventLevel.Informational, sampleProfiler.EventLevel);
            var runtimeProvider = cpuSampling.FirstOrDefault(x => x.Name == "Microsoft-Windows-DotNETRuntime");
            Assert.NotNull(runtimeProvider);
            Assert.Equal(EventLevel.Informational, runtimeProvider.EventLevel);
            Assert.Equal((long)ClrTraceEventParser.Keywords.Default, runtimeProvider.Keywords);

            // Verify the 'gc-verbose' profile.
            var gcVerbose = profiles["gc-verbose"];
            Assert.Single(gcVerbose);
            var gcVerboseProvider = gcVerbose[0];
            Assert.Equal("Microsoft-Windows-DotNETRuntime", gcVerboseProvider.Name);
            Assert.Equal(EventLevel.Verbose, gcVerboseProvider.EventLevel);
            long expectedGcVerboseKeywords = (long)ClrTraceEventParser.Keywords.GC |
                                             (long)ClrTraceEventParser.Keywords.GCHandle |
                                             (long)ClrTraceEventParser.Keywords.Exception;
            Assert.Equal(expectedGcVerboseKeywords, gcVerboseProvider.Keywords);

            // Verify the 'gc-collect' profile.
            var gcCollect = profiles["gc-collect"];
            Assert.Single(gcCollect);
            var gcCollectProvider = gcCollect[0];
            Assert.Equal("Microsoft-Windows-DotNETRuntime", gcCollectProvider.Name);
            Assert.Equal(EventLevel.Informational, gcCollectProvider.EventLevel);
            long expectedGcCollectKeywords = (long)ClrTraceEventParser.Keywords.GC |
                                             (long)ClrTraceEventParser.Keywords.Exception;
            Assert.Equal(expectedGcCollectKeywords, gcCollectProvider.Keywords);
        }
    }
}
