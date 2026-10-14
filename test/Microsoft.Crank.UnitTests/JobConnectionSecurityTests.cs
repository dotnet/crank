// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class JobConnectionSecurityTests
    {
        [Fact]
        public void RelayTokenIsKeptOnAgentClientAndCanBeUpdated()
        {
            var connection = new JobConnection(new Job(), new Uri("https://relay.example/benchmark"));
            using var client = GetAgentClient(connection);
            connection.ConfigureRelay("old-test-token");
            connection.ConfigureRelay("new-test-token");

            Assert.Equal("new-test-token", Assert.Single(client.DefaultRequestHeaders.GetValues("ServiceBusAuthorization")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void InvalidRelayTokenDoesNotReplaceExistingHeader(string token)
        {
            var connection = new JobConnection(new Job(), new Uri("https://relay.example/benchmark"));
            using var client = GetAgentClient(connection);
            connection.ConfigureRelay("test-token");

            Assert.ThrowsAny<ArgumentException>(() => connection.ConfigureRelay(token));

            Assert.Equal("test-token", Assert.Single(client.DefaultRequestHeaders.GetValues("ServiceBusAuthorization")));
        }

        [Fact]
        public void RelayAuthenticationRequiresHttps()
        {
            var connection = new JobConnection(new Job(), new Uri("http://localhost:5010"));
            using var client = GetAgentClient(connection);

            Assert.Throws<InvalidOperationException>(() => connection.ConfigureRelay("test-token"));
        }

        [Fact]
        public async Task AgentClientRejectsUntrustedServerCertificate()
        {
            var requests = 0;
            await using var server = await LoopbackServer.StartAsync(context =>
            {
                Interlocked.Increment(ref requests);
                return context.Response.WriteAsync("{}");
            }, https: true);
            var connection = new JobConnection(new Job(), server.Address);
            using var client = GetAgentClient(connection);

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => connection.GetInfoAsync());

            Assert.IsType<AuthenticationException>(exception.InnerException);
            Assert.Equal(0, requests);
        }

        [Fact]
        public async Task AgentClientDoesNotFollowRedirects()
        {
            var redirectedRequests = 0;
            await using var destination = await LoopbackServer.StartAsync(context =>
            {
                Interlocked.Increment(ref redirectedRequests);
                return context.Response.WriteAsync("{}");
            });
            await using var server = await LoopbackServer.StartAsync(context =>
            {
                context.Response.Redirect(destination.Address.ToString());
                return Task.CompletedTask;
            });
            var connection = new JobConnection(new Job(), server.Address);
            using var client = GetAgentClient(connection);

            await Assert.ThrowsAsync<HttpRequestException>(() => connection.GetInfoAsync());

            Assert.Equal(0, redirectedRequests);
        }

        [Fact]
        public async Task PlainHttpAgentStillWorksWithoutRelayCredentials()
        {
            await using var server = await LoopbackServer.StartAsync(context =>
                context.Response.WriteAsync("{\"value\":\"ok\"}"));
            var connection = new JobConnection(new Job(), server.Address);
            using var client = GetAgentClient(connection);

            var info = await connection.GetInfoAsync();

            Assert.Equal("ok", info["value"].ToString());
        }

        [Fact]
        public async Task ExternalDownloadsDoNotReceiveRelayCredentials()
        {
            string authorization = null;
            await using var server = await LoopbackServer.StartAsync(context =>
            {
                authorization = context.Request.Headers["ServiceBusAuthorization"].FirstOrDefault();
                return context.Response.WriteAsync("artifact");
            });
            var connection = new JobConnection(new Job(), new Uri("https://relay.example/benchmark"));
            using var client = GetAgentClient(connection);
            connection.ConfigureRelay("test-token");
            var download = typeof(JobConnection).GetMethod("DownloadTemporaryFileAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            var filename = await Assert.IsAssignableFrom<Task<string>>(download.Invoke(connection, new object[] { server.Address.ToString() }));
            try
            {
                Assert.Equal("artifact", await File.ReadAllTextAsync(filename));
                Assert.Null(authorization);
            }
            finally
            {
                File.Delete(filename);
                Directory.Delete(Path.GetDirectoryName(filename));
            }
        }

        [Fact]
        public async Task ExternalDownloadClientRejectsUntrustedServerCertificate()
        {
            await using var server = await LoopbackServer.StartAsync(context =>
                context.Response.WriteAsync("artifact"), https: true);
            var client = Assert.IsType<HttpClient>(typeof(JobConnection)
                .GetField("_downloadHttpClient", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(server.Address));
            Assert.IsType<AuthenticationException>(exception.InnerException);
        }

        [Theory]
        [InlineData("crank-agent", "Microsoft.Crank.Agent.Startup")]
        [InlineData("crank", "Microsoft.Crank.Controller.Program")]
        [InlineData("Microsoft.Crank.Jobs.Bombardier", "Microsoft.Crank.Jobs.Bombardier.Program")]
        [InlineData("Microsoft.Crank.Jobs.HttpClient", "Microsoft.Crank.Jobs.HttpClientClient.Program")]
        [InlineData("Microsoft.Crank.Jobs.K6", "Microsoft.Crank.Jobs.K6.Program")]
        [InlineData("crank-pr", "Microsoft.Crank.PullRequestBot.Program")]
        [InlineData("Microsoft.Crank.RegressionBot", "Microsoft.Crank.RegressionBot.Program")]
        public async Task TrustedContentClientsRejectUntrustedServerCertificates(string assemblyName, string typeName)
        {
            await using var server = await LoopbackServer.StartAsync(context =>
                context.Response.WriteAsync("untrusted content"), https: true);
            var type = Assembly.Load(assemblyName).GetType(typeName, throwOnError: true);
            var client = Assert.IsType<HttpClient>(type.GetField("_httpClient", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(server.Address));
            Assert.IsType<AuthenticationException>(exception.InnerException);
        }

        [Theory]
        [InlineData("crank-agent", "Microsoft.Crank.Agent.Startup")]
        [InlineData("Microsoft.Crank.Jobs.Bombardier", "Microsoft.Crank.Jobs.Bombardier.Program")]
        [InlineData("Microsoft.Crank.Jobs.K6", "Microsoft.Crank.Jobs.K6.Program")]
        public async Task BenchmarkOnlyClientsStillAcceptSelfSignedTargets(string assemblyName, string typeName)
        {
            await using var server = await LoopbackServer.StartAsync(context =>
                context.Response.WriteAsync("benchmark response"), https: true);
            var type = Assembly.Load(assemblyName).GetType(typeName, throwOnError: true);
            var client = Assert.IsType<HttpClient>(type.GetField("_benchmarkHttpClient", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null));

            Assert.Equal("benchmark response", await client.GetStringAsync(server.Address));
        }

        private static HttpClient GetAgentClient(JobConnection connection)
            => Assert.IsType<HttpClient>(typeof(JobConnection)
                .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(connection));

        private sealed class LoopbackServer : IAsyncDisposable
        {
            private readonly IHost _host;
            private readonly X509Certificate2 _certificate;

            private LoopbackServer(IHost host, X509Certificate2 certificate)
            {
                _host = host;
                _certificate = certificate;
                Address = new Uri(host.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>().Addresses.Single());
            }

            public Uri Address { get; }

            public static async Task<LoopbackServer> StartAsync(RequestDelegate handler, bool https = false)
            {
                X509Certificate2 certificate = null;
                if (https)
                {
                    using var key = RSA.Create(2048);
                    var request = new CertificateRequest("CN=crank-security-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
                    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
                    {
                        new Oid("1.3.6.1.5.5.7.3.1")
                    }, true));
                    var names = new SubjectAlternativeNameBuilder();
                    names.AddIpAddress(IPAddress.Loopback);
                    request.CertificateExtensions.Add(names.Build());
                    using var selfSigned = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
                    // Windows SChannel needs an imported key rather than the certificate's ephemeral key.
                    certificate = X509CertificateLoader.LoadPkcs12(selfSigned.Export(X509ContentType.Pkcs12), null);
                }

                var host = new HostBuilder()
                    .ConfigureLogging(logging => logging.ClearProviders())
                    .ConfigureWebHost(web =>
                    {
                        web.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listen =>
                        {
                            if (certificate != null)
                            {
                                listen.UseHttps(certificate);
                            }
                        }));
                        web.Configure(app => app.Run(handler));
                    })
                    .Build();

                try
                {
                    await host.StartAsync();
                    return new LoopbackServer(host, certificate);
                }
                catch
                {
                    host.Dispose();
                    certificate?.Dispose();
                    throw;
                }
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _host.StopAsync();
                }
                finally
                {
                    _host.Dispose();
                    _certificate?.Dispose();
                }
            }
        }
    }
}
