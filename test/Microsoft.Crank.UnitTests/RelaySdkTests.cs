// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Relay;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Crank.UnitTests;

public class RelaySdkTests
{
    private const string ConnectionString = "Endpoint=sb://unused.invalid/;EntityPath=entity;" +
        "SharedAccessKeyName=test;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private static readonly Uri PublicAddress = new("https://unused.invalid/entity");
    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [Fact]
    public async Task SdkListenerRejectsUnusedConnectionUpgrades()
    {
        var fixture = CreateSdkExchange();
        try
        {
            Assert.False(await fixture.Listener.AcceptHandler(fixture.Context));
            Assert.Equal(HttpStatusCode.NotImplemented, fixture.Context.Response.StatusCode);
            Assert.Equal("Crank Relay supports HTTP requests only.", fixture.Context.Response.StatusDescription);
        }
        finally
        {
            await fixture.Context.Response.CloseAsync();
            await fixture.Listener.CloseAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SdkListenerRetainsConfiguredTokenProviderForSdkManagedAuthentication()
    {
        var tokenRequests = 0;
        var provider = TokenProvider.CreateAzureActiveDirectoryTokenProvider((_, _, _) =>
        {
            Interlocked.Increment(ref tokenRequests);
            return Task.FromResult("not-used");
        }, null);
        var options = new RelayServerOptions("Endpoint=sb://unused.invalid/;EntityPath=entity;", provider);
        var fixture = CreateSdkExchange(options);
        try
        {
            Assert.Same(provider, fixture.Listener.TokenProvider);
            Assert.Equal(options.ListenerAddress, fixture.Listener.Address);
            Assert.Equal(0, tokenRequests);
        }
        finally
        {
            await fixture.Context.Response.CloseAsync();
            await fixture.Listener.CloseAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CompleteAsyncClosesResponseBeforeApplicationReturns()
    {
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            context.Response.StatusCode = 202;
            await context.Response.CompleteAsync();
            Assert.Equal(1, exchange.CloseCount);
        });

        await new RelayHttpContext(exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);

        Assert.Null(application.Error);
        Assert.Equal(1, exchange.CloseCount);
    }

    [Fact]
    public async Task SdkCompleteAsyncClosesButDoesNotRunCompletionCallbacks()
    {
        var fixture = CreateSdkExchange();
        var socket = AttachSdkResponseStream(fixture.Context, fixture.Listener);
        var completed = false;
        var application = new Application(async context =>
        {
            context.Response.OnCompleted(() =>
            {
                completed = true;
                return Task.CompletedTask;
            });
            await context.Response.CompleteAsync();
            Assert.False(completed);
            Assert.Equal(WebSocketState.Closed, socket.State);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await context.Response.Body.WriteAsync(new byte[] { 1 }));
        });

        await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);

        Assert.True(completed);
        Assert.Equal(1, socket.CloseCount);
        Assert.Null(application.Error);
        await fixture.Listener.CloseAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResponseCloseFailureIsNotRetried(bool synchronous)
    {
        var exchange = new Exchange { CloseError = new IOException("close failed"), ThrowSynchronously = synchronous };
        var application = new Application(async context => await context.Response.CompleteAsync());
        await new RelayHttpContext(exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);
        Assert.Equal(1, exchange.CloseCount);
    }

    [Fact]
    public async Task CompleteAsyncRejectsFurtherBodyWritesAndRunsCleanupOnce()
    {
        var exchange = new Exchange();
        var completions = 0;
        var application = new Application(async context =>
        {
            context.Response.OnCompleted(() =>
            {
                completions++;
                return Task.CompletedTask;
            });
            await context.Response.CompleteAsync();
            Assert.Equal(0, completions);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await context.Response.Body.WriteAsync(new byte[] { 1 }));
        });
        await new RelayHttpContext(exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);
        Assert.Null(application.Error);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, completions);
    }

    [Fact]
    public async Task SdkErrorResponseDoesNotRetainPartiallyCommittedHeaders()
    {
        var fixture = CreateSdkExchange();
        var application = new Application(context =>
        {
            context.Response.ContentLength = 12345;
            context.Response.Headers.Location = "/jobs/uncreated";
            context.Response.Headers["Invalid Header Name"] = "invalid";
            return Task.CompletedTask;
        });

        await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);

        Assert.NotNull(application.Error);
        Assert.Equal(HttpStatusCode.InternalServerError, fixture.Context.Response.StatusCode);
        Assert.Null(fixture.Context.Response.Headers["Content-Length"]);
        Assert.Null(fixture.Context.Response.Headers["Location"]);
        await fixture.Listener.CloseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SdkApplicationFailureCannotBeMaskedByAStartingCallbackWrite()
    {
        var fixture = CreateSdkExchange();
        var socket = AttachSdkResponseStream(fixture.Context, fixture.Listener);
        var started = false;
        var application = new Application(context =>
        {
            context.Response.StatusCode = 202;
            context.Response.OnStarting(async () =>
            {
                started = true;
                await context.Response.Body.WriteAsync(new byte[] { 42 });
            });
            throw new IOException("application failed");
        });
        try
        {
            await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);
            Assert.Equal(HttpStatusCode.InternalServerError, fixture.Context.Response.StatusCode);
            Assert.False(started);
            Assert.Equal(0, socket.BinaryBytes);
            Assert.NotNull(application.Error);
            Assert.Equal(WebSocketState.Closed, socket.State);
        }
        finally
        {
            await fixture.Listener.CloseAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SdkStartingCallbackFailureCannotBeMaskedByALaterCallbackWrite()
    {
        var fixture = CreateSdkExchange();
        var socket = AttachSdkResponseStream(fixture.Context, fixture.Listener);
        var wrote = false;
        var application = new Application(context =>
        {
            context.Response.OnStarting(async () =>
            {
                wrote = true;
                await context.Response.Body.WriteAsync(new byte[] { 42 });
            });
            context.Response.OnStarting(() => throw new IOException("starting failed"));
            return Task.CompletedTask;
        });
        try
        {
            await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);
            Assert.Equal(HttpStatusCode.InternalServerError, fixture.Context.Response.StatusCode);
            Assert.False(wrote);
            Assert.Equal(0, socket.BinaryBytes);
            Assert.NotNull(application.Error);
        }
        finally
        {
            await fixture.Listener.CloseAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KestrelDoesNotRunStartingCallbacksAfterAnApplicationOrCallbackFailure(bool failInCallback)
    {
        var wrote = 0;
        using var host = new HostBuilder().ConfigureWebHost(web =>
            web.UseKestrel().UseUrls("http://127.0.0.1:0").Configure(app => app.Run(context =>
            {
                context.Response.OnStarting(async () =>
                {
                    Interlocked.Increment(ref wrote);
                    await context.Response.Body.WriteAsync(new byte[] { 42 });
                });
                if (failInCallback)
                {
                    context.Response.OnStarting(() => throw new IOException("starting failed"));
                    return Task.CompletedTask;
                }
                return Task.FromException(new IOException("application failed"));
            }))).Build();
        await host.StartAsync();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var address = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
            using var response = await client.GetAsync(address);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(0, Volatile.Read(ref wrote));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task SdkOnStartingReentrantWriteDoesNotCommitTwice()
    {
        var fixture = CreateSdkExchange();
        var socket = AttachSdkResponseStream(fixture.Context, fixture.Listener);
        var application = new Application(async context =>
        {
            context.Response.OnStarting(async () => await context.Response.Body.WriteAsync(new byte[] { 42 }));
            await context.Response.StartAsync();
        });

        await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);

        Assert.Null(application.Error);
        Assert.Equal(WebSocketState.Closed, socket.State);
        await fixture.Listener.CloseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SdkAsynchronousOnStartingWriteDoesNotAwaitItsOwnStartTask()
    {
        var fixture = CreateSdkExchange();
        AttachSdkResponseStream(fixture.Context, fixture.Listener);
        Task write = null;
        var application = new Application(async context =>
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            context.Response.OnStarting(async () =>
            {
                await release.Task;
                write = context.Response.Body.WriteAsync(new byte[] { 42 }).AsTask();
                await write.WaitAsync(TimeSpan.FromSeconds(2));
            });
            var start = context.Response.StartAsync();
            release.SetResult();
            await start;
        });

        await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application)
            .WaitAsync(TimeSpan.FromSeconds(3));
        if (write != null)
        {
            await Record.ExceptionAsync(() => write);
        }
        await fixture.Listener.CloseAsync(CancellationToken.None);
        Assert.Null(application.Error);
    }

    [Fact]
    public async Task SdkStreamingResponseCompletesWithoutNetwork()
    {
        var fixture = CreateSdkExchange();
        var socket = AttachSdkResponseStream(fixture.Context, fixture.Listener);
        var application = new Application(async context =>
        {
            context.Response.Headers["X-Review"] = "actual SDK";
            await context.Response.Body.WriteAsync(new byte[65537]);
            Assert.True(context.Response.HasStarted);
        });

        await new RelayHttpContext(fixture.Exchange, PublicAddress, default, NullLogger.Instance).ProcessAsync(application);

        Assert.Null(application.Error);
        Assert.Equal(65537, socket.BinaryBytes);
        Assert.Equal(WebSocketState.Closed, socket.State);
        await fixture.Listener.CloseAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KestrelAllowsOnStartingReentrantWrite(bool asynchronous)
    {
        var finished = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder().ConfigureWebHost(web =>
            web.UseKestrel().UseUrls("http://127.0.0.1:0").Configure(app => app.Run(async context =>
            {
                try
                {
                    context.Response.OnStarting(async () =>
                    {
                        if (asynchronous)
                        {
                            await Task.Yield();
                        }
                        await context.Response.Body.WriteAsync(new byte[] { 42 });
                    });
                    await context.Response.StartAsync();
                    finished.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    finished.TrySetResult(exception);
                    throw;
                }
            }))).Build();
        await host.StartAsync();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            var address = Assert.Single(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses);
            Assert.Equal("*", await client.GetStringAsync(address));
            Assert.Null(await finished.Task.WaitAsync(TimeSpan.FromSeconds(3)));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task StopDoesNotWaitForUncancellableSdkTokenAcquisition()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var token = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = TokenProvider.CreateAzureActiveDirectoryTokenProvider((_, _, _) =>
        {
            entered.TrySetResult();
            return token.Task;
        }, null);
        using var server = new RelayServer(new RelayServerOptions(ConnectionString + ";OperationTimeout=00:00:00.050", provider)
        {
            CloseTimeout = TimeSpan.FromMilliseconds(30),
            DrainTimeout = TimeSpan.FromMilliseconds(30),
            CancellationGracePeriod = TimeSpan.FromMilliseconds(30)
        }, new RelayListenerFactory(), NullLogger<RelayServer>.Instance);
        var start = server.StartAsync(new Application(_ => Task.CompletedTask), CancellationToken.None);
        var stopCompleted = false;
        Task stop = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            stop = server.StopAsync(new CancellationToken(canceled: true));
            await Task.WhenAny(stop, Task.Delay(500));
            stopCompleted = stop.IsCompleted;
        }
        finally
        {
            token.TrySetException(new IOException("release blocked authentication without making a network connection"));
            await Record.ExceptionAsync(() => start);
            if (stop != null)
            {
                await stop.WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        Assert.True(stopCompleted, "Stop remained blocked on SDK token acquisition despite cancelled shutdown and 50ms open timeout.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartDeadlineBoundsUncancellableSdkTokenAcquisition(bool externalCancellation)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var token = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = TokenProvider.CreateAzureActiveDirectoryTokenProvider((_, _, _) =>
        {
            entered.TrySetResult();
            return token.Task;
        }, null);
        var connectionString = externalCancellation ? ConnectionString : ConnectionString + ";OperationTimeout=00:00:00.050";
        using var server = new RelayServer(new RelayServerOptions(connectionString, provider),
            new RelayListenerFactory(), NullLogger<RelayServer>.Instance);
        using var cancellation = new CancellationTokenSource();
        var start = server.StartAsync(new Application(_ => Task.CompletedTask), cancellation.Token);
        var startCompleted = false;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            if (externalCancellation)
            {
                cancellation.Cancel();
            }
            await Task.WhenAny(start, Task.Delay(500));
            startCompleted = start.IsCompleted;
        }
        finally
        {
            token.TrySetException(new IOException("release blocked authentication without making a network connection"));
            await Record.ExceptionAsync(() => start);
            await server.StopAsync(CancellationToken.None);
        }
        Assert.True(startCompleted, externalCancellation
            ? "Start remained blocked after its caller cancelled."
            : "Start remained blocked after its 50ms OperationTimeout.");
    }

    private static (RelayHttpExchange Exchange, RelayedHttpListenerContext Context, HybridConnectionListener Listener) CreateSdkExchange(
        RelayServerOptions options = null)
    {
        // SDK HTTP contexts are not publicly constructible. Keep reflection in this test
        // harness so real SDK header/stream behavior is covered without a Relay service.
        var wrapper = new RelayListenerFactory().Create(options ?? new RelayServerOptions(ConnectionString));
        var listener = (HybridConnectionListener)wrapper.GetType().GetField("_listener", InstanceMembers).GetValue(wrapper);
        var context = (RelayedHttpListenerContext)Activator.CreateInstance(typeof(RelayedHttpListenerContext),
            InstanceMembers, null,
            new object[] { listener, new Uri("sb://unused.invalid/entity/jobs"), Guid.NewGuid().ToString(), "GET", new Dictionary<string, string>() },
            null);
        RelayHttpExchange exchange = null;
        wrapper.RequestHandler = value => exchange = value;
        listener.RequestHandler(context);
        return (exchange, context, listener);
    }

    private static RecordingWebSocket AttachSdkResponseStream(RelayedHttpListenerContext context, HybridConnectionListener listener)
    {
        var connectionType = typeof(HybridConnectionListener).Assembly.GetType("Microsoft.Azure.Relay.HybridHttpConnection");
        var connection = Activator.CreateInstance(connectionType, InstanceMembers, null,
            new object[] { listener, null, "wss://unused.invalid/$hc/entity?id=" + Guid.NewGuid() }, null);
        var socket = new RecordingWebSocket();
        connectionType.GetField("rendezvousWebSocket", InstanceMembers).SetValue(connection, socket);
        var responseStreamType = connectionType.GetNestedType("ResponseStream", BindingFlags.NonPublic);
        var stream = (Stream)Activator.CreateInstance(responseStreamType, InstanceMembers, null,
            new object[] { connection, context }, null);
        typeof(RelayedHttpListenerResponse).GetProperty(nameof(RelayedHttpListenerResponse.OutputStream)).SetValue(context.Response, stream);
        return socket;
    }

    private sealed class Application(Func<DefaultHttpContext, Task> process) : IHttpApplication<DefaultHttpContext>
    {
        public Exception Error { get; private set; }
        public DefaultHttpContext CreateContext(IFeatureCollection features) => new(features);
        public Task ProcessRequestAsync(DefaultHttpContext context) => process(context);
        public void DisposeContext(DefaultHttpContext context, Exception exception) => Error = exception;
    }

    private sealed class Exchange : RelayHttpExchange
    {
        public int CloseCount { get; private set; }
        public Exception CloseError { get; init; }
        public bool ThrowSynchronously { get; init; }
        public override Uri Url { get; } = new("sb://unused.invalid/entity/jobs");
        public override string Method => "GET";
        public override WebHeaderCollection Headers { get; } = new();
        public override bool HasEntityBody => false;
        public override Stream InputStream => Stream.Null;
        public override Stream OutputStream => Stream.Null;
        public override IPEndPoint RemoteEndPoint => null;
        public override void Commit(int statusCode, string reasonPhrase, IHeaderDictionary headers) { }
        public override Task CloseAsync()
        {
            CloseCount++;
            if (ThrowSynchronously)
            {
                throw CloseError;
            }
            return CloseError == null ? Task.CompletedTask : Task.FromException(CloseError);
        }
    }

    private sealed class RecordingWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;
        public int BinaryBytes { get; private set; }
        public int CloseCount { get; private set; }
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string SubProtocol => null;
        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() => _state = WebSocketState.Closed;
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            CloseCount++;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            => CloseAsync(closeStatus, statusDescription, cancellationToken);
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (messageType == WebSocketMessageType.Binary)
            {
                BinaryBytes += buffer.Count;
            }
            return Task.CompletedTask;
        }
    }
}
