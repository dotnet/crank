// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Controller
{
    public class JobResults
    {
        public Dictionary<string, JobResult> Jobs { get; set; } = new Dictionary<string, JobResult>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class JobResult
    {
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public MeasurementMetadata[] Metadata { get; set; } = Array.Empty<MeasurementMetadata>();
        public List<Measurement[]> Measurements { get; set; } = new List<Measurement[]>();
        public Dictionary<string, object> Environment { get; set; } = new Dictionary<string, object>();
    }
}
