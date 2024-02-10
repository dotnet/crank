// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.Models
{
    public class CommandDefinition
    {
        public string Condition { get; set; } = "true";
        public ScriptType ScriptType { get; set; } = ScriptType.Powershell;
        public string Script { get; set; }
        public string FilePath { get; set; }
        public bool ContinueOnError { get; set; } = false;
        public List<int> SuccessExitCodes { get; set; } = new List<int> { 0 };
    }
}
