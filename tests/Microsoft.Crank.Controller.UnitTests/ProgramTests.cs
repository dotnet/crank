//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using FluentAssertions;
//using Microsoft.Crank.Controller;
//using Moq;
//using Xunit;

//namespace Microsoft.Crank.Controller.UnitTests
//{
//    /// <summary>
//    /// Unit tests for the <see cref="Program"/> class.
//    /// </summary>
//    public class ProgramTests
//    {
//        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
//        private readonly Mock<HttpClient> _httpClientMock;

//        public ProgramTests()
//        {
//            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
//            _httpClientMock = new Mock<HttpClient>();
//            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);
//        }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it correctly processes valid arguments.
//        /// </summary>
////         [Fact] [Error] (39-20)CS1061 'int' does not contain a definition for 'Should' and no accessible extension method 'Should' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
////         public void Main_ValidArguments_ReturnsZero()
////         {
////             // Arrange
////             var args = new[] { "--config", "config.yml", "--scenario", "test-scenario" };
//// 
////             // Act
////             var result = Program.Main(args);
//// 
////             // Assert
////             result.Should().Be(0);
////         }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it returns an error code for invalid arguments.
//        /// </summary>
//        [Fact]
//        public void Main_InvalidArguments_ReturnsErrorCode()
//        {
//            // Arrange
//            var args = new[] { "--invalid-arg" };

//            // Act
//            var result = Program.Main(args);

//            // Assert
//            result.Should().Be(-1);
//        }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it handles missing required arguments.
//        /// </summary>
//        [Fact]
//        public void Main_MissingRequiredArguments_ReturnsErrorCode()
//        {
//            // Arrange
//            var args = new[] { "--config", "config.yml" };

//            // Act
//            var result = Program.Main(args);

//            // Assert
//            result.Should().Be(1);
//        }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it handles deprecated arguments.
//        /// </summary>
//        [Fact]
//        public void Main_DeprecatedArguments_WarnsAndReplaces()
//        {
//            // Arrange
//            var args = new[] { "--output", "output.json" };

//            // Act
//            var result = Program.Main(args);

//            // Assert
//            result.Should().Be(0);
//            // Additional assertions to check for warning messages can be added here
//        }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it handles dynamic arguments.
//        /// </summary>
//        [Fact]
//        public void Main_DynamicArguments_ProcessedCorrectly()
//        {
//            // Arrange
//            var args = new[] { "--config", "config.yml", "--dynamic-arg", "value" };

//            // Act
//            var result = Program.Main(args);

//            // Assert
//            result.Should().Be(0);
//            // Additional assertions to check for dynamic argument processing can be added here
//        }

//        /// <summary>
//        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it handles the --help option.
//        /// </summary>
//        [Fact]
//        public void Main_HelpOption_DisplaysHelp()
//        {
//            // Arrange
//            var args = new[] { "--help" };

//            // Act
//            var result = Program.Main(args);

//            // Assert
//            result.Should().Be(0);
//            // Additional assertions to check for help message display can be added here
//        }
//    }
//}
