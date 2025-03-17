using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Job"/> class.
    /// </summary>
    [TestClass]
    public class JobTests
    {
        private readonly Job _job;

        public JobTests()
        {
            _job = new Job();
        }

        /// <summary>
        /// Tests the <see cref="Job.IsDocker"/> method to ensure it returns true when Docker properties are set.
        /// </summary>
        [TestMethod]
        public void IsDocker_WhenDockerPropertiesAreSet_ReturnsTrue()
        {
            // Arrange
            _job.DockerFile = "Dockerfile";

            // Act
            var result = _job.IsDocker();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests the <see cref="Job.IsDocker"/> method to ensure it returns false when Docker properties are not set.
        /// </summary>
        [TestMethod]
        public void IsDocker_WhenDockerPropertiesAreNotSet_ReturnsFalse()
        {
            // Act
            var result = _job.IsDocker();

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tests the <see cref="Job.GetNormalizedImageName"/> method to ensure it returns the correct image name when DockerPull is set.
        /// </summary>
        [TestMethod]
        public void GetNormalizedImageName_WhenDockerPullIsSet_ReturnsDockerPull()
        {
            // Arrange
            _job.DockerPull = "myimage";

            // Act
            var result = _job.GetNormalizedImageName();

            // Assert
            Assert.AreEqual("myimage", result);
        }

        /// <summary>
        /// Tests the <see cref="Job.GetNormalizedImageName"/> method to ensure it returns the correct image name when DockerLoad is set.
        /// </summary>
        [TestMethod]
        public void GetNormalizedImageName_WhenDockerLoadIsSet_ReturnsDockerImageName()
        {
            // Arrange
            _job.DockerLoad = "myload";
            _job.DockerImageName = "myimage";

            // Act
            var result = _job.GetNormalizedImageName();

            // Assert
            Assert.AreEqual("myimage", result);
        }

        /// <summary>
        /// Tests the <see cref="Job.GetNormalizedImageName"/> method to ensure it returns the correct image name when DockerImageName is set.
        /// </summary>
        [TestMethod]
        public void GetNormalizedImageName_WhenDockerImageNameIsSet_ReturnsBenchmarksPrefixedImageName()
        {
            // Arrange
            _job.DockerImageName = "myimage";

            // Act
            var result = _job.GetNormalizedImageName();

            // Assert
            Assert.AreEqual("benchmarks_myimage", result);
        }

        /// <summary>
        /// Tests the <see cref="Job.GetNormalizedImageName"/> method to ensure it returns the correct image name when DockerFile is set.
        /// </summary>
        [TestMethod]
        public void GetNormalizedImageName_WhenDockerFileIsSet_ReturnsBenchmarksPrefixedFileName()
        {
            // Arrange
            _job.DockerFile = "Dockerfile";

            // Act
            var result = _job.GetNormalizedImageName();

            // Assert
            Assert.AreEqual("benchmarks_dockerfile", result);
        }

        /// <summary>
        /// Tests the <see cref="Job.CalculateCpuList"/> method to ensure it returns the correct CPU list when CpuSet is set.
        /// </summary>
        [TestMethod]
        public void CalculateCpuList_WhenCpuSetIsSet_ReturnsCorrectCpuList()
        {
            // Arrange
            _job.CpuSet = "0-2,4";

            // Act
            var result = _job.CalculateCpuList();

            // Assert
            CollectionAssert.AreEqual(new List<int> { 0, 1, 2, 4 }, result);
        }

        /// <summary>
        /// Tests the <see cref="Job.CalculateCpuList"/> method to ensure it returns an empty list when CpuSet is not set.
        /// </summary>
        [TestMethod]
        public void CalculateCpuList_WhenCpuSetIsNotSet_ReturnsEmptyList()
        {
            // Act
            var result = _job.CalculateCpuList();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests the <see cref="Job.GetBuildKeyData"/> method to ensure it returns the correct BuildKeyData.
        /// </summary>
        [TestMethod]
        public void GetBuildKeyData_WhenCalled_ReturnsCorrectBuildKeyData()
        {
            // Arrange
            _job.Project = "MyProject";
            _job.RuntimeVersion = "1.0.0";

            // Act
            var result = _job.GetBuildKeyData();

            // Assert
            Assert.AreEqual("MyProject", result.Project);
            Assert.AreEqual("1.0.0", result.RuntimeVersion);
        }
    }
}
