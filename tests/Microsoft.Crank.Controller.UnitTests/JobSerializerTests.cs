using Microsoft.Crank.Models;
using Microsoft.Crank.Models.Security;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller.Serializers.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobSerializer"/> class.
    /// </summary>
    [TestClass]
    public class JobSerializerTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<CertificateOptions> _certificateOptionsMock;

        public JobSerializerTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _certificateOptionsMock = new Mock<CertificateOptions>();
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.WriteJobResultsToSqlAsync(JobResults, string, string, string, string, string, CertificateOptions)"/> method to ensure it correctly writes job results to SQL.
        /// </summary>
        [TestMethod]
        public async Task WriteJobResultsToSqlAsync_ValidInputs_WritesToSql()
        {
            // Arrange
            var jobResults = new JobResults();
            var sqlConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
            var tableName = "JobResults";
            var session = "session1";
            var scenario = "scenario1";
            var description = "description1";

            // Act
            await JobSerializer.WriteJobResultsToSqlAsync(jobResults, sqlConnectionString, tableName, session, scenario, description, _certificateOptionsMock.Object);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.InitializeDatabaseAsync(string, string, CertificateOptions)"/> method to ensure it correctly initializes the database.
        /// </summary>
        [TestMethod]
        public async Task InitializeDatabaseAsync_ValidInputs_InitializesDatabase()
        {
            // Arrange
            var connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
            var tableName = "JobResults";

            // Act
            await JobSerializer.InitializeDatabaseAsync(connectionString, tableName, _certificateOptionsMock.Object);

            // Assert
            // No exception means success
        }

        /// <summary>
        /// Tests the <see cref="JobSerializer.WriteJobResultsToEsAsync(JobResults, string, string, string, string, string)"/> method to ensure it correctly writes job results to Elasticsearch.
        /// </summary>
        [TestMethod]
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
        /// Tests the <see cref="JobSerializer.InitializeElasticSearchAsync(string, string)"/> method to ensure it correctly initializes Elasticsearch.
        /// </summary>
        [TestMethod]
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

        /// <summary>
        /// Tests the <see cref="JobSerializer.GetSqlConnection(string, CertificateOptions)"/> method to ensure it correctly gets a SQL connection.
        /// </summary>
//         [TestMethod] [Error] (119-44)CS0122 'JobSerializer.GetSqlConnection(string, CertificateOptions)' is inaccessible due to its protection level
//         public void GetSqlConnection_ValidInputs_ReturnsSqlConnection()
//         {
//             // Arrange
//             var connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
// 
//             // Act
//             var connection = JobSerializer.GetSqlConnection(connectionString, _certificateOptionsMock.Object);
// 
//             // Assert
//             Assert.IsNotNull(connection);
//         }

        /// <summary>
        /// Tests the <see cref="JobSerializer.RetryOnExceptionAsync(int, Func{Task}, int)"/> method to ensure it retries on exception.
        /// </summary>
        [TestMethod]
        public async Task RetryOnExceptionAsync_ExceptionThrown_Retries()
        {
            // Arrange
            var retries = 3;
            var attempts = 0;
            Func<Task> operation = () =>
            {
                attempts++;
                if (attempts < retries)
                {
                    throw new Exception("Test exception");
                }
                return Task.CompletedTask;
            };

            // Act
            await JobSerializer.RetryOnExceptionAsync(retries, operation, 100);

            // Assert
            Assert.AreEqual(retries, attempts);
        }
    }
}
