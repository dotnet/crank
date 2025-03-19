using System.Collections.Generic;
using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Configuration"/> class.
    /// </summary>
    public class ConfigurationTests
    {
        private readonly Configuration _configuration;

        public ConfigurationTests()
        {
            _configuration = new Configuration();
        }

        /// <summary>
        /// Tests that the default properties of the Configuration class are initialized correctly.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_InitializesDictionariesAndLists()
        {
            // Assert
            Assert.NotNull(_configuration.Variables);
            Assert.NotNull(_configuration.Jobs);
            Assert.NotNull(_configuration.Scenarios);
            Assert.NotNull(_configuration.Profiles);
            Assert.NotNull(_configuration.Scripts);
            Assert.NotNull(_configuration.DefaultScripts);
            Assert.NotNull(_configuration.OnResultsCreating);
            Assert.NotNull(_configuration.Counters);
            Assert.NotNull(_configuration.Results);
            Assert.NotNull(_configuration.OnResultsCreated);
            Assert.NotNull(_configuration.Commands);
            
            // Ensure they are empty
            Assert.Empty(_configuration.Variables);
            Assert.Empty(_configuration.Jobs);
            Assert.Empty(_configuration.Scenarios);
            Assert.Empty(_configuration.Profiles);
            Assert.Empty(_configuration.Scripts);
            Assert.Empty(_configuration.DefaultScripts);
            Assert.Empty(_configuration.OnResultsCreating);
            Assert.Empty(_configuration.Counters);
            Assert.Empty(_configuration.Results);
            Assert.Empty(_configuration.OnResultsCreated);
            Assert.Empty(_configuration.Commands);
        }

        /// <summary>
        /// Tests that modifications to the DefaultScripts list are independent of OnResultsCreating list.
        /// </summary>
        [Fact]
        public void DefaultScriptsAndOnResultsCreating_WhenModified_AreIndependent()
        {
            // Arrange
            string script1 = "script1";
            string script2 = "script2";

            // Act
            _configuration.DefaultScripts.Add(script1);
            _configuration.OnResultsCreating.Add(script2);

            // Assert
            Assert.Single(_configuration.DefaultScripts);
            Assert.Single(_configuration.OnResultsCreating);
            Assert.Contains(script1, _configuration.DefaultScripts);
            Assert.Contains(script2, _configuration.OnResultsCreating);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Service"/> class.
    /// </summary>
    public class ServiceTests
    {
        /// <summary>
        /// Tests that the properties of Service can be set and retrieved.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsExpectedValues()
        {
            // Arrange
            var service = new Service();
            string expectedJob = "JobA";
            string expectedAgent = "AgentX";

            // Act
            service.Job = expectedJob;
            service.Agent = expectedAgent;

            // Assert
            Assert.Equal(expectedJob, service.Job);
            Assert.Equal(expectedAgent, service.Agent);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="CounterList"/> class.
    /// </summary>
    public class CounterListTests
    {
        private readonly CounterList _counterList;

        public CounterListTests()
        {
            _counterList = new CounterList();
        }

        /// <summary>
        /// Tests that the CounterList constructor initializes the Values list.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_InitializesValuesList()
        {
            // Assert
            Assert.NotNull(_counterList.Values);
            Assert.Empty(_counterList.Values);
        }

        /// <summary>
        /// Tests that the Provider property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Provider_PropertySetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            string expectedProvider = "System.Runtime";

            // Act
            _counterList.Provider = expectedProvider;

            // Assert
            Assert.Equal(expectedProvider, _counterList.Provider);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Counter"/> class.
    /// </summary>
    public class CounterTests
    {
        /// <summary>
        /// Tests that all properties of Counter can be set and retrieved.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsExpectedValues()
        {
            // Arrange
            var counter = new Counter();
            string expectedName = "cpu-usage";
            string expectedMeasurement = "CPU";
            string expectedDescription = "Usage of the CPU";

            // Act
            counter.Name = expectedName;
            counter.Measurement = expectedMeasurement;
            counter.Description = expectedDescription;

            // Assert
            Assert.Equal(expectedName, counter.Name);
            Assert.Equal(expectedMeasurement, counter.Measurement);
            Assert.Equal(expectedDescription, counter.Description);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Result"/> class.
    /// </summary>
    public class ResultTests
    {
        /// <summary>
        /// Tests that all properties of Result can be set and retrieved.
        /// </summary>
        [Fact]
        public void Properties_SetAndGet_ReturnsExpectedValues()
        {
            // Arrange
            var result = new Result();
            string expectedMeasurement = "Latency";
            string expectedName = "AverageLatency";
            string expectedDescription = "Average latency of the operation";
            string expectedFormat = "ms";
            string expectedAggregate = "avg";
            string expectedReduce = "none";
            bool expectedExcluded = false;

            // Act
            result.Measurement = expectedMeasurement;
            result.Name = expectedName;
            result.Description = expectedDescription;
            result.Format = expectedFormat;
            result.Aggregate = expectedAggregate;
            result.Reduce = expectedReduce;
            result.Excluded = expectedExcluded;

            // Assert
            Assert.Equal(expectedMeasurement, result.Measurement);
            Assert.Equal(expectedName, result.Name);
            Assert.Equal(expectedDescription, result.Description);
            Assert.Equal(expectedFormat, result.Format);
            Assert.Equal(expectedAggregate, result.Aggregate);
            Assert.Equal(expectedReduce, result.Reduce);
            Assert.Equal(expectedExcluded, result.Excluded);
        }
    }
}
