// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.EventSources
{
    internal enum Operations
    {
        First,
        Last,
        Avg,
        Sum,
        Median,
        Max,
        Min,
        Count,
        All,
        Delta // Difference between min and max of the set
    }
}
