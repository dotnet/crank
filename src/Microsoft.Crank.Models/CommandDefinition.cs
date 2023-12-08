// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.Models
{
    public class CommandDefinition
    {
        public string Condition { get; set; }
        public ShellType Shell { get; set; }
        public string Script { get; set; }
        public string File { get; set; }
        public bool ContinueOnError { get; set; } = false;
        public List<int> SuccessExitCodes { get; set; } = new List<int> { 0 };
    }
}
