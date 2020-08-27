// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot
{
    public class Source
    {
        // The name of the SQL table to load
        public string Table { get; set; }

        // The list of rules to apply
        public List<Rule> Rules { get; set; } = new List<Rule>();
        
        // The probes used to detect regressions
        public List<Probe> RegressionProbes { get; set; } = new List<Probe>();

        // The probes used to detect errors
        public List<Probe> ErrorProbes { get; set; } = new List<Probe>();

        // Number of days to load in order to build a trend
        public int DaysToLoad { get; set; } = 7;

        // Numbers of days to analyze
        public int DaysToAnalyze { get; set; } = 3;

        // Labels added to the issues created when a benchmark is not running
        public List<string> NotRunningLabels = new List<string>(); 

        // Labels added to the issues created when a benchmark has regressions
        public List<string> RegressionLabels = new List<string>(); 

        // Labels added to the issues created when a benchmark has errors
        public List<string> ErrorLabels = new List<string>(); 

        /// <summary>
        /// Returns the list of <see cref="Rule" /> that match a descriptor
        /// </summary>
        public IEnumerable<Rule> Match(string descriptor)
        {
            foreach(var rule in Rules)
            {
                if (!string.IsNullOrEmpty(rule.Include))
                {
                    rule.IncludeRegex ??= new Regex(rule.Include);

                    if (!rule.IncludeRegex.IsMatch(descriptor))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(rule.Exclude))
                {
                    rule.ExcludeRegex ??= new Regex(rule.Exclude);

                    if (rule.ExcludeRegex.IsMatch(descriptor))
                    {
                        continue;
                    }
                }

                yield return rule;                
            }
        }
    }
}
