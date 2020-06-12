// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
