// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Microsoft.Crank.Agent
{
    internal sealed class AgentResponseCompressionProvider : ResponseCompressionProvider
    {
        internal const int MinimumResponseSize = 1024;

        public AgentResponseCompressionProvider(IServiceProvider services, IOptions<ResponseCompressionOptions> options)
            : base(services, options)
        {
        }

        public override bool ShouldCompressResponse(HttpContext context)
        {
            return context.Response.ContentLength is >= MinimumResponseSize
                && base.ShouldCompressResponse(context);
        }
    }
}
