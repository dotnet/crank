using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Agent.Controllers;
using Moq;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.Agent.Controllers.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HomeController"/> class.
    /// </summary>
//     public class HomeControllerTests [Error] (26-13)CS0272 The property or indexer 'Startup.Hardware' cannot be used in this context because the set accessor is inaccessible [Error] (27-13)CS0272 The property or indexer 'Startup.HardwareVersion' cannot be used in this context because the set accessor is inaccessible [Error] (28-13)CS0200 Property or indexer 'Startup.OperatingSystem' cannot be assigned to -- it is read only
//     {
//         private readonly HomeController _controller;
// 
//         /// <summary>
//         /// Initializes a new instance of the <see cref="HomeControllerTests"/> class.
//         /// Sets default values for static properties in Startup before each test.
//         /// </summary>
//         public HomeControllerTests()
//         {
//             // Set default values for Startup static properties.
//             // These values ensure that tests not explicitly setting them will have known defaults.
//             Startup.Hardware = "DefaultHardware";
//             Startup.HardwareVersion = "DefaultHardwareVersion";
//             Startup.OperatingSystem = "DefaultOperatingSystem";
//             _controller = new HomeController();
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="HomeController.Index"/> method to ensure it redirects to the "GetQueue" action of the "Jobs" controller.
//         /// </summary>
//         [Fact]
//         public void Index_WhenCalled_ReturnsRedirectToGetQueueAction()
//         {
//             // Act
//             IActionResult result = _controller.Index();
// 
//             // Assert
//             RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
//             Assert.Equal("GetQueue", redirectResult.ActionName);
//             Assert.Equal("Jobs", redirectResult.ControllerName);
//         }
// 
//         /// <summary>
//         /// Tests the <see cref="HomeController.Info"/> method to ensure it returns a JsonResult containing correct startup and environment information.
//         /// It verifies that the JSON contains the properties "hw", "env", "os", "arch", "proc", and "version" with expected values.
//         /// </summary>
//         [Fact] [Error] (56-13)CS0272 The property or indexer 'Startup.Hardware' cannot be used in this context because the set accessor is inaccessible [Error] (57-13)CS0272 The property or indexer 'Startup.HardwareVersion' cannot be used in this context because the set accessor is inaccessible [Error] (58-13)CS0200 Property or indexer 'Startup.OperatingSystem' cannot be assigned to -- it is read only
//         public void Info_WhenCalled_ReturnsExpectedJsonResult()
//         {
//             // Arrange
//             // Set specific test values for the Startup static properties.
//             Startup.Hardware = "TestHardware";
//             Startup.HardwareVersion = "TestHardwareVersion";
//             Startup.OperatingSystem = "TestOperatingSystem";
// 
//             string expectedArch = RuntimeInformation.ProcessArchitecture.ToString();
//             int expectedProc = Environment.ProcessorCount;
//             string expectedVersion = typeof(HomeController)
//                                         .GetTypeInfo()
//                                         .Assembly
//                                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
//                                         .InformationalVersion;
// 
//             // Act
//             IActionResult actionResult = _controller.Info();
// 
//             // Assert
//             JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
//             Assert.NotNull(jsonResult.Value);
//             var jsonObject = jsonResult.Value;
//             Type jsonType = jsonObject.GetType();
// 
//             var hwProperty = jsonType.GetProperty("hw");
//             var envProperty = jsonType.GetProperty("env");
//             var osProperty = jsonType.GetProperty("os");
//             var archProperty = jsonType.GetProperty("arch");
//             var procProperty = jsonType.GetProperty("proc");
//             var versionProperty = jsonType.GetProperty("version");
// 
//             Assert.NotNull(hwProperty);
//             Assert.NotNull(envProperty);
//             Assert.NotNull(osProperty);
//             Assert.NotNull(archProperty);
//             Assert.NotNull(procProperty);
//             Assert.NotNull(versionProperty);
// 
//             string hwValue = hwProperty.GetValue(jsonObject)?.ToString();
//             string envValue = envProperty.GetValue(jsonObject)?.ToString();
//             string osValue = osProperty.GetValue(jsonObject)?.ToString();
//             string archValue = archProperty.GetValue(jsonObject)?.ToString();
//             int procValue = Convert.ToInt32(procProperty.GetValue(jsonObject));
//             string versionValue = versionProperty.GetValue(jsonObject)?.ToString();
// 
//             Assert.Equal("TestHardware", hwValue);
//             Assert.Equal("TestHardwareVersion", envValue);
//             Assert.Equal("TestOperatingSystem", osValue);
//             Assert.Equal(expectedArch, archValue);
//             Assert.Equal(expectedProc, procValue);
//             Assert.Equal(expectedVersion, versionValue);
//         }
//     }
}
