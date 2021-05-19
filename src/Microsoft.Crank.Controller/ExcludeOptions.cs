// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Controller
{
    public struct ExcludeOptions
    {
        public static readonly ExcludeOptions Empty = new ExcludeOptions();

        public int Low;
        public int High;
        public string Job;
        public string Result;
    } 
}
