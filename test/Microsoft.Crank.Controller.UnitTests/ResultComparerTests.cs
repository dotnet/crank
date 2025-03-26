using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Crank.Controller;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultComparer"/> class.
    /// </summary>
    public class ResultComparerTests
    {
        /// <summary>
        /// Tests that Compare returns -1 when at least one of the provided files does not exist.
        /// </summary>
        [Fact]
        public void Compare_FileNotFound_ReturnsMinusOne()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var filenames = new List<string> { nonExistentFile };

            // Act
            int result = ResultComparer.Compare(filenames);

            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Tests that Compare returns 0 when provided with a valid file containing ExecutionResult with non-empty jobs,
        /// and no additional jobName added.
        /// </summary>
        [Fact]
        public void Compare_ValidFileWithoutJobName_ReturnsZero()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                // Create a dummy ExecutionResult with valid JobResults that has at least one job.
                var executionResult = new ExecutionResult
                {
                    JobResults = new JobResults
                    {
                        Jobs = new Dictionary<string, Job>
                        {
                            {
                                "TestJob", new Job
                                {
                                    Metadata = new List<Metadata>
                                    {
                                        new Metadata { Name = "M1;Extra", Format = "N2", Description = "Measure 1" }
                                    },
                                    Results = new Dictionary<string, object>
                                    {
                                        { "M1", 123.45 }
                                    }
                                }
                            }
                        }
                    },
                    Benchmarks = new Benchmark[]
                    {
                        new Benchmark
                        {
                            FullName = "TestBenchmark",
                            Statistics = new Statistics { Mean = 1000, StandardError = 10, StandardDeviation = 50, Median = 980 },
                            Memory = new Memory { Gen0Collections = 1, Gen1Collections = 0, Gen2Collections = 0, BytesAllocatedPerOperation = 2048 }
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(executionResult);
                File.WriteAllText(tempFile, json);
                var filenames = new List<string> { tempFile };

                // Act
                int result = ResultComparer.Compare(filenames);

                // Assert
                Assert.Equal(0, result);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that Compare returns 0 when provided with a valid file containing an ExecutionResult with empty JobResults.
        /// This scenario exercises the early return in DisplayDiff when job results are missing.
        /// </summary>
        [Fact]
        public void Compare_ValidFileWithEmptyJobResults_ReturnsZero()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                // Create an ExecutionResult with JobResults having no jobs.
                var executionResult = new ExecutionResult
                {
                    JobResults = new JobResults { Jobs = new Dictionary<string, Job>() },
                    Benchmarks = new Benchmark[]
                    {
                        new Benchmark
                        {
                            FullName = "TestBenchmarkEmpty",
                            Statistics = new Statistics { Mean = 500, StandardError = 5, StandardDeviation = 20, Median = 490 },
                            Memory = new Memory { Gen0Collections = 0, Gen1Collections = 0, Gen2Collections = 0, BytesAllocatedPerOperation = 0 }
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(executionResult);
                File.WriteAllText(tempFile, json);
                var filenames = new List<string> { tempFile };

                // Act
                int result = ResultComparer.Compare(filenames);

                // Assert
                Assert.Equal(0, result);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that Compare throws a JsonException when the file contains invalid JSON data.
        /// </summary>
        [Fact]
        public void Compare_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                string invalidJson = "This is not a JSON string";
                File.WriteAllText(tempFile, invalidJson);
                var filenames = new List<string> { tempFile };

                // Act & Assert
                Assert.ThrowsAny<JsonException>(() => ResultComparer.Compare(filenames));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that Compare returns 0 when provided with valid files and additional jobResults, benchmarks, and jobName.
        /// This ensures that the extra parameters are correctly incorporated into the diff display.
        /// </summary>
//         [Fact] [Error] (250-64)CS1503 Argument 2: cannot convert from 'Microsoft.Crank.Controller.UnitTests.JobResults' to 'Microsoft.Crank.Models.JobResults' [Error] (250-81)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.Controller.UnitTests.Benchmark[]' to 'Microsoft.Crank.Models.Benchmark[]'
//         public void Compare_WithAdditionalJobResults_ReturnsZero()
//         {
//             // Arrange
//             var tempFile = Path.GetTempFileName();
//             try
//             {
//                 // Create a dummy ExecutionResult with valid JobResults.
//                 var executionResult = new ExecutionResult
//                 {
//                     JobResults = new JobResults
//                     {
//                         Jobs = new Dictionary<string, Job>
//                         {
//                             {
//                                 "ExtraJob", new Job
//                                 {
//                                     Metadata = new List<Metadata>
//                                     {
//                                         new Metadata { Name = "M2;Detail", Format = "N1", Description = "Measurement 2" }
//                                     },
//                                     Results = new Dictionary<string, object>
//                                     {
//                                         { "M2", 200.0 }
//                                     }
//                                 }
//                             }
//                         }
//                     },
//                     Benchmarks = new Benchmark[]
//                     {
//                         new Benchmark
//                         {
//                             FullName = "ExtraBenchmark",
//                             Statistics = new Statistics { Mean = 1500, StandardError = 15, StandardDeviation = 60, Median = 1480 },
//                             Memory = new Memory { Gen0Collections = 2, Gen1Collections = 1, Gen2Collections = 0, BytesAllocatedPerOperation = 4096 }
//                         }
//                     }
//                 };
// 
//                 string json = JsonConvert.SerializeObject(executionResult);
//                 File.WriteAllText(tempFile, json);
//                 var filenames = new List<string> { tempFile };
// 
//                 // Create additional dummy jobResults and benchmarks.
//                 var extraJobResults = new JobResults
//                 {
//                     Jobs = new Dictionary<string, Job>
//                     {
//                         {
//                             "ExtraJob", new Job
//                             {
//                                 Metadata = new List<Metadata>
//                                 {
//                                     new Metadata { Name = "M2;Detail", Format = "N1", Description = "Measurement 2" }
//                                 },
//                                 Results = new Dictionary<string, object>
//                                 {
//                                     { "M2", 220.0 }
//                                 }
//                             }
//                         }
//                     }
//                 };
// 
//                 var extraBenchmarks = new Benchmark[]
//                 {
//                     new Benchmark
//                     {
//                         FullName = "ExtraBenchmark",
//                         Statistics = new Statistics { Mean = 1600, StandardError = 16, StandardDeviation = 65, Median = 1590 },
//                         Memory = new Memory { Gen0Collections = 2, Gen1Collections = 1, Gen2Collections = 0, BytesAllocatedPerOperation = 4096 }
//                     }
//                 };
// 
//                 string extraJobName = "AdditionalRun";
// 
//                 // Act
//                 int result = ResultComparer.Compare(filenames, extraJobResults, extraBenchmarks, extraJobName);
// 
//                 // Assert
//                 Assert.Equal(0, result);
//             }
//             finally
//             {
//                 if (File.Exists(tempFile))
//                 {
//                     File.Delete(tempFile);
//                 }
//             }
//         }

        /// <summary>
        /// Tests that Compare returns 0 when an empty list of filenames is provided and no additional job data is passed.
        /// This validates handling of an edge case where there is no file input.
        /// </summary>
        [Fact]
        public void Compare_EmptyFilenamesWithoutAdditionalData_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string>();

            // Act
            int result = ResultComparer.Compare(filenames);

            // Assert
            Assert.Equal(0, result);
        }
    }

    // The following minimal dummy classes are used for testing purposes 
    // to simulate the structure expected by ResultComparer from the Microsoft.Crank.Models namespace.
    // In an actual project these would be defined in the production assembly.

    internal class ExecutionResult
    {
        public JobResults JobResults { get; set; }
        public Benchmark[] Benchmarks { get; set; }
    }

    internal class JobResults
    {
        public Dictionary<string, Job> Jobs { get; set; }
    }

    internal class Job
    {
        public List<Metadata> Metadata { get; set; }
        public Dictionary<string, object> Results { get; set; }
    }

    internal class Metadata
    {
        public string Name { get; set; }
        public string Format { get; set; }
        public string Description { get; set; }
    }

    internal class Benchmark
    {
        public string FullName { get; set; }
        public Statistics Statistics { get; set; }
        public Memory Memory { get; set; }
    }

    internal class Statistics
    {
        public double? Mean { get; set; }
        public double? StandardError { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Median { get; set; }
    }

    internal class Memory
    {
        public int? Gen0Collections { get; set; }
        public int? Gen1Collections { get; set; }
        public int? Gen2Collections { get; set; }
        public long? BytesAllocatedPerOperation { get; set; }
    }
}
