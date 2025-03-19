using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Crank.Controller.Serializers;
using Microsoft.Crank.Models;
using Microsoft.Crank.Models.Security;
using Moq;
using Xunit;

namespace Microsoft.Crank.Controller.Serializers.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobSerializer"/> class.
    /// </summary>
    public class JobSerializerTests
    {
        // Using a very short connection timeout to avoid long delays in tests.
        private const string InvalidSqlConnectionString = "Data Source=localhost;Initial Catalog=FakeDb;User Id=fake;Password=fake;Connection Timeout=1";
        private const string InvalidElasticSearchUrl = "http://localhost:1";
        private const string TableName = "TestTable";
        private const string Session = "TestSession";
        private const string Scenario = "TestScenario";
        private const string Description = "TestDescription";

        /// <summary>
        /// Tests that WriteJobResultsToSqlAsync throws an exception when provided with an invalid SQL connection string.
        /// This test invokes the method and expects an exception due to connection failure.
        /// </summary>
//         [Fact] [Error] (40-21)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.Controller.Serializers.UnitTests.JobResults' to 'Microsoft.Crank.Models.JobResults'
//         public async Task WriteJobResultsToSqlAsync_InvalidSqlConnection_ThrowsException()
//         {
//             // Arrange
//             var dummyJobResults = new JobResults();
//             CertificateOptions certificateOptions = null;
// 
//             // Act & Assert: Expect an exception due to invalid SQL connection (connection open failure)
//             await Assert.ThrowsAsync<Exception>(async () =>
//             {
//                 await JobSerializer.WriteJobResultsToSqlAsync(
//                     dummyJobResults,
//                     InvalidSqlConnectionString,
//                     TableName,
//                     Session,
//                     Scenario,
//                     Description,
//                     certificateOptions);
//             });
//         }

        /// <summary>
        /// Tests that WriteJobResultsToSqlAsync throws an ApplicationException when certificate options are provided
        /// but GetClientCertificateCredential returns null.
        /// </summary>
//         [Fact] [Error] (63-84)CS1503 Argument 1: cannot convert from 'object' to 'Azure.Identity.ClientCertificateCredential' [Error] (69-21)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.Controller.Serializers.UnitTests.JobResults' to 'Microsoft.Crank.Models.JobResults'
//         public async Task WriteJobResultsToSqlAsync_CertificateOptionsInvalid_ThrowsApplicationException()
//         {
//             // Arrange
//             var dummyJobResults = new JobResults();
//             // Create a mock for CertificateOptions. Assuming GetClientCertificateCredential is virtual.
//             var mockCertOptions = new Mock<CertificateOptions>();
//             // Set the Path property (if available) and setup GetClientCertificateCredential to return null.
//             mockCertOptions.SetupGet(x => x.Path).Returns("fakePath");
//             mockCertOptions.Setup(x => x.GetClientCertificateCredential()).Returns((object)null);
// 
//             // Act & Assert: Expect an ApplicationException due to missing certificate credential.
//             var exception = await Assert.ThrowsAsync<ApplicationException>(async () =>
//             {
//                 await JobSerializer.WriteJobResultsToSqlAsync(
//                     dummyJobResults,
//                     InvalidSqlConnectionString,
//                     TableName,
//                     Session,
//                     Scenario,
//                     Description,
//                     mockCertOptions.Object);
//             });
//             Assert.Contains("The requested certificate could not be found", exception.Message);
//         }

        /// <summary>
        /// Tests that InitializeDatabaseAsync throws an exception when provided with an invalid SQL connection string.
        /// This test invokes the method and expects an exception from the database initialization.
        /// </summary>
        [Fact]
        public async Task InitializeDatabaseAsync_InvalidSqlConnection_ThrowsException()
        {
            // Arrange
            CertificateOptions certificateOptions = null;

            // Act & Assert: Expect an exception due to inability to open SQL connection.
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await JobSerializer.InitializeDatabaseAsync(
                    InvalidSqlConnectionString,
                    TableName,
                    certificateOptions);
            });
        }

        /// <summary>
        /// Tests that WriteJobResultsToEsAsync throws an exception when provided with an invalid ElasticSearch URL.
        /// This test invokes the method and expects an exception from the HTTP call.
        /// </summary>
//         [Fact] [Error] (114-21)CS1503 Argument 1: cannot convert from 'Microsoft.Crank.Controller.Serializers.UnitTests.JobResults' to 'Microsoft.Crank.Models.JobResults'
//         public async Task WriteJobResultsToEsAsync_InvalidElasticSearchUrl_ThrowsException()
//         {
//             // Arrange
//             var dummyJobResults = new JobResults();
// 
//             // Act & Assert: Expect an exception due to HTTP failure when posting to ElasticSearch.
//             await Assert.ThrowsAsync<Exception>(async () =>
//             {
//                 await JobSerializer.WriteJobResultsToEsAsync(
//                     dummyJobResults,
//                     InvalidElasticSearchUrl,
//                     "TestIndex",
//                     Session,
//                     Scenario,
//                     Description);
//             });
//         }

        /// <summary>
        /// Tests that InitializeElasticSearchAsync throws an exception when provided with an invalid ElasticSearch URL.
        /// This test invokes the method and expects an exception from the HTTP call during initialization.
        /// </summary>
        [Fact]
        public async Task InitializeElasticSearchAsync_InvalidElasticSearchUrl_ThrowsException()
        {
            // Arrange
            string indexName = "TestIndex";

            // Act & Assert: Expect an exception because the HTTP HEAD request or PUT request will fail.
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await JobSerializer.InitializeElasticSearchAsync(InvalidElasticSearchUrl, indexName);
            });
        }
    }

    /// <summary>
    /// A stub implementation of JobResults for testing purposes.
    /// </summary>
    public class JobResults
    {
        // Add properties or methods if needed for serialization tests.
    }
}
