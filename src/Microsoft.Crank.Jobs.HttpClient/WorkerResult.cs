// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    public class WorkerResult
    {
        public long AverageRps => (long)(TotalRequests / (Stopped - Started).TotalSeconds);
        public long TotalRequests => Status1xx + Status2xx + Status3xx + Status4xx + Status5xx;
        public double LatencyMeanMs { get; set; }
        public double LatencyMaxMs { get; set; }
        public long ThroughputBps { get; set; }
        public long DurationMs => (long)(Stopped - Started).TotalMilliseconds;
        public long BadResponses => Status1xx + Status4xx + Status5xx;
        public DateTime Started { get; set; }
        public DateTime Stopped { get; set; }
        public int Status1xx { get; set; }
        public int Status2xx { get; set; }
        public int Status3xx { get; set; }
        public int Status4xx { get; set; }
        public int Status5xx { get; set; }
        public int SocketErrors { get; set; }
        public int Connections { get; set; }
    }
}
