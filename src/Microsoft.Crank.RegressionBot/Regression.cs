// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.RegressionBot
{
    public class Regression
    {
        public BenchmarksResult PreviousResult { get; set; }
        public BenchmarksResult CurrentResult { get; set; }
        public double Deviation { get; set; }
        public double StandardDeviation { get; set; }
        public double Average { get; set; }

        /// <summary>
        /// Gets a string representing this regression.
        /// Used to determine if two regressions are similar.
        /// </summary>
        public string Identifier => $"Id:{CurrentResult.Scenario}{CurrentResult.Description}{CurrentResult.DateTimeUtc}";
    }
}
