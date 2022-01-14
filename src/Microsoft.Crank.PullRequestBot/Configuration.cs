// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.PullRequestBot
{
    public class Configuration
    {
        public string Organization { get; set; }
        public string Repository { get; set; }
        public string Build { get; set; }
        public string Defaults { get; set; }
        public List<NameValue> Environments { get; set; } = new List<NameValue>();
        public List<NameValue> Benchmarks { get; set; } = new List<NameValue>();
    }

    public class NameValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
    }
}
