// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class CompositeServerTests
    {
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

        [Fact]
        public async Task StartFailureRollsBackInReverseOrderWithoutDisposingChildren()
        {
            var stopped = new List<int>();
            var first = new TestServer { Stop = () => stopped.Add(1) };
            var second = new TestServer { Start = () => throw new InvalidOperationException("start"), Stop = () => stopped.Add(2) };
            var composite = new CompositeServer([first, second]);

            await Assert.ThrowsAsync<InvalidOperationException>(() => composite.StartAsync<object>(null, CancellationToken.None));

            Assert.Equal(new[] { 2, 1 }, stopped);
            Assert.Equal(0, first.DisposeCount);
            Assert.Equal(0, second.DisposeCount);
        }

        [Fact]
        public async Task StopFailureDoesNotSkipOtherChildren()
        {
            var firstStopped = false;
            var composite = new CompositeServer([
                new TestServer { Stop = () => firstStopped = true },
                new TestServer { Stop = () => throw new InvalidOperationException("stop") }
            ]);

            await Assert.ThrowsAsync<AggregateException>(() => composite.StopAsync(CancellationToken.None));
            Assert.True(firstStopped);
        }

        private sealed class TestServer : IServer
        {
            public IFeatureCollection Features { get; } = new FeatureCollection();

            public int DisposeCount { get; private set; }
            public Action Start { get; init; } = () => { };
            public Action Stop { get; init; } = () => { };

            public void Dispose() => DisposeCount++;

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
            {
                Start();
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                Stop();
                return Task.CompletedTask;
            }
        }
    }
}
