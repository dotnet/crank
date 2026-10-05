// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingTraceReportsAgentErrorOrHttpFailure(bool refreshFails)
        {
            using var httpClient = new HttpClient(new TraceFailureHandler(refreshFails));
            var job = new Job { Service = "client", Collect = true };
            var connection = new JobConnection(job, new Uri("http://agent.invalid"));

            var clientField = typeof(JobConnection).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
            ((HttpClient)clientField.GetValue(connection)).Dispose();
            clientField.SetValue(connection, httpClient);
            typeof(JobConnection).GetField("_serverJobUri", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(connection, "http://agent.invalid/jobs/1");

            using var output = new StringWriter();
            var originalOutput = Console.Out;
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
            Assert.Contains(refreshFails ? "404 (Not Found)" : "LTTng not installed.", output.ToString());
            if (!refreshFails)
            {
                Assert.DoesNotContain("404 (Not Found)", output.ToString());
                Assert.Contains("LTTng not installed.", connection.Job.Error);
            }
        }

        private sealed class TraceFailureHandler(bool refreshFails) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri.AbsolutePath;
                if (path == "/jobs/1")
                {
                    if (refreshFails)
                    {
                        throw new HttpRequestException("Agent unavailable.");
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
        }
    }
}
