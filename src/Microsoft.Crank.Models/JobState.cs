// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Models
{
    public enum JobState
    {
        New, // The job was submitted
        Initializing, // The job is processed, the driver update it or submit attachments
        Waiting, // The job is ready to start, following a POST from the client to /start
        Starting, // The application has been started, the server is waiting for it to be responsive
        Running, // The application is running
        Failed,
        Stopping,
        Stopped,
        TraceCollecting, // The driver has requested the trace to be collected
        TraceCollected,
        Deleting,
        Deleted,
        NotSupported, // The job is not supported by the server
    }
}
