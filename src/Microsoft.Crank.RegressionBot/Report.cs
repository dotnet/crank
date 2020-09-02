// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.RegressionBot
{
    /// <summary>
    /// This class is used as a model for template reports.
    /// </summary>
    public class Report
    {
        public List<Regression> Regressions { get; set; } = new List<Regression>();
    }
}
