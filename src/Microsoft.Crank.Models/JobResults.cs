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

        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public Benchmark[] Benchmarks { get; set; } = Array.Empty<Benchmark>();
    }

    public class ResultMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
    }

    public class Benchmark
    {
        public string FullName { get; set; }
        public BenchmarkStatistics Statistics { get; set; }
        public BenchmarkMemory Memory { get; set; }
    }

    public class BenchmarkStatistics
    {
        public double Min { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double Max { get; set; }
        public double StandardError { get; set; }
        public double StandardDeviation { get; set; }
    }

    public class BenchmarkMemory
    {
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long BytesAllocatedPerOperation { get; set; }
        public long TotalOperations { get; set; }
    }
}
