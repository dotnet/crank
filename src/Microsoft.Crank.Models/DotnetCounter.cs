// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Models
{
    public class DotnetCounter
    {
        /// <summary>
        /// Provider name, e.g., System.Runtime
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Name of the counter, cpu-usage
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the measurement, runtime/cpu-usage
        /// </summary>
        public string Measurement { get; set; }
    }
}
