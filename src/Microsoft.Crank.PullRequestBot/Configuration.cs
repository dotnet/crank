// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.PullRequestBot
{
    public class Configuration
    {
        public string Defaults { get; set; }
        public Dictionary<string, Build> Components { get; set; } = new Dictionary<string, Build>();
        public Dictionary<string, Profile> Profiles { get; set; } = new Dictionary<string, Profile>();
        public Dictionary<string, Benchmark> Benchmarks { get; set; } = new Dictionary<string, Benchmark>();
    }

    public class Profile
    {
        public string Description { get; set; }
        public string Arguments { get; set; }
    }

    public class Benchmark
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Arguments { get; set; }
    }

    public class Build
    {
        public string Script { get; set; }
        public string Arguments { get; set; }
    }
}
