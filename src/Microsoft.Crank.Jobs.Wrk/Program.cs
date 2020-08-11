// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Crank.Wrk
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("WRK Client");
            Console.WriteLine("args: " + string.Join(' ', args));

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Console.WriteLine($"Platform not supported: {Environment.OSVersion.Platform}");
                return -1;
            }

            await WrkProcess.MeasureFirstRequest(args);
            
            await WrkProcess.DownloadWrkAsync();
            return await WrkProcess.RunAsync(args);
        }
    }
}
