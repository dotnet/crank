// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
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
                    if (summaries.TryGetValue(benchmark.FullName, out var summary))
                    {
                        summary.Add(new BenchmarkSummary()
                        {
                            Name = benchmark.FullName,
                            MeanNanoseconds = benchmark.Statistics?.Mean ?? double.NaN,
                            StandardErrorNanoseconds = benchmark.Statistics?.StandardError ?? double.NaN,
                            StandardDeviationNanoseconds = benchmark.Statistics?.StandardDeviation ?? double.NaN,
                            MedianNanoseconds = benchmark.Statistics?.Median ?? double.NaN,
                            Gen0 = benchmark.Memory?.Gen0Collections ?? 0,
                            Gen1 = benchmark.Memory?.Gen1Collections ?? 0,
                            Gen2 = benchmark.Memory?.Gen2Collections ?? 0,
                            AllocatedBytes = benchmark.Memory?.BytesAllocatedPerOperation ?? 0
                        });
                    }
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

                // single benchmark with same full name
                if (nameSegments.Count == 1 && nameSegments.First().Value.Length <= 1)
                {
                    break;
                }
            }

            // Map the full names to their simplified name
            var simplifiedNames = nameSegments.ToDictionary(key => key.Key, value => string.Join(".", value.Value));

            var anyAllocations = summaries.Values
                .SelectMany(list => list)
                .Select(summary => summary.AllocatedBytes)
                .Any(allocatedBytes => allocatedBytes > 0);

            // Name + baseline mean (firstBenchmarks) + (other benchmarks' mean * 2 (value + ratio)) +
            // baseline allocations + (other benchmarks' mean * 2 (value + ratio))
            var otherCount = allNames.Count() - 1;
            var table = new ResultTable(1 + 1 + (anyAllocations ? 1 : 0) + (otherCount * (anyAllocations ? 4 : 2)));

            var firstName = allNames.First();

            table.Headers.Add("benchmark");
            table.Headers.Add($"mean ({firstName})");

            foreach (var name in allNames.Skip(1))
            {
                table.Headers.Add($"mean ({name})");
                table.Headers.Add("ratio");
            }

            if (anyAllocations)
            {
                table.Headers.Add($"allocated ({firstName})");

                foreach (var name in allNames.Skip(1))
                {
                    table.Headers.Add($"allocated ({name})");
                    table.Headers.Add("ratio");
                }
            }

            foreach (var benchmark in summaries.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
            {
                var firstBenchmark = benchmark.First();
                var simplifiedName = simplifiedNames[firstBenchmark.Name];

                var benchmarks = summaries[firstBenchmark.Name];

                var row = table.AddRow();
                var cell = new Cell();
                cell.Elements.Add(new CellElement() { Text = simplifiedName, Alignment = CellTextAlignment.Left });
                row.Add(cell);

                AddCells(row, summary => summary.MeanNanoseconds, UnitType.Time, benchmarks);

                if (anyAllocations)
                {
                    AddCells(row, summary => summary.AllocatedBytes, UnitType.Size, benchmarks);
                }
            }

            Console.WriteLine("```md"); // Format as a GitHub-flavored Markdown table

            table.Render(Console.Out);

            Console.WriteLine("```");

            void AddCells(
                List<Cell> row,
                Func<BenchmarkSummary, object> valueFactory,
                UnitType unitType,
                List<BenchmarkSummary> summaries)
            {
                var rawValues = summaries
                    .Select(summary => valueFactory(summary))
                    .Select(value => Convert.ToDouble(value))
                    .ToArray();

                var precision = PrecisionHelper.GetPrecision(rawValues);
                var sizeUnit = unitType == UnitType.Size ? SizeUnit.GetBestSizeUnit(rawValues) : null;
                var timeUnit = unitType == UnitType.Time ? TimeUnit.GetBestTimeUnit(rawValues) : null;

                var units = unitType switch
                {
                    UnitType.Size => sizeUnit.Name,
                    UnitType.Time => timeUnit.Name,
                    _ => string.Empty
                };

                var baseline = summaries[0];

                for (var i = 0; i < summaries.Count; i++)
                {
                    var measure = rawValues[i];
                    var previous = rawValues[0];

                    var ratio = measure == 0
                    ? 0
                    : measure / previous;

                    var formattedValue = unitType switch
                    {
                        UnitType.Size => new SizeValue((long)measure).ToString(sizeUnit),
                        UnitType.Time => new TimeInterval(measure).ToString(timeUnit, precision),
                        _ => measure.ToString("N" + precision, CultureInfo.InvariantCulture)
                    };

                    var cell = new Cell();
                    cell.Elements.Add(new CellElement
                    {
                        Text = $"{formattedValue} {units}",
                        Alignment = CellTextAlignment.Right
                    });
                    row.Add(cell);

                    // Don't render the ratio on baseline benchmark
                    if (summaries[i] != baseline)
                    {
                        row.Add(cell = new Cell());

                        if (measure != 0)
                        {
                            cell.Elements.Add(new CellElement
                            {
                                Text = ratio.ToString("N2", CultureInfo.InvariantCulture),
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
            public double StandardErrorNanoseconds { get; set; }
            public double StandardDeviationNanoseconds { get; set; }
            public double MedianNanoseconds { get; set; }
            public int Gen0 { get; set; }
            public int Gen1 { get; set; }
            public int Gen2 { get; set; }
            public long AllocatedBytes { get; set; }
        }

        // The types below are based on BenchmarkDotNet's DisplayPrecisionManager, SizeUnit and SizeValue types,
        // and perfolizer's TimeInterval and TimeUnit types. It would be preferable to reference BenchmarkDotNet
        // directly to allow use of these types directly when formatting the results from the JSON files.

        private enum UnitType
        {
            Dimensionless,
            Time,
            Size
        }

        private sealed class PrecisionHelper
        {
            private const int MinPrecision = 1;
            private const int MaxPrecision = 4;

            internal static int GetPrecision(IList<double> values)
            {
                if (values.Count == 0)
                {
                    return MinPrecision;
                }

                var oneNanosecond = 1e-9;

                var allValuesAreZeros = values.All(value => Math.Abs(value) < oneNanosecond);
                if (allValuesAreZeros)
                {
                    return MinPrecision;
                }

                var minValue = values.Any() ? values.Min(value => Math.Abs(value)) : 0;

                if (double.IsNaN(minValue) || double.IsInfinity(minValue))
                {
                    return MinPrecision;
                }

                if (minValue < 1 - oneNanosecond)
                {
                    return MaxPrecision;
                }

                return Clamp((int)Math.Truncate(-Math.Log10(minValue)) + 3, MinPrecision, MaxPrecision);
            }

            private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
        }

        private sealed class SizeUnit
        {
            public string Name { get; }
            public string Description { get; }
            public long ByteAmount { get; }

            public SizeUnit(string name, string description, long byteAmount)
            {
                Name = name;
                Description = description;
                ByteAmount = byteAmount;
            }

            private const long BytesInKiloByte = 1024L;

            public static readonly SizeUnit B = new SizeUnit("B", "Byte", 1L);
            public static readonly SizeUnit KB = new SizeUnit("KB", "Kilobyte", BytesInKiloByte);
            public static readonly SizeUnit MB = new SizeUnit("MB", "Megabyte", BytesInKiloByte * BytesInKiloByte);
            public static readonly SizeUnit GB = new SizeUnit("GB", "Gigabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
            public static readonly SizeUnit TB = new SizeUnit("TB", "Terabyte", BytesInKiloByte * BytesInKiloByte * BytesInKiloByte * BytesInKiloByte);
            public static readonly SizeUnit[] All = { B, KB, MB, GB, TB };

            public static SizeUnit GetBestSizeUnit(params double[] values)
            {
                if (!values.Any())
                {
                    return B;
                }

                var minValue = values.Min();

                foreach (var sizeUnit in All)
                {
                    if (minValue < sizeUnit.ByteAmount * BytesInKiloByte)
                    {
                        return sizeUnit;
                    }
                }

                return All.Last();
            }

            public static double Convert(long value, SizeUnit from, SizeUnit to)
                => value * (double)from.ByteAmount / (to ?? GetBestSizeUnit(value)).ByteAmount;
        }

        private readonly struct SizeValue
        {
            public long Bytes { get; }

            public SizeValue(long bytes) => Bytes = bytes;

            public SizeValue(long bytes, SizeUnit unit) : this(bytes * unit.ByteAmount) { }

            public static readonly SizeValue B = new SizeValue(1, SizeUnit.B);
            public static readonly SizeValue KB = new SizeValue(1, SizeUnit.KB);
            public static readonly SizeValue MB = new SizeValue(1, SizeUnit.MB);
            public static readonly SizeValue GB = new SizeValue(1, SizeUnit.GB);
            public static readonly SizeValue TB = new SizeValue(1, SizeUnit.TB);

            public string ToString(SizeUnit sizeUnit)
            {
                var unitValue = SizeUnit.Convert(Bytes, SizeUnit.B, sizeUnit);
                return unitValue.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        private sealed class TimeUnit
        {
            public string Name { get; }

            public string Description { get; }
            public long NanosecondAmount { get; }

            private TimeUnit(string name, string description, long nanosecondAmount)
            {
                Name = name;
                Description = description;
                NanosecondAmount = nanosecondAmount;
            }

            public static readonly TimeUnit Nanosecond = new TimeUnit("ns", "Nanosecond", 1);
            public static readonly TimeUnit Microsecond = new TimeUnit("\u03BCs", "Microsecond", 1000);
            public static readonly TimeUnit Millisecond = new TimeUnit("ms", "Millisecond", 1000 * 1000);
            public static readonly TimeUnit Second = new TimeUnit("s", "Second", 1000 * 1000 * 1000);
            public static readonly TimeUnit Minute = new TimeUnit("m", "Minute", Second.NanosecondAmount * 60);
            public static readonly TimeUnit Hour = new TimeUnit("h", "Hour", Minute.NanosecondAmount * 60);
            public static readonly TimeUnit Day = new TimeUnit("d", "Day", Hour.NanosecondAmount * 24);
            public static readonly TimeUnit[] All = { Nanosecond, Microsecond, Millisecond, Second, Minute, Hour, Day };

            public static TimeUnit GetBestTimeUnit(params double[] values)
            {
                if (values.Length == 0)
                {
                    return Nanosecond;
                }

                var minValue = values.Min();

                foreach (var timeUnit in All)
                {
                    if (minValue < timeUnit.NanosecondAmount * 1000)
                    {
                        return timeUnit;
                    }
                }

                return All.Last();
            }

            public static double Convert(double value, TimeUnit from, TimeUnit to) =>
                value * from.NanosecondAmount / (to ?? GetBestTimeUnit(value)).NanosecondAmount;
        }

        private readonly struct TimeInterval
        {
            public double Nanoseconds { get; }

            public TimeInterval(double nanoseconds) => Nanoseconds = nanoseconds;
            public TimeInterval(double value, TimeUnit unit) : this(value * unit.NanosecondAmount) { }

            public static readonly TimeInterval Nanosecond = new TimeInterval(1, TimeUnit.Nanosecond);
            public static readonly TimeInterval Microsecond = new TimeInterval(1, TimeUnit.Microsecond);
            public static readonly TimeInterval Millisecond = new TimeInterval(1, TimeUnit.Millisecond);
            public static readonly TimeInterval Second = new TimeInterval(1, TimeUnit.Second);
            public static readonly TimeInterval Minute = new TimeInterval(1, TimeUnit.Minute);
            public static readonly TimeInterval Hour = new TimeInterval(1, TimeUnit.Hour);
            public static readonly TimeInterval Day = new TimeInterval(1, TimeUnit.Day);

            public string ToString(TimeUnit timeUnit, int precision)
            {
                var provider = CultureInfo.InvariantCulture;
                var unitValue = TimeUnit.Convert(Nanoseconds, TimeUnit.Nanosecond, timeUnit);
                return unitValue.ToString("N" + precision.ToString(provider), provider);
            }
        }
    }
}
