using Moq;
using System;
using System.Reflection;

namespace Microsoft.Crank.EventSources.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="BenchmarksEventSource"/> class.
    /// </summary>
    [TestClass]
    public class BenchmarksEventSourceTests
    {
        private readonly Mock<BenchmarksEventSource> _mockEventSource;

        public BenchmarksEventSourceTests()
        {
            _mockEventSource = new Mock<BenchmarksEventSource>("TestEventSource");
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.Measure(string, long)"/> method to ensure it correctly logs a long value.
        /// </summary>
        [TestMethod]
        public void Measure_WhenCalledWithLongValue_LogsCorrectly()
        {
            // Arrange
            string name = "TestLong";
            long value = 12345;

            // Act
            BenchmarksEventSource.Measure(name, value);

            // Assert
            _mockEventSource.Verify(m => m.MeasureLong(name, value), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.Measure(string, double)"/> method to ensure it correctly logs a double value.
        /// </summary>
        [TestMethod]
        public void Measure_WhenCalledWithDoubleValue_LogsCorrectly()
        {
            // Arrange
            string name = "TestDouble";
            double value = 123.45;

            // Act
            BenchmarksEventSource.Measure(name, value);

            // Assert
            _mockEventSource.Verify(m => m.MeasureDouble(name, value), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.Measure(string, string)"/> method to ensure it correctly logs a string value.
        /// </summary>
        [TestMethod]
        public void Measure_WhenCalledWithStringValue_LogsCorrectly()
        {
            // Arrange
            string name = "TestString";
            string value = "TestValue";

            // Act
            BenchmarksEventSource.Measure(name, value);

            // Assert
            _mockEventSource.Verify(m => m.MeasureString(name, value), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.MeasureAndRegister(string, string, string)"/> method to ensure it correctly registers and logs a string value.
        /// </summary>
        [TestMethod]
        public void MeasureAndRegister_WhenCalled_RegistersAndLogsCorrectly()
        {
            // Arrange
            string name = "TestMeasure";
            string value = "TestValue";
            string description = "TestDescription";

            // Act
            BenchmarksEventSource.MeasureAndRegister(name, value, description);

            // Assert
            _mockEventSource.Verify(m => m.Metadata(name, "First", "First", description, description, ""), Times.Once);
            _mockEventSource.Verify(m => m.MeasureString(name, value), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.SetChildProcessId(int)"/> method to ensure it correctly writes the process id to the console.
        /// </summary>
        [TestMethod]
        public void SetChildProcessId_WhenCalled_WritesToConsole()
        {
            // Arrange
            int pid = 1234;
            using var consoleOutput = new ConsoleOutput();

            // Act
            BenchmarksEventSource.SetChildProcessId(pid);

            // Assert
            Assert.IsTrue(consoleOutput.GetOutput().Contains($"##ChildProcessId:{pid}"));
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.Register(string, Operations, Operations, string, string, string)"/> method to ensure it correctly registers metadata.
        /// </summary>
        [TestMethod]
        public void Register_WhenCalled_RegistersMetadataCorrectly()
        {
            // Arrange
            string name = "TestRegister";
            Operations aggregate = Operations.First;
            Operations reduce = Operations.First;
            string shortDescription = "ShortDescription";
            string longDescription = "LongDescription";
            string format = "n2";

            // Act
            BenchmarksEventSource.Register(name, aggregate, reduce, shortDescription, longDescription, format);

            // Assert
            _mockEventSource.Verify(m => m.Metadata(name, aggregate.ToString(), reduce.ToString(), shortDescription, longDescription, format), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.MeasureAspNetVersion"/> method to ensure it correctly measures the ASP.NET Core version.
        /// </summary>
        [TestMethod]
        public void MeasureAspNetVersion_WhenCalled_MeasuresCorrectly()
        {
            // Arrange
            var aspnetCoreVersion = "5.0.0";
            var mockType = new Mock<Type>();
            mockType.Setup(t => t.GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>())
                    .Returns(new AssemblyInformationalVersionAttribute(aspnetCoreVersion));

            // Act
            BenchmarksEventSource.MeasureAspNetVersion();

            // Assert
            _mockEventSource.Verify(m => m.MeasureAndRegister("AspNetCoreVersion", aspnetCoreVersion, "ASP.NET Core Version"), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.MeasureNetCoreAppVersion"/> method to ensure it correctly measures the .NET Core version.
        /// </summary>
        [TestMethod]
        public void MeasureNetCoreAppVersion_WhenCalled_MeasuresCorrectly()
        {
            // Arrange
            var netCoreAppVersion = "5.0.0";
            var mockType = new Mock<Type>();
            mockType.Setup(t => t.GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>())
                    .Returns(new AssemblyInformationalVersionAttribute(netCoreAppVersion));

            // Act
            BenchmarksEventSource.MeasureNetCoreAppVersion();

            // Assert
            _mockEventSource.Verify(m => m.MeasureAndRegister("NetCoreAppVersion", netCoreAppVersion, ".NET Runtime Version"), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="BenchmarksEventSource.Start"/> method to ensure it correctly logs the start event.
        /// </summary>
        [TestMethod]
        public void Start_WhenCalled_LogsStartEvent()
        {
            // Act
            BenchmarksEventSource.Start();

            // Assert
            _mockEventSource.Verify(m => m.Started(), Times.Once);
        }
    }

    /// <summary>
    /// Helper class to capture console output.
    /// </summary>
    public class ConsoleOutput : IDisposable
    {
        private readonly System.IO.StringWriter _stringWriter;
        private readonly System.IO.TextWriter _originalOutput;

        public ConsoleOutput()
        {
            _stringWriter = new System.IO.StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_stringWriter);
        }

        public string GetOutput()
        {
            return _stringWriter.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            _stringWriter.Dispose();
        }
    }
}

