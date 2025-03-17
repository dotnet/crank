using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobResult"/> class.
    /// </summary>
    [TestClass]
    public class JobResultTests
    {
        private readonly Mock<IUrlHelper> _mockUrlHelper;
        private readonly Job _job;

        public JobResultTests()
        {
            _mockUrlHelper = new Mock<IUrlHelper>();
            _job = new Job
            {
                Id = 1,
                RunId = "run123",
                State = JobState.Running
            };
        }

        /// <summary>
        /// Tests the <see cref="JobResult.JobResult(Job, IUrlHelper)"/> constructor to ensure it correctly initializes properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_InitializesProperties()
        {
            // Arrange
            _mockUrlHelper.Setup(u => u.ActionLink("GetById", "Jobs", It.IsAny<object>())).Returns("detailsUrl");
            _mockUrlHelper.Setup(u => u.ActionLink("BuildLog", "Jobs", It.IsAny<object>())).Returns("buildLogsUrl");
            _mockUrlHelper.Setup(u => u.ActionLink("Output", "Jobs", It.IsAny<object>())).Returns("outputLogsUrl");

            // Act
            var jobResult = new JobResult(_job, _mockUrlHelper.Object);

            // Assert
            Assert.AreEqual(_job.Id, jobResult.Id);
            Assert.AreEqual(_job.RunId, jobResult.RunId);
            Assert.AreEqual(_job.State.ToString(), jobResult.State);
            Assert.AreEqual("detailsUrl", jobResult.DetailsUrl);
            Assert.AreEqual("buildLogsUrl", jobResult.BuildLogsUrl);
            Assert.AreEqual("outputLogsUrl", jobResult.OutputLogsUrl);
        }

        /// <summary>
        /// Tests the <see cref="JobResult.JobResult(Job, IUrlHelper)"/> constructor to ensure it throws an <see cref="ArgumentNullException"/> when job is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenJobIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            Job nullJob = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new JobResult(nullJob, _mockUrlHelper.Object));
        }

        /// <summary>
        /// Tests the <see cref="JobResult.JobResult(Job, IUrlHelper)"/> constructor to ensure it throws an <see cref="ArgumentNullException"/> when urlHelper is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenUrlHelperIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            IUrlHelper nullUrlHelper = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new JobResult(_job, nullUrlHelper));
        }
    }
}

