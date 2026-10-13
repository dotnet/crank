// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller
{
    internal sealed class AgentHttpMessageHandler : DelegatingHandler
    {
        private readonly Uri _serverUri;
        private string _relayToken;

        public AgentHttpMessageHandler(Uri serverUri)
        {
            _serverUri = serverUri;
        }

        public void ConfigureRelay(string token)
        {
            if (_serverUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Azure Relay authentication requires an HTTPS agent endpoint.");
            }

            ArgumentException.ThrowIfNullOrEmpty(token);
            _relayToken = token;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;

            if (uri == null || !uri.IsAbsoluteUri
                || uri.GetComponents(UriComponents.StrongAuthority, UriFormat.UriEscaped).Contains('@')
                || !uri.Scheme.Equals(_serverUri.Scheme, StringComparison.OrdinalIgnoreCase)
                || !uri.IdnHost.Equals(_serverUri.IdnHost, StringComparison.OrdinalIgnoreCase)
                || uri.Port != _serverUri.Port)
            {
                throw new InvalidOperationException("Agent requests must target the configured endpoint authority.");
            }

            if (_relayToken != null)
            {
                request.Headers.Remove("ServiceBusAuthorization");
                request.Headers.Add("ServiceBusAuthorization", _relayToken);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
