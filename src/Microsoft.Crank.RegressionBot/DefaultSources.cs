// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot
{
    public class DefaultSources
    {
        public static SourceConfiguration Value = new SourceConfiguration 
        {
            Sources = new List<Source>
            {
                new Source
                {
                    Table = "TrendBenchmarks",
                    RegressionProbes = 
                    {   
                        new Probe { Path = "jobs.load.results['wrk/rps/mean']" },
                        new Probe { Path = "jobs.load.results['bombardier/rps/mean']" },
                    },
                    ErrorProbes = 
                    {
                        new Probe { Path = "jobs.load.results['bombardier/badresponses']" },
                        new Probe { Path = "jobs.load.results['wrk/errors/badresponses']" },
                        new Probe { Path = "jobs.load.results['wrk/errors/socketerrors']" },
                    },
                    Rules = new List<Rule>
                    {
                        new Rule { Include = "." },
                        new Rule { Include = "Mvc", Labels = { "area-mvc" } },
                    },
                    DaysToLoad = 7,
                    DaysToAnalyze = 7,
                    RegressionLabels = { "Perf", "perf-regression" },
                    ErrorLabels = { "Perf", "perf-bad-response" },
                    NotRunningLabels = { "Perf", "perf-not-running" },
                }
            }
        };
    }
}
