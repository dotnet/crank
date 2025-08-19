using System;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ExecutionResult"/> class.
    /// </summary>
    public class ExecutionResultTests
    {
        private readonly ExecutionResult _executionResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionResultTests"/> class.
        /// </summary>
        public ExecutionResultTests()
        {
            _executionResult = new ExecutionResult();
        }

        /// <summary>
        /// Tests that the constructor of <see cref="ExecutionResult"/> initializes default property values.
        /// Expected: ReturnCode equals 0, JobResults is not null and Benchmarks is an empty array.
        /// </summary>
//         [Fact] [Error] (34-36)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?)
//         public void Constructor_DefaultValues_AreInitializedCorrectly()
//         {
//             // Arrange & Act
//             var result = new ExecutionResult();
// 
//             // Assert
//             Assert.Equal(0, result.ReturnCode);
//             Assert.NotNull(result.JobResults);
//             Assert.NotNull(result.Benchmarks);
//             Assert.Empty(result.Benchmarks);
//         }

        /// <summary>
        /// Tests that the ReturnCode property can be set and retrieved as expected.
        /// Expected: After setting a value, the getter returns the same value.
        /// </summary>
//         [Fact] [Error] (51-30)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?) [Error] (52-53)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?)
//         public void ReturnCode_SetAndGetValue_ReturnsSameValue()
//         {
//             // Arrange
//             int expectedReturnCode = 42;
// 
//             // Act
//             _executionResult.ReturnCode = expectedReturnCode;
//             int actualReturnCode = _executionResult.ReturnCode;
// 
//             // Assert
//             Assert.Equal(expectedReturnCode, actualReturnCode);
//         }

        /// <summary>
        /// Tests that the JobResults property can be set and retrieved.
        /// Expected: After setting a new instance to JobResults, the getter returns the same instance.
        /// </summary>
        [Fact]
        public void JobResults_SetAndGetValue_ReturnsSameInstance()
        {
            // Arrange
            var expectedJobResults = new JobResults();

            // Act
            _executionResult.JobResults = expectedJobResults;
            var actualJobResults = _executionResult.JobResults;

            // Assert
            Assert.Equal(expectedJobResults, actualJobResults);
        }

        /// <summary>
        /// Tests that the JobResults property can handle null assignments.
        /// Expected: After setting JobResults to null, the getter returns null.
        /// </summary>
        [Fact]
        public void JobResults_SetToNull_ReturnsNull()
        {
            // Act
            _executionResult.JobResults = null;

            // Assert
            Assert.Null(_executionResult.JobResults);
        }

        /// <summary>
        /// Tests that the Benchmarks property can be set and retrieved.
        /// Expected: After setting a non-empty array, the getter returns the same array.
        /// </summary>
        [Fact]
        public void Benchmarks_SetAndGetValue_ReturnsSameArray()
        {
            // Arrange
            var benchmark = new Benchmark();
            var expectedBenchmarks = new Benchmark[] { benchmark };

            // Act
            _executionResult.Benchmarks = expectedBenchmarks;
            var actualBenchmarks = _executionResult.Benchmarks;

            // Assert
            Assert.Equal(expectedBenchmarks, actualBenchmarks);
        }

        /// <summary>
        /// Tests that the Benchmarks property can handle setting to an empty array.
        /// Expected: After setting an empty array, the getter returns an empty array.
        /// </summary>
        [Fact]
        public void Benchmarks_SetToEmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var expectedBenchmarks = Array.Empty<Benchmark>();

            // Act
            _executionResult.Benchmarks = expectedBenchmarks;
            var actualBenchmarks = _executionResult.Benchmarks;

            // Assert
            Assert.Empty(actualBenchmarks);
        }

        /// <summary>
        /// Tests that the Benchmarks property can handle null assignments.
        /// Expected: After setting Benchmarks to null, the getter returns null.
        /// </summary>
        [Fact]
        public void Benchmarks_SetToNull_ReturnsNull()
        {
            // Act
            _executionResult.Benchmarks = null;

            // Assert
            Assert.Null(_executionResult.Benchmarks);
        }
    }
}
