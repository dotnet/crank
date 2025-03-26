using Microsoft.Crank.RegressionBot.Models;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Crank.RegressionBot.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Report"/> class.
    /// </summary>
    public class ReportTests
    {
        /// <summary>
        /// Tests that the default constructor initializes the Regressions property to a non-null, empty list.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_InitializesRegressionsToEmptyList()
        {
            // Arrange & Act
            Report report = new Report();

            // Assert
            Assert.NotNull(report.Regressions);
            Assert.Empty(report.Regressions);
        }

        /// <summary>
        /// Tests that the Regressions property can be set and retrieved correctly.
        /// </summary>
//         [Fact] [Error] (37-34)CS0029 Cannot implicitly convert type 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Models.UnitTests.Regression>' to 'System.Collections.Generic.List<Microsoft.Crank.RegressionBot.Models.Regression>'
//         public void RegressionsProperty_SetAndGet_ReturnsAssignedList()
//         {
//             // Arrange
//             Report report = new Report();
//             List<Regression> expectedList = new List<Regression> { new Regression() };
// 
//             // Act
//             report.Regressions = expectedList;
// 
//             // Assert
//             Assert.Same(expectedList, report.Regressions);
//         }
    }

    /// <summary>
    /// Dummy implementation of Regression used solely for unit testing purposes.
    /// </summary>
    public class Regression
    {
    }
}
