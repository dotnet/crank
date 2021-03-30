// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.Models
{
    /// <summary>
    /// The dump type determines the kinds of information that are collected from the process.
    /// </summary>
    public enum DumpTypeOption
    {
        Full,       // The largest dump containing all memory including the module images.

        Heap,       // A large and relatively comprehensive dump containing module lists, thread lists, all 
                    // stacks, exception information, handle information, and all memory except for mapped images.

        Mini,       // A small dump containing module lists, thread lists, exception information and all stacks.
    }
}
