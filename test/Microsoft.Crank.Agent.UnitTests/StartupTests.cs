using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using Microsoft.Crank.Agent;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Startup"/> class.
    /// </summary>
    public class StartupTests
    {
        /// <summary>
        /// Tests that ConfigureServices successfully registers MVC and routing services.
        /// Expected outcome: The supplied service collection contains registrations for MVC controllers and routing.
        /// </summary>
        [Fact]
        public void ConfigureServices_ValidServiceCollection_RegistersMvcAndRoutingServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var startup = new Startup();

            // Act
            startup.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            // Check that at least one service related to MVC controller support has been added.
            var mvcService = services.FirstOrDefault(s => s.ServiceType.FullName != null &&
                                                          s.ServiceType.FullName.Contains("IActionDescriptorCollectionProvider"));
            Assert.NotNull(mvcService);

            // Check that routing services are registered.
            var routeOptions = serviceProvider.GetService(typeof(Microsoft.AspNetCore.Routing.RouteOptions));
            Assert.NotNull(routeOptions);
        }

        /// <summary>
        /// Tests that Configure method registers a shutdown callback with the host application lifetime.
        /// Expected outcome: A callback is registered on the ApplicationStopping token.
        /// </summary>
//         [Fact] [Error] (72-31)CS0452 The type 'CancellationTokenRegistration' must be a reference type in order to use it as parameter 'T' in the generic type or method 'Mock.Of<T>()'
//         public void Configure_ValidApplicationBuilderAndHostApplicationLifetime_RegistersShutdownCallback()
//         {
//             // Arrange
//             var services = new ServiceCollection().BuildServiceProvider();
//             var appBuilder = new ApplicationBuilder(services);
// 
//             // Create a CancellationTokenSource to simulate ApplicationStopping.
//             var cts = new CancellationTokenSource();
// 
//             // Setup a flag to capture invocation of the shutdown callback.
//             var shutdownCallbackInvoked = false;
// 
//             // Setup a mock for IHostApplicationLifetime.
//             var mockHostLifetime = new Mock<IHostApplicationLifetime>();
//             // Return our token and simulate registration by invoking the callback on cancellation.
//             mockHostLifetime.SetupGet(m => m.ApplicationStopping).Returns(cts.Token);
//             mockHostLifetime.Setup(m => m.ApplicationStopping.Register(It.IsAny<Action>()))
//                 .Callback<Action>(callback =>
//                 {
//                     // Hook the callback so that when token is cancelled, we invoke it manually.
//                     cts.Token.Register(() => shutdownCallbackInvoked = true);
//                 })
//                 .Returns(Mock.Of<CancellationTokenRegistration>());
// 
//             var startup = new Startup();
// 
//             // Act
//             startup.Configure(appBuilder, mockHostLifetime.Object);
// 
//             // Simulate application stopping.
//             cts.Cancel();
// 
//             // Allow some time for the callback to be invoked.
//             Thread.Sleep(100);
// 
//             // Assert
//             Assert.True(shutdownCallbackInvoked, "Shutdown callback was not invoked on ApplicationStopping cancellation.");
//         }

        /// <summary>
        /// Tests that Main method when invoked with help argument returns a non-negative exit code.
        /// Expected outcome: Main method completes and returns an exit code greater than or equal to zero.
        /// </summary>
        [Fact]
        public void Main_WithHelpArgument_ReturnsNonNegativeExitCode()
        {
            // Arrange
            string[] args = new string[] { "--help" };

            // Act
            int exitCode = Startup.Main(args);

            // Assert
            Assert.True(exitCode >= 0, "Main method returned a negative exit code.");
        }

        /// <summary>
        /// Tests that EnsureDotnetInstallExistsAsync completes successfully without throwing exceptions.
        /// Expected outcome: The method completes and does not throw.
        /// </summary>
        [Fact]
        public async Task EnsureDotnetInstallExistsAsync_CompletesWithoutError()
        {
            // Act & Assert
            var exception = await Record.ExceptionAsync(() => Startup.EnsureDotnetInstallExistsAsync());
            Assert.Null(exception);
        }
    }
}
