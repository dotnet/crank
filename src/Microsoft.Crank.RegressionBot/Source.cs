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
        
        public SourceSection Regressions { get; set; } = new SourceSection();
        public SourceSection Errors { get; set; } = new SourceSection();
        public SourceSection NotRunning { get; set; } = new SourceSection();

        public int DaysToLoad { get; set; } = 7;

        // Numbers of days to analyze
        public int DaysToAnalyze { get; set; } = 3;
        
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
