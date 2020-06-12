// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Octokit;

namespace Microsoft.Crank.JobProducer
{
    public class PRBenchmarkRequest
    {
        public PullRequest PullRequest { get; set; }
        public string ScenarioName { get; set; }
    }
}
