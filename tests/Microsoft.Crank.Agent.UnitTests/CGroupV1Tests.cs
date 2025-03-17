using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CGroupV1"/> class.
    /// </summary>
    [TestClass]
    public class CGroupV1Tests
    {
        private readonly CGroupV1 _cGroupV1;

        public CGroupV1Tests()
        {
            _cGroupV1 = new CGroupV1();
        }

        /// <summary>
        /// Tests the <see cref="CGroupV1.SetAsync(Job)"/> method to ensure it sets the memory limit correctly.
        /// </summary>
//         [TestMethod] [Error] (32-44)CS0176 Member 'CGroup.GetCGroupController(Job)' cannot be accessed with an instance reference; qualify it with a type name instead
//         public async Task SetAsync_WhenMemoryLimitIsSet_SetsMemoryLimit()
//         {
//             // Arrange
//             var job = new Job { MemoryLimitInBytes = 1024 };
//             var controller = "testController";
//             Mock.Get(_cGroupV1).Setup(c => c.GetCGroupController(job)).Returns(controller);
// 
//             // Act
//             await _cGroupV1.SetAsync(job);
// 
//             // Assert
//             Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r memory.limit_in_bytes=1024 {controller}", true), Times.Once);
//         }

        /// <summary>
        /// Tests the <see cref="CGroupV1.SetAsync(Job)"/> method to ensure it sets the CPU limit correctly.
        /// </summary>
        [TestMethod]
        public async Task SetAsync_WhenCpuLimitIsSet_SetsCpuLimit()
        {
            // Arrange
            var job = new Job { CpuLimitRatio = 0.5 };
            var controller = "testController";
            Mock.Get(_cGroupV1).Setup(c => c.GetCGroupController(job)).Returns(controller);

            // Act
            await _cGroupV1.SetAsync(job);

            // Assert
            Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r cpu.cfs_period_us={CGroupV1.DefaultDockerCfsPeriod} {controller}", true), Times.Once);
            Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r cpu.cfs_quota_us={Math.Floor(0.5 * CGroupV1.DefaultDockerCfsPeriod)} {controller}", true), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CGroupV1.SetAsync(Job)"/> method to ensure it sets the CPU set correctly.
        /// </summary>
        [TestMethod]
        public async Task SetAsync_WhenCpuSetIsSet_SetsCpuSet()
        {
            // Arrange
            var job = new Job { CpuSet = "0-3" };
            var controller = "testController";
            Mock.Get(_cGroupV1).Setup(c => c.GetCGroupController(job)).Returns(controller);

            // Act
            await _cGroupV1.SetAsync(job);

            // Assert
            Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r cpuset.cpus=0-3 {controller}", true), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CGroupV1.GetCpuStatAsync(Job)"/> method to ensure it retrieves the CPU stats correctly.
        /// </summary>
        [TestMethod]
        public async Task GetCpuStatAsync_WhenCalled_ReturnsCpuStats()
        {
            // Arrange
            var job = new Job();
            var controller = "testController";
            var expectedOutput = "cpu stats";
            Mock.Get(_cGroupV1).Setup(c => c.GetCGroupController(job)).Returns(controller);
            Mock.Get(ProcessUtil).Setup(p => p.RunAsync("cat", $"/sys/fs/cgroup/cpu/{controller}/cpu.stat", false, true)).ReturnsAsync(new ProcessResult { StandardOutput = expectedOutput });

            // Act
            var result = await _cGroupV1.GetCpuStatAsync(job);

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }
    }
}
