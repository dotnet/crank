// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.TypeConversion;

namespace Microsoft.Crank.Controller
{
    public static class BenchmarkDotNetHelper
    {
        private static IEnumerable<CsvResult> ParseBenchmarkDotNetResults(string fileName, string csvFileContent)
        {
            var results = new List<JobResults>();

            using (var sr = new StringReader(csvFileContent))
            {
                using (var csv = new CsvReader(sr))
                {
                    var isRecordBad = false;

                    csv.Configuration.BadDataFound = context =>
                    {
                        isRecordBad = true;
                    };

                    csv.Configuration.RegisterClassMap<CsvResultMap>();
                    csv.Configuration.TypeConverterOptionsCache.AddOptions(typeof(double), new TypeConverterOptions { NumberStyle = NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint });

                    var localResults = new List<CsvResult>();

                    while (csv.Read())
                    {
                        if (!isRecordBad)
                        {
                            localResults.Add(csv.GetRecord<CsvResult>());
                        }

                        isRecordBad = false;
                    }

                    var className = Path.GetFileNameWithoutExtension(fileName).Split('.').LastOrDefault().Split('-', 2).FirstOrDefault();

                    foreach (var result in localResults)
                    {
                        result.Class = className;

                        yield return result;
                    }
                }
            }
        }
    }
}
