// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class JobInvocationTests
    {
        [Theory]
        [InlineData(JobState.New)]
        [InlineData(JobState.Waiting)]
        [InlineData(JobState.Initializing)]
        [InlineData(JobState.Starting)]
        [InlineData(JobState.Failed)]
        [InlineData(JobState.Stopping)]
        [InlineData(JobState.Stopped)]
        [InlineData(JobState.TraceCollecting)]
        [InlineData(JobState.TraceCollected)]
        [InlineData(JobState.Deleting)]
        [InlineData(JobState.Deleted)]
        [InlineData(JobState.NotSupported)]
        public async Task Invoke_RequiresRunningJobBeforeUsingUrl(JobState state)
        {
            await using var server = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            var controller = CreateController(server.Url.AbsoluteUri, state);

            var result = Assert.IsType<ConflictObjectResult>(await controller.Invoke(1, "/test"));

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
            Assert.Contains(state.ToString(), Assert.IsType<string>(result.Value));
            Assert.Equal(0, server.Requests);
        }

        [Fact]
        public async Task Invoke_MissingJob_ReturnsNotFound()
        {
            var controller = new JobsController(new InMemoryJobRepository());

            Assert.IsType<NotFoundResult>(await controller.Invoke(999, "/test"));
        }

        [Fact]
        public async Task Create_CannotSubmitRunningJobWithClientUrl()
        {
            await using var server = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            var controller = new JobsController(new InMemoryJobRepository());
            var job = new Job { DriverVersion = 1, State = JobState.Running, Url = server.Url.AbsoluteUri };

            Assert.IsType<BadRequestObjectResult>(controller.Create(job));
            Assert.IsType<NotFoundResult>(await controller.Invoke(1, "/test"));
            Assert.Equal(0, server.Requests);
        }

        [Theory]
        [InlineData("relative", "/root/echo?value=42")]
        [InlineData("root", "/echo?value=42")]
        [InlineData("absolute", "/echo?value=42")]
        public async Task Invoke_RunningJob_AllowsPrivateSameOriginTarget(string kind, string expectedPath)
        {
            await using var server = await InvocationServer.StartAsync(context =>
                context.Response.WriteAsync(context.Request.Method + ":" + context.Request.Path + context.Request.QueryString));
            var controller = CreateController(new Uri(server.Url, "root/").AbsoluteUri);
            var path = kind switch
            {
                "relative" => "echo?value=42",
                "root" => "/echo?value=42",
                _ => new Uri(server.Url, "echo?value=42").AbsoluteUri
            };

            var result = Assert.IsType<ContentResult>(await controller.Invoke(1, path));

            Assert.Equal("GET:" + expectedPath, result.Content);
            Assert.Equal(1, server.Requests);
        }

        [Theory]
        [InlineData("host")]
        [InlineData("scheme")]
        [InlineData("port")]
        [InlineData("userinfo")]
        [InlineData("empty-userinfo")]
        [InlineData("network-path")]
        [InlineData("file")]
        public async Task Invoke_RejectsDifferentAuthorityBeforeSendingRequest(string kind)
        {
            await using var server = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            await using var otherServer = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            var controller = CreateController(server.Url.AbsoluteUri);
            var path = DifferentAuthority(server.Url, otherServer.Url, kind);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, path));

            Assert.Equal("Cannot invoke requests to different hosts.", result.Value);
            Assert.Equal(0, server.Requests);
            Assert.Equal(0, otherServer.Requests);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-valid-url")]
        [InlineData("http://[")]
        [InlineData("file:///C:/test")]
        [InlineData("ftp://127.0.0.1/test")]
        [InlineData("http://user:password@127.0.0.1:1/")]
        [InlineData("http://@127.0.0.1:1/")]
        public async Task Invoke_InvalidJobUrl_ReturnsExplicitBadRequest(string url)
        {
            var controller = CreateController(url);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, "/test"));

            Assert.Equal("Invalid job configuration.", result.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("http://[")]
        [InlineData("/invalid\npath")]
        [InlineData("/invalid\rpath")]
        [InlineData("/invalid\0path")]
        public async Task Invoke_InvalidPath_ReturnsExplicitBadRequestWithoutRequest(string path)
        {
            await using var server = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, path));

            Assert.Equal("Invalid invocation path.", result.Value);
            Assert.Equal(0, server.Requests);
        }

        [Theory]
        [InlineData(StatusCodes.Status301MovedPermanently)]
        [InlineData(StatusCodes.Status302Found)]
        [InlineData(StatusCodes.Status303SeeOther)]
        [InlineData(StatusCodes.Status307TemporaryRedirect)]
        [InlineData(StatusCodes.Status308PermanentRedirect)]
        public async Task Invoke_FollowsRelativeAndAbsoluteSameOriginRedirects(int statusCode)
        {
            await using var server = await InvocationServer.StartAsync(context =>
            {
                if (context.Request.Path == "/start")
                {
                    context.Response.StatusCode = statusCode;
                    context.Response.Headers.Location = "next";
                }
                else if (context.Request.Path == "/next")
                {
                    context.Response.StatusCode = statusCode;
                    context.Response.Headers.Location = $"{context.Request.Scheme}://{context.Request.Host}/final?value=42";
                }
                else
                {
                    return context.Response.WriteAsync("redirected:" + context.Request.QueryString);
                }

                return Task.CompletedTask;
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<ContentResult>(await controller.Invoke(1, "/start"));

            Assert.Equal("redirected:?value=42", result.Content);
            Assert.Equal(3, server.Requests);
        }

        [Theory]
        [InlineData("host")]
        [InlineData("scheme")]
        [InlineData("port")]
        [InlineData("userinfo")]
        [InlineData("empty-userinfo")]
        [InlineData("network-path")]
        [InlineData("file")]
        public async Task Invoke_RejectsRedirectAuthorityChangesWithoutContactingTarget(string kind)
        {
            await using var otherServer = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            await using var server = await InvocationServer.StartAsync(context =>
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                var source = new Uri($"{context.Request.Scheme}://{context.Request.Host}/");
                context.Response.Headers.Location = DifferentAuthority(source, otherServer.Url, kind);
                return Task.CompletedTask;
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, "/start"));

            Assert.Equal("Cannot invoke requests to different hosts.", result.Value);
            Assert.Equal(1, server.Requests);
            Assert.Equal(0, otherServer.Requests);
        }

        [Fact]
        public async Task Invoke_ValidatesEveryRedirectHop()
        {
            await using var otherServer = await InvocationServer.StartAsync(context => context.Response.WriteAsync("unexpected"));
            await using var server = await InvocationServer.StartAsync(context =>
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = context.Request.Path == "/start"
                    ? "/next"
                    : otherServer.Url.AbsoluteUri;
                return Task.CompletedTask;
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, "/start"));
            Assert.Equal(2, server.Requests);
            Assert.Equal(0, otherServer.Requests);
        }

        [Fact]
        public async Task Invoke_RedirectLoop_IsBounded()
        {
            await using var server = await InvocationServer.StartAsync(context =>
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = "/loop";
                return Task.CompletedTask;
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, "/loop"));

            Assert.Equal("Too many invocation redirects.", result.Value);
            Assert.Equal(51, server.Requests);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("http://[")]
        public async Task Invoke_InvalidRedirectLocation_ReturnsExplicitBadRequest(string location)
        {
            await using var server = await InvocationServer.StartAsync(context =>
            {
                context.Response.StatusCode = StatusCodes.Status302Found;
                if (location != null)
                {
                    context.Response.Headers.Location = location;
                }

                return Task.CompletedTask;
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<BadRequestObjectResult>(await controller.Invoke(1, "/start"));

            Assert.Equal("Invalid invocation redirect location.", result.Value);
            Assert.Equal(1, server.Requests);
        }

        [Fact]
        public async Task Invoke_UpstreamFailure_DoesNotReturnSuccessfulContent()
        {
            await using var server = await InvocationServer.StartAsync(context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return context.Response.WriteAsync("upstream failure");
            });
            var controller = CreateController(server.Url.AbsoluteUri);

            var result = Assert.IsType<ObjectResult>(await controller.Invoke(1, "/test"));

            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.Equal(1, server.Requests);
        }

        private static JobsController CreateController(string url, JobState state = JobState.Running)
        {
            var jobs = new InMemoryJobRepository();
            jobs.Add(new Job { State = state, Url = url });
            return new JobsController(jobs);
        }

        private static string DifferentAuthority(Uri source, Uri other, string kind)
        {
            return kind switch
            {
                "host" => new UriBuilder(source) { Host = "localhost" }.Uri.AbsoluteUri,
                "scheme" => new UriBuilder(source) { Scheme = "https", Port = source.Port }.Uri.AbsoluteUri,
                "port" => other.AbsoluteUri,
                "userinfo" => new UriBuilder(source) { UserName = "user", Password = "password" }.Uri.AbsoluteUri,
                "empty-userinfo" => $"{source.Scheme}://@{source.Authority}/test",
                "network-path" => $"//{other.Authority}/test",
                "file" => "file:///C:/test",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private sealed class InvocationServer : IAsyncDisposable
        {
            private IHost _host;
            private int _requests;

            public Uri Url { get; private set; }
            public int Requests => Volatile.Read(ref _requests);

            public static async Task<InvocationServer> StartAsync(RequestDelegate handler)
            {
                var server = new InvocationServer();
                server._host = new HostBuilder()
                    .ConfigureLogging(logging => logging.ClearProviders())
                    .ConfigureWebHost(web =>
                    {
                        web.UseKestrel()
                            .UseUrls("http://127.0.0.1:0")
                            .Configure(app => app.Run(async context =>
                            {
                                Interlocked.Increment(ref server._requests);
                                await handler(context);
                            }));
                    })
                    .Build();

                await server._host.StartAsync();
                var addresses = server._host.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>();
                server.Url = new Uri(addresses.Addresses.Single() + "/");
                return server;
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
                }
            }
        }
    }
}
