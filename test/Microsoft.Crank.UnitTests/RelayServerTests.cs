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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Relay;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Repository;
using Xunit;

namespace Microsoft.Crank.UnitTests;

public class RelayServerTests
{
    private const string ConnectionString = "Endpoint=sb://unused.invalid/;EntityPath=test/nested;" +
        "SharedAccessKeyName=test;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ProductionRegistrationStartsAndPublishesTheCorrectAddress(bool enableHttp, bool configureKestrelFirst)
    {
        var factory = new ListenerFactory();
        using var host = CreateHost(factory, enableHttp, app => app.Run(context => context.Response.WriteAsync("ready")),
            configureKestrelFirst: configureKestrelFirst);
        await host.StartAsync();

        var server = host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>().Addresses;
        Assert.Equal(1, factory.Listener.OpenCount);
        Assert.Equal("sb://unused.invalid/test/nested", factory.Options.ListenerAddress.AbsoluteUri);
        if (configureKestrelFirst)
        {
            Assert.Equal(10L * 1024 * 1024 * 1024,
                host.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.Limits.MaxRequestBodySize);
        }
        if (enableHttp)
        {
            Assert.IsType<CompositeServer>(server);
            Assert.NotNull(host.Services.GetKeyedService<IServer>(RelayWebHostBuilderExtensions.HttpServerKey));
            using var client = new HttpClient();
            Assert.Equal("ready", await client.GetStringAsync(Assert.Single(addresses)));
        }
        else
        {
            Assert.IsType<RelayServer>(server);
            Assert.Null(host.Services.GetKeyedService<IServer>(RelayWebHostBuilderExtensions.HttpServerKey));
            Assert.Equal("https://unused.invalid/test/nested", Assert.Single(addresses));
        }

        var exchange = new Exchange();
        factory.Listener.RequestHandler(exchange);
        await exchange.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(200, exchange.StatusCode);
        Assert.Equal("ready", Encoding.UTF8.GetString(exchange.Output.ToArray()));
        await host.StopAsync();
        await server.StopAsync(CancellationToken.None);
        server.Dispose();
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task HttpOnlyDoesNotResolveOrOpenRelay()
    {
        using var host = new HostBuilder().ConfigureWebHost(web => web.UseKestrel().UseUrls("http://127.0.0.1:0")
            .Configure(app => app.Run(context => context.Response.WriteAsync("http")))).Build();
        await host.StartAsync();
        Assert.Null(host.Services.GetService<RelayServer>());
        using var client = new HttpClient();
        var address = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
        Assert.Equal("http", await client.GetStringAsync(address));
        await host.StopAsync();
    }

    [Fact]
    public async Task SdkSbRequestUriIsExposedAsPublicHttps()
    {
        var factory = new ListenerFactory();
        using var server = CreateServer(factory);
        await server.StartAsync(new Application(context =>
        {
            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal("unused.invalid", context.Request.Host.Value);
            Assert.Equal("/test/nested", context.Request.PathBase.Value);
            Assert.Equal("/jobs", context.Request.Path.Value);
            return Task.CompletedTask;
        }), CancellationToken.None);
        var request = new Exchange(scheme: "sb");
        factory.Listener.RequestHandler(request);
        await request.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(200, request.StatusCode);
    }

    [Fact]
    public async Task ExistingJobControllersShareRepositoryAcrossRelayAndHttp()
    {
        var factory = new ListenerFactory();
        var repository = new InMemoryJobRepository();
        using var host = CreateHost(factory, true, app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("jobs/{id}/touch", JobsApis.GetTouch);
            });
        }, services =>
        {
            services.AddSingleton<IJobRepository>(repository);
            services.AddControllers().AddApplicationPart(typeof(JobsController).Assembly).AddNewtonsoftJson();
        });
        await host.StartAsync();

        var post = new Exchange("POST", "/jobs", "{\"driverVersion\":2}");
        post.Headers["Content-Type"] = "application/json";
        factory.Listener.RequestHandler(post);
        await post.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(202, post.StatusCode);
        var location = post.ResponseHeaders["Location"].ToString();
        Assert.StartsWith("/jobs/", location);

        var get = new Exchange("GET", location);
        factory.Listener.RequestHandler(get);
        await get.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(200, get.StatusCode);
        using var client = new HttpClient();
        var address = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
        Assert.Equal(Encoding.UTF8.GetString(get.Output.ToArray()), await client.GetStringAsync(address + location));
        var touch = new Exchange("GET", location + "/touch");
        factory.Listener.RequestHandler(touch);
        await touch.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(200, touch.StatusCode);
        Assert.Empty(touch.Output.ToArray());
        await host.StopAsync();
    }

    [Theory]
    [InlineData(false, 65537)]
    [InlineData(true, 65537)]
    [InlineData(true, 8388625)]
    public async Task ExistingControllerAcceptsUnknownLengthBuildUpload(bool gzip, int length)
    {
        var factory = new ListenerFactory();
        var repository = new InMemoryJobRepository();
        var job = repository.Add(new Job { State = JobState.Initializing });
        long? requestLimit = null;
        using var host = CreateHost(factory, false, app =>
        {
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                await next();
                requestLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize;
            });
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }, services =>
        {
            services.AddSingleton<IJobRepository>(repository);
            services.AddControllers().AddApplicationPart(typeof(JobsController).Assembly).AddNewtonsoftJson();
        });
        await host.StartAsync();
        var payload = new byte[length];
        new Random(17).NextBytes(payload);
        var wirePayload = payload;
        if (gzip)
        {
            using var compressed = new MemoryStream();
            using (var compressor = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                compressor.Write(payload);
            }
            wirePayload = compressed.ToArray();
        }

        try
        {
            var upload = new Exchange("POST", $"/jobs/{job.Id}/build", data: wirePayload, asyncInput: true);
            upload.Headers["destinationFilename"] = "artifact.bin";
            if (gzip)
            {
                upload.Headers["Content-Encoding"] = "gzip";
            }
            factory.Listener.RequestHandler(upload);
            await upload.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(200, upload.StatusCode);
            Assert.Equal(10_000_000_000L, requestLimit);
            var attachment = Assert.Single(job.BuildAttachments);
            Assert.Equal("artifact.bin", attachment.Filename);
            Assert.Equal(payload, await File.ReadAllBytesAsync(attachment.TempFilename));
        }
        finally
        {
            await host.StopAsync();
            foreach (var attachment in job.BuildAttachments)
            {
                File.Delete(attachment.TempFilename);
            }
        }
    }

    [Fact]
    public async Task OpenFailureClosesListenerAndRollsBackKestrel()
    {
        var factory = new ListenerFactory();
        using var host = CreateHost(factory, true, app => app.Run(_ => Task.CompletedTask));
        string address = null;
        factory.Listener.Open = _ =>
        {
            address = Assert.Single(host.Services.GetRequiredKeyedService<IServer>(RelayWebHostBuilderExtensions.HttpServerKey)
                .Features.Get<IServerAddressesFeature>().Addresses);
            throw new IOException("open failed");
        };
        await Assert.ThrowsAsync<IOException>(() => host.StartAsync());
        Assert.Equal(1, factory.Listener.CloseCount);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(address));
    }

    [Fact]
    public async Task OpenCancellationAndRepeatedDisposalCloseOnce()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Listener.Open = async token =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        };
        var server = CreateServer(factory);
        using var cancelled = new CancellationTokenSource();
        var start = server.StartAsync(new Application(_ => Task.CompletedTask), cancelled.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        server.Dispose();
        server.Dispose();
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task StopCancelsConcurrentOpen()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Listener.Open = async token =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        };
        using var server = CreateServer(factory);
        var start = server.StartAsync(new Application(_ => Task.CompletedTask), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await server.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateOpenCannotReactivateServerAndRetainsTokenUntilClose(bool failOpen)
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOpen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseClose = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken openToken = default;
        factory.Listener.Open = async token =>
        {
            openToken = token;
            entered.SetResult();
            await releaseOpen.Task;
            opened.SetResult();
            if (failOpen)
            {
                throw new IOException("late open failure");
            }
        };
        factory.Listener.Close = async _ =>
        {
            await opened.Task;
            await releaseClose.Task;
            closed.SetResult(Record.Exception(() => Assert.True(openToken.WaitHandle.WaitOne(0))));
        };
        using var server = CreateServer(factory, new RelayServerOptions(ConnectionString)
        {
            CloseTimeout = TimeSpan.FromMilliseconds(30)
        });
        var application = new Application(_ => throw new InvalidOperationException("Late requests must not be admitted"));
        using var cancellation = new CancellationTokenSource();
        var start = server.StartAsync(application, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(TimeSpan.FromSeconds(5)));
            await server.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            releaseOpen.SetResult();
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Empty(server.Features.Get<IServerAddressesFeature>().Addresses);
            var rejected = new Exchange();
            factory.Listener.RequestHandler(rejected);
            await rejected.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(503, rejected.StatusCode);
            Assert.Equal(0, application.DisposeCount);
            await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync(application, CancellationToken.None));
        }
        finally
        {
            releaseOpen.TrySetResult();
            releaseClose.TrySetResult();
            await server.StopAsync(CancellationToken.None);
            Assert.Null(await closed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task SynchronouslyBlockedOpenDoesNotBlockCancellationOrStop()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        factory.Listener.Open = _ =>
        {
            entered.SetResult();
            release.Wait();
            finished.SetResult();
            return Task.CompletedTask;
        };
        using var server = CreateServer(factory);
        using var cancellation = new CancellationTokenSource();
        var start = server.StartAsync(new Application(_ => Task.CompletedTask), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(TimeSpan.FromSeconds(5)));
            await server.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            release.Set();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task StopDrainsWithTransportOpenAndRejectsNewRequests()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = CreateServer(factory);
        var application = new Application(async context =>
        {
            entered.TrySetResult();
            await release.Task;
            Assert.Equal(0, factory.Listener.CloseCount);
            await context.Response.WriteAsync("finished");
        });
        await server.StartAsync(application, CancellationToken.None);
        var request = new Exchange();
        factory.Listener.RequestHandler(request);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = server.StopAsync(CancellationToken.None);
        var rejected = new Exchange();
        factory.Listener.RequestHandler(rejected);
        await rejected.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(503, rejected.StatusCode);
        Assert.False(stop.IsCompleted);
        release.SetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, application.DisposeCount);
        Assert.Equal(1, request.CloseCount);
        Assert.Equal(1, rejected.CloseCount);
    }

    [Fact]
    public async Task DrainDeadlineCancelsRequestAndObservesItsCleanup()
    {
        var factory = new ListenerFactory();
        using var server = CreateServer(factory, new RelayServerOptions(ConnectionString)
        {
            DrainTimeout = TimeSpan.FromMilliseconds(30)
        });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var application = new Application(async context =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        });
        await server.StartAsync(application, CancellationToken.None);
        var exchange = new Exchange();
        factory.Listener.RequestHandler(exchange);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await server.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, application.DisposeCount);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task RequestsDuringOpenAreRejectedAndRepeatedStartOpensOnce()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Listener.Open = async _ =>
        {
            entered.SetResult();
            await release.Task;
        };
        using var server = CreateServer(factory);
        var application = new Application(_ => throw new InvalidOperationException("Must not dispatch before open"));
        var start = server.StartAsync(application, CancellationToken.None);
        Assert.Same(start, server.StartAsync(application, CancellationToken.None));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var request = new Exchange();
        factory.Listener.RequestHandler(request);
        await request.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(503, request.StatusCode);
        release.SetResult();
        await start;
        Assert.Equal(1, factory.Listener.OpenCount);
        Assert.Equal(0, application.DisposeCount);
    }

    [Fact]
    public async Task UncancellableResponseCloseIsObservedAfterBoundedStop()
    {
        var factory = new ListenerFactory();
        var logger = new RecordingLogger();
        using var server = new RelayServer(new RelayServerOptions(ConnectionString)
        {
            DrainTimeout = TimeSpan.FromMilliseconds(30),
            CancellationGracePeriod = TimeSpan.FromMilliseconds(30)
        }, factory, logger);
        var application = new Application(_ => Task.CompletedTask);
        await server.StartAsync(application, CancellationToken.None);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new Exchange { Close = () => release.Task };
        factory.Listener.RequestHandler(request);
        await request.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await server.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains(logger.Messages, message => message.Contains("still completing"));
        release.SetException(new IOException("late close failure"));
        await application.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, request.CloseCount);
        Assert.Equal(1, application.DisposeCount);
    }

    [Fact]
    public async Task ListenerCloseFailureIsLoggedAndRepeatedStopDoesNotCloseAgain()
    {
        var factory = new ListenerFactory();
        factory.Listener.Close = _ => throw new IOException("close failed");
        var logger = new RecordingLogger();
        using var server = new RelayServer(new RelayServerOptions(ConnectionString), factory, logger);
        await server.StartAsync(new Application(_ => Task.CompletedTask), CancellationToken.None);
        await server.StopAsync(CancellationToken.None);
        await server.StopAsync(CancellationToken.None);
        Assert.Contains(logger.Messages, message => message.Contains("Failed to close"));
        Assert.Equal(1, factory.Listener.CloseCount);
    }

    [Fact]
    public async Task ApplicationFailureDoesNotCancelFollowingRequestsOrRestartTheListener()
    {
        var factory = new ListenerFactory();
        using var server = CreateServer(factory);
        var application = new Application(context => context.Request.Path == "/fail"
            ? Task.FromException(new IOException("request failed"))
            : context.Response.WriteAsync("healthy"));
        await server.StartAsync(application, CancellationToken.None);
        var failed = new Exchange("GET", "/fail");
        factory.Listener.RequestHandler(failed);
        await failed.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(500, failed.StatusCode);

        var following = new Exchange();
        factory.Listener.RequestHandler(following);
        await following.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(200, following.StatusCode);
        Assert.Equal("healthy", Encoding.UTF8.GetString(following.Output.ToArray()));
        Assert.Equal(1, factory.Listener.OpenCount);
        Assert.Equal(0, factory.Listener.CloseCount);

        await server.StopAsync(CancellationToken.None);
        Assert.Equal(2, application.DisposeCount);
        Assert.Equal(1, failed.CloseCount);
        Assert.Equal(1, following.CloseCount);
    }

    [Fact]
    public async Task ConcurrentUploadDoesNotStarveTouchAndSynchronousCompletionsAreTracked()
    {
        var factory = new ListenerFactory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = CreateServer(factory);
        var application = new Application(async context =>
        {
            if (context.Request.Path == "/upload")
            {
                entered.SetResult();
                await release.Task;
            }
            context.Response.StatusCode = 204;
        });
        await server.StartAsync(application, CancellationToken.None);
        factory.Listener.RequestHandler(new Exchange("POST", "/upload"));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var touches = Enumerable.Range(0, 100).Select(_ => new Exchange("GET", "/jobs/1/touch")).ToArray();
        foreach (var touch in touches)
        {
            factory.Listener.RequestHandler(touch);
        }
        await Task.WhenAll(touches.Select(touch => touch.Closed.Task)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.All(touches, touch => Assert.Equal(204, touch.StatusCode));
        release.SetResult();
        await server.StopAsync(CancellationToken.None);
        Assert.Equal(101, application.DisposeCount);
    }

    [Fact]
    public async Task OptionsForwardExplicitTokenProviderWithoutExposingItInPublicAddress()
    {
        var tokenProvider = TokenProvider.CreateAzureActiveDirectoryTokenProvider(
            (_, _, _) => Task.FromResult("unused-token"), null);
        var options = new RelayServerOptions(ConnectionString, tokenProvider);
        var factory = new ListenerFactory();
        using var server = CreateServer(factory, options);
        await server.StartAsync(new Application(_ => Task.CompletedTask), CancellationToken.None);
        Assert.Same(tokenProvider, factory.Options.TokenProvider);
        Assert.DoesNotContain("SharedAccess", options.PublicAddress.AbsoluteUri);
        Assert.DoesNotContain("unused-token", options.PublicAddress.AbsoluteUri);
        // Exercise the published SDK constructors, not just fake registration.
        await new RelayListenerFactory().Create(options).CloseAsync(CancellationToken.None);
        await new RelayListenerFactory().Create(new RelayServerOptions(ConnectionString)).CloseAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("Endpoint=sb://unused.invalid/;")]
    [InlineData("Endpoint=https://unused.invalid/;EntityPath=test;")]
    [InlineData("Endpoint=sb://unused.invalid/;EntityPath=../test;")]
    [InlineData("Endpoint=sb://unused.invalid/;EntityPath=https://different.invalid/test;")]
    public void InvalidOptionsFailBeforeOpening(string connectionString)
        => Assert.Throws<ArgumentException>(() => new RelayServerOptions(connectionString));

    private static RelayServer CreateServer(ListenerFactory factory, RelayServerOptions options = null)
        => new(options ?? new RelayServerOptions(ConnectionString), factory, NullLogger<RelayServer>.Instance);

    private static IHost CreateHost(ListenerFactory factory, bool enableHttp, Action<IApplicationBuilder> configure,
        Action<IServiceCollection> services = null, bool configureKestrelFirst = true)
        => new HostBuilder().ConfigureWebHost(web =>
        {
            if (configureKestrelFirst)
            {
                web.UseKestrel().ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024);
            }
            web.UseUrls("http://127.0.0.1:0");
            web.ConfigureServices(collection =>
            {
                collection.AddSingleton<IRelayListenerFactory>(factory);
                services?.Invoke(collection);
            });
            web.UseCrankRelay(new RelayServerOptions(ConnectionString), enableHttp);
            web.Configure(configure);
        }).Build();

    private sealed class ListenerFactory : IRelayListenerFactory
    {
        public Listener Listener { get; } = new();
        public RelayServerOptions Options { get; private set; }
        public IRelayListener Create(RelayServerOptions options)
        {
            Options = options;
            return Listener;
        }
    }

    private sealed class Listener : IRelayListener
    {
        public Action<RelayHttpExchange> RequestHandler { get; set; }
        public Func<CancellationToken, Task> Open { get; set; } = _ => Task.CompletedTask;
        public Func<CancellationToken, Task> Close { get; set; } = _ => Task.CompletedTask;
        public int OpenCount { get; private set; }
        public int CloseCount { get; private set; }
        public Task OpenAsync(CancellationToken cancellationToken)
        {
            OpenCount++;
            return Open(cancellationToken);
        }
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            CloseCount++;
            return Close(cancellationToken);
        }
    }

    private sealed class Application(Func<DefaultHttpContext, Task> process) : IHttpApplication<DefaultHttpContext>
    {
        private int _disposeCount;
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int DisposeCount => _disposeCount;
        public DefaultHttpContext CreateContext(IFeatureCollection features) => new(features);
        public Task ProcessRequestAsync(DefaultHttpContext context) => process(context);
        public void DisposeContext(DefaultHttpContext context, Exception exception)
        {
            Interlocked.Increment(ref _disposeCount);
            Disposed.TrySetResult();
        }
    }

    private sealed class Exchange(string method = "GET", string path = "/jobs", string body = "", byte[] data = null,
        bool asyncInput = false, string scheme = "https") : RelayHttpExchange
    {
        public TaskCompletionSource Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Func<Task> Close { get; init; } = () => Task.CompletedTask;
        public MemoryStream Output { get; } = new();
        public override Uri Url { get; } = new(scheme + "://unused.invalid/test/nested" + path);
        public override string Method => method;
        public override WebHeaderCollection Headers { get; } = new();
        public override bool HasEntityBody => body.Length != 0 || data is { Length: > 0 };
        public override Stream InputStream { get; } = asyncInput
            ? new AsyncInputStream(data ?? Encoding.UTF8.GetBytes(body))
            : new MemoryStream(data ?? Encoding.UTF8.GetBytes(body));
        public override Stream OutputStream => Output;
        public override IPEndPoint RemoteEndPoint => null;
        public int StatusCode { get; private set; }
        public IHeaderDictionary ResponseHeaders { get; private set; }
        public int CloseCount { get; private set; }
        public override void Commit(int statusCode, string reasonPhrase, IHeaderDictionary headers)
        {
            StatusCode = statusCode;
            ResponseHeaders = headers;
        }
        public override Task CloseAsync()
        {
            CloseCount++;
            Closed.TrySetResult();
            return Close();
        }
    }

    private sealed class AsyncInputStream(byte[] data) : MemoryStream(data)
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return base.Read(buffer.Span);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return base.Read(buffer, offset, count);
        }
    }

    private sealed class RecordingLogger : ILogger<RelayServer>
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => Messages.Enqueue(formatter(state, exception));
    }
}
