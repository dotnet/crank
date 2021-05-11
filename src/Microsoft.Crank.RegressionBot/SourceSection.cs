// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Crank.RegressionBot
{
    public class SourceSection
    {
        public List<Probe> Probes { get; set; } = new List<Probe>();

        // Labels added to the issues created
        public List<string> Labels = new List<string>(); 

        // Labels added to the issues created
        public List<string> Owners = new List<string>(); 

        // The name of the template to use to render regressions for this source
        public string Template { get; set; } = "";

        // The templated title of the issue. Leave empty for an auto-generated one.
        public string Title { get; set; } = "";
    }
}
