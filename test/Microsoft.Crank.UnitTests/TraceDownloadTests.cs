// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Controller;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class TraceDownloadTests
    {
        [Theory]
        [InlineData("success")]
        [InlineData("http-error")]
        [InlineData("canceled")]
        [InlineData("not-found")]
        [InlineData("empty")]
        [InlineData("invalid-json")]
        [InlineData("no-error")]
        [InlineData("hang")]
        public async Task MissingTraceReportsOnlyFreshAgentErrorOrOriginalHttpFailure(string refreshMode)
        {
            var handler = new TraceFailureHandler(refreshMode);
            using var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            var job = new Job { Service = "client", Collect = true, Error = "Stale benchmark error." };
            var connection = new JobConnection(job, new Uri("http://agent.invalid"));

            var clientField = typeof(JobConnection).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
            ((HttpClient)clientField.GetValue(connection)).Dispose();
            clientField.SetValue(connection, httpClient);
            typeof(JobConnection).GetField("_serverJobUri", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(connection, "http://agent.invalid/jobs/1");

            using var output = new StringWriter();
            var originalOutput = Console.Out;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Console.SetOut(output);
                await connection.DownloadTraceAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally
            {
                connection.StopKeepAlive();
                Console.SetOut(originalOutput);
            }

            Assert.Contains("The trace was not captured on the server:", output.ToString());
            Assert.DoesNotContain("Stale benchmark error.", output.ToString());
            Assert.Contains(refreshMode == "success" ? "LTTng not installed." : "404 (Not Found)", output.ToString());
            if (refreshMode == "success")
            {
                Assert.DoesNotContain("404 (Not Found)", output.ToString());
                Assert.Contains("LTTng not installed.", connection.Job.Error);
            }
            if (refreshMode == "hang")
            {
                Assert.True(handler.RefreshCanceled);
                Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(9));
            }
        }

        private sealed class TraceFailureHandler(string refreshMode) : HttpMessageHandler
        {
            public bool RefreshCanceled { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri.AbsolutePath;
                if (path == "/jobs/1")
                {
                    if (refreshMode == "hang")
                    {
                        return WaitForCancellationAsync(cancellationToken);
                    }
                    if (refreshMode == "canceled")
                    {
                        throw new TaskCanceledException("Agent refresh timed out.");
                    }

                    if (refreshMode == "http-error")
                    {
                        throw new HttpRequestException("Agent unavailable.");
                    }
                    if (refreshMode == "not-found")
                    {
                        return Respond(HttpStatusCode.NotFound, "");
                    }
                    if (refreshMode == "empty")
                    {
                        return Respond(HttpStatusCode.OK, "");
                    }
                    if (refreshMode == "invalid-json")
                    {
                        return Respond(HttpStatusCode.OK, "invalid job JSON");
                    }
                    if (refreshMode == "no-error")
                    {
                        return Respond(HttpStatusCode.OK, "{\"Service\":\"client\",\"Collect\":true}");
                    }

                    return Respond(HttpStatusCode.OK, "{\"Service\":\"client\",\"Collect\":true,\"Error\":\"Perfcollect failed with exit code 1. LTTng not installed.\"}");
                }
                if (path == "/jobs/1/state")
                {
                    return Respond(HttpStatusCode.OK, "Stopped");
                }
                if (path == "/info")
                {
                    return Respond(HttpStatusCode.OK, "{\"os\":\"linux\"}");
                }
                if (path == "/jobs/1/trace")
                {
                    return Respond(request.Method == HttpMethod.Post ? HttpStatusCode.Accepted : HttpStatusCode.NotFound, "");
                }
                if (path == "/jobs/1/touch")
                {
                    return Respond(HttpStatusCode.OK, "");
                }

                throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
            }

            private static Task<HttpResponseMessage> Respond(HttpStatusCode status, string content)
                => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) });

            private async Task<HttpResponseMessage> WaitForCancellationAsync(CancellationToken cancellationToken)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    throw new InvalidOperationException("The job refresh was not canceled.");
                }
                catch (OperationCanceledException)
                {
                    RefreshCanceled = true;
                    throw;
                }
            }
        }
    }
}
