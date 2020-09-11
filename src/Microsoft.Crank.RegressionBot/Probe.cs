// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Crank.RegressionBot
{
    public class Probe
    {
        // The JSON path to the value
        public string Path { get; set; }

        // The minimum value triggering an issue 
        public double Threshold { get; set; } = 1;

        public ThresholdUnits Unit { get; set; } = ThresholdUnits.StDev;
    }

    public enum ThresholdUnits
    {
        None,
        StDev,
        Percent,
        Absolute
    }
}
