using Microsoft.Crank.Models;
using Moq;
using System.Collections.Generic;
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
        /// Tests the <see cref="Configuration.Variables"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Variables_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var variables = _configuration.Variables;

            // Assert
            Assert.NotNull(variables);
            Assert.Empty(variables);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Jobs"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Jobs_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var jobs = _configuration.Jobs;

            // Assert
            Assert.NotNull(jobs);
            Assert.Empty(jobs);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Scenarios"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Scenarios_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var scenarios = _configuration.Scenarios;

            // Assert
            Assert.NotNull(scenarios);
            Assert.Empty(scenarios);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Profiles"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Profiles_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var profiles = _configuration.Profiles;

            // Assert
            Assert.NotNull(profiles);
            Assert.Empty(profiles);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Scripts"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Scripts_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var scripts = _configuration.Scripts;

            // Assert
            Assert.NotNull(scripts);
            Assert.Empty(scripts);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.DefaultScripts"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void DefaultScripts_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var defaultScripts = _configuration.DefaultScripts;

            // Assert
            Assert.NotNull(defaultScripts);
            Assert.Empty(defaultScripts);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.OnResultsCreating"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void OnResultsCreating_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var onResultsCreating = _configuration.OnResultsCreating;

            // Assert
            Assert.NotNull(onResultsCreating);
            Assert.Empty(onResultsCreating);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Counters"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Counters_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var counters = _configuration.Counters;

            // Assert
            Assert.NotNull(counters);
            Assert.Empty(counters);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Results"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Results_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var results = _configuration.Results;

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.OnResultsCreated"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void OnResultsCreated_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var onResultsCreated = _configuration.OnResultsCreated;

            // Assert
            Assert.NotNull(onResultsCreated);
            Assert.Empty(onResultsCreated);
        }

        /// <summary>
        /// Tests the <see cref="Configuration.Commands"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Commands_WhenAccessed_ReturnsEmptyDictionary()
        {
            // Act
            var commands = _configuration.Commands;

            // Assert
            Assert.NotNull(commands);
            Assert.Empty(commands);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Service"/> class.
    /// </summary>
    public class ServiceTests
    {
        private readonly Service _service;

        public ServiceTests()
        {
            _service = new Service();
        }

        /// <summary>
        /// Tests the <see cref="Service.Job"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Job_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var job = "TestJob";

            // Act
            _service.Job = job;

            // Assert
            Assert.Equal(job, _service.Job);
        }

        /// <summary>
        /// Tests the <see cref="Service.Agent"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Agent_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var agent = "TestAgent";

            // Act
            _service.Agent = agent;

            // Assert
            Assert.Equal(agent, _service.Agent);
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
        /// Tests the <see cref="CounterList.Provider"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Provider_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var provider = "System.Runtime";

            // Act
            _counterList.Provider = provider;

            // Assert
            Assert.Equal(provider, _counterList.Provider);
        }

        /// <summary>
        /// Tests the <see cref="CounterList.Values"/> property to ensure it initializes correctly.
        /// </summary>
        [Fact]
        public void Values_WhenAccessed_ReturnsEmptyList()
        {
            // Act
            var values = _counterList.Values;

            // Assert
            Assert.NotNull(values);
            Assert.Empty(values);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Counter"/> class.
    /// </summary>
    public class CounterTests
    {
        private readonly Counter _counter;

        public CounterTests()
        {
            _counter = new Counter();
        }

        /// <summary>
        /// Tests the <see cref="Counter.Name"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Name_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var name = "TestCounter";

            // Act
            _counter.Name = name;

            // Assert
            Assert.Equal(name, _counter.Name);
        }

        /// <summary>
        /// Tests the <see cref="Counter.Measurement"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Measurement_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var measurement = "TestMeasurement";

            // Act
            _counter.Measurement = measurement;

            // Assert
            Assert.Equal(measurement, _counter.Measurement);
        }

        /// <summary>
        /// Tests the <see cref="Counter.Description"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Description_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var description = "TestDescription";

            // Act
            _counter.Description = description;

            // Assert
            Assert.Equal(description, _counter.Description);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="Result"/> class.
    /// </summary>
    public class ResultTests
    {
        private readonly Result _result;

        public ResultTests()
        {
            _result = new Result();
        }

        /// <summary>
        /// Tests the <see cref="Result.Measurement"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Measurement_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var measurement = "TestMeasurement";

            // Act
            _result.Measurement = measurement;

            // Assert
            Assert.Equal(measurement, _result.Measurement);
        }

        /// <summary>
        /// Tests the <see cref="Result.Name"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Name_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var name = "TestName";

            // Act
            _result.Name = name;

            // Assert
            Assert.Equal(name, _result.Name);
        }

        /// <summary>
        /// Tests the <see cref="Result.Description"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Description_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var description = "TestDescription";

            // Act
            _result.Description = description;

            // Assert
            Assert.Equal(description, _result.Description);
        }

        /// <summary>
        /// Tests the <see cref="Result.Format"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Format_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var format = "TestFormat";

            // Act
            _result.Format = format;

            // Assert
            Assert.Equal(format, _result.Format);
        }

        /// <summary>
        /// Tests the <see cref="Result.Aggregate"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Aggregate_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var aggregate = "TestAggregate";

            // Act
            _result.Aggregate = aggregate;

            // Assert
            Assert.Equal(aggregate, _result.Aggregate);
        }

        /// <summary>
        /// Tests the <see cref="Result.Reduce"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Reduce_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var reduce = "TestReduce";

            // Act
            _result.Reduce = reduce;

            // Assert
            Assert.Equal(reduce, _result.Reduce);
        }

        /// <summary>
        /// Tests the <see cref="Result.Excluded"/> property to ensure it can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Excluded_WhenSetAndRetrieved_ReturnsCorrectValue()
        {
            // Arrange
            var excluded = true;

            // Act
            _result.Excluded = excluded;

            // Assert
            Assert.Equal(excluded, _result.Excluded);
        }
    }
}
