// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.Models
{
    public class JobStatistics
    {
        public List<MeasurementMetadata> Metadata = new List<MeasurementMetadata>();
        public List<Measurement> Measurements = new List<Measurement>();
    }
}
