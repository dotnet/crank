using Microsoft.Crank.RegressionBot;
using System;
using Xunit;

namespace Microsoft.Crank.RegressionBot.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Queries"/> class.
    /// </summary>
    public class QueriesTests
    {
        private readonly string _latestQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueriesTests"/> class.
        /// </summary>
        public QueriesTests()
        {
            _latestQuery = Queries.Latest;
        }

        /// <summary>
        /// Tests that the <see cref="Queries.Latest"/> constant is not null or whitespace.
        /// This ensures that the query string is correctly defined.
        /// </summary>
        [Fact]
        public void Latest_WhenAccessed_ReturnsNonNullNonEmptyString()
        {
            // Act & Assert
            Assert.False(string.IsNullOrWhiteSpace(_latestQuery), "Queries.Latest should not be null, empty, or whitespace.");
        }

        /// <summary>
        /// Tests that the <see cref="Queries.Latest"/> constant contains key SQL clauses.
        /// The assertions verify that the query includes SELECT, TOP, FROM, ORDER BY, and WHERE clauses.
        /// </summary>
        [Fact]
        public void Latest_WhenAccessed_ContainsExpectedSqlClauses()
        {
            // Act & Assert
            Assert.Contains("SELECT *", _latestQuery, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TOP (10000)", _latestQuery, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FROM [dbo]", _latestQuery, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY [Id] DESC", _latestQuery, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[DateTimeUtc] >= @startDate", _latestQuery, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that the <see cref="Queries.Latest"/> constant contains a placeholder for table substitution.
        /// This confirms that the query is designed to allow dynamic table names.
        /// </summary>
        [Fact]
        public void Latest_WhenAccessed_ContainsPlaceholderForTableName()
        {
            // Act & Assert
            Assert.Contains("{0}", _latestQuery, StringComparison.Ordinal);
        }
    }
}
