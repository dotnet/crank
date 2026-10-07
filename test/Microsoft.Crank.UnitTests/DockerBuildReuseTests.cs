// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class DockerBuildReuseTests
    {
        [Theory]
        [InlineData(false, false, false, true)]
        [InlineData(false, true, true, true)]
        [InlineData(true, false, true, true)]
        [InlineData(true, true, false, true)]
        [InlineData(true, true, true, false)]
        public void RequiresDockerBuild_AccountsForCachedImage(
            bool reuseFolder,
            bool noBuild,
            bool imageExists,
            bool expected)
        {
            Assert.Equal(expected, Startup.RequiresDockerBuild(reuseFolder, noBuild, imageExists));
        }
    }
}
