// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Crank.Agent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Crank.UnitTests;

public class RelayHttpContextTests
{
    private static readonly Uri PublicAddress = new("https://example.servicebus.windows.net/entity");

    [Theory]
    [InlineData("/entity/jobs/a%2Fb%20c?name=a%2Fb&other=%252F", "/entity", "/jobs/a%2Fb c")]
    [InlineData("/entity/jobs/entity", "/entity", "/jobs/entity")]
    [InlineData("/entity", "/entity", "")]
    [InlineData("/entity/", "/entity", "/")]
    [InlineData("/entityish/jobs", "", "/entityish/jobs")]
    public async Task RequestPreservesPublicUriAndCustomHeaders(string target, string pathBase, string path)
    {
        var exchange = new Exchange { Address = new Uri("https://example.servicebus.windows.net" + target), Verb = "PATCH" };
        exchange.RequestHeaders.Add("filename", "name.zip");
        exchange.RequestHeaders.Add("destinationFilename", "destination.zip");
        exchange.RequestHeaders.Add("id", "42");
        exchange.RequestHeaders.Add("X-Custom", "one");
        exchange.RequestHeaders.Add("X-Custom", "two");
        var application = new Application(context =>
        {
            Assert.Equal("PATCH", context.Request.Method);
            Assert.Equal("HTTP/1.1", context.Request.Protocol);
            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal("example.servicebus.windows.net", context.Request.Host.Value);
            Assert.Equal(pathBase, context.Request.PathBase.Value);
            Assert.Equal(path, context.Request.Path.Value);
            Assert.Equal(exchange.Url.Query, context.Request.QueryString.Value);
            Assert.Equal(target, context.Features.Get<IHttpRequestFeature>().RawTarget);
            Assert.Equal("name.zip", context.Request.Headers["filename"]);
            Assert.Equal("destination.zip", context.Request.Headers["destinationFilename"]);
            Assert.Equal("42", context.Request.Headers["id"]);
            Assert.Equal(new[] { "one", "two" }, context.Request.Headers["X-Custom"].ToArray());
            Assert.Equal(IPAddress.Loopback, context.Connection.RemoteIpAddress);
            Assert.Equal(1234, context.Connection.RemotePort);
            Assert.Null(context.Features.Get<IHttpUpgradeFeature>());
            Assert.Null(context.Features.Get<IHttpWebSocketFeature>());
            Assert.Null(context.Features.Get<IHttpResponseTrailersFeature>());
            return Task.CompletedTask;
        });

        await Process(exchange, application);

        Assert.Equal(200, exchange.Status);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task EncodedEntityPrefixIsDecodedOnlyAsAPathSegment()
    {
        var address = new Uri("https://example.servicebus.windows.net/ent%20ity%2Fchild/");
        var exchange = new Exchange { Address = new Uri(address, "jobs/a%2Fb") };
        var application = new Application(context =>
        {
            Assert.Equal("/ent ity%2Fchild", context.Request.PathBase.Value);
            Assert.Equal("/jobs/a%2Fb", context.Request.Path.Value);
            return Task.CompletedTask;
        });
        await new RelayHttpContext(exchange, address, default, new Logger()).ProcessAsync(application);
        Assert.Null(application.Error);
    }

    [Fact]
    public async Task TransportSchemeDoesNotLeakIntoThePublicRequest()
    {
        var exchange = new Exchange { Address = new Uri("sb://example.servicebus.windows.net/entity/jobs?value=a%2Fb") };
        var application = new Application(context =>
        {
            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal("example.servicebus.windows.net", context.Request.Host.Value);
            Assert.Equal("/entity", context.Request.PathBase.Value);
            Assert.Equal("/jobs", context.Request.Path.Value);
            Assert.Equal("/entity/jobs?value=a%2Fb", context.Features.Get<IHttpRequestFeature>().RawTarget);
            return Task.CompletedTask;
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task UnknownLengthGzipIsNotDecodedOrTreatedAsEmpty()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"name\":\"test\"}");
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(bytes);
        }
        var payload = compressed.ToArray();
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(payload) };
        exchange.RequestHeaders.Add("Content-Encoding", "gzip");
        var application = new Application(async context =>
        {
            Assert.True(context.Features.Get<IHttpRequestBodyDetectionFeature>().CanHaveBody);
            Assert.Null(context.Request.ContentLength);
            Assert.Equal("gzip", context.Request.Headers.ContentEncoding);
            using var received = new MemoryStream();
            await context.Request.Body.CopyToAsync(received);
            Assert.Equal(payload, received.ToArray());
        });

        await Process(exchange, application);
        Assert.Null(application.Error);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task JsonRequestCanRouteAndPreserveTheControllerLocation()
    {
        var exchange = new Exchange
        {
            HasBody = true,
            Verb = "POST",
            Input = new TrackingStream(Encoding.UTF8.GetBytes("{\"id\":42}"))
        };
        exchange.RequestHeaders.Add("Content-Type", "application/json");
        var application = new Application(async context =>
        {
            Assert.Equal("/jobs", context.Request.Path);
            var body = await context.Request.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("id").GetInt32();
            context.Response.StatusCode = 202;
            context.Response.Headers.Location = "/jobs/" + id;
            await context.Response.WriteAsJsonAsync(new
            {
                id,
                url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/jobs/{id}"
            });
        });

        await Process(exchange, application);

        Assert.Null(application.Error);
        Assert.Equal(202, exchange.Status);
        Assert.Equal("/jobs/42", exchange.ResponseHeaders.Location);
        using var document = JsonDocument.Parse(exchange.Output.ToArray());
        Assert.Equal("https://example.servicebus.windows.net/entity/jobs/42", document.RootElement.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("/jobs/1", "/jobs/1")]
    [InlineData("/entity/jobs/1", "/entity/jobs/1")]
    [InlineData("/entity?x=1", "/entity?x=1")]
    [InlineData("/entityish/jobs/1", "/entityish/jobs/1")]
    [InlineData("https://elsewhere/jobs/1", "https://elsewhere/jobs/1")]
    [InlineData("//elsewhere/jobs/1", "//elsewhere/jobs/1")]
    [InlineData("jobs/1", "jobs/1")]
    public async Task LocationIsPreservedExactlyAsProvidedByTheApplication(string location, string expected)
    {
        var exchange = new Exchange();
        await Process(exchange, new Application(context =>
        {
            context.Response.Headers.Location = location;
            return Task.CompletedTask;
        }));
        Assert.Equal(expected, exchange.ResponseHeaders.Location);
    }

    [Fact]
    public async Task StartAndCompleteAreIdempotentAndCallbacksAreLifo()
    {
        var events = new List<string>();
        var exchange = new Exchange();
        var disposed = new CallbackDisposable(() => events.Add("dispose"));
        var application = new Application(async context =>
        {
            var feature = context.Features.Get<IHttpResponseBodyFeature>();
            context.Response.OnStarting(() =>
            {
                Assert.False(context.Response.HasStarted);
                events.Add("start1");
                context.Response.Headers["X-Started"] = "yes";
                return Task.CompletedTask;
            });
            context.Response.OnStarting(() => { events.Add("start2"); return Task.CompletedTask; });
            context.Response.OnCompleted(() => { events.Add("end1"); return Task.CompletedTask; });
            context.Response.RegisterForDispose(disposed);
            context.Response.OnCompleted(() =>
            {
                Assert.Equal(1, exchange.CloseCount);
                events.Add("end2");
                return Task.CompletedTask;
            });
            Assert.False(context.Response.HasStarted);
            await feature.StartAsync();
            await feature.StartAsync();
            Assert.True(context.Response.HasStarted);
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers["Late"] = "value");
            Assert.Throws<InvalidOperationException>(() => context.Response.StatusCode = 500);
            Assert.Throws<InvalidOperationException>(() => context.Response.OnStarting(() => Task.CompletedTask));
            Assert.Throws<InvalidOperationException>(() => context.Features.Get<IHttpResponseFeature>().Headers = new HeaderDictionary());
            await feature.CompleteAsync();
            await feature.CompleteAsync();
        });

        await Process(exchange, application);

        Assert.Null(application.Error);
        Assert.Equal(new[] { "start2", "start1", "end2", "dispose", "end1" }, events);
        Assert.Equal("yes", exchange.ResponseHeaders["X-Started"]);
        Assert.Equal(1, exchange.CommitCount);
        Assert.Equal(1, disposed.Count);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ReentrantStartKeepsCallbackOrderAndOtherCallersAwaitTheOuterStart()
    {
        var events = new List<int>();
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var feature = context.Features.Get<IHttpResponseBodyFeature>();
            context.Response.OnStarting(async () =>
            {
                await Task.Yield();
                events.Add(1);
                context.Response.Headers["X-Starting"] = "ready";
            });
            context.Response.OnStarting(async () =>
            {
                await release.Task;
                events.Add(2);
                await context.Response.Body.WriteAsync(new byte[] { 42 });
                events.Add(3);
            });
            var first = feature.StartAsync();
            Assert.Same(first, feature.StartAsync());
            Assert.False(context.Response.HasStarted);
            release.SetResult();
            await first;
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(new[] { 2, 1, 3 }, events);
        Assert.Equal(1, exchange.CommitCount);
        Assert.Equal("ready", exchange.ResponseHeaders["X-Starting"]);
        Assert.Equal(new byte[] { 42 }, exchange.Output.ToArray());
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task CompleteWaitsForCloseButCleanupWaitsForApplicationReturn()
    {
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodyCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var returnApplication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbacks = 0;
        var exchange = new Exchange { Closing = closing, Closed = closed };
        var application = new Application(async context =>
        {
            context.Response.OnCompleted(() =>
            {
                callbacks++;
                return Task.CompletedTask;
            });
            var feature = context.Features.Get<IHttpResponseBodyFeature>();
            var completion = feature.CompleteAsync();
            Assert.Same(completion, feature.CompleteAsync());
            await completion;
            bodyCompleted.SetResult();
            await returnApplication.Task;
        });
        var process = Process(exchange, application);
        try
        {
            await closing.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(bodyCompleted.Task.IsCompleted);
            Assert.Equal(0, callbacks);
            closed.SetResult();
            await bodyCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, callbacks);
            Assert.Equal(0, application.DisposeCount);
        }
        finally
        {
            closed.TrySetResult();
            returnApplication.TrySetResult();
            await process.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Null(application.Error);
        Assert.Equal(1, callbacks);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task StartWaitsForAsynchronousCallbacksBeforeReportingHasStarted()
    {
        var exchange = new Exchange();
        var callbacks = new List<int>();
        var application = new Application(async context =>
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var body = context.Features.Get<IHttpResponseBodyFeature>();
            context.Response.OnStarting(async () =>
            {
                await release.Task;
                callbacks.Add(1);
            });
            context.Response.OnStarting(async () =>
            {
                callbacks.Add(2);
                await Task.Yield();
            });
            var first = body.StartAsync();
            var second = body.StartAsync();
            Assert.Same(first, second);
            Assert.False(context.Response.HasStarted);
            Assert.Equal(0, exchange.CommitCount);
            release.SetResult();
            await Task.WhenAll(first, second);
            Assert.True(context.Response.HasStarted);
            var completion = body.CompleteAsync();
            Assert.Same(completion, body.CompleteAsync());
            await completion;
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(new[] { 2, 1 }, callbacks);
        Assert.Equal(1, exchange.CommitCount);
    }

    [Fact]
    public async Task PipeWriterPendingBytesAreFlushedAtCompletion()
    {
        var exchange = new Exchange();
        var application = new Application(context =>
        {
            var writer = context.Response.BodyWriter;
            var bytes = Encoding.UTF8.GetBytes("pending JSON");
            bytes.CopyTo(writer.GetMemory(bytes.Length));
            writer.Advance(bytes.Length);
            Assert.False(context.Response.HasStarted);
            return Task.CompletedTask;
        });

        await Process(exchange, application);

        Assert.Null(application.Error);
        Assert.Equal("pending JSON", Encoding.UTF8.GetString(exchange.Output.ToArray()));
        Assert.Equal(1, exchange.CommitCount);
    }

    [Fact]
    public async Task PipeWriterPendingBytesAreDiscardedWhenApplicationFails()
    {
        var exchange = new Exchange();
        var application = new Application(context =>
        {
            var writer = context.Response.BodyWriter;
            writer.GetMemory(4).Span[..4].Fill(42);
            writer.Advance(4);
            throw new IOException("before flush");
        });
        await Process(exchange, application);
        Assert.Equal(500, exchange.Status);
        Assert.Empty(exchange.Output.ToArray());
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ApplicationCanCompleteTheBodyWriterItself()
    {
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            await context.Response.BodyWriter.WriteAsync(new byte[] { 1, 2, 3 });
            await context.Response.BodyWriter.CompleteAsync();
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(new byte[] { 1, 2, 3 }, exchange.Output.ToArray());
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ResponseBodyCanBeWrappedByMiddleware()
    {
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            var original = context.Response.Body;
            await using var gzip = new GZipStream(original, CompressionLevel.Fastest, leaveOpen: true);
            context.Response.Body = gzip;
            await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("wrapped"));
            await context.Response.BodyWriter.FlushAsync();
            context.Response.Body = original;
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        using var compressed = new MemoryStream(exchange.Output.ToArray());
        using var decoder = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(decoder);
        Assert.Equal("wrapped", await reader.ReadToEndAsync());
    }

    [Theory]
    [InlineData("HEAD", 200)]
    [InlineData("GET", 204)]
    [InlineData("GET", 304)]
    public async Task BodylessResponsesCommitWithoutWritingPayload(string method, int status)
    {
        var exchange = new Exchange { Verb = method };
        var application = new Application(async context =>
        {
            context.Response.StatusCode = status;
            context.Response.ContentLength = method == "HEAD" ? 4 : null;
            await context.Response.Body.WriteAsync(new byte[4]);
            await context.Response.BodyWriter.WriteAsync(new byte[4]);
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(status, exchange.Status);
        Assert.Equal(0, exchange.Output.Length);
        Assert.Equal(1, exchange.CommitCount);
    }

    [Fact]
    public async Task EmptyResponseCommitsStatusAndHeaders()
    {
        var exchange = new Exchange();
        await Process(exchange, new Application(context =>
        {
            context.Response.StatusCode = 204;
            context.Response.Headers["X-Empty"] = "true";
            return Task.CompletedTask;
        }));
        Assert.Equal(204, exchange.Status);
        Assert.Equal("true", exchange.ResponseHeaders["X-Empty"]);
        Assert.Equal(0, exchange.Output.Length);
    }

    [Fact]
    public async Task NoContentDoesNotAdvertiseAnUnsentBody()
    {
        var exchange = new Exchange();
        await Process(exchange, new Application(context =>
        {
            context.Response.StatusCode = 204;
            context.Response.ContentLength = 10;
            return Task.CompletedTask;
        }));
        Assert.Equal(204, exchange.Status);
        Assert.Null(exchange.ResponseHeaders.ContentLength);
    }

    [Theory]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(65537)]
    [InlineData(8 * 1024 * 1024 + 17)]
    public async Task UploadAndDownloadStreamAcrossRelayBufferBoundaries(int length)
    {
        var payload = new byte[length];
        new Random(42).NextBytes(payload);
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(payload) };
        var application = new Application(async context =>
        {
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await context.Request.Body.ReadAsync(buffer)) != 0)
            {
                await context.Response.Body.WriteAsync(buffer.AsMemory(0, read));
            }
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(length, exchange.Input.BytesRead);
        Assert.Equal(length, exchange.Output.Length);
        Assert.Equal(payload, exchange.Output.ToArray());
        Assert.InRange(exchange.Input.MaxReadSize, 1, 16 * 1024);
        Assert.InRange(exchange.Output.MaxWriteSize, 1, 16 * 1024);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task SendFileCopiesOnlyRequestedRangeInBoundedChunks()
    {
        var filename = Path.Combine(Directory.GetCurrentDirectory(), "relay-sendfile-" + Guid.NewGuid().ToString("N"));
        var payload = Enumerable.Range(0, 180000).Select(index => (byte)index).ToArray();
        await File.WriteAllBytesAsync(filename, payload);
        try
        {
            var exchange = new Exchange();
            var application = new Application(async context =>
            {
                var feature = context.Features.Get<IHttpResponseBodyFeature>();
                feature.DisableBuffering();
                await feature.SendFileAsync(filename, 17, 140000);
            });
            await Process(exchange, application);
            Assert.Null(application.Error);
            Assert.Equal(payload.Skip(17).Take(140000), exchange.Output.ToArray());
            Assert.InRange(exchange.Output.MaxWriteSize, 1, 64 * 1024);
        }
        finally
        {
            File.Delete(filename);
        }
    }

    [Fact]
    public async Task SendFileFailureCancelsRequestAndIsLogged()
    {
        var exchange = new Exchange();
        var logger = new Logger();
        var application = new Application(async context =>
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                context.Features.Get<IHttpResponseBodyFeature>().SendFileAsync(
                    Path.Combine(Directory.GetCurrentDirectory(), "relay-missing-" + Guid.NewGuid().ToString("N")), 0, null));
            Assert.True(context.RequestAborted.IsCancellationRequested);
        });
        await Process(exchange, application, logger);
        Assert.Contains(logger.Errors, error => error is FileNotFoundException);
        AssertCleanup(exchange, application);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GzipFileResultSupportsAsyncAndSynchronousCompressionWrites(bool gzip)
    {
        var filename = Path.Combine(Directory.GetCurrentDirectory(), "relay-gzip-" + Guid.NewGuid().ToString("N"));
        var payload = Enumerable.Range(0, 65537).Select(index => (byte)index).ToArray();
        await File.WriteAllBytesAsync(filename, payload);
        try
        {
            var exchange = new Exchange();
            if (gzip)
            {
                exchange.RequestHeaders.Add("Accept-Encoding", "gzip");
            }
            var application = new Application(context => new GZipFileResult(filename).ExecuteResultAsync(
                new ActionContext(context, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())));
            await Process(exchange, application);
            Assert.Null(application.Error);
            Assert.Equal(payload.Length.ToString(), exchange.ResponseHeaders["FileLength"]);
            using var result = new MemoryStream(exchange.Output.ToArray());
            using var decoded = new MemoryStream();
            if (gzip)
            {
                Assert.Equal("gzip", exchange.ResponseHeaders.ContentEncoding);
                using var unzip = new GZipStream(result, CompressionMode.Decompress);
                await unzip.CopyToAsync(decoded);
            }
            else
            {
                await result.CopyToAsync(decoded);
            }
            Assert.Equal(payload, decoded.ToArray());
            AssertCleanup(exchange, application);
        }
        finally
        {
            File.Delete(filename);
        }
    }

    [Fact]
    public async Task SynchronousReadWriteAndFlushAreSupported()
    {
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(new byte[] { 1, 2, 3 }) };
        var application = new Application(context =>
        {
            var buffer = new byte[3];
            Assert.Equal(3, context.Request.Body.Read(buffer, 0, buffer.Length));
            context.Response.Body.Write(buffer, 0, buffer.Length);
            context.Response.Body.Flush();
            return Task.CompletedTask;
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(new byte[] { 1, 2, 3 }, exchange.Output.ToArray());
    }

    [Fact]
    public async Task StartingCallbackFailuresAttemptAllCallbacksAndProduce500()
    {
        var events = new List<int>();
        var exchange = new Exchange();
        var logger = new Logger();
        var application = new Application(context =>
        {
            context.Response.OnStarting(() => { events.Add(1); return Task.CompletedTask; });
            context.Response.OnStarting(() => { events.Add(2); throw new IOException("starting"); });
            context.Response.OnStarting(() => { events.Add(3); return Task.CompletedTask; });
            context.Response.OnCompleted(() => { events.Add(4); return Task.CompletedTask; });
            return Task.CompletedTask;
        });
        await Process(exchange, application, logger);
        Assert.Equal(new[] { 3, 2, 1, 4 }, events);
        Assert.Equal(500, exchange.Status);
        Assert.NotNull(application.Error);
        Assert.Contains(logger.Errors, error => error.Message == "starting");
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task EveryCompletionAndDisposalFailureIsObservedAndCleanupContinues()
    {
        var exchange = new Exchange { CloseError = new IOException("close") };
        exchange.Input.DisposeError = new IOException("input disposal");
        var logger = new Logger();
        var called = false;
        var disposable = new CallbackDisposable(() => throw new IOException("registered disposal"));
        var application = new Application(context =>
        {
            context.Response.OnCompleted(() =>
            {
                Assert.True(context.RequestAborted.IsCancellationRequested);
                called = true;
                return Task.CompletedTask;
            });
            context.Response.RegisterForDispose(disposable);
            context.Response.OnCompleted(() => throw new IOException("completion"));
            return Task.CompletedTask;
        }) { DisposeError = new IOException("context disposal") };

        await Process(exchange, application, logger);

        Assert.True(called);
        Assert.Equal(1, disposable.Count);
        AssertCleanup(exchange, application);
        foreach (var message in new[] { "close", "input disposal", "registered disposal", "completion", "context disposal" })
        {
            Assert.Contains(logger.Errors, error => error.Message == message);
        }
    }

    [Fact]
    public async Task ApplicationFailureBeforeStartClearsUncommittedHeaders()
    {
        var exchange = new Exchange();
        var started = false;
        var application = new Application(context =>
        {
            context.Response.OnStarting(() =>
            {
                Assert.False(context.Response.HasStarted);
                started = true;
                return Task.CompletedTask;
            });
            context.Response.StatusCode = 202;
            context.Response.Headers.Location = "/jobs/1";
            throw new IOException("application");
        });
        await Process(exchange, application);
        Assert.True(started);
        Assert.Equal(500, exchange.Status);
        Assert.Empty(exchange.ResponseHeaders);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ApplicationFailureAfterStartCannotChangeStatusOrHeaders()
    {
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            context.Response.StatusCode = 202;
            context.Response.Headers["X-Test"] = "keep";
            await context.Response.StartAsync();
            throw new IOException("after start");
        });
        await Process(exchange, application);
        Assert.Equal(202, exchange.Status);
        Assert.Equal("keep", exchange.ResponseHeaders["X-Test"]);
        Assert.Equal(1, exchange.CommitCount);
        Assert.NotNull(application.Error);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task CreateContextFailureStillClosesAndDisposesInput()
    {
        var exchange = new Exchange();
        var application = new Application(_ => Task.CompletedTask) { CreateError = new IOException("create") };
        var logger = new Logger();
        await Process(exchange, application, logger);
        Assert.Equal(500, exchange.Status);
        Assert.Equal(0, application.DisposeCount);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, exchange.Input.DisposeCount);
        Assert.Contains(logger.Errors, error => error.Message == "create");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MalformedUrisFailInsideProcessingAndStillCleanUp(bool invalidPublicAddress)
    {
        var relative = new Uri("relative", UriKind.Relative);
        var exchange = new Exchange { Address = invalidPublicAddress ? new Uri(PublicAddress + "/jobs") : relative };
        var application = new Application(_ => throw new InvalidOperationException("The application must not run."));
        var logger = new Logger();
        RelayHttpContext adapter = null;
        Assert.Null(Record.Exception(() => adapter = new RelayHttpContext(exchange,
            invalidPublicAddress ? relative : PublicAddress, default, logger)));

        await adapter.ProcessAsync(application);

        Assert.Equal(500, exchange.Status);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, exchange.Input.DisposeCount);
        Assert.Equal(0, application.DisposeCount);
        Assert.Contains(logger.Errors, error => error is InvalidOperationException);
    }

    [Fact]
    public async Task FailedCommitDoesNotClaimResponseHasStarted()
    {
        var exchange = new Exchange { CommitError = new IOException("commit") };
        var application = new Application(async context =>
        {
            await Assert.ThrowsAsync<IOException>(() => context.Response.StartAsync());
            Assert.False(context.Response.HasStarted);
        });
        var logger = new Logger();
        await Process(exchange, application, logger);
        Assert.Contains(logger.Errors, error => error.Message == "commit");
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task RejectObservesCommitCloseAndDisposeFailures()
    {
        var exchange = new Exchange { CommitError = new IOException("commit"), CloseError = new IOException("close") };
        exchange.Input.DisposeError = new IOException("dispose");
        var logger = new Logger();
        await RelayHttpContext.RejectAsync(exchange, logger);
        Assert.Equal(1, exchange.CommitCount);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, exchange.Input.DisposeCount);
        Assert.Equal(new[] { "commit", "close", "dispose" }, logger.Errors.Select(error => error.Message));
    }

    [Fact]
    public async Task RejectCommits503AndCleansUp()
    {
        var exchange = new Exchange();
        await RelayHttpContext.RejectAsync(exchange, new Logger());
        Assert.Equal(503, exchange.Status);
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, exchange.Input.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IoFailureCancelsRequestAbortedAndStillCleansUp(bool read)
    {
        var exchange = new Exchange { HasBody = true };
        var failure = new IOException(read ? "read" : "write");
        if (read)
        {
            exchange.Input.ReadError = failure;
        }
        else
        {
            exchange.Output.WriteError = failure;
        }
        var logger = new Logger();
        var application = new Application(async context =>
        {
            var token = context.RequestAborted;
            if (read)
            {
                await Assert.ThrowsAsync<IOException>(async () => { _ = await context.Request.Body.ReadAsync(new byte[1]); });
            }
            else
            {
                await Assert.ThrowsAsync<IOException>(async () => await context.Response.Body.WriteAsync(new byte[1]));
            }
            Assert.True(token.IsCancellationRequested);
        });
        await Process(exchange, application, logger);
        Assert.Contains(logger.Errors, error => ReferenceEquals(error, failure));
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task FlushFailureCancelsRequestWithoutRewritingStartedResponse()
    {
        var exchange = new Exchange();
        var logger = new Logger();
        exchange.Output.FlushError = new IOException("flush");
        var application = new Application(async context =>
        {
            context.Response.StatusCode = 202;
            await Assert.ThrowsAsync<IOException>(() => context.Response.Body.FlushAsync());
            Assert.True(context.RequestAborted.IsCancellationRequested);
            Assert.True(context.Response.HasStarted);
        });
        await Process(exchange, application, logger);
        Assert.Equal(202, exchange.Status);
        Assert.Contains(logger.Errors, error => error.Message == "flush");
        AssertCleanup(exchange, application);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShutdownCancelsAnInFlightIoOperation(bool read)
    {
        using var shutdown = new CancellationTokenSource();
        var exchange = new Exchange { HasBody = true };
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (read)
        {
            exchange.Input.WaitForCancellation = entered;
        }
        else
        {
            exchange.Output.WaitForCancellation = entered;
        }
        var application = new Application(async context =>
        {
            var token = context.RequestAborted;
            var operation = read
                ? context.Request.Body.ReadAsync(new byte[1]).AsTask()
                : context.Response.Body.WriteAsync(new byte[1]).AsTask();
            await entered.Task;
            shutdown.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.True(token.IsCancellationRequested);
        });
        await new RelayHttpContext(exchange, PublicAddress, shutdown.Token, new Logger()).ProcessAsync(application);
        AssertCleanup(exchange, application);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallerIoCancellationAlsoCancelsRequestAborted(bool read)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var exchange = new Exchange();
        var application = new Application(async context =>
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                if (read)
                {
                    _ = await context.Request.Body.ReadAsync(new byte[1], cancellation.Token);
                }
                else
                {
                    await context.Response.Body.WriteAsync(new byte[1], cancellation.Token);
                }
            });
            Assert.True(context.RequestAborted.IsCancellationRequested);
        });
        await Process(exchange, application);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ApplicationCancellationOverrideRemainsLinkedToShutdown()
    {
        using var shutdown = new CancellationTokenSource();
        using var applicationCancellation = new CancellationTokenSource();
        var exchange = new Exchange();
        var application = new Application(context =>
        {
            context.RequestAborted = applicationCancellation.Token;
            Assert.False(context.RequestAborted.IsCancellationRequested);
            shutdown.Cancel();
            Assert.True(context.RequestAborted.IsCancellationRequested);
            return Task.CompletedTask;
        });
        await new RelayHttpContext(exchange, PublicAddress, shutdown.Token, new Logger()).ProcessAsync(application);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task AbortObservesThrowingCancellationCallbacks()
    {
        var exchange = new Exchange();
        var logger = new Logger();
        var application = new Application(context =>
        {
            context.RequestAborted.Register(() => throw new IOException("cancellation callback"));
            context.Abort();
            Assert.True(context.RequestAborted.IsCancellationRequested);
            return Task.CompletedTask;
        });
        await Process(exchange, application, logger);
        Assert.Contains(logger.Errors, error => error is AggregateException aggregate
            && aggregate.InnerExceptions.Any(inner => inner.Message == "cancellation callback"));
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task ShutdownObservesThrowingApplicationCancellationCallbacks()
    {
        using var shutdown = new CancellationTokenSource();
        var exchange = new Exchange();
        var logger = new Logger();
        var application = new Application(context =>
        {
            context.RequestAborted.Register(() => throw new IOException("shutdown callback"));
            shutdown.Cancel();
            Assert.True(context.RequestAborted.IsCancellationRequested);
            return Task.CompletedTask;
        });
        await new RelayHttpContext(exchange, PublicAddress, shutdown.Token, logger).ProcessAsync(application);
        Assert.Contains(logger.Errors, error => error is AggregateException aggregate
            && aggregate.InnerExceptions.Any(inner => inner.Message == "shutdown callback"));
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task CompletionCallbacksCannotWriteAfterAnErrorResponseIsClosed()
    {
        var exchange = new Exchange();
        var logger = new Logger();
        var application = new Application(context =>
        {
            context.Response.OnCompleted(async () => await context.Response.Body.WriteAsync(new byte[1]));
            throw new IOException("application");
        });
        await Process(exchange, application, logger);
        Assert.Equal(500, exchange.Status);
        Assert.Empty(exchange.Output.ToArray());
        Assert.Contains(logger.Errors, error => error is InvalidOperationException);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task CloseIsAwaitedEvenAfterShutdownBecauseSdkHasNoCancellationApi()
    {
        using var shutdown = new CancellationTokenSource();
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exchange = new Exchange { Closing = closing, Closed = closed };
        var application = new Application(_ => Task.CompletedTask);
        var process = new RelayHttpContext(exchange, PublicAddress, shutdown.Token, new Logger()).ProcessAsync(application);
        await closing.Task;
        shutdown.Cancel();
        Assert.False(process.IsCompleted);
        Assert.Equal(0, application.DisposeCount);
        closed.SetResult();
        await process;
        AssertCleanup(exchange, application);
    }

    [Theory]
    [InlineData(0, 0, 200)]
    [InlineData(0, 1, 413)]
    [InlineData(5, 4, 200)]
    [InlineData(5, 5, 200)]
    [InlineData(5, 6, 413)]
    public async Task SmallConfiguredLimitsEnforceTheStreamedBoundary(long limit, int length, int status)
    {
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(new byte[length]) };
        var application = new Application(async context =>
        {
            Assert.Equal(limit, context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize);
            await context.Request.Body.CopyToAsync(Stream.Null);
        });
        await new RelayHttpContext(exchange, PublicAddress, default, new Logger(), limit).ProcessAsync(application);
        Assert.Equal(status, exchange.Status);
        Assert.InRange(exchange.Input.BytesRead, 0, limit + 1);
        AssertCleanup(exchange, application);
    }

    [Fact]
    public async Task EndpointCanOverrideDefaultLimitUntilReadingStarts()
    {
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(new byte[3]) };
        var application = new Application(async context =>
        {
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(10L * 1024 * 1024 * 1024, feature.MaxRequestBodySize);
            Assert.False(feature.IsReadOnly);
            feature.MaxRequestBodySize = 10_000_000_000;
            Assert.Equal(10_000_000_000, feature.MaxRequestBodySize);
            feature.MaxRequestBodySize = 3;
            await context.Request.Body.CopyToAsync(Stream.Null);
            Assert.True(feature.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => feature.MaxRequestBodySize = null);
        });
        await Process(exchange, application);
        Assert.Null(application.Error);
        Assert.Equal(200, exchange.Status);
    }

    [Fact]
    public async Task EndpointCanDisableLimitAndInputIsDisposedOnlyOnce()
    {
        var exchange = new Exchange { HasBody = true, Input = new TrackingStream(new byte[17]) };
        var application = new Application(async context =>
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            await context.Request.Body.CopyToAsync(Stream.Null);
            context.Request.Body.Dispose();
            await context.Request.Body.DisposeAsync();
        });
        await new RelayHttpContext(exchange, PublicAddress, default, new Logger(), 1).ProcessAsync(application);
        Assert.Null(application.Error);
        AssertCleanup(exchange, application);
    }

    private static Task Process(Exchange exchange, Application application, Logger logger = null)
        => new RelayHttpContext(exchange, PublicAddress, default, logger ?? new Logger()).ProcessAsync(application);

    private static void AssertCleanup(Exchange exchange, Application application)
    {
        Assert.Equal(1, exchange.CloseCount);
        Assert.Equal(1, exchange.Input.DisposeCount);
        Assert.Equal(1, application.DisposeCount);
    }

    private sealed class Application(Func<DefaultHttpContext, Task> process) : IHttpApplication<DefaultHttpContext>
    {
        public int DisposeCount { get; private set; }
        public Exception Error { get; private set; }
        public Exception CreateError { get; init; }
        public Exception DisposeError { get; init; }

        public DefaultHttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            if (CreateError != null)
            {
                throw CreateError;
            }
            return new DefaultHttpContext(contextFeatures);
        }

        public Task ProcessRequestAsync(DefaultHttpContext context) => process(context);

        public void DisposeContext(DefaultHttpContext context, Exception exception)
        {
            DisposeCount++;
            Error = exception;
            if (DisposeError != null)
            {
                throw DisposeError;
            }
        }
    }

    private sealed class Exchange : RelayHttpExchange
    {
        public Uri Address { get; init; } = new(PublicAddress + "/jobs");
        public string Verb { get; init; } = "GET";
        public WebHeaderCollection RequestHeaders { get; } = new();
        public bool HasBody { get; init; }
        public TrackingStream Input { get; init; } = new(Array.Empty<byte>());
        public TrackingStream Output { get; } = new();
        public int CommitCount { get; private set; }
        public int CloseCount { get; private set; }
        public int Status { get; private set; }
        public IHeaderDictionary ResponseHeaders { get; private set; }
        public Exception CommitError { get; init; }
        public Exception CloseError { get; init; }
        public TaskCompletionSource Closing { get; init; }
        public TaskCompletionSource Closed { get; init; }
        public override Uri Url => Address;
        public override string Method => Verb;
        public override WebHeaderCollection Headers => RequestHeaders;
        public override bool HasEntityBody => HasBody;
        public override Stream InputStream => Input;
        public override Stream OutputStream => Output;
        public override IPEndPoint RemoteEndPoint => new(IPAddress.Loopback, 1234);

        public override void Commit(int statusCode, string reasonPhrase, IHeaderDictionary headers)
        {
            CommitCount++;
            if (CommitError != null)
            {
                throw CommitError;
            }
            Status = statusCode;
            ResponseHeaders = new HeaderDictionary(headers.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public override Task CloseAsync()
        {
            CloseCount++;
            if (Closing != null)
            {
                Closing.TrySetResult();
                return Closed.Task;
            }
            return CloseError == null ? Task.CompletedTask : Task.FromException(CloseError);
        }
    }

    private sealed class TrackingStream : MemoryStream
    {
        public TrackingStream() { }
        public TrackingStream(byte[] buffer) : base(buffer) { }
        public int DisposeCount { get; private set; }
        public long BytesRead { get; private set; }
        public int MaxReadSize { get; private set; }
        public int MaxWriteSize { get; private set; }
        public Exception ReadError { get; set; }
        public Exception WriteError { get; set; }
        public Exception FlushError { get; set; }
        public Exception DisposeError { get; set; }
        public TaskCompletionSource WaitForCancellation { get; set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            MaxReadSize = Math.Max(MaxReadSize, buffer.Length);
            if (ReadError != null)
            {
                throw ReadError;
            }
            if (WaitForCancellation != null)
            {
                WaitForCancellation.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            var read = await base.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            MaxWriteSize = Math.Max(MaxWriteSize, buffer.Length);
            if (WriteError != null)
            {
                throw WriteError;
            }
            if (WaitForCancellation != null)
            {
                WaitForCancellation.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            await base.WriteAsync(buffer, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => FlushError == null ? Task.CompletedTask : Task.FromException(FlushError);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
                if (DisposeError != null)
                {
                    throw DisposeError;
                }
            }
            base.Dispose(disposing);
        }
    }

    private sealed class CallbackDisposable(Action dispose) : IDisposable
    {
        public int Count { get; private set; }
        public void Dispose()
        {
            Count++;
            dispose();
        }
    }

    private sealed class Logger : ILogger
    {
        public List<Exception> Errors { get; } = new();
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception != null)
            {
                Errors.Add(exception);
            }
        }
    }
}
