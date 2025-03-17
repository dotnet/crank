using Moq;
using System;
using System.Reflection;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Crank.Agent.Controllers.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HomeController"/> class.
    /// </summary>
    [TestClass]
    public class HomeControllerTests
    {
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _controller = new HomeController();
        }

        /// <summary>
        /// Tests the <see cref="HomeController.Index"/> method to ensure it redirects to the "GetQueue" action in the "Jobs" controller.
        /// </summary>
        [TestMethod]
        public void Index_WhenCalled_RedirectsToGetQueueInJobsController()
        {
            // Act
            var result = _controller.Index() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result, "Result should be a RedirectToActionResult.");
            Assert.AreEqual("GetQueue", result.ActionName, "Action name should be 'GetQueue'.");
            Assert.AreEqual("Jobs", result.ControllerName, "Controller name should be 'Jobs'.");
        }

        /// <summary>
        /// Tests the <see cref="HomeController.Info"/> method to ensure it returns the correct JSON result.
        /// </summary>
        [TestMethod]
        public void Info_WhenCalled_ReturnsCorrectJsonResult()
        {
            // Arrange
            var hardware = "TestHardware";
            var hardwareVersion = "TestHardwareVersion";
            var operatingSystem = "TestOS";
            var processArchitecture = "TestArchitecture";
            var processorCount = 4;
            var version = "1.0.0";

            var startupMock = new Mock<Startup>();
            startupMock.SetupGet(s => s.Hardware).Returns(hardware);
            startupMock.SetupGet(s => s.HardwareVersion).Returns(hardwareVersion);
            startupMock.SetupGet(s => s.OperatingSystem).Returns(operatingSystem);

            var runtimeInformationMock = new Mock<System.Runtime.InteropServices.RuntimeInformation>();
            runtimeInformationMock.SetupGet(r => r.ProcessArchitecture).Returns(processArchitecture);

            var environmentMock = new Mock<Environment>();
            environmentMock.SetupGet(e => e.ProcessorCount).Returns(processorCount);

            var assemblyMock = new Mock<Assembly>();
            var attributeMock = new Mock<AssemblyInformationalVersionAttribute>(version);
            assemblyMock.Setup(a => a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()).Returns(attributeMock.Object);

            // Act
            var result = _controller.Info() as JsonResult;

            // Assert
            Assert.IsNotNull(result, "Result should be a JsonResult.");
            dynamic json = result.Value;
            Assert.AreEqual(hardware, json.hw, "Hardware should match.");
            Assert.AreEqual(hardwareVersion, json.env, "Hardware version should match.");
            Assert.AreEqual(operatingSystem, json.os, "Operating system should match.");
            Assert.AreEqual(processArchitecture, json.arch, "Process architecture should match.");
            Assert.AreEqual(processorCount, json.proc, "Processor count should match.");
            Assert.AreEqual(version, json.version, "Version should match.");
        }
    }
}

