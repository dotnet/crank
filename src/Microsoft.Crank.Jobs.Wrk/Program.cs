// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Crank.Wrk
{
    class Program
    {
        const string WrkFilename = "./wrk";

        static async Task Main(string[] args)
        {
            Console.WriteLine("WRK Client");
            Console.WriteLine("args: " + string.Join(' ', args));

            Console.Write("Measuring first request ... ");
            await WrkProcess.MeasureFirstRequest(args);

            using var process = Process.Start("chmod", "+x " + WrkFilename);
            process.WaitForExit();

            await WrkProcess.RunAsync(WrkFilename, args);
        }
    }
}