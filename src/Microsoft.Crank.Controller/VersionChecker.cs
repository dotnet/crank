// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
        static TimeSpan CacheTimeout = TimeSpan.FromDays(1);
        static string PackageVersionUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.crank.controller/index.json";

        public static async Task CheckUpdateAsync(HttpClient client)
        {
            // The NuGet version is cached in a local file for a day (CacheTimeout) so we don't query nuget.org
            // on every run.
            
            try
            {
                var versionFilename = Path.Combine(Path.GetTempPath(), ".crank", "controller", "version.txt");

                if (!File.Exists(versionFilename))
                {            
                    Directory.CreateDirectory(Path.GetDirectoryName(versionFilename));
                }
                
                NuGetVersion latest;

                // If the file is older than CacheTimeout, get the version from NuGet.org
                if (DateTime.UtcNow - new FileInfo(versionFilename).LastWriteTimeUtc > CacheTimeout)
                {
                    var content = await client.GetStringAsync(PackageVersionUrl);
                    var document = JObject.Parse(content);
                    var versions = (JArray)document["versions"];
                    latest = versions.Select(x => new NuGetVersion(x.ToString())).Max();

                    File.WriteAllText(versionFilename, latest.ToNormalizedString());
                }
                else
                {
                    latest = NuGetVersion.Parse(File.ReadAllText(versionFilename));
                }

                var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var current = new NuGetVersion(attribute.InformationalVersion);

                if (latest > current)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"A new version is available on NuGet.org ({latest}). Run 'dotnet tool update Microsoft.Crank.Controller -g --version \"0.1.0-*\"' to update");
                    Console.ResetColor();
                }
            }
            catch
            {
            }
        }
    }
}
