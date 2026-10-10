// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Azure.Relay;

namespace Microsoft.Crank.Agent;

internal sealed class RelayServerOptions
{
    public RelayServerOptions(string connectionString, TokenProvider tokenProvider = null)
    {
        var builder = new RelayConnectionStringBuilder(connectionString);
        if (builder.Endpoint == null || builder.Endpoint.Scheme != "sb" ||
            !string.IsNullOrEmpty(builder.Endpoint.UserInfo) ||
            builder.Endpoint.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(builder.Endpoint.Query) ||
            !string.IsNullOrEmpty(builder.Endpoint.Fragment))
        {
            throw new ArgumentException("Relay requires an sb:// namespace endpoint.", nameof(connectionString));
        }

        var path = builder.EntityPath?.Trim('/');
        if (string.IsNullOrWhiteSpace(path) || path.Contains('?') || path.Contains('#') || path.Contains(':') ||
            path.Contains('\\') || Array.Exists(path.Split('/'), segment => segment is "." or ".." or ""))
        {
            throw new ArgumentException("Relay requires a valid EntityPath.", nameof(connectionString));
        }

        builder.EntityPath = path;
        ConnectionString = builder.ToString();
        TokenProvider = tokenProvider;
        ListenerAddress = new Uri(builder.Endpoint, path);
        PublicAddress = new UriBuilder(ListenerAddress) { Scheme = "https", Port = -1 }.Uri;
        OpenTimeout = builder.OperationTimeout;
    }

    public string ConnectionString { get; }
    public TokenProvider TokenProvider { get; }
    public Uri ListenerAddress { get; }
    public Uri PublicAddress { get; }
    public TimeSpan OpenTimeout { get; }
    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan CancellationGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    internal void ValidateTimeouts()
    {
        if (DrainTimeout <= TimeSpan.Zero || CloseTimeout <= TimeSpan.Zero ||
            CancellationGracePeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(DrainTimeout), "Relay shutdown timeouts must be positive.");
        }
    }
}
