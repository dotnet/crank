// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.PullRequestBot
{
    public class PullRequestBotException : Exception
    {
        public PullRequestBotException(string message) : base(message)
        {

        }
    }
}
