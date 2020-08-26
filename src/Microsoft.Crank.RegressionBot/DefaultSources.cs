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
                    MetricPath = "jobs.load.results['wrk/rps/mean']",
                    DeviationFactor = 2.0,
                    Rules = new List<Rule>
                    {
                        new Rule { Include = "." },
                        new Rule { Include = "Mvc", Labels = { "area-mvc" } },
                    }
                }
            }
        };
    }
}
