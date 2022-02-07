// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crank.Models;
using Newtonsoft.Json;

namespace Microsoft.Crank.Controller
{
    public class ResultComparer
    {
        public static int Compare(
            IEnumerable<string> filenames,
            JobResults jobResults = null,
            Benchmark[] benchmarks = null,
            string jobName = null)
        {
            foreach (var filename in filenames)
            {
                if (!File.Exists(filename))
                {
                    Log.Write($"Results file not found: '{new FileInfo(filename).FullName}'", notime: true);
                    return -1;
                }
            }

            var executionResults = filenames
                .Select(filename => JsonConvert.DeserializeObject<ExecutionResult>(File.ReadAllText(filename)))
                .Select(executionResult => executionResult)
                .ToList();

            var compareJobResults = executionResults
                .Select(executionResult => executionResult.JobResults)
                .ToList();

            var compareBenchmarks = executionResults
                .Select(executionResult => executionResult.Benchmarks)
                .ToList();

            var resultNames = filenames.Select(filename => Path.GetFileNameWithoutExtension(filename)).ToList();

            if ((jobResults != null || benchmarks != null) && jobName != null)
            {
                if (jobResults != null)
                {
                    compareJobResults.Add(jobResults);
                }

                if (benchmarks != null)
                {
                    compareBenchmarks.Add(benchmarks);
                }

                resultNames.Add(jobName);
            }

            DisplayDiff(compareJobResults, resultNames);

            DisplayDiff(compareBenchmarks, resultNames);

            return 0;
        }

        private static void DisplayDiff(IEnumerable<JobResults> allResults, IEnumerable<string> allNames)
        {
            // Use the first job results as the reference for metadata:
            var firstJob = allResults.FirstOrDefault();

            if (firstJob == null || firstJob.Jobs.Count < 1)
            {
                return;
            }

            foreach (var jobEntry in firstJob.Jobs)
            {
                var jobName = jobEntry.Key;
                var jobResult = jobEntry.Value;

                Console.WriteLine();

                // description + baseline job (firstJob) + other jobs * 2 (value + percentage) 
                var table = new ResultTable(1 + 1 + (allNames.Count() - 1) * 2);

                table.Headers.Add(jobName);

                table.Headers.Add(allNames.First());

                foreach (var name in allNames.Skip(1))
                {
                    table.Headers.Add(name);
                    table.Headers.Add(""); // percentage
                }

                foreach (var metadata in jobResult.Metadata)
                {
                    var metadataKey = metadata.Name.Split(';').First();

                    if (!jobResult.Results.ContainsKey(metadataKey))
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

                    cell.Elements.Add(new CellElement() { Text = metadata.Description, Alignment = CellTextAlignment.Left });

                    row.Add(cell);

                    foreach (var result in allResults)
                    {
                        // Skip jobs that have no data for this measure
                        if (!result.Jobs.ContainsKey(jobName))
                        {
                            foreach (var n in allNames)
                            {
                                row.Add(new Cell());
                            }

                            row.Add(new Cell());

                            continue;
                        }

                        var job = result.Jobs[jobName];

                        if (!String.IsNullOrEmpty(metadata.Format))
                        {
                            var measure = Convert.ToDouble(job.Results.ContainsKey(metadataKey) ? job.Results[metadataKey] : 0);
                            var previous = Convert.ToDouble(firstJob.Jobs[jobName].Results.ContainsKey(metadataKey) ? jobResult.Results[metadataKey] : 0);

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
                            var measure = job.Results.ContainsKey(metadataKey) ? job.Results[metadataKey] : 0;

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

        private static void DisplayDiff(IEnumerable<Benchmark[]> allBenchmarks, IEnumerable<string> allNames)
        {
            // Use the first job's benchmarks as the reference for the rows:
            var firstBenchmarks = allBenchmarks.FirstOrDefault();

            if (firstBenchmarks == null || firstBenchmarks.Length < 1)
            {
                return;
            }

            var summaries = new Dictionary<string, List<BenchmarkSummary>>();

            foreach (var benchmark in firstBenchmarks)
            {
                summaries[benchmark.FullName] = new List<BenchmarkSummary>();
            }

            foreach (var benchmarks in allBenchmarks)
            {
                foreach (var benchmark in benchmarks)
                {
                    summaries[benchmark.FullName].Add(new BenchmarkSummary()
                    {
                        Name = benchmark.FullName,
                        MeanNanoseconds = benchmark.Statistics.Mean,
                        AllocatedBytes = benchmark.Memory?.BytesAllocatedPerOperation
                    });
                }
            }

            // Simplfy the benchmarks' names where possible to remove prefixes that
            // are all the same to reduce the width of the first column of the table
            // to the shortest unique string required across all benchmarks.
            var nameSegments = summaries.Keys.ToDictionary(key => key, value => value.Split('.'));

            while (true)
            {
                var areAllFirstSegmentsTheSame = nameSegments.Values
                    .Select(segments => segments[0])
                    .Distinct()
                    .Count() == 1;

                if (!areAllFirstSegmentsTheSame)
                {
                    // The names cannot be simplified further
                    break;
                }

                foreach (var pair in nameSegments)
                {
                    nameSegments[pair.Key] = pair.Value.Skip(1).ToArray();
                }
            }

            // Map the full names to their simplified name
            var simplifiedNames = nameSegments.ToDictionary(key => key.Key, value => string.Join(".", value.Value));

            foreach (var benchmark in summaries.Values)
            {
                var firstBenchmark = benchmark.First();
                var simplifiedName = simplifiedNames[firstBenchmark.Name];

                Console.WriteLine();

                // Description + baseline benchmark (firstBenchmarks) + other benchmarks * 2 (value + percentage) 
                var table = new ResultTable(1 + 1 + ((allNames.Count() - 1) * 2));

                table.Headers.Add(simplifiedName);
                table.Headers.Add(allNames.First());

                foreach (var name in allNames.Skip(1))
                {
                    table.Headers.Add(name);
                    table.Headers.Add(""); // Percentage
                }

                var benchmarks = summaries[firstBenchmark.Name];

                // TODO Convert rows to the best unit of measure for the smallest value (ns, us, ms, etc.)
                // TODO What other rows are useful to add?
                AddRow(table, "Mean (ns)", summary => summary.MeanNanoseconds, "n4", benchmarks);
                AddRow(table, "Allocated (bytes)", summary => summary.AllocatedBytes, "n0", benchmarks);

                table.Render(Console.Out);

                Console.WriteLine();
            }

            void AddRow(
                ResultTable table,
                string name,
                Func<BenchmarkSummary, object> valueFactory,
                string valueFormat,
                List<BenchmarkSummary> summaries)
            {
                var row = table.AddRow();
                var cell = new Cell();
                cell.Elements.Add(new CellElement() { Text = name, Alignment = CellTextAlignment.Left });
                row.Add(cell);

                var baseline = summaries[0];

                foreach (var summary in summaries)
                {
                    var measure = Convert.ToDouble(valueFactory(summary));
                    var previous = Convert.ToDouble(valueFactory(baseline));

                    var improvement = measure == 0
                    ? 0
                    : (measure - previous) / previous * 100;

                    row.Add(cell = new Cell());

                    cell.Elements.Add(new CellElement
                    {
                        Text = measure.ToString(valueFormat),
                        Alignment = CellTextAlignment.Right
                    });

                    // Don't render % on baseline job
                    if (summary != baseline)
                    {
                        row.Add(cell = new Cell());

                        if (measure != 0)
                        {
                            var sign = improvement > 0 ? "+" : string.Empty;
                            cell.Elements.Add(new CellElement
                            {
                                Text = $"{sign}{improvement:n2}%",
                                Alignment = CellTextAlignment.Right
                            });
                        }
                    }
                }
            }
        }

        private sealed class BenchmarkSummary
        {
            public string Name { get; set; }
            public double MeanNanoseconds { get; set; }
            public long? AllocatedBytes { get; set; }
        }
    }
}
