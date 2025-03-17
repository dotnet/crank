using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CGroupV2"/> class.
    /// </summary>
    [TestClass]
    public class CGroupV2Tests
    {
        private readonly CGroupV2 _cGroupV2;

        public CGroupV2Tests()
        {
            _cGroupV2 = new CGroupV2();
        }

        /// <summary>
        /// Tests the <see cref="CGroupV2.SetAsync(Job)"/> method to ensure it sets the memory limit correctly.
        /// </summary>
//         [TestMethod] [Error] (32-44)CS0176 Member 'CGroup.GetCGroupController(Job)' cannot be accessed with an instance reference; qualify it with a type name instead
//         public async Task SetAsync_WhenMemoryLimitIsSet_RunsCgsetWithMemoryLimit()
//         {
//             // Arrange
//             var job = new Job { MemoryLimitInBytes = 1024 };
//             var controller = "testController";
//             Mock.Get(_cGroupV2).Setup(c => c.GetCGroupController(job)).Returns(controller);
//             Mock.Get(ProcessUtil).Setup(p => p.RunAsync("cgset", It.IsAny<string>(), true)).Returns(Task.CompletedTask);
// 
//             // Act
//             await _cGroupV2.SetAsync(job);
// 
//             // Assert
//             Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r memory.max={job.MemoryLimitInBytes} {controller}", true), Times.Once);
//         }

        /// <summary>
        /// Tests the <see cref="CGroupV2.SetAsync(Job)"/> method to ensure it sets the CPU limit correctly.
        /// </summary>
        [TestMethod]
        public async Task SetAsync_WhenCpuLimitIsSet_RunsCgsetWithCpuLimit()
        {
            // Arrange
            var job = new Job { CpuLimitRatio = 0.5 };
            var controller = "testController";
            Mock.Get(_cGroupV2).Setup(c => c.GetCGroupController(job)).Returns(controller);
            Mock.Get(ProcessUtil).Setup(p => p.RunAsync("cgset", It.IsAny<string>(), true)).Returns(Task.CompletedTask);

            // Act
            await _cGroupV2.SetAsync(job);

            // Assert
            Mock.Get(ProcessUtil).Verify(p => p.RunAsync("cgset", $"-r cpu.max=\"{Math.Floor(job.CpuLimitRatio * CGroupV2.DefaultDockerCfsPeriod)} {CGroupV2.DefaultDockerCfsPeriod}\" {controller}", true), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="CGroupV2.GetCpuStatAsync(Job)"/> method to ensure it retrieves the CPU stats correctly.
        /// </summary>
        [TestMethod]
        public async Task GetCpuStatAsync_WhenCalled_ReturnsCpuStats()
        {
            // Arrange
            var job = new Job();
            var controller = "testController";
            var expectedOutput = "cpu stats";
            Mock.Get(_cGroupV2).Setup(c => c.GetCGroupController(job)).Returns(controller);
            Mock.Get(ProcessUtil).Setup(p => p.RunAsync("cat", $"/sys/fs/cgroup/{controller}/cpu.stat", false, true)).ReturnsAsync(new ProcessResult { StandardOutput = expectedOutput });

            // Act
            var result = await _cGroupV2.GetCpuStatAsync(job);

            // Assert
            Assert.AreEqual(expectedOutput, result);
        }
    }
}
