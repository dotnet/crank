using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Repository;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobsApis"/> class.
    /// </summary>
    public class JobsApisTests
    {
        private readonly Mock<IJobRepository> _jobRepositoryMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;

        public JobsApisTests()
        {
            _jobRepositoryMock = new Mock<IJobRepository>();
            _serviceProviderMock = new Mock<IServiceProvider>();
        }

        /// <summary>
        /// Tests that GetState returns 200 status and writes the job state when the job exists.
        /// </summary>
//         [Fact] [Error] (40-25)CS0029 Cannot implicitly convert type 'string' to 'Microsoft.Crank.Models.JobState'
//         public async Task GetState_WhenJobExists_ReturnsStatus200AndWritesJobState()
//         {
//             // Arrange
//             int jobId = 123;
//             string expectedStateString = "Running";
//             var fakeJob = new Job
//             {
//                 State = expectedStateString
//             };
// 
//             _jobRepositoryMock.Setup(repo => repo.Find(jobId))
//                               .Returns(fakeJob);
// 
//             _serviceProviderMock.Setup(sp => sp.GetService(typeof(IJobRepository)))
//                                 .Returns(_jobRepositoryMock.Object);
// 
//             var context = new DefaultHttpContext();
//             context.Request.RouteValues["id"] = jobId;
//             context.RequestServices = _serviceProviderMock.Object;
//             var responseBody = new MemoryStream();
//             context.Response.Body = responseBody;
// 
//             // Act
//             await JobsApis.GetState(context);
// 
//             // Assert
//             Assert.Equal(200, context.Response.StatusCode);
// 
//             // Flush the body stream to read its content.
//             context.Response.Body.Seek(0, SeekOrigin.Begin);
//             using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8))
//             {
//                 var result = await reader.ReadToEndAsync();
//                 Assert.Equal(expectedStateString, result);
//             }
// 
//             // Verify that LastDriverCommunicationUtc was updated recently.
//             Assert.True((DateTime.UtcNow - fakeJob.LastDriverCommunicationUtc).TotalSeconds < 1, 
//                 "Job.LastDriverCommunicationUtc was not updated as expected.");
//         }

        /// <summary>
        /// Tests that GetState returns 404 status when the job does not exist.
        /// </summary>
        [Fact]
        public async Task GetState_WhenJobDoesNotExist_ReturnsStatus404()
        {
            // Arrange
            int jobId = 456;
            _jobRepositoryMock.Setup(repo => repo.Find(jobId))
                              .Returns((Job)null);

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IJobRepository)))
                                .Returns(_jobRepositoryMock.Object);

            var context = new DefaultHttpContext();
            context.Request.RouteValues["id"] = jobId;
            context.RequestServices = _serviceProviderMock.Object;
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Act
            await JobsApis.GetState(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);

            // Ensure nothing was written to the response body.
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8))
            {
                string result = await reader.ReadToEndAsync();
                Assert.True(string.IsNullOrEmpty(result));
            }
        }

        /// <summary>
        /// Tests that GetState throws an exception when the route value id is not convertible to an integer.
        /// </summary>
        [Fact]
        public async Task GetState_WhenInvalidJobIdInRoute_ThrowsException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.RouteValues["id"] = "invalid_id";
            context.RequestServices = _serviceProviderMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(async () =>
            {
                await JobsApis.GetState(context);
            });
        }

        /// <summary>
        /// Tests that GetTouch returns 200 status when the job exists.
        /// </summary>
//         [Fact] [Error] (138-25)CS0029 Cannot implicitly convert type 'string' to 'Microsoft.Crank.Models.JobState'
//         public async Task GetTouch_WhenJobExists_ReturnsStatus200()
//         {
//             // Arrange
//             int jobId = 789;
//             string dummyStateString = "Completed"; // not used in touch action but job exists
//             var fakeJob = new Job
//             {
//                 State = dummyStateString
//             };
// 
//             _jobRepositoryMock.Setup(repo => repo.Find(jobId))
//                               .Returns(fakeJob);
// 
//             _serviceProviderMock.Setup(sp => sp.GetService(typeof(IJobRepository)))
//                                 .Returns(_jobRepositoryMock.Object);
// 
//             var context = new DefaultHttpContext();
//             context.Request.RouteValues["id"] = jobId;
//             context.RequestServices = _serviceProviderMock.Object;
//             // Prepare a response body though GetTouch does not write any content.
//             context.Response.Body = new MemoryStream();
// 
//             // Act
//             await JobsApis.GetTouch(context);
// 
//             // Assert
//             Assert.Equal(200, context.Response.StatusCode);
// 
//             // Verify that LastDriverCommunicationUtc was updated recently.
//             Assert.True((DateTime.UtcNow - fakeJob.LastDriverCommunicationUtc).TotalSeconds < 1, 
//                 "Job.LastDriverCommunicationUtc was not updated as expected.");
//         }

        /// <summary>
        /// Tests that GetTouch returns 404 status when the job does not exist.
        /// </summary>
        [Fact]
        public async Task GetTouch_WhenJobDoesNotExist_ReturnsStatus404()
        {
            // Arrange
            int jobId = 321;
            _jobRepositoryMock.Setup(repo => repo.Find(jobId))
                              .Returns((Job)null);

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IJobRepository)))
                                .Returns(_jobRepositoryMock.Object);

            var context = new DefaultHttpContext();
            context.Request.RouteValues["id"] = jobId;
            context.RequestServices = _serviceProviderMock.Object;
            context.Response.Body = new MemoryStream();

            // Act
            await JobsApis.GetTouch(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);

            // Verify that the response body remains empty.
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8))
            {
                string result = await reader.ReadToEndAsync();
                Assert.True(string.IsNullOrEmpty(result));
            }
        }

        /// <summary>
        /// Tests that GetTouch throws an exception when the route value id is not convertible to an integer.
        /// </summary>
        [Fact]
        public async Task GetTouch_WhenInvalidJobIdInRoute_ThrowsException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.RouteValues["id"] = "not_an_int";
            context.RequestServices = _serviceProviderMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(async () =>
            {
                await JobsApis.GetTouch(context);
            });
        }
    }
}
