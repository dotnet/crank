// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Crank.Models
{
    public class JobResults
    {
        public Dictionary<string, JobResult> Jobs { get; set; } = new Dictionary<string, JobResult>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class JobResult
    {
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public ResultMetadata[] Metadata { get; set; } = Array.Empty<ResultMetadata>();
        public Dependency[] Dependencies { get; set; } = Array.Empty<Dependency>();
        public List<Measurement[]> Measurements { get; set; } = new List<Measurement[]>();
        public Dictionary<string, object> Environment { get; set; } = new Dictionary<string, object>();
    }
    public class ResultMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
    }
}
