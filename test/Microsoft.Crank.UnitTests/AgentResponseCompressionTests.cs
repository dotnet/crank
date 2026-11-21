// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repository;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class AgentResponseCompressionTests
    {
        [Theory]
        [InlineData(false, "br", "br")]
        [InlineData(false, "gzip", "gzip")]
        [InlineData(true, "br", "br")]
        [InlineData(true, "gzip", "gzip")]
        [InlineData(false, "br;q=0.1, gzip;q=1.0", "gzip")]
        [InlineData(false, "br;q=0, gzip;q=0", null)]
        [InlineData(false, "identity", null)]
        [InlineData(false, null, null)]
        public async Task NegotiatesLargeJobResponses(bool https, string acceptEncoding, string expectedEncoding)
        {
            await using var server = await CompressionServer.StartAsync(https);
            using var client = server.CreateRawClient();
            using var baseline = await GetAsync(client, server.Address, "jobs/1", null);
            var expected = await baseline.Content.ReadAsByteArrayAsync();
            Assert.Empty(baseline.Content.Headers.ContentEncoding);

            using var response = await GetAsync(client, server.Address, "jobs/1", acceptEncoding);
            var bytes = await response.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedEncoding, response.Content.Headers.ContentEncoding.SingleOrDefault());
            Assert.Equal(expected, Decompress(bytes, expectedEncoding));
            if (expectedEncoding != null)
            {
                Assert.Contains("Accept-Encoding", response.Headers.Vary);
                Assert.True(bytes.Length < expected.Length);
            }
        }

        [Fact]
        public async Task CompressesOnlyResponsesAtOrAboveTheMinimumSize()
        {
            await using var server = await CompressionServer.StartAsync();
            using var client = server.CreateRawClient();
            var minimum = AgentResponseCompressionProvider.MinimumResponseSize;

            server.Job.Output.Clear();
            server.Job.Output.AddLine(new string('x', minimum - 1 - Encoding.UTF8.GetByteCount(Environment.NewLine)));
            using var smallText = await GetAsync(client, server.Address, "jobs/1/output", "br");
            Assert.Empty(smallText.Content.Headers.ContentEncoding);
            Assert.Equal(minimum - 1, (await smallText.Content.ReadAsByteArrayAsync()).Length);

            server.Job.Output.Clear();
            server.Job.Output.AddLine(new string('x', minimum - Encoding.UTF8.GetByteCount(Environment.NewLine)));
            using var largeText = await GetAsync(client, server.Address, "jobs/1/output", "br");
            Assert.Equal("br", Assert.Single(largeText.Content.Headers.ContentEncoding));
            Assert.Equal(minimum, Decompress(await largeText.Content.ReadAsByteArrayAsync(), "br").Length);

            server.Job.Output.Clear();
            server.Job.Output.AddLine(new string('x', minimum - 5));
            using var smallJson = await GetAsync(client, server.Address, "jobs/1/output/0", "gzip");
            Assert.Empty(smallJson.Content.Headers.ContentEncoding);
            Assert.Equal(minimum - 1, (await smallJson.Content.ReadAsByteArrayAsync()).Length);

            server.Job.Output.Clear();
            server.Job.Output.AddLine(new string('x', minimum - 4));
            using var largeJson = await GetAsync(client, server.Address, "jobs/1/output/0", "gzip");
            Assert.Equal("gzip", Assert.Single(largeJson.Content.Headers.ContentEncoding));
            Assert.Equal(minimum, Decompress(await largeJson.Content.ReadAsByteArrayAsync(), "gzip").Length);

            server.Job.Output.Clear();
            server.Job.Output.AddLine("A \"quoted\" line\nwith unicode: \u03bb");
            using var json = await GetAsync(client, server.Address, "jobs/1/output/0", null);
            Assert.Equal("application/json", json.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonConvert.SerializeObject(server.Job.Output.Get(0)), await json.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PollingAndAlreadyCompressedArtifactsKeepTheirExistingEncoding()
        {
            await using var server = await CompressionServer.StartAsync();
            using var client = server.CreateRawClient();

            foreach (var endpoint in new[] { "state", "touch" })
            {
                using var response = await GetAsync(client, server.Address, $"jobs/1/{endpoint}", "br, gzip");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(response.Content.Headers.ContentEncoding);
                Assert.Equal(endpoint == "state" ? "Running" : "", await response.Content.ReadAsStringAsync());
            }

            foreach (var endpoint in new[] { "trace", "dump", "eventpipe", "download?path=artifact.txt" })
            {
                using var response = await GetAsync(client, server.Address, $"jobs/1/{endpoint}", "br, gzip");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("gzip", Assert.Single(response.Content.Headers.ContentEncoding));
                Assert.Equal(server.Artifact, Decompress(await response.Content.ReadAsByteArrayAsync(), "gzip"));
            }

            using var brotliOnly = await GetAsync(client, server.Address, "jobs/1/trace", "br");
            Assert.Empty(brotliOnly.Content.Headers.ContentEncoding);
            Assert.Equal(server.Artifact, await brotliOnly.Content.ReadAsByteArrayAsync());

            using var fetch = await GetAsync(client, server.Address, "jobs/1/fetch", "br, gzip");
            Assert.Equal(HttpStatusCode.OK, fetch.StatusCode);
            Assert.Empty(fetch.Content.Headers.ContentEncoding);
            using var archive = new ZipArchive(new MemoryStream(await fetch.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
            Assert.Contains(archive.Entries, entry => entry.FullName == "artifact.txt");
        }

        [Fact]
        public async Task FileListsKeepTheirJsonContractWhenCompressed()
        {
            await using var server = await CompressionServer.StartAsync();
            using var client = server.CreateRawClient();
            for (var i = 0; i < 80; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(server.Job.BasePath, $"file-{i:D3}-example.txt"), "");
            }

            using var response = await GetAsync(client, server.Address, "jobs/1/list?path=*.txt", "br");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("br", Assert.Single(response.Content.Headers.ContentEncoding));
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            var json = Encoding.UTF8.GetString(Decompress(await response.Content.ReadAsByteArrayAsync(), "br"));
            var files = JsonConvert.DeserializeObject<string[]>(json);
            Assert.Equal(81, files.Length);
            Assert.Contains("artifact.txt", files);
            Assert.Contains("file-079-example.txt", files);
        }

        [Fact]
        public async Task ControllerAndLegacyClientsReadNegotiatedResponses()
        {
            await using var server = await CompressionServer.StartAsync();
            var connection = new JobConnection(new Job(), server.Address);
            var connectionType = typeof(JobConnection);
            connectionType.GetField("_serverJobUri", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(connection, new Uri(server.Address, "jobs/1").ToString());
            using var client = (HttpClient)connectionType.GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(connection);

            Assert.Equal(server.Job.EnvironmentVariables["payload"], (await connection.GetJobAsync()).EnvironmentVariables["payload"]);
            Assert.Contains("compressed log", await connection.StreamOutputAsync());
            Assert.Contains("compressed build log", await connection.StreamBuildLogAsync());
            Assert.Equal(10, (await connection.GetQueueAsync()).Count());

            var controllerClient = (HttpClient)typeof(Microsoft.Crank.Controller.Program)
                .GetField("_httpClient", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.Equal(10, JsonConvert.DeserializeObject<JobView[]>(await controllerClient.GetStringAsync(new Uri(server.Address, "jobs"))).Length);

            using var legacy = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            Assert.Contains("payload", await legacy.GetStringAsync(new Uri(server.Address, "jobs/1")));

            foreach (var path in new[] { "/jobs/1", "/jobs/1/output/0", "/jobs/1/buildlog/0", "/jobs" })
            {
                Assert.Contains(server.Responses, response => response.Path == path
                    && response.AcceptEncoding.Contains("br", StringComparison.Ordinal)
                    && response.ContentEncoding == "br");
            }
            Assert.Contains(server.Responses, response => response.Path == "/jobs/1" && response.ContentEncoding == "gzip");
        }

        private static async Task<HttpResponseMessage> GetAsync(HttpClient client, Uri address, string path, string acceptEncoding)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(address, path));
            if (acceptEncoding != null)
            {
                request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
            }

            return await client.SendAsync(request);
        }

        private static byte[] Decompress(byte[] bytes, string encoding)
        {
            if (encoding == null)
            {
                return bytes;
            }

            using var input = new MemoryStream(bytes);
            using Stream decompressor = encoding == "br"
                ? new BrotliStream(input, CompressionMode.Decompress)
                : new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            decompressor.CopyTo(output);
            return output.ToArray();
        }

        private sealed class CompressionServer : IAsyncDisposable
        {
            private readonly IHost _host;
            private readonly X509Certificate2 _certificate;
            private readonly string _directory;

            private CompressionServer(IHost host, X509Certificate2 certificate, string directory, Job job, byte[] artifact)
            {
                _host = host;
                _certificate = certificate;
                _directory = directory;
                Job = job;
                Artifact = artifact;
                Address = new Uri(host.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>().Addresses.Single() + "/");
            }

            public Uri Address { get; }
            public Job Job { get; }
            public byte[] Artifact { get; }
            public ConcurrentQueue<(string Path, string AcceptEncoding, string ContentEncoding)> Responses { get; } = new();

            public HttpClient CreateRawClient()
            {
                return new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.None,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            }

            public static async Task<CompressionServer> StartAsync(bool https = false)
            {
                var directory = Path.Combine(Path.GetTempPath(), "crank-compression-tests", Guid.NewGuid().ToString("n"));
                var published = Path.Combine(directory, "published");
                Directory.CreateDirectory(published);
                var artifact = Encoding.UTF8.GetBytes(new string('a', 8192));
                await File.WriteAllBytesAsync(Path.Combine(published, "artifact.txt"), artifact);
                await File.WriteAllBytesAsync(Path.Combine(published, "sample.netperf"), artifact);

                var repository = new InMemoryJobRepository();
                var job = repository.Add(new Job
                {
                    State = JobState.Running,
                    Service = "compression-test",
                    BasePath = published,
                    PerfViewTraceFile = Path.Combine(published, "artifact.txt"),
                    DumpFile = Path.Combine(published, "artifact.txt")
                });
                job.EnvironmentVariables["payload"] = new string('a', 8192);
                job.Output.AddLine("compressed log " + new string('o', 8192));
                job.BuildLog.AddLine("compressed build log " + new string('b', 8192));
                for (var i = 0; i < 9; i++)
                {
                    repository.Add(new Job { State = JobState.Running });
                }

                X509Certificate2 certificate = null;
                if (https)
                {
                    using var key = RSA.Create(2048);
                    var request = new CertificateRequest("CN=crank-compression-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    var names = new SubjectAlternativeNameBuilder();
                    names.AddIpAddress(IPAddress.Loopback);
                    request.CertificateExtensions.Add(names.Build());
                    using var selfSigned = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
                    certificate = X509CertificateLoader.LoadPkcs12(selfSigned.Export(X509ContentType.Pkcs12), null);
                }

                CompressionServer server = null;
                var startup = new Startup();
                var host = new HostBuilder()
                    .ConfigureLogging(logging => logging.ClearProviders())
                    .ConfigureWebHost(web =>
                    {
                        web.UseSetting(WebHostDefaults.ApplicationKey, typeof(Startup).Assembly.GetName().Name);
                        web.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listen =>
                        {
                            if (certificate != null)
                            {
                                listen.UseHttps(certificate);
                            }
                        }));
                        web.ConfigureServices(services =>
                        {
                            startup.ConfigureServices(services);
                            services.AddControllersWithViews().AddApplicationPart(typeof(JobsController).Assembly);
                            services.AddSingleton<IJobRepository>(repository);
                        });
                        web.Configure(app =>
                        {
                            app.Use(async (context, next) =>
                            {
                                context.Response.OnStarting(() =>
                                {
                                    server.Responses.Enqueue((context.Request.Path, context.Request.Headers.AcceptEncoding.ToString(),
                                        context.Response.Headers.ContentEncoding.ToString()));
                                    return Task.CompletedTask;
                                });
                                await next();
                            });
                            startup.Configure(app, new NeverStoppingLifetime());
                        });
                    })
                    .Build();

                try
                {
                    await host.StartAsync();
                    server = new CompressionServer(host, certificate, directory, job, artifact);
                    return server;
                }
                catch
                {
                    host.Dispose();
                    certificate?.Dispose();
                    Directory.Delete(directory, true);
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
                    Directory.Delete(_directory, true);
                }
            }
        }

        private sealed class NeverStoppingLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted => CancellationToken.None;
            public CancellationToken ApplicationStopping => CancellationToken.None;
            public CancellationToken ApplicationStopped => CancellationToken.None;
            public void StopApplication() { }
        }
    }
}
