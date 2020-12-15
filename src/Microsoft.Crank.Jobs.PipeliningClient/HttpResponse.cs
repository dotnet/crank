// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Jobs.PipeliningClient
{
    public enum HttpResponseState
    {
        StartLine,
        Headers,
        Body,
        ChunkedBody,
        Completed,
        Error
    }

    public class HttpResponse
    {
        public HttpResponseState State { get; set; } = HttpResponseState.StartLine;
        public int StatusCode { get; set; }
        public long ContentLength { get; set; }
        public long ContentLengthRemaining { get; set; }
        public bool HasContentLengthHeader { get; set; }
        public int LastChunkRemaining { get; set; }

        public void Reset()
        {
            State = HttpResponseState.StartLine;
            StatusCode = default;
            ContentLength = default;
            ContentLengthRemaining = default;
            HasContentLengthHeader = default;
            LastChunkRemaining = default;
        }
    }
}
