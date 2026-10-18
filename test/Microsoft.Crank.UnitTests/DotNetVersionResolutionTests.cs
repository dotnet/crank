// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class DotNetVersionResolutionTests
    {
        [Theory]
        [InlineData("ResolveSdkVersion", "Latest", "11.0.100-rtm.26480.113")]
        [InlineData("ResolveRuntimeVersion", "Latest", "11.0.0-rtm.26480.113")]
        [InlineData("ResolveAspNetCoreVersion", "Latest", "11.0.0-rtm.26480.113")]
        [InlineData("ResolveSdkVersion", "lAtEsT", "11.0.100-rtm.26480.113")]
        [InlineData("ResolveRuntimeVersion", "lAtEsT", "11.0.0-rtm.26480.113")]
        [InlineData("ResolveAspNetCoreVersion", "lAtEsT", "11.0.0-rtm.26480.113")]
        [InlineData("ResolveSdkVersion", "11.0.100-custom", "11.0.100-custom")]
        [InlineData("ResolveRuntimeVersion", "11.0.0-custom", "11.0.0-custom")]
        [InlineData("ResolveAspNetCoreVersion", "11.0.0-custom", "11.0.0-custom")]
        [InlineData("ResolveSdkVersion", "Current", "11.0.100")]
        [InlineData("ResolveRuntimeVersion", "Current", "11.0.0")]
        [InlineData("ResolveAspNetCoreVersion", "Current", "11.0.0")]
        public async Task Net11LatestUsesTemporaryPinsWithoutChangingOverrides(string method, string requested, string expected)
        {
            Assert.Equal(expected, await ResolveVersion(method, requested, "net11.0"));
        }

        [Theory]
        [InlineData("ResolveSdkVersion", "11.0.100-rtm.26480.113")]
        [InlineData("ResolveRuntimeVersion", "11.0.0-rtm.26480.113")]
        [InlineData("ResolveAspNetCoreVersion", "11.0.0-rtm.26480.113")]
        public async Task CurrentStillFallsBackToLatestWhenReleaseMetadataIsUnavailable(string method, string expected)
        {
            Assert.Equal(expected, await ResolveVersion(method, "Current", "net11.0", hasCurrentVersion: false));
        }

        [Theory]
        [InlineData("ResolveSdkVersion", "Latest", "net10.0", "10.0.100-daily")]
        [InlineData("ResolveRuntimeVersion", "Latest", "net10.0", "10.0.0-daily")]
        [InlineData("ResolveAspNetCoreVersion", "Latest", "net10.0", "10.0.0-daily")]
        [InlineData("ResolveSdkVersion", "Latest", "net12.0", "12.0.100-daily")]
        [InlineData("ResolveRuntimeVersion", "Latest", "net12.0", "12.0.0-daily")]
        [InlineData("ResolveAspNetCoreVersion", "Latest", "net12.0", "12.0.0-daily")]
        [InlineData("ResolveSdkVersion", "Edge", "net11.0", "11.0.100-daily")]
        public async Task UnpinnedVersionsStillUseProductCommit(string method, string requested, string targetFramework, string expected)
        {
            Assert.Equal(expected, await ResolveVersion(method, requested, targetFramework));
        }

        private static async Task<string> ResolveVersion(string method, string requested, string targetFramework, bool hasCurrentVersion = true)
        {
            var loggerProperty = typeof(Startup).GetProperty("Logger", BindingFlags.Static | BindingFlags.NonPublic);
            var originalLogger = loggerProperty.GetValue(null);
            using var logger = new LoggerConfiguration().CreateLogger();
            loggerProperty.SetValue(null, logger);
            var cache = Assert.IsType<MemoryCache>(typeof(Startup)
                .GetField("_fileContentCache", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null));
            var version = targetFramework["net".Length..];
            var platform = typeof(Startup).GetMethod("GetPlatformMoniker", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
            var metadataKey = ($"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{version}/releases.json", (TimeSpan?)TimeSpan.FromDays(1));
            var productCommitKey = ($"https://aka.ms/dotnet/{version}.1xx/daily/productCommit-{platform}.json", (TimeSpan?)TimeSpan.FromDays(1));
            var options = new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) };
            cache.Set(metadataKey, hasCurrentVersion ? $$"""
                {
                  "releases": [{
                    "runtime": { "version": "{{version}}.0" },
                    "windowsdesktop": { "version": "{{version}}.0" },
                    "aspnetcore-runtime": { "version": "{{version}}.0" },
                    "sdk": { "version": "{{version}}.100" }
                  }]
                }
                """ : "{}", options);
            cache.Set(productCommitKey, $$"""
                {
                  "runtime": { "version": "{{version}}.0-daily" },
                  "aspnetcore": { "version": "{{version}}.0-daily" },
                  "sdk": { "version": "{{version}}.100-daily" }
                }
                """, options);

            try
            {
                var resolver = typeof(Startup).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
                object[] arguments = method == "ResolveRuntimeVersion"
                    ? [null, targetFramework, requested]
                    : [requested, targetFramework];

                return await Assert.IsAssignableFrom<Task<string>>(resolver.Invoke(null, arguments));
            }
            finally
            {
                cache.Remove(metadataKey);
                cache.Remove(productCommitKey);
                loggerProperty.SetValue(null, originalLogger);
            }
        }
    }
}
