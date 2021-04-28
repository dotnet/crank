// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Models
{
    public class Dependency
    {
        public string[] AssemblyNames { get; set; }
        public string RepositoryUrl { get; set; }
        public string Version { get; set; }
        public string CommitHash { get; set; }
    }
}
