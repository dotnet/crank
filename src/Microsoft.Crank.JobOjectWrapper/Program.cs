// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine("Arguments are missing.");
    Environment.Exit(-1);
}

Console.WriteLine("Job Object Wrapper");
Console.WriteLine("Waiting for Job Object to be setup...");

await Task.Delay(1000);

var process = new Process()
{
    StartInfo = {
        FileName = args[0],
        Arguments = string.Join(" ", args[1..]),
        UseShellExecute = false
    }
};

Console.WriteLine("Starting process...");
Console.WriteLine($"Filename: {process.StartInfo.FileName}");
Console.WriteLine($"Args: {process.StartInfo.Arguments}");

process.Start();
Console.Error.WriteLine($"##ChildProcessId:{process.Id}");
process.WaitForExit();

await Task.Delay(1000);
