using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Controller
{
    class JobDefinition : Dictionary<string, JObject>
    {
        public JobDefinition() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
