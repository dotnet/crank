// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

var files = new Dictionary<string, object>();
foreach (var name in new[] { "source-proof.bin", "build-proof.bin", "payload-65535.bin",
    "payload-65536.bin", "payload-65537.bin", "payload-8388608.bin" })
{
    var path = Path.Combine(AppContext.BaseDirectory, name);
    await using var stream = File.OpenRead(path);
    files.Add(name, new { length = stream.Length, sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream)) });
    File.Copy(path, Path.Combine(Environment.CurrentDirectory, "returned-" + name), overwrite: true);
}

var result = new
{
    agentUrl = Environment.GetEnvironmentVariable("CRANK_AGENT_URL"),
    jobLocalUrl = Environment.GetEnvironmentVariable("CRANK_JOB_LOCAL_URL"),
    jobUrl = Environment.GetEnvironmentVariable("CRANK_JOB_URL"),
    jobId = Environment.GetEnvironmentVariable("CRANK_JOB_ID"),
    files
};
await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "smoke-result.json"),
    JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Relay smoke ready");
// Leave time for the real controller's two-second keepalive loop.
await Task.Delay(TimeSpan.FromSeconds(8));
Console.WriteLine("Relay smoke complete");
