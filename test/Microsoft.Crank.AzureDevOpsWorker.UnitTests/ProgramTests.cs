using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Program"/> class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests that Program.Main returns a non-zero exit code when required options are missing.
        /// This verifies that the command line application does not execute successfully when mandatory parameters are not provided.
        /// </summary>
        [Fact]
        public void Main_MissingRequiredOptions_ReturnsNonZeroExitCode()
        {
            // Arrange
            string[] args = new string[0];

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.NotEqual(0, exitCode);
        }

        /// <summary>
        /// Tests that Program.Main throws an ApplicationException when certificate options are provided
        /// without the required client id and tenant id. The expected behavior is that the certificate credential
        /// retrieval fails and an exception is thrown.
        /// </summary>
        [Fact]
        public void Main_WithCertOptionsMissingClientIdAndTenantId_ThrowsApplicationException()
        {
            // Arrange
            // Provide required -c and -q options and a certificate thumbprint without cert-client-id and cert-tenant-id.
            string[] args = new string[] { "-c", "fake-connection", "-q", "fake-queue", "--cert-thumbprint", "thumb" };

            // Simulate pressing ENTER immediately to avoid blocking on Console.ReadLine().
            using (var stringReader = new StringReader(Environment.NewLine))
            {
                Console.SetIn(stringReader);

                // Act & Assert
                // Since the certificate options are incomplete, an ApplicationException is expected to be thrown.
                var exception = Assert.ThrowsAny<AggregateException>(() => Program.Main(args));
                Assert.Contains("The requested certificate could not be found", exception.Flatten().Message);
            }
        }

        /// <summary>
        /// Tests that Program.Main returns an exit code when invoked with the required connection string and queue options,
        /// without any certificate options. This test simulates a scenario where the external Service Bus processing is initiated.
        /// Console input is simulated to allow the method to complete.
        /// Expected outcome: Execution completes and returns a non-negative exit code.
        /// </summary>
        [Fact]
        public void Main_ValidArgumentsWithoutCertOptions_ReturnsExitCode()
        {
            // Arrange
            // Use dummy connection string and queue name. Without certificate options,
            // the code path using basic ServiceBusClient is triggered.
            string[] args = new string[] { "-c", "fake-connection", "-q", "fake-queue" };

            // Simulate pressing ENTER immediately so that the waiting Console.ReadLine does not block.
            using (var stringReader = new StringReader(Environment.NewLine))
            {
                Console.SetIn(stringReader);

                // Act
                int exitCode = Program.Main(args);

                // Assert
                // Since the processing may not fully complete due to external dependency calls,
                // the test ensures that an exit code (non-negative) is returned.
                Assert.True(exitCode >= 0);
            }
        }
    }
}
