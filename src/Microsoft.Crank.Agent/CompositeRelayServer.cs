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

namespace Microsoft.Crank.Agent
{
    public class CompositeServer : IServer
    {
        private readonly IEnumerable<IServer> _servers;

        public CompositeServer(IEnumerable<IServer> servers)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            if (servers.Count() < 2)
            {
                throw new ArgumentException("Expected at least 2 servers.", nameof(servers));
            }

            _servers = servers;
        }
        public IFeatureCollection Features => _servers.First().Features;

        public void Dispose()
        {
            foreach (var server in _servers)
            {
                server.Dispose();
            }
        }

        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            foreach (var server in _servers)
            {
                await server.StartAsync(application, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var server in _servers)
            {
                await server.StopAsync(cancellationToken);
            }
        }
    }
}
