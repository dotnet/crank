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
                    RegressionTemplate = @"
A performance regression has been detected for the following scenarios:

| Scenario | Environment | Date | Old RPS | New RPS | Change | Deviation |
| -------- | ----------- | ---- | ------- | ------- | ------ | --------- |

{% for regression in Regressions -%}
    {% assign r = regression.CurrentResult %}
    {% assign p = regression.PreviousResult %}
    {% assign rps = r.Data.jobs.load.results['wrk/rps/mean'] %}
    {% assign prevRps = p.Data.jobs.load.results['wrk/rps/mean'] %}
    {% assign change = rps | minus: prevRps | divided_by: prevRps | times: 100 | round: 2 %}
    {% assign deviation = rps | minus: prevRps | divided_by: regression.Deviation | round: 2 %}
| {{r.Scenario}} | {{r.Description}} | {{r.DateTimeUtc}} | {{prevRps}} | {{rps}} | {{change}} % | {{deviation}} σ |
{% endfor %}
                    ",
                }
            }
        };
    }
}
