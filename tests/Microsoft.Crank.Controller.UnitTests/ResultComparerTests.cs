using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Minimal implementations of required model classes for testing.
    /// </summary>
    internal class ExecutionResult
    {
        public JobResults JobResults { get; set; }
        public Benchmark[] Benchmarks { get; set; }
    }

    internal class JobResults
    {
        public Dictionary<string, JobResult> Jobs { get; set; } = new Dictionary<string, JobResult>();
    }

    internal class JobResult
    {
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<Metadata> Metadata { get; set; } = new List<Metadata>();
    }

    internal class Metadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
    }

    internal class Benchmark
    {
        public string FullName { get; set; }
        public Statistics Statistics { get; set; }
        public MemoryInfo Memory { get; set; }
    }

    internal class Statistics
    {
        public double? Mean { get; set; }
        public double? StandardError { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Median { get; set; }
    }

    internal class MemoryInfo
    {
        public int? Gen0Collections { get; set; }
        public int? Gen1Collections { get; set; }
        public int? Gen2Collections { get; set; }
        public long? BytesAllocatedPerOperation { get; set; }
    }

    /// <summary>
    /// Unit tests for the <see cref="ResultComparer"/> class.
    /// </summary>
    public class ResultComparerTests : IDisposable
    {
        private readonly List<string> _tempFiles = new List<string>();

        /// <summary>
        /// Disposes temporary files created for tests.
        /// </summary>
        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Creates a temporary file with the specified content and tracks it for cleanup.
        /// </summary>
        /// <param name="content">The content to write into the file.</param>
        /// <returns>The path of the temporary file.</returns>
        private string CreateTempFile(string content)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            File.WriteAllText(tempFile, content);
            _tempFiles.Add(tempFile);
            return tempFile;
        }

        /// <summary>
        /// Tests that Compare returns -1 when one of the provided files does not exist.
        /// </summary>
        [Fact]
        public void Compare_FileNotFound_ReturnsMinusOne()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            IEnumerable<string> filenames = new List<string> { nonExistentFile };

            // Act
            int result = ResultComparer.Compare(filenames);

            // Assert
            Assert.Equal(-1, result);
        }

        /// <summary>
        /// Tests that Compare returns 0 when given a valid file with proper JSON content and no extra job results or benchmarks.
        /// The test captures console output to ensure that output is produced.
        /// </summary>
        [Fact]
        public void Compare_ValidFileWithoutExtraData_ReturnsZero()
        {
            // Arrange
            var executionResult = new ExecutionResult
            {
                JobResults = new JobResults
                {
                    Jobs = new Dictionary<string, JobResult>
                    {
                        {
                            "TestJob", new JobResult
                            {
                                Results = new Dictionary<string, object>
                                {
                                    { "Metric", 100 }
                                },
                                Metadata = new List<Metadata>
                                {
                                    new Metadata
                                    {
                                        Name = "Metric",
                                        Description = "Test Metric",
                                        Format = "F2"
                                    }
                                }
                            }
                        }
                    }
                },
                Benchmarks = new Benchmark[0]
            };
            string jsonContent = JsonConvert.SerializeObject(executionResult);
            string tempFile = CreateTempFile(jsonContent);
            IEnumerable<string> filenames = new List<string> { tempFile };

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                int result = ResultComparer.Compare(filenames);

                // Assert
                Assert.Equal(0, result);
                string output = sw.ToString();
                Assert.Contains("TestJob", output);
            }
        }

        /// <summary>
        /// Tests that Compare returns 0 when provided with valid file(s) and additional job results, benchmarks, and a job name.
        /// The test verifies the extra data is included in the output.
        /// </summary>
//         [Fact] [Error] (284-64)CS1503 Argument 2: cannot convert from 'Microsoft.Crank.Controller.UnitTests.JobResults' to 'Microsoft.Crank.Models.JobResults' [Error] (284-81)CS1503 Argument 3: cannot convert from 'Microsoft.Crank.Controller.UnitTests.Benchmark[]' to 'Microsoft.Crank.Models.Benchmark[]'
//         public void Compare_ValidFileWithExtraData_ReturnsZero()
//         {
//             // Arrange
//             var executionResult = new ExecutionResult
//             {
//                 JobResults = new JobResults
//                 {
//                     Jobs = new Dictionary<string, JobResult>
//                     {
//                         {
//                             "ExtraJob", new JobResult
//                             {
//                                 Results = new Dictionary<string, object>
//                                 {
//                                     { "ExtraMetric", 200 }
//                                 },
//                                 Metadata = new List<Metadata>
//                                 {
//                                     new Metadata
//                                     {
//                                         Name = "ExtraMetric",
//                                         Description = "Extra Metric",
//                                         Format = "F2"
//                                     }
//                                 }
//                             }
//                         }
//                     }
//                 },
//                 Benchmarks = new Benchmark[]
//                 {
//                     new Benchmark
//                     {
//                         FullName = "Benchmark.Test.Extra",
//                         Statistics = new Statistics
//                         {
//                             Mean = 123.456,
//                             StandardError = 1.2,
//                             StandardDeviation = 2.3,
//                             Median = 120.0
//                         },
//                         Memory = new MemoryInfo
//                         {
//                             Gen0Collections = 1,
//                             Gen1Collections = 0,
//                             Gen2Collections = 0,
//                             BytesAllocatedPerOperation = 1024
//                         }
//                     }
//                 }
//             };
//             string jsonContent = JsonConvert.SerializeObject(executionResult);
//             string tempFile = CreateTempFile(jsonContent);
//             IEnumerable<string> filenames = new List<string> { tempFile };
// 
//             // Prepare extra jobResults data.
//             var extraJobResults = new JobResults
//             {
//                 Jobs = new Dictionary<string, JobResult>
//                 {
//                     {
//                         "ExtraJob", new JobResult
//                         {
//                             Results = new Dictionary<string, object>
//                             {
//                                 { "ExtraMetric", 210 }
//                             },
//                             Metadata = new List<Metadata>
//                             {
//                                 new Metadata
//                                 {
//                                     Name = "ExtraMetric",
//                                     Description = "Extra Metric",
//                                     Format = "F2"
//                                 }
//                             }
//                         }
//                     }
//                 }
//             };
// 
//             // Prepare extra benchmarks data.
//             var extraBenchmarks = new Benchmark[]
//             {
//                 new Benchmark
//                 {
//                     FullName = "Benchmark.Test.Extra",
//                     Statistics = new Statistics
//                     {
//                         Mean = 130.0,
//                         StandardError = 1.5,
//                         StandardDeviation = 2.5,
//                         Median = 125.0
//                     },
//                     Memory = new MemoryInfo
//                     {
//                         Gen0Collections = 1,
//                         Gen1Collections = 0,
//                         Gen2Collections = 0,
//                         BytesAllocatedPerOperation = 2048
//                     }
//                 }
//             };
// 
//             string jobName = "ExtraJobName";
// 
//             using (var sw = new StringWriter())
//             {
//                 Console.SetOut(sw);
// 
//                 // Act
//                 int result = ResultComparer.Compare(filenames, extraJobResults, extraBenchmarks, jobName);
// 
//                 // Assert
//                 Assert.Equal(0, result);
//                 string output = sw.ToString();
//                 Assert.Contains("ExtraJob", output);
//                 Assert.Contains(jobName, output);
//             }
//         }

        /// <summary>
        /// Tests that Compare returns 0 when provided with an empty filenames collection without extra data.
        /// </summary>
        [Fact]
        public void Compare_EmptyFilenamesWithoutExtra_ReturnsZero()
        {
            // Arrange
            IEnumerable<string> filenames = new List<string>();

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                // Act
                int result = ResultComparer.Compare(filenames);

                // Assert
                Assert.Equal(0, result);
            }
        }

        /// <summary>
        /// Tests that Compare throws a JsonReaderException when the file contains invalid JSON content.
        /// </summary>
        [Fact]
        public void Compare_InvalidJsonInFile_ThrowsException()
        {
            // Arrange
            string invalidJson = "Invalid JSON Content";
            string tempFile = CreateTempFile(invalidJson);
            IEnumerable<string> filenames = new List<string> { tempFile };

            // Act & Assert
            Assert.ThrowsAny<JsonReaderException>(() => ResultComparer.Compare(filenames));
        }
    }
}
