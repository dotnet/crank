// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Azure.Relay;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Passerelle;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class RelayHostingTests
    {
        private const string SasConnectionString =
            "Endpoint=sb://unused.invalid/;SharedAccessKeyName=test;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        [Theory]
        [InlineData(null, "original")]
        [InlineData("", "original")]
        [InlineData("agent", "agent")]
        [InlineData("agents/linux", "agents/linux")]
        public void SasConnectionStringPreservesOrOverridesEntityPath(string entityPath, string expectedPath)
        {
            var options = new RelayOptions();

            var address = Startup.ConfigureRelayOptions(options, SasConnectionString + ";EntityPath=original", entityPath, null);

            Assert.Equal("sb://unused.invalid/" + expectedPath, address.AbsoluteUri);
            Assert.Equal(expectedPath, new RelayConnectionStringBuilder(options.ConnectionString).EntityPath);
            Assert.Null(options.Address);
            Assert.Null(options.TokenProvider);
            Assert.Equal(10L * 1024 * 1024 * 1024, options.MaxRequestBodySize);
        }

        [Theory]
        [InlineData(null, "original")]
        [InlineData("agent", "agent")]
        public void TokenProviderTakesPrecedenceOverSasCredentials(string entityPath, string expectedPath)
        {
            var options = new RelayOptions();
            var tokenProvider = TokenProvider.CreateAzureActiveDirectoryTokenProvider(
                (audience, authority, state) => Task.FromResult("unused"), null);

            var address = Startup.ConfigureRelayOptions(options, SasConnectionString + ";EntityPath=original", entityPath, tokenProvider);

            Assert.Equal(new Uri("sb://unused.invalid/" + expectedPath), address);
            Assert.Equal(address, options.Address);
            Assert.Same(tokenProvider, options.TokenProvider);
            Assert.Null(options.ConnectionString);
            Assert.Equal(10L * 1024 * 1024 * 1024, options.MaxRequestBodySize);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RelayHostBuildsAndDisposesWithOrWithoutLocalHttp(bool enableHttp)
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(web =>
                {
                    web.UseKestrel();
                    web.UseUrls("http://127.0.0.1:0");
                    web.Configure(_ => { });

                    void ConfigureRelay(RelayOptions options) =>
                        Startup.ConfigureRelayOptions(options, SasConnectionString + ";EntityPath=agent", null, null);

                    if (enableHttp)
                    {
                        web.AddPasserelle(ConfigureRelay);
                    }
                    else
                    {
                        web.UsePasserelle(ConfigureRelay);
                    }
                })
                .Build();

            var server = host.Services.GetRequiredService<IServer>();

            Assert.Equal(typeof(RelayOptions).Assembly, server.GetType().Assembly);
            Assert.Equal(enableHttp, server.Features.Get<IServerAddressesFeature>() is not null);
        }
    }
}
