// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Crank.Controller
{
    public class ResultComparer
    {
        public static int Compare(IEnumerable<string> filenames, JobResults jobResults = null, string jobName = null)
        {
            foreach (var filename in filenames)
            {
                if (!File.Exists(filename))
                {
                    Log.Write($"Results file not found: '{new FileInfo(filename).FullName}'", notime: true);
                    return -1;
                }
            }

            var compareResults = filenames
                .Select(filename => JsonConvert.DeserializeObject<ExecutionResult>(File.ReadAllText(filename)))
                .Select(executionResult => executionResult.JobResults)
                .ToList();

            var resultNames = filenames.Select(filename => Path.GetFileNameWithoutExtension(filename)).ToList();

            if (jobResults != null && jobName != null)
            {
                compareResults.Add(jobResults);
                resultNames.Add(jobName);

            }

            DisplayDiff(compareResults, resultNames);

            return 0;
        }

        private static void DisplayDiff(IEnumerable<JobResults> allResults, IEnumerable<string> allNames)
        {
            // Use the first job results as the reference for metadata:
            var firstJob = allResults.First();

            foreach (var jobEntry in firstJob.Jobs)
            {
                var jobName = jobEntry.Key;
                var jobResult = jobEntry.Value;

                Console.WriteLine();

                var table = new ResultTable(allNames.Count() * 2 + 1 - 1); // two columns per job, minus the first job, plus the description

                table.Headers.Add(jobName);

                foreach (var name in allNames)
                {
                    table.Headers.Add(name);

                    if (name != allNames.First())
                    {
                        table.Headers.Add(""); // percentage
                    }
                }

                foreach (var metadata in jobResult.Metadata)
                {
                    if (!jobResult.Results.ContainsKey(metadata.Name))
                    {
                        continue;
                    }

                    // We don't render the result if it's a raw object

                    if (metadata.Format == "object")
                    {
                        continue;
                    }

                    var row = table.AddRow();

                    var cell = new Cell();

                    cell.Elements.Add(new CellElement() { Text = metadata.ShortDescription, Alignment = CellTextAlignment.Left });

                    row.Add(cell);

                    foreach (var result in allResults)
                    {
                        // Skip jobs that have no data for this measure
                        if (!result.Jobs.ContainsKey(jobName))
                        {
                            row.Add(new Cell());
                            row.Add(new Cell());

                            continue;
                        }

                        var job = result.Jobs[jobName];

                        if (!String.IsNullOrEmpty(metadata.Format))
                        {
                            var measure = Convert.ToDouble(job.Results.ContainsKey(metadata.Name) ? job.Results[metadata.Name] : 0);
                            var previous = Convert.ToDouble(jobResult.Results.ContainsKey(metadata.Name) ? jobResult.Results[metadata.Name] : 0);

                            var improvement = measure == 0
                            ? 0
                            : (measure - previous) / previous * 100;

                            row.Add(cell = new Cell());

                            cell.Elements.Add(new CellElement { Text = Convert.ToDouble(measure).ToString(metadata.Format), Alignment = CellTextAlignment.Right });

                            // Don't render % on baseline job
                            if (firstJob != result)
                            {
                                row.Add(cell = new Cell());

                                if (measure != 0)
                                {
                                    var sign = improvement > 0 ? "+" : "";
                                    cell.Elements.Add(new CellElement { Text = $"{sign}{improvement:n2}%", Alignment = CellTextAlignment.Right });
                                }
                            }
                        }
                        else
                        {
                            var measure = job.Results.ContainsKey(metadata.Name) ? job.Results[metadata.Name] : 0;

                            row.Add(cell = new Cell());
                            cell.Elements.Add(new CellElement { Text = measure.ToString(), Alignment = CellTextAlignment.Right });

                            // Don't render % on baseline job
                            if (firstJob != result)
                            {
                                row.Add(new Cell());
                            }

                        }
                    }
                }

                table.Render(Console.Out);

                Console.WriteLine();
            }
        }
    }
}
