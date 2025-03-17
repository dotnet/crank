using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CGroup"/> class.
    /// </summary>
    [TestClass]
    public class CGroupTests
    {
        private readonly Mock<Job> _mockJob;

        public CGroupTests()
        {
            _mockJob = new Mock<Job>();
        }

        /// <summary>
        /// Tests the <see cref="CGroup.GetCGroupController(Job)"/> method to ensure it returns the correct controller string.
        /// </summary>
        [TestMethod]
        public void GetCGroupController_ValidJob_ReturnsCorrectController()
        {
            // Arrange
            var job = new Job { Id = "test-job-id" };
            var expectedController = $"benchmarks-{Environment.ProcessId}-test-job-id";

            // Act
            var actualController = CGroup.GetCGroupController(job);

            // Assert
            Assert.AreEqual(expectedController, actualController);
        }

        /// <summary>
        /// Tests the <see cref="CGroup.GetCGroupVersionAsync"/> method to ensure it returns the correct CGroup version.
        /// </summary>
        [TestMethod]
        public async Task GetCGroupVersionAsync_CGroupV2_ReturnsCGroupV2()
        {
            // Arrange
            var mockProcessUtil = new Mock<IProcessUtil>();
            mockProcessUtil.Setup(p => p.RunAsync("stat", "-fc %T /sys/fs/cgroup/", true, true))
                .ReturnsAsync(new ProcessResult { StandardOutput = "cgroup2fs" });

            // Act
            var cgroup = await CGroup.GetCGroupVersionAsync();

            // Assert
            Assert.IsInstanceOfType(cgroup, typeof(CGroupV2));
        }

        /// <summary>
        /// Tests the <see cref="CGroup.GetCGroupVersionAsync"/> method to ensure it returns the correct CGroup version.
        /// </summary>
        [TestMethod]
        public async Task GetCGroupVersionAsync_CGroupV1_ReturnsCGroupV1()
        {
            // Arrange
            var mockProcessUtil = new Mock<IProcessUtil>();
            mockProcessUtil.Setup(p => p.RunAsync("stat", "-fc %T /sys/fs/cgroup/", true, true))
                .ReturnsAsync(new ProcessResult { StandardOutput = "tmpfs" });

            // Act
            var cgroup = await CGroup.GetCGroupVersionAsync();

            // Assert
            Assert.IsInstanceOfType(cgroup, typeof(CGroupV1));
        }

        /// <summary>
        /// Tests the <see cref="CGroup.DeleteAsync(Job)"/> method to ensure it runs the correct command.
        /// </summary>
        [TestMethod]
        public async Task DeleteAsync_ValidJob_RunsCorrectCommand()
        {
            // Arrange
            var job = new Job { Id = "test-job-id" };
            var mockProcessUtil = new Mock<IProcessUtil>();
            var cgroup = new Mock<CGroup> { CallBase = true };

            cgroup.Setup(c => c.GetCGroupController(job)).Returns("benchmarks-1234-test-job-id");
            mockProcessUtil.Setup(p => p.RunAsync("cgdelete", "cpu,memory,cpuset:benchmarks-1234-test-job-id", true, false))
                .Returns(Task.CompletedTask);

            // Act
            await cgroup.Object.DeleteAsync(job);

            // Assert
            mockProcessUtil.Verify(p => p.RunAsync("cgdelete", "cpu,memory,cpuset:benchmarks-1234-test-job-id", true, false), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CGroup.CreateAsync(Job)"/> method to ensure it creates the cgroup and returns the correct command.
        /// </summary>
        [TestMethod]
        public async Task CreateAsync_ValidJob_ReturnsCorrectCommand()
        {
            // Arrange
            var job = new Job { Id = "test-job-id" };
            var mockProcessUtil = new Mock<IProcessUtil>();
            var cgroup = new Mock<CGroup> { CallBase = true };

            cgroup.Setup(c => c.GetCGroupController(job)).Returns("benchmarks-1234-test-job-id");
            mockProcessUtil.Setup(p => p.RunAsync("cgcreate", "-g memory,cpu,cpuset:benchmarks-1234-test-job-id", true))
                .ReturnsAsync(new ProcessResult { ExitCode = 0 });
            cgroup.Setup(c => c.SetAsync(job)).Returns(Task.CompletedTask);

            // Act
            var (executable, commandLine) = await cgroup.Object.CreateAsync(job);

            // Assert
            Assert.AreEqual("cgexec", executable);
            Assert.AreEqual("-g memory,cpu,cpuset:benchmarks-1234-test-job-id", commandLine);
        }

        /// <summary>
        /// Tests the <see cref="CGroup.CreateAsync(Job)"/> method to ensure it handles errors correctly.
        /// </summary>
        [TestMethod]
        public async Task CreateAsync_CGroupCreationFails_ReturnsNull()
        {
            // Arrange
            var job = new Job { Id = "test-job-id" };
            var mockProcessUtil = new Mock<IProcessUtil>();
            var cgroup = new Mock<CGroup> { CallBase = true };

            cgroup.Setup(c => c.GetCGroupController(job)).Returns("benchmarks-1234-test-job-id");
            mockProcessUtil.Setup(p => p.RunAsync("cgcreate", "-g memory,cpu,cpuset:benchmarks-1234-test-job-id", true))
                .ReturnsAsync(new ProcessResult { ExitCode = 1 });

            // Act
            var (executable, commandLine) = await cgroup.Object.CreateAsync(job);

            // Assert
            Assert.IsNull(executable);
            Assert.IsNull(commandLine);
            Assert.IsTrue(job.Error.Contains("Could not create cgroup"));
        }
    }
}

