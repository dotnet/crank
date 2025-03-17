using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Startup"/> class.
    /// </summary>
    [TestClass]
    public class StartupTests
    {
        private readonly Mock<IServiceCollection> _servicesMock;
        private readonly Mock<IApplicationBuilder> _appMock;
        private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetimeMock;
        private readonly Startup _startup;

        public StartupTests()
        {
            _servicesMock = new Mock<IServiceCollection>();
            _appMock = new Mock<IApplicationBuilder>();
            _hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            _startup = new Startup();
        }

        /// <summary>
        /// Tests the <see cref="Startup.ConfigureServices(IServiceCollection)"/> method to ensure it correctly adds services.
        /// </summary>
        [TestMethod]
        public void ConfigureServices_WhenCalled_AddsServices()
        {
            // Act
            _startup.ConfigureServices(_servicesMock.Object);

            // Assert
            _servicesMock.Verify(s => s.AddControllersWithViews(), Times.Once);
            _servicesMock.Verify(s => s.AddSingleton(It.IsAny<object>()), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="Startup.Configure(IApplicationBuilder, IHostApplicationLifetime)"/> method to ensure it correctly configures the application.
        /// </summary>
        [TestMethod]
        public void Configure_WhenCalled_ConfiguresApplication()
        {
            // Arrange
            var endpointsMock = new Mock<IEndpointRouteBuilder>();
            _appMock.Setup(a => a.UseRouting()).Returns(_appMock.Object);
            _appMock.Setup(a => a.UseEndpoints(It.IsAny<Action<IEndpointRouteBuilder>>()))
                .Callback<Action<IEndpointRouteBuilder>>(endpoints => endpoints(endpointsMock.Object));

            // Act
            _startup.Configure(_appMock.Object, _hostApplicationLifetimeMock.Object);

            // Assert
            _hostApplicationLifetimeMock.Verify(h => h.ApplicationStopping.Register(It.IsAny<Action>()), Times.Once);
            _appMock.Verify(a => a.UseRouting(), Times.Once);
            _appMock.Verify(a => a.UseEndpoints(It.IsAny<Action<IEndpointRouteBuilder>>()), Times.Once);
            endpointsMock.Verify(e => e.MapGet("jobs/{id}/state", It.IsAny<RequestDelegate>()), Times.Once);
            endpointsMock.Verify(e => e.MapGet("jobs/{id}/touch", It.IsAny<RequestDelegate>()), Times.Once);
            endpointsMock.Verify(e => e.MapDefaultControllerRoute(), Times.Once);
        }
    }
}
