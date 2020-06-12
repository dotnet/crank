// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Crank.Models
{
    public class Measurement
    {
        public const string Delimiter = "$$Delimiter$$";

        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        [JsonIgnore]
        public bool IsDelimiter => String.Equals(Name, Delimiter, StringComparison.OrdinalIgnoreCase);
    }
}
