// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Relay;

namespace Microsoft.Crank.Agent;

internal interface IRelayListener
{
    Action<RelayHttpExchange> RequestHandler { set; }
    Task OpenAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}

internal interface IRelayListenerFactory
{
    IRelayListener Create(RelayServerOptions options);
}

// Only this boundary depends on SDK HTTP contexts, which cannot be constructed publicly.
internal abstract class RelayHttpExchange
{
    public abstract Uri Url { get; }
    public abstract string Method { get; }
    public abstract WebHeaderCollection Headers { get; }
    public abstract bool HasEntityBody { get; }
    public abstract Stream InputStream { get; }
    public abstract Stream OutputStream { get; }
    public abstract IPEndPoint RemoteEndPoint { get; }
    public abstract void Commit(int statusCode, string reasonPhrase, IHeaderDictionary headers);
    public abstract Task CloseAsync();
}

internal sealed class RelayListenerFactory : IRelayListenerFactory
{
    public IRelayListener Create(RelayServerOptions options) => new RelayListener(options);

    private sealed class RelayListener : IRelayListener
    {
        private readonly HybridConnectionListener _listener;

        public RelayListener(RelayServerOptions options)
        {
            _listener = options.TokenProvider == null
                ? new HybridConnectionListener(options.ConnectionString)
                : new HybridConnectionListener(options.ListenerAddress, options.TokenProvider);
            _listener.AcceptHandler = context =>
            {
                context.Response.StatusCode = HttpStatusCode.NotImplemented;
                context.Response.StatusDescription = "Crank Relay supports HTTP requests only.";
                return Task.FromResult(false);
            };
        }

        public Action<RelayHttpExchange> RequestHandler
        {
            set => _listener.RequestHandler = context => value(new SdkHttpExchange(context));
        }

        public Task OpenAsync(CancellationToken cancellationToken) => _listener.OpenAsync(cancellationToken);
        public Task CloseAsync(CancellationToken cancellationToken) => _listener.CloseAsync(cancellationToken);
    }

    private sealed class SdkHttpExchange(RelayedHttpListenerContext context) : RelayHttpExchange
    {
        public override Uri Url => context.Request.Url;
        public override string Method => context.Request.HttpMethod;
        public override WebHeaderCollection Headers => context.Request.Headers;
        public override bool HasEntityBody => context.Request.HasEntityBody;
        public override Stream InputStream => context.Request.InputStream;
        public override Stream OutputStream => context.Response.OutputStream;
        public override IPEndPoint RemoteEndPoint => context.Request.RemoteEndPoint;

        public override void Commit(int statusCode, string reasonPhrase, IHeaderDictionary headers)
        {
            // A previous pre-start commit may have failed after adding some headers.
            context.Response.Headers.Clear();
            context.Response.StatusCode = (HttpStatusCode)statusCode;
            context.Response.StatusDescription = reasonPhrase;

            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    context.Response.Headers.Add(header.Key, value);
                }
            }
        }

        public override Task CloseAsync() => context.Response.CloseAsync();
    }
}
