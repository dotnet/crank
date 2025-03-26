using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref = "Configuration"/> class.
    /// </summary>
    public class ConfigurationTests
    {
        private readonly Configuration _configuration;
        public ConfigurationTests()
        {
            _configuration = new Configuration();
        }

        /// <summary>
        /// Tests that the Configuration constructor initializes all dictionary and list properties.
        /// </summary>
        [Fact]
        public void Constructor_InitializesProperties()
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
        }

        /// <summary>
        /// Tests that the DefaultScripts property can be set and retrieved.
        /// </summary>
        [Fact]
        public void DefaultScripts_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedScripts = new List<string>
            {
                "script1",
                "script2"
            };
            // Act
            _configuration.DefaultScripts = expectedScripts;
            // Assert
            Assert.Equal(expectedScripts, _configuration.DefaultScripts);
        }

        /// <summary>
        /// Tests that the OnResultsCreating property can be set and retrieved.
        /// </summary>
        [Fact]
        public void OnResultsCreating_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedScripts = new List<string>
            {
                "initScript"
            };
            // Act
            _configuration.OnResultsCreating = expectedScripts;
            // Assert
            Assert.Equal(expectedScripts, _configuration.OnResultsCreating);
        }

        /// <summary>
        /// Tests that the Counters property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Counters_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var counterList = new CounterList
            {
                Provider = "System.Runtime"
            };
            counterList.Values.Add(new Counter { Name = "counter1", Measurement = "ms", Description = "Test counter" });
            var expectedCounters = new List<CounterList>
            {
                counterList
            };
            // Act
            _configuration.Counters = expectedCounters;
            // Assert
            Assert.Equal(expectedCounters, _configuration.Counters);
        }

        /// <summary>
        /// Tests that the Results property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Results_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var result = new Result
            {
                Measurement = "ms",
                Name = "ResponseTime",
                Description = "Response time measurement",
                Format = "N2",
                Aggregate = "Average",
                Reduce = "Sum",
                Excluded = false
            };
            var expectedResults = new List<Result>
            {
                result
            };
            // Act
            _configuration.Results = expectedResults;
            // Assert
            Assert.Equal(expectedResults, _configuration.Results);
        }

        /// <summary>
        /// Tests that the Commands property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Commands_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            // Assuming CommandDefinition is a class from Microsoft.Crank.Models with a parameterless constructor.
            var commandDefinition = new CommandDefinition();
            var expectedCommands = new Dictionary<string, List<CommandDefinition>>
            {
                {
                    "Group1",
                    new List<CommandDefinition>
                    {
                        commandDefinition
                    }
                }
            };
            // Act
            _configuration.Commands = expectedCommands;
            // Assert
            Assert.Equal(expectedCommands, _configuration.Commands);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref = "Service"/> class.
    /// </summary>
    public class ServiceTests
    {
        private readonly Service _service;
        public ServiceTests()
        {
            _service = new Service();
        }

        /// <summary>
        /// Tests that the Job property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Job_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedJob = "TestJob";
            // Act
            _service.Job = expectedJob;
            // Assert
            Assert.Equal(expectedJob, _service.Job);
        }

        /// <summary>
        /// Tests that the Agent property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Agent_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedAgent = "TestAgent";
            // Act
            _service.Agent = expectedAgent;
            // Assert
            Assert.Equal(expectedAgent, _service.Agent);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref = "CounterList"/> class.
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
        public void Constructor_InitializesValues_ListIsNotNull()
        {
            // Assert
            Assert.NotNull(_counterList.Values);
        }

        /// <summary>
        /// Tests that the Provider property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Provider_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedProvider = "System.Diagnostics";
            // Act
            _counterList.Provider = expectedProvider;
            // Assert
            Assert.Equal(expectedProvider, _counterList.Provider);
        }

        /// <summary>
        /// Tests adding a Counter to the Values list.
        /// </summary>
        [Fact]
        public void Values_AddCounter_ListContainsCounter()
        {
            // Arrange
            var counter = new Counter
            {
                Name = "cpu",
                Measurement = "percent",
                Description = "CPU Usage"
            };
            // Act
            _counterList.Values.Add(counter);
            // Assert
            Assert.Contains(counter, _counterList.Values);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref = "Counter"/> class.
    /// </summary>
    public class CounterTests
    {
        private readonly Counter _counter;
        public CounterTests()
        {
            _counter = new Counter();
        }

        /// <summary>
        /// Tests that the Name property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Name_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedName = "Memory";
            // Act
            _counter.Name = expectedName;
            // Assert
            Assert.Equal(expectedName, _counter.Name);
        }

        /// <summary>
        /// Tests that the Measurement property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Measurement_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedMeasurement = "MB";
            // Act
            _counter.Measurement = expectedMeasurement;
            // Assert
            Assert.Equal(expectedMeasurement, _counter.Measurement);
        }

        /// <summary>
        /// Tests that the Description property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Description_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedDescription = "Memory Usage";
            // Act
            _counter.Description = expectedDescription;
            // Assert
            Assert.Equal(expectedDescription, _counter.Description);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref = "Result"/> class.
    /// </summary>
    public class ResultTests
    {
        private readonly Result _result;
        public ResultTests()
        {
            _result = new Result();
        }

        /// <summary>
        /// Tests that the Measurement property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Measurement_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedMeasurement = "latency";
            // Act
            _result.Measurement = expectedMeasurement;
            // Assert
            Assert.Equal(expectedMeasurement, _result.Measurement);
        }

        /// <summary>
        /// Tests that the Name property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Name_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedName = "ResponseTime";
            // Act
            _result.Name = expectedName;
            // Assert
            Assert.Equal(expectedName, _result.Name);
        }

        /// <summary>
        /// Tests that the Description property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Description_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedDescription = "Time taken to respond";
            // Act
            _result.Description = expectedDescription;
            // Assert
            Assert.Equal(expectedDescription, _result.Description);
        }

        /// <summary>
        /// Tests that the Format property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Format_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedFormat = "N2";
            // Act
            _result.Format = expectedFormat;
            // Assert
            Assert.Equal(expectedFormat, _result.Format);
        }

        /// <summary>
        /// Tests that the Aggregate property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Aggregate_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedAggregate = "Average";
            // Act
            _result.Aggregate = expectedAggregate;
            // Assert
            Assert.Equal(expectedAggregate, _result.Aggregate);
        }

        /// <summary>
        /// Tests that the Reduce property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Reduce_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedReduce = "Sum";
            // Act
            _result.Reduce = expectedReduce;
            // Assert
            Assert.Equal(expectedReduce, _result.Reduce);
        }

        /// <summary>
        /// Tests that the Excluded property can be set and retrieved.
        /// </summary>
        [Fact]
        public void Excluded_SetAndGet_ReturnsExpectedValue()
        {
            // Arrange
            var expectedExcluded = true;
            // Act
            _result.Excluded = expectedExcluded;
            // Assert
            Assert.Equal(expectedExcluded, _result.Excluded);
        }
    }
}