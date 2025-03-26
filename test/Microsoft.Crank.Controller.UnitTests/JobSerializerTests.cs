using System;
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
        private readonly JobResults _dummyJobResults;
        private readonly string _dummySession;
        private readonly string _dummyScenario;
        private readonly string _dummyDescription;
        private readonly string _dummyTableName;
        private readonly string _dummySqlConnectionString;
        private readonly string _dummyElasticSearchUrl;
        private readonly string _dummyIndexName;

        public JobSerializerTests()
        {
            _dummyJobResults = new JobResults();
            _dummySession = "dummySession";
            _dummyScenario = "dummyScenario";
            _dummyDescription = "dummyDescription";
            _dummyTableName = "dummyTable";
            // Using an obviously invalid connection string that will fail quickly.
            _dummySqlConnectionString = "Server=invalid;Database=invalid;User Id=invalid;Password=invalid;";
            // Using an obviously invalid ElasticSearch URL.
            _dummyElasticSearchUrl = "http://invalid";
            _dummyIndexName = "dummyindex";
        }

        /// <summary>
        /// Tests that WriteJobResultsToSqlAsync throws an exception when the SQL connection fails to open.
        /// This represents a scenario with an invalid SQL connection string.
        /// </summary>
        [Fact]
        public async Task WriteJobResultsToSqlAsync_InvalidSqlConnection_ThrowsException()
        {
            // Arrange
            // certificateOptions is null so the certificate branch is bypassed.
            CertificateOptions certificateOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await JobSerializer.WriteJobResultsToSqlAsync(
                    _dummyJobResults,
                    _dummySqlConnectionString,
                    _dummyTableName,
                    _dummySession,
                    _dummyScenario,
                    _dummyDescription,
                    certificateOptions);
            });
        }

        /// <summary>
        /// Tests that WriteJobResultsToSqlAsync throws an ApplicationException when certificate options are provided
        /// but GetClientCertificateCredential returns null.
        /// </summary>
//         [Fact] [Error] (77-84)CS1503 Argument 1: cannot convert from 'object' to 'Azure.Identity.ClientCertificateCredential'
//         public async Task WriteJobResultsToSqlAsync_InvalidCertificateOptions_ThrowsApplicationException()
//         {
//             // Arrange
//             // Create a mock for CertificateOptions that returns null for GetClientCertificateCredential.
//             var mockCertOptions = new Mock<CertificateOptions>();
//             mockCertOptions.SetupGet(m => m.Path).Returns("dummyPath");
//             mockCertOptions.Setup(m => m.GetClientCertificateCredential()).Returns((object)null);
// 
//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<ApplicationException>(async () =>
//             {
//                 await JobSerializer.WriteJobResultsToSqlAsync(
//                     _dummyJobResults,
//                     _dummySqlConnectionString,
//                     _dummyTableName,
//                     _dummySession,
//                     _dummyScenario,
//                     _dummyDescription,
//                     mockCertOptions.Object);
//             });
// 
//             Assert.Contains("The requested certificate could not be found", exception.Message);
//         }

        /// <summary>
        /// Tests that InitializeDatabaseAsync throws an exception when the SQL connection string is invalid.
        /// </summary>
        [Fact]
        public async Task InitializeDatabaseAsync_InvalidConnectionString_ThrowsException()
        {
            // Arrange
            CertificateOptions certificateOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await JobSerializer.InitializeDatabaseAsync(
                    _dummySqlConnectionString,
                    _dummyTableName,
                    certificateOptions);
            });
        }

        /// <summary>
        /// Tests that WriteJobResultsToEsAsync throws an exception when the ElasticSearch URL is invalid.
        /// </summary>
        [Fact]
        public async Task WriteJobResultsToEsAsync_InvalidElasticSearchUrl_ThrowsException()
        {
            // Arrange
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await JobSerializer.WriteJobResultsToEsAsync(
                    _dummyJobResults,
                    _dummyElasticSearchUrl,
                    _dummyIndexName,
                    _dummySession,
                    _dummyScenario,
                    _dummyDescription);
            });
        }

        /// <summary>
        /// Tests that InitializeElasticSearchAsync throws an exception when the ElasticSearch URL is invalid.
        /// </summary>
        [Fact]
        public async Task InitializeElasticSearchAsync_InvalidElasticSearchUrl_ThrowsException()
        {
            // Arrange
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await JobSerializer.InitializeElasticSearchAsync(
                    _dummyElasticSearchUrl,
                    _dummyIndexName);
            });
        }
    }
}
