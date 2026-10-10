// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Crank.Agent;

internal static class RelayWebHostBuilderExtensions
{
    internal const string HttpServerKey = "Crank.HttpServer";

    internal static IWebHostBuilder UseCrankRelay(this IWebHostBuilder builder, RelayServerOptions options, bool enableHttp)
    {
        if (enableHttp)
        {
            builder.UseKestrel();
        }

        return builder.ConfigureServices(services =>
        {
            ServiceDescriptor kestrel = null;
            if (enableHttp)
            {
                // Retain the framework's actual Kestrel implementation and all of its transports.
                // The public KestrelServer constructor only accepts one transport factory.
                // UseKestrel can be called by both Startup and this helper; select the
                // effective registration using the same last-registration rule as DI.
                kestrel = services.Last(descriptor => descriptor.ServiceType == typeof(IServer) && !descriptor.IsKeyedService);
            }

            services.RemoveAll<IServer>();
            services.AddSingleton(options);
            services.TryAddSingleton<IRelayListenerFactory, RelayListenerFactory>();
            services.AddSingleton<RelayServer>();
            if (enableHttp)
            {
                services.Add(kestrel.ImplementationFactory != null
                    ? ServiceDescriptor.KeyedSingleton<IServer>(HttpServerKey, (provider, _) => (IServer)kestrel.ImplementationFactory(provider))
                    : ServiceDescriptor.KeyedSingleton(typeof(IServer), HttpServerKey, kestrel.ImplementationType));
                services.AddSingleton<IServer>(provider => new CompositeServer(
                    [provider.GetRequiredKeyedService<IServer>(HttpServerKey), provider.GetRequiredService<RelayServer>()],
                    provider.GetRequiredService<ILogger<CompositeServer>>()));
            }
            else
            {
                services.AddSingleton<IServer>(provider => provider.GetRequiredService<RelayServer>());
            }
        });
    }
}
