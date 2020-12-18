// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace Microsoft.Crank.Controller
{
    public static class VersionChecker
    {
        public static async Task CheckUpdateAsync(HttpClient client)
        {
            var packageVersionUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.crank.controller/index.json";

            try
            {
                var content = await client.GetStringAsync(packageVersionUrl);
                var document = JObject.Parse(content);
                var versions = (JArray)document["versions"];
                var latest = versions.Select(x => new NuGetVersion(x.ToString())).Max();

                var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var current = new NuGetVersion(attribute.InformationalVersion);

                if (latest > current)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"A new version is available on NuGet.org ({latest}). Run 'dotnet tool update Microsoft.Crank.Controller -g --version \"0.2.0-*\"' to update");
                    Console.ResetColor();
                }
            }
            catch
            {
            }
        }
    }
}
