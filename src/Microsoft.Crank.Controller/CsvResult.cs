// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CsvHelper.Configuration;

namespace Microsoft.Crank.Controller
{
    public class CsvResult
    {
        public string Class { get; set; }
        public string Method { get; set; }
        public string Params { get; set; }
        public double Mean { get; set; }
        public double Error { get; set; }
        public double StdDev { get; set; }
        public double OperationsPerSecond { get; set; }
        public double Allocated { get; set; }
    }

    public sealed class CsvResultMap : ClassMap<CsvResult>
    {
        public CsvResultMap()
        {
            Map(m => m.Method).Name("Method");
            Map(m => m.Params).Name("Params").Optional();
            Map(m => m.Mean).Name("Mean [us]").Default(0);
            Map(m => m.Error).Name("Error [us]").Default(0);
            Map(m => m.StdDev).Name("StdDev [us]").Default(0);
            Map(m => m.OperationsPerSecond).Name("Op/s").Default(0);
            Map(m => m.Allocated).Name("Allocated [KB]").Default(0);
        }
    }
}
