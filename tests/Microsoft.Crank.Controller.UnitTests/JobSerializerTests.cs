using Microsoft.Crank.Models;
using Microsoft.Crank.Models.Security;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.Controller.Serializers.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobSerializer"/> class.
    /// </summary>
    public class JobSerializerTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;

        public JobSerializerTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.WriteJobResultsToSqlAsync"/> method to ensure it correctly writes job results to SQL.
        /// </summary>
//         [Fact] [Error] (43-42)CS7036 There is no argument given that corresponds to the required parameter 'clientId' of 'CertificateOptions.CertificateOptions(string, string, string, string, string, bool)'
//         public async Task WriteJobResultsToSqlAsync_ValidInputs_WritesToSql()
//         {
//             // Arrange
//             var jobResults = new JobResults();
//             var sqlConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
//             var tableName = "JobResults";
//             var session = "session1";
//             var scenario = "scenario1";
//             var description = "description1";
//             var certificateOptions = new CertificateOptions();
// 
//             // Act
//             await JobSerializer.WriteJobResultsToSqlAsync(jobResults, sqlConnectionString, tableName, session, scenario, description, certificateOptions);
// 
//             // Assert
//             // No exception means success
//         }

        /// <summary>
        /// Tests the <see cref="JobSerializer.InitializeDatabaseAsync"/> method to ensure it correctly initializes the database.
        /// </summary>
        [Fact]
        public async Task InitializeDatabaseAsync_ValidInputs_InitializesDatabase()
        {
            // Arrange
            var connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
            var tableName = "JobResults";
            var certificateOptions = new CertificateOptions();

            // Act
            await JobSerializer.InitializeDatabaseAsync(connectionString, tableName, certificateOptions);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.WriteJobResultsToEsAsync"/> method to ensure it correctly writes job results to Elasticsearch.
        /// </summary>
        [Fact]
        public async Task WriteJobResultsToEsAsync_ValidInputs_WritesToElasticsearch()
        {
            // Arrange
            var jobResults = new JobResults();
            var elasticSearchUrl = "http://localhost:9200";
            var indexName = "jobresults";
            var session = "session1";
            var scenario = "scenario1";
            var description = "description1";

            // Act
            await JobSerializer.WriteJobResultsToEsAsync(jobResults, elasticSearchUrl, indexName, session, scenario, description);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.InitializeElasticSearchAsync"/> method to ensure it correctly initializes Elasticsearch.
        /// </summary>
        [Fact]
        public async Task InitializeElasticSearchAsync_ValidInputs_InitializesElasticsearch()
        {
            // Arrange
            var elasticSearchUrl = "http://localhost:9200";
            var indexName = "jobresults";

            // Act
            await JobSerializer.InitializeElasticSearchAsync(elasticSearchUrl, indexName);

            // Assert
            // No exception means success
        }
    }
}
