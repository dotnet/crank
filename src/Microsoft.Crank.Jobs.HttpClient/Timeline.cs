using System;
using System.Collections.Generic;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    internal class Timeline
    {
        public Uri Uri { get; set; }
        public TimeSpan Delay { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Headers {  get; set; } = new Dictionary<string, string>();
    }
}
