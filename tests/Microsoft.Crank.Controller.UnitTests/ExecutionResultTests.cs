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

        public ExecutionResultTests()
        {
            _executionResult = new ExecutionResult();
        }

        /// <summary>
        /// Tests that the ReturnCode property can be set to a positive integer and retrieved correctly.
        /// </summary>
//         [Fact] [Error] (29-30)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?) [Error] (30-53)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?)
//         public void ReturnCode_SetPositiveValue_ReturnsSameValue()
//         {
//             // Arrange
//             int expectedReturnCode = 200;
// 
//             // Act
//             _executionResult.ReturnCode = expectedReturnCode;
//             int actualReturnCode = _executionResult.ReturnCode;
// 
//             // Assert
//             Assert.Equal(expectedReturnCode, actualReturnCode);
//         }

        /// <summary>
        /// Tests that the ReturnCode property can be set to a negative integer and retrieved correctly.
        /// </summary>
//         [Fact] [Error] (46-30)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?) [Error] (47-53)CS1061 'ExecutionResult' does not contain a definition for 'ReturnCode' and no accessible extension method 'ReturnCode' accepting a first argument of type 'ExecutionResult' could be found (are you missing a using directive or an assembly reference?)
//         public void ReturnCode_SetNegativeValue_ReturnsSameValue()
//         {
//             // Arrange
//             int expectedReturnCode = -1;
// 
//             // Act
//             _executionResult.ReturnCode = expectedReturnCode;
//             int actualReturnCode = _executionResult.ReturnCode;
// 
//             // Assert
//             Assert.Equal(expectedReturnCode, actualReturnCode);
//         }

        /// <summary>
        /// Tests that the JobResults property is initialized by default and is not null.
        /// </summary>
        [Fact]
        public void JobResults_DefaultValue_IsNotNull()
        {
            // Act
            JobResults actualJobResults = _executionResult.JobResults;

            // Assert
            Assert.NotNull(actualJobResults);
        }

        /// <summary>
        /// Tests that the JobResults property allows setting and retrieving a new value.
        /// </summary>
        [Fact]
        public void JobResults_SetNewValue_ReturnsSameValue()
        {
            // Arrange
            JobResults expectedJobResults = new JobResults();

            // Act
            _executionResult.JobResults = expectedJobResults;
            JobResults actualJobResults = _executionResult.JobResults;

            // Assert
            Assert.Equal(expectedJobResults, actualJobResults);
        }

        /// <summary>
        /// Tests that the JobResults property accepts a null value.
        /// </summary>
        [Fact]
        public void JobResults_SetNullValue_ReturnsNull()
        {
            // Act
            _executionResult.JobResults = null;
            
            // Assert
            Assert.Null(_executionResult.JobResults);
        }

        /// <summary>
        /// Tests that the Benchmarks property is initialized by default to an empty array.
        /// </summary>
        [Fact]
        public void Benchmarks_DefaultValue_IsEmptyArray()
        {
            // Act
            Benchmark[] benchmarks = _executionResult.Benchmarks;

            // Assert
            Assert.NotNull(benchmarks);
            Assert.Empty(benchmarks);
        }

        /// <summary>
        /// Tests that the Benchmarks property allows setting and retrieving a non-empty array.
        /// </summary>
        [Fact]
        public void Benchmarks_SetNonEmptyArray_ReturnsSameArray()
        {
            // Arrange
            Benchmark[] expectedBenchmarks = new Benchmark[]
            {
                new Benchmark(),
                new Benchmark()
            };

            // Act
            _executionResult.Benchmarks = expectedBenchmarks;
            Benchmark[] actualBenchmarks = _executionResult.Benchmarks;

            // Assert
            Assert.Equal(expectedBenchmarks, actualBenchmarks);
        }

        /// <summary>
        /// Tests that the Benchmarks property accepts a null value.
        /// </summary>
        [Fact]
        public void Benchmarks_SetNullValue_ReturnsNull()
        {
            // Act
            _executionResult.Benchmarks = null;
            
            // Assert
            Assert.Null(_executionResult.Benchmarks);
        }
    }
}
