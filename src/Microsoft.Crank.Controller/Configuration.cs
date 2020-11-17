// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Controller
{
    public class Configuration
    {
        public object Variables { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, Job> Jobs { get; set; } = new Dictionary<string, Job>();

        public Dictionary<string, Dictionary<string, Scenario>> Scenarios { get; set; } = new Dictionary<string, Dictionary<string, Scenario>>();

        public Dictionary<string, object> Profiles { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> Scripts { get; set; } = new Dictionary<string, string>();
    }

    public class Scenario
    {
        public string Job { get; set; }
    }
}
