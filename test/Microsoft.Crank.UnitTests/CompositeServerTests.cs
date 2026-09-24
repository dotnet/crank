// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class CompositeServerTests
    {
        [Fact]
        public void CombinedRelayAndHttpHostDisposesWithoutThrowing()
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(web =>
                {
                    web.UseKestrel();
                    web.UseAzureRelay(options => options.UrlPrefixes.Add(
                        "Endpoint=sb://unused.invalid/;EntityPath=test;" +
                        "SharedAccessKeyName=test;" +
                        "SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));
                    web.Configure(_ => { });
                    web.ConfigureServices(services =>
                    {
                        var descriptors = services.Where(d =>
                            d.Lifetime == ServiceLifetime.Singleton &&
                            typeof(IServer).IsAssignableFrom(d.ServiceType)).ToArray();

                        foreach (var descriptor in descriptors)
                        {
                            services.Remove(descriptor);
                            services.AddSingleton(descriptor.ImplementationType);
                        }

                        services.AddSingleton<IServer>(provider => new CompositeServer(
                            descriptors.Select(d => (IServer)provider.GetRequiredService(d.ImplementationType))));
                    });
                })
                .Build();

            Assert.IsType<CompositeServer>(host.Services.GetRequiredService<IServer>());
        }

        [Fact]
        public void ServiceProviderOwnsChildServerDisposal()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IServer>(_ => new TestServer());
            services.AddSingleton<IServer>(_ => new TestServer());
            services.AddSingleton(provider => new CompositeServer(provider.GetServices<IServer>()));

            using var provider = services.BuildServiceProvider();
            var servers = provider.GetServices<IServer>().Cast<TestServer>().ToArray();
            var composite = provider.GetRequiredService<CompositeServer>();

            composite.Dispose();

            Assert.All(servers, server => Assert.Equal(0, server.DisposeCount));

            provider.Dispose();

            Assert.All(servers, server => Assert.Equal(1, server.DisposeCount));
        }

        private sealed class TestServer : IServer
        {
            public IFeatureCollection Features { get; } = new FeatureCollection();

            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
