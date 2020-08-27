// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot
{
    public class Rule
    {
        internal Regex IncludeRegex { get; set; }
        internal Regex ExcludeRegex { get; set; }

        public string Include { get; set; }

        public string Exclude { get; set; }

        /// <summary>
        /// Gets or sets the list of labels to assign to the issues
        /// </summary>
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of users that are cced in the issue
        /// </summary>
        public List<string> Owners = new List<string>();

        /// <summary>
        /// Gets or sets whether the regressions on the scenario should be ignored
        /// </summary>
        public bool? IgnoreRegressions { get; set; }
        public bool? IgnoreErrors { get; set; }
        public bool? IgnoreFailures { get; set; }
    }
}
