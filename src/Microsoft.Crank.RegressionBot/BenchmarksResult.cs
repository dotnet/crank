// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.RegressionBot
{
    public class BenchmarksResult
    {
        public int Id { get; set; }
        public bool Excluded { get; set; }
        public DateTimeOffset DateTimeUtc { get; set; }
        public string Session { get; set; }
        public string Scenario { get; set; }
        public string Description { get; set; }
        public string Document { get; set; }
        
        private JObject _data;
        public JObject Data => _data ??= JObject.Parse(Document);
    }
}
