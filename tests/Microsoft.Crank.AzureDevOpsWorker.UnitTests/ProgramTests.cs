using McMaster.Extensions.CommandLineUtils;
using Microsoft.Crank.Models.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    [TestClass]
    public class ProgramTests
    {
        private readonly Mock<CommandLineApplication> _mockApp;
        private readonly Mock<CommandOption> _mockConnectionStringOption;
        private readonly Mock<CommandOption> _mockQueueOption;
        private readonly Mock<CommandOption> _mockCertClientIdOption;
        private readonly Mock<CommandOption> _mockCertTenantIdOption;
        private readonly Mock<CommandOption> _mockCertThumbprintOption;
        private readonly Mock<CommandOption> _mockCertPathOption;
        private readonly Mock<CommandOption> _mockCertPasswordOption;
        private readonly Mock<CommandOption> _mockCertSniAuthOption;
        private readonly Mock<CommandOption> _mockVerboseOption;

        public ProgramTests()
        {
            _mockApp = new Mock<CommandLineApplication>();
            _mockConnectionStringOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockQueueOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertClientIdOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertTenantIdOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertThumbprintOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertPathOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertPasswordOption = new Mock<CommandOption>(null, CommandOptionType.SingleValue);
            _mockCertSniAuthOption = new Mock<CommandOption>(null, CommandOptionType.NoValue);
            _mockVerboseOption = new Mock<CommandOption>(null, CommandOptionType.NoValue);
        }

        /// <summary>
        /// Tests the <see cref="Program.Main(string[])"/> method to ensure it correctly sets up the command line application and executes it.
        /// </summary>
        [TestMethod]
        public void Main_ValidArguments_ExecutesSuccessfully()
        {
            // Arrange
            var args = new[] { "-c", "connectionString", "-q", "queue" };
            _mockApp.Setup(app => app.Execute(args)).Returns(0);

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests the <see cref="Program.ProcessAzureQueue(string, string, CertificateOptions)"/> method to ensure it correctly processes the Azure queue.
        /// </summary>
        [TestMethod]
        public async Task ProcessAzureQueue_ValidConnectionStringAndQueue_ProcessesSuccessfully()
        {
            // Arrange
            var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testKey";
            var queue = "test-queue";
            var certificateOptions = (CertificateOptions)null;

            var mockClient = new Mock<ServiceBusClient>(MockBehavior.Strict, connectionString);
            var mockProcessor = new Mock<ServiceBusProcessor>(MockBehavior.Strict);

            mockClient.Setup(client => client.CreateProcessor(queue, It.IsAny<ServiceBusProcessorOptions>())).Returns(mockProcessor.Object);
            mockProcessor.Setup(processor => processor.StartProcessingAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            await Program.ProcessAzureQueue(connectionString, queue, certificateOptions);

            // Assert
            mockProcessor.Verify(processor => processor.StartProcessingAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="Program.MessageHandler(ProcessMessageEventArgs)"/> method to ensure it correctly handles a message.
        /// </summary>
        [TestMethod]
        public async Task MessageHandler_ValidMessage_ProcessesSuccessfully()
        {
            // Arrange
            var mockArgs = new Mock<ProcessMessageEventArgs>(MockBehavior.Strict, null, null, null, null, null, null);
            var mockMessage = new Mock<ServiceBusReceivedMessage>(MockBehavior.Strict);
            mockArgs.Setup(args => args.Message).Returns(mockMessage.Object);

            // Act
            await Program.MessageHandler(mockArgs.Object);

            // Assert
            mockArgs.Verify(args => args.CompleteMessageAsync(mockMessage.Object, It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="Program.ErrorHandler(ProcessErrorEventArgs)"/> method to ensure it correctly handles an error.
        /// </summary>
        [TestMethod]
        public async Task ErrorHandler_ValidError_HandlesSuccessfully()
        {
            // Arrange
            var mockArgs = new Mock<ProcessErrorEventArgs>(MockBehavior.Strict, null, null, null, null, null, null);
            var exception = new Exception("Test exception");
            mockArgs.Setup(args => args.Exception).Returns(exception);

            // Act
            await Program.ErrorHandler(mockArgs.Object);

            // Assert
            mockArgs.Verify(args => args.Exception, Times.Once);
        }
    }
}

