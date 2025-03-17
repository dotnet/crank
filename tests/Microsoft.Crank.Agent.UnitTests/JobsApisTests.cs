using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Repository;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobsApis"/> class.
    /// </summary>
    [TestClass]
    public class JobsApisTests
    {
        private readonly Mock<IJobRepository> _jobRepositoryMock;
        private readonly Mock<HttpContext> _httpContextMock;
        private readonly Mock<HttpRequest> _httpRequestMock;
        private readonly Mock<HttpResponse> _httpResponseMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;

        public JobsApisTests()
        {
            _jobRepositoryMock = new Mock<IJobRepository>();
            _httpContextMock = new Mock<HttpContext>();
            _httpRequestMock = new Mock<HttpRequest>();
            _httpResponseMock = new Mock<HttpResponse>();
            _serviceProviderMock = new Mock<IServiceProvider>();

            _httpContextMock.SetupGet(c => c.Request).Returns(_httpRequestMock.Object);
            _httpContextMock.SetupGet(c => c.Response).Returns(_httpResponseMock.Object);
            _httpContextMock.SetupGet(c => c.RequestServices).Returns(_serviceProviderMock.Object);
        }

        /// <summary>
        /// Tests the <see cref="JobsApis.GetState(HttpContext)"/> method to ensure it returns 404 when the job is not found.
        /// </summary>
        [TestMethod]
        public async Task GetState_JobNotFound_Returns404()
        {
            // Arrange
            _httpRequestMock.Setup(r => r.RouteValues["id"]).Returns("1");
            _serviceProviderMock.Setup(s => s.GetService(typeof(IJobRepository))).Returns(_jobRepositoryMock.Object);
            _jobRepositoryMock.Setup(r => r.Find(It.IsAny<int>())).Returns((Job)null);

            // Act
            await JobsApis.GetState(_httpContextMock.Object);

            // Assert
            _httpResponseMock.VerifySet(r => r.StatusCode = 404);
        }

        /// <summary>
        /// Tests the <see cref="JobsApis.GetState(HttpContext)"/> method to ensure it returns the job state when the job is found.
        /// </summary>
        [TestMethod]
        public async Task GetState_JobFound_ReturnsJobState()
        {
            // Arrange
            var job = new Job { State = JobState.Running };
            _httpRequestMock.Setup(r => r.RouteValues["id"]).Returns("1");
            _serviceProviderMock.Setup(s => s.GetService(typeof(IJobRepository))).Returns(_jobRepositoryMock.Object);
            _jobRepositoryMock.Setup(r => r.Find(It.IsAny<int>())).Returns(job);
            _httpResponseMock.Setup(r => r.WriteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await JobsApis.GetState(_httpContextMock.Object);

            // Assert
            _httpResponseMock.VerifySet(r => r.StatusCode = 200);
            _httpResponseMock.Verify(r => r.WriteAsync(job.State.ToString()));
        }

        /// <summary>
        /// Tests the <see cref="JobsApis.GetTouch(HttpContext)"/> method to ensure it returns 404 when the job is not found.
        /// </summary>
        [TestMethod]
        public async Task GetTouch_JobNotFound_Returns404()
        {
            // Arrange
            _httpRequestMock.Setup(r => r.RouteValues["id"]).Returns("1");
            _serviceProviderMock.Setup(s => s.GetService(typeof(IJobRepository))).Returns(_jobRepositoryMock.Object);
            _jobRepositoryMock.Setup(r => r.Find(It.IsAny<int>())).Returns((Job)null);

            // Act
            await JobsApis.GetTouch(_httpContextMock.Object);

            // Assert
            _httpResponseMock.VerifySet(r => r.StatusCode = 404);
        }

        /// <summary>
        /// Tests the <see cref="JobsApis.GetTouch(HttpContext)"/> method to ensure it returns 200 when the job is found.
        /// </summary>
        [TestMethod]
        public async Task GetTouch_JobFound_Returns200()
        {
            // Arrange
            var job = new Job { State = JobState.Running };
            _httpRequestMock.Setup(r => r.RouteValues["id"]).Returns("1");
            _serviceProviderMock.Setup(s => s.GetService(typeof(IJobRepository))).Returns(_jobRepositoryMock.Object);
            _jobRepositoryMock.Setup(r => r.Find(It.IsAny<int>())).Returns(job);

            // Act
            await JobsApis.GetTouch(_httpContextMock.Object);

            // Assert
            _httpResponseMock.VerifySet(r => r.StatusCode = 200);
        }
    }
}

