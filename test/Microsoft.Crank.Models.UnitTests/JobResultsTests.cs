using System;
using System.Collections.Generic;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobResults"/> class.
    /// </summary>
    public class JobResultsTests
    {
        private readonly JobResults _jobResults;

        public JobResultsTests()
        {
            _jobResults = new JobResults();
        }

        /// <summary>
        /// Tests that the default constructor of JobResults initializes the Jobs and Properties dictionaries.
        /// </summary>
        [Fact]
        public void Constructor_InitializesDefaultDictionaries()
        {
            // Assert
            Assert.NotNull(_jobResults.Jobs);
            Assert.Empty(_jobResults.Jobs);
            Assert.NotNull(_jobResults.Properties);
            Assert.Empty(_jobResults.Properties);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="JobResult"/> class.
    /// </summary>
    public class JobResultTests
    {
        private readonly JobResult _jobResult;

        public JobResultTests()
        {
            _jobResult = new JobResult();
        }

        /// <summary>
        /// Tests that the default constructor of JobResult initializes its collection properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_InitializesDefaultCollections()
        {
            // Assert
            Assert.NotNull(_jobResult.Results);
            Assert.Empty(_jobResult.Results);

            Assert.NotNull(_jobResult.Metadata);
            Assert.Empty(_jobResult.Metadata);

            Assert.NotNull(_jobResult.Dependencies);
            Assert.Empty(_jobResult.Dependencies);

            Assert.NotNull(_jobResult.Measurements);
            Assert.Empty(_jobResult.Measurements);

            Assert.NotNull(_jobResult.Environment);
            Assert.Empty(_jobResult.Environment);

            Assert.NotNull(_jobResult.Variables);
            Assert.Empty(_jobResult.Variables);

            Assert.NotNull(_jobResult.Benchmarks);
            Assert.Empty(_jobResult.Benchmarks);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="ResultMetadata"/> class.
    /// </summary>
    public class ResultMetadataTests
    {
        /// <summary>
        /// Tests that properties of ResultMetadata can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Property_SetAndGet_WorksAsExpected()
        {
            // Arrange
            var metadata = new ResultMetadata();
            string expectedName = "TestName";
            string expectedDescription = "TestDescription";
            string expectedFormat = "TestFormat";

            // Act
            metadata.Name = expectedName;
            metadata.Description = expectedDescription;
            metadata.Format = expectedFormat;

            // Assert
            Assert.Equal(expectedName, metadata.Name);
            Assert.Equal(expectedDescription, metadata.Description);
            Assert.Equal(expectedFormat, metadata.Format);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Benchmark"/> class.
    /// </summary>
    public class BenchmarkTests
    {
        /// <summary>
        /// Tests that properties of Benchmark can be set and retrieved correctly, including default values.
        /// </summary>
        [Fact]
        public void Property_SetAndGet_WorksAsExpected()
        {
            // Arrange
            var benchmark = new Benchmark();
            string expectedFullName = "TestBenchmark";
            var expectedStatistics = new BenchmarkStatistics
            {
                Min = 1.0,
                Mean = 2.0,
                Median = 2.0,
                Max = 3.0,
                StandardError = 0.1,
                StandardDeviation = 0.2
            };
            var expectedMemory = new BenchmarkMemory
            {
                Gen0Collections = 10,
                Gen1Collections = 5,
                Gen2Collections = 2,
                BytesAllocatedPerOperation = 1000,
                TotalOperations = 50
            };

            // Act
            benchmark.FullName = expectedFullName;
            benchmark.Statistics = expectedStatistics;
            benchmark.Memory = expectedMemory;

            // Assert
            Assert.Equal(expectedFullName, benchmark.FullName);
            Assert.Equal(expectedStatistics, benchmark.Statistics);
            Assert.Equal(expectedMemory, benchmark.Memory);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="BenchmarkStatistics"/> class.
    /// </summary>
    public class BenchmarkStatisticsTests
    {
        /// <summary>
        /// Tests that properties of BenchmarkStatistics can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Property_SetAndGet_WorksAsExpected()
        {
            // Arrange
            var statistics = new BenchmarkStatistics();
            double? expectedMin = 1.1;
            double? expectedMean = 2.2;
            double? expectedMedian = 2.2;
            double? expectedMax = 3.3;
            double? expectedStandardError = 0.1;
            double? expectedStandardDeviation = 0.2;

            // Act
            statistics.Min = expectedMin;
            statistics.Mean = expectedMean;
            statistics.Median = expectedMedian;
            statistics.Max = expectedMax;
            statistics.StandardError = expectedStandardError;
            statistics.StandardDeviation = expectedStandardDeviation;

            // Assert
            Assert.Equal(expectedMin, statistics.Min);
            Assert.Equal(expectedMean, statistics.Mean);
            Assert.Equal(expectedMedian, statistics.Median);
            Assert.Equal(expectedMax, statistics.Max);
            Assert.Equal(expectedStandardError, statistics.StandardError);
            Assert.Equal(expectedStandardDeviation, statistics.StandardDeviation);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="BenchmarkMemory"/> class.
    /// </summary>
    public class BenchmarkMemoryTests
    {
        /// <summary>
        /// Tests that properties of BenchmarkMemory can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Property_SetAndGet_WorksAsExpected()
        {
            // Arrange
            var memory = new BenchmarkMemory();
            int? expectedGen0 = 3;
            int? expectedGen1 = 2;
            int? expectedGen2 = 1;
            long? expectedBytesAllocatedPerOperation = 2048;
            long? expectedTotalOperations = 100;

            // Act
            memory.Gen0Collections = expectedGen0;
            memory.Gen1Collections = expectedGen1;
            memory.Gen2Collections = expectedGen2;
            memory.BytesAllocatedPerOperation = expectedBytesAllocatedPerOperation;
            memory.TotalOperations = expectedTotalOperations;

            // Assert
            Assert.Equal(expectedGen0, memory.Gen0Collections);
            Assert.Equal(expectedGen1, memory.Gen1Collections);
            Assert.Equal(expectedGen2, memory.Gen2Collections);
            Assert.Equal(expectedBytesAllocatedPerOperation, memory.BytesAllocatedPerOperation);
            Assert.Equal(expectedTotalOperations, memory.TotalOperations);
        }
    }
}
