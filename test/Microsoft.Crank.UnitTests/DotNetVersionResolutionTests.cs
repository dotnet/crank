// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class DotNetVersionResolutionTests
    {
        [Theory]
        [InlineData("net12.0", null, "latest")]
        [InlineData("net12.0", "", "latest")]
        [InlineData("net11.0", null, "latest")]
        [InlineData("net11.0", "", "latest")]
        [InlineData("net10.0", null, "current")]
        [InlineData("net8.0", "", "current")]
        [InlineData("net13.0", "", "current")]
        [InlineData("net12.0", "Current", "Current")]
        [InlineData("net12.0", "EDGE", "EDGE")]
        [InlineData("net12.0", "latest", "latest")]
        [InlineData("net11.0", "current", "current")]
        [InlineData("net10.0", "latest", "latest")]
        public void ResolveChannel_PreservesExplicitChannelAndVersionScopedDefaults(
            string targetFramework, string requestedChannel, string expected)
        {
            Assert.Equal(expected, Startup.ResolveChannel(targetFramework, requestedChannel));
        }
    }
}
