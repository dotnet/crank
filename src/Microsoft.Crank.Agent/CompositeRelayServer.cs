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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Crank.Agent
{
    public class CompositeServer : IServer
    {
        private readonly IServer[] _servers;
        private readonly ILogger<CompositeServer> _logger;

        public CompositeServer(IEnumerable<IServer> servers, ILogger<CompositeServer> logger = null)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            _servers = servers.ToArray();
            if (_servers.Length < 2)
            {
                throw new ArgumentException("Expected at least 2 servers.", nameof(servers));
            }

            _logger = logger ?? NullLogger<CompositeServer>.Instance;
        }
        public IFeatureCollection Features => _servers.First().Features;

        public void Dispose()
        {
            // DI owns the child servers; enumerating them here can resolve services from a disposed provider.
        }

        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            var started = new List<IServer>();
            try
            {
                foreach (var server in _servers)
                {
                    started.Add(server);
                    await server.StartAsync(application, cancellationToken);
                }
            }
            catch
            {
                using var rollback = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                foreach (var server in started.AsEnumerable().Reverse())
                {
                    try
                    {
                        await server.StopAsync(rollback.Token);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Failed to roll back composite server startup.");
                    }
                }

                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var failures = new List<Exception>();
            foreach (var server in _servers.Reverse())
            {
                try
                {
                    await server.StopAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }

            if (failures.Count != 0)
            {
                throw new AggregateException("Failed to stop composite servers.", failures);
            }
        }
    }
}
