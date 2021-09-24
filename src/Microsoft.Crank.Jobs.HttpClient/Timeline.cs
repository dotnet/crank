// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    internal class Timeline
    {
        public Uri Uri { get; set; }
        public TimeSpan Delay { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public HttpContent HttpContent { get; set; }
        public string MimeType { get; set; } 
    }
}
