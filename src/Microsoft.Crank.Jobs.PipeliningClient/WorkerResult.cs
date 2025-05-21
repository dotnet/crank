// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Crank.Jobs.PipeliningClient
{
    public class WorkerResult
    {
        public int Status1xx { get; set; }
        public int Status2xx { get; set; }
        public int Status3xx { get; set; }
        public int Status4xx { get; set; }
        public int Status5xx { get; set; }
        public int SocketErrors { get; set; }
        public List<int> StatusCodes { get; set; } = new();
    }
}
