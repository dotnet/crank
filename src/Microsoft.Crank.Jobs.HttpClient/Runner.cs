// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using Jint;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    internal class Worker
    {
        public HttpMessageInvoker Invoker { get; set; }
        public SocketsHttpHandler Handler { get; set; }
        public Engine Script { get; set; }
    }
}
