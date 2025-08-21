// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    public class AzureWorkerTests
    {
        // To decode the payloads used in the unit tests:
        // Console.WriteLine(System.Text.Encoding.UTF8.GetString(Convert.FromHexString("...")));
        // To encode a json payload:
        // Console.WriteLine(Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(jsonString)));

        [Theory]
        [InlineData("4006737472696E670833687474703A2F2F736368656D61732E6D6963726F736F66742E636F6D2F323030332F31302F53657269616C697A6174696F6E2F9ADF037B0A2020226E616D65223A20226372616E6B222C0A202022636F6E646974696F6E223A2022287472756529222C0A20202261726773223A205B20222D2D7363656E6172696F2073696E676C655F7175657279202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F7363656E6172696F732F706C6174666F726D2E62656E63686D61726B732E796D6C202D2D70726F7065727479207363656E6172696F3D53696E676C655175657279506C6174666F726D50676F496E6C696E65202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F53595354454D5F4E45545F534F434B4554535F494E4C494E455F434F4D504C4554494F4E533D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54696572656450474F3D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54435F517569636B4A6974466F724C6F6F70733D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F5265616479546F52756E3D30202D2D6170706C69636174696F6E2E6368616E6E656C2065646765202D2D6170706C69636174696F6E2E6672616D65776F726B206E6574382E30202D2D70726F7065727479206672616D65776F726B3D6E6574382E30202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F63692E70726F66696C652E796D6C202D2D70726F66696C6520696E74656C2D6C696E2D617070202D2D70726F66696C6520696E74656C2D6C6F61642D6C6F6164202D2D70726F66696C6520696E74656C2D64622D646220202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F617A7572652E70726F66696C652E796D6C202D2D7661726961626C65206475726174696F6E3D3330202D2D6E6F2D6D65746164617461202D2D6E6F2D6D6561737572656D656E7473202D2D73657373696F6E2032303233303231362E31202D2D636F6D6D616E642D6C696E652D70726F7065727479202D2D7461626C6520426173656C696E6542656E63686D61726B73202D2D73716C2053514C5F434F4E4E454354494F4E5F535452494E47202D2D636861727422205D0A7D0A01")]
        [InlineData("4006737472696E33687474703A2F2F736368656D61732E6D6963726F736F66742E636F6D2F323030332F31302F53657269616C697A6174696F6E2FEFBFBD7B037B0A2020226E616D65223A20226372616E6B222C0A202022636F6E646974696F6E223A2022287472756529222C0A20202261726773223A205B20222D2D7363656E6172696F2073696E676C655F7175657279202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F7363656E6172696F732F706C6174666F726D2E62656E63686D61726B732E796D6C202D2D70726F7065727479207363656E6172696F3D53696E676C655175657279506C6174666F726D50676F496E6C696E65202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F53595354454D5F4E45545F534F434B4554535F494E4C494E455F434F4D504C4554494F4E533D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54696572656450474F3D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54435F517569636B4A6974466F724C6F6F70733D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F5265616479546F52756E3D30202D2D6170706C69636174696F6E2E6368616E6E656C2065646765202D2D6170706C69636174696F6E2E6672616D65776F726B206E6574382E30202D2D70726F7065727479206672616D65776F726B3D6E6574382E30202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F63692E70726F66696C652E796D6C202D2D70726F66696C6520696E74656C2D6C696E2D617070202D2D70726F66696C6520696E74656C2D6C6F61642D6C6F6164202D2D70726F66696C6520696E74656C2D64622D646220202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F617A7572652E70726F66696C652E796D6C202D2D7661726961626C65206475726174696F6E3D3330202D2D6E6F2D6D65746164617461202D2D6E6F2D6D6561737572656D656E7473202D2D73657373696F6E2032303233303231362E31202D2D636F6D6D616E642D6C696E652D70726F7065727479202D2D7461626C6520426173656C696E6542656E63686D61726B73202D2D73716C2053514C5F434F4E4E454354494F4E5F535452494E47202D2D636861727422205D0A7D0A01")]
        [InlineData("7B0A2020226E616D65223A20226372616E6B222C0A202022636F6E646974696F6E223A2022287472756529222C0A20202261726773223A205B20222D2D7363656E6172696F2073696E676C655F7175657279202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F7363656E6172696F732F706C6174666F726D2E62656E63686D61726B732E796D6C202D2D70726F7065727479207363656E6172696F3D53696E676C655175657279506C6174666F726D50676F496E6C696E65202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F53595354454D5F4E45545F534F434B4554535F494E4C494E455F434F4D504C4554494F4E533D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54696572656450474F3D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F54435F517569636B4A6974466F724C6F6F70733D31202D2D6170706C69636174696F6E2E656E7669726F6E6D656E745661726961626C657320444F544E45545F5265616479546F52756E3D30202D2D6170706C69636174696F6E2E6368616E6E656C2065646765202D2D6170706C69636174696F6E2E6672616D65776F726B206E6574382E30202D2D70726F7065727479206672616D65776F726B3D6E6574382E30202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F63692E70726F66696C652E796D6C202D2D70726F66696C6520696E74656C2D6C696E2D617070202D2D70726F66696C6520696E74656C2D6C6F61642D6C6F6164202D2D70726F66696C6520696E74656C2D64622D646220202D2D636F6E6669672068747470733A2F2F7261772E67697468756275736572636F6E74656E742E636F6D2F6173706E65742F42656E63686D61726B732F6D61696E2F6275696C642F617A7572652E70726F66696C652E796D6C202D2D7661726961626C65206475726174696F6E3D3330202D2D6E6F2D6D65746164617461202D2D6E6F2D6D6561737572656D656E7473202D2D73657373696F6E2032303233303231362E31202D2D636F6D6D616E642D6C696E652D70726F7065727479202D2D7461626C6520426173656C696E6542656E63686D61726B73202D2D73716C2053514C5F434F4E4E454354494F4E5F535452494E47202D2D636861727422205D0A7D0A01")]
        public void ShouldParsePublishTaskPayload(string hexPayload)
        {
            #region Examples
            /* 
            @strin3http://schemas.microsoft.com/2003/10/Serialization/�{{
                "name": "...",
                "condition": "...",
                "args": [ "..." ]
            }
            
            @string3http://schemas.microsoft.com/2003/10/Serialization/��{
                "name": "...",
                "condition": "...",
                "args": [ "..." ]
            }
            
            */
            #endregion


            var bytes = Convert.FromHexString(hexPayload);

            var payload = JobPayload.Deserialize(bytes);

            Assert.Single(payload.Args);
            Assert.Equal("--scenario single_query --config https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/platform.benchmarks.yml --property scenario=SingleQueryPlatformPgoInline --application.environmentVariables DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS=1 --application.environmentVariables DOTNET_TieredPGO=1 --application.environmentVariables DOTNET_TC_QuickJitForLoops=1 --application.environmentVariables DOTNET_ReadyToRun=0 --application.channel edge --application.framework net8.0 --property framework=net8.0 --config https://raw.githubusercontent.com/aspnet/Benchmarks/main/build/ci.profile.yml --profile intel-lin-app --profile intel-load-load --profile intel-db-db  --config https://raw.githubusercontent.com/aspnet/Benchmarks/main/build/azure.profile.yml --variable duration=30 --no-metadata --no-measurements --session 20230216.1 --command-line-property --table BaselineBenchmarks --sql SQL_CONNECTION_STRING --chart", payload.Args[0]);
            Assert.Equal("crank", payload.Name);
            Assert.Equal("(true)", payload.Condition);
        }

        [Theory]
        [InlineData("")]
        [InlineData("68656C6C6F")] // hello
        [InlineData("7D")] // }
        [InlineData("7B20")] // { 
        [InlineData("7B207B")] // { {
        public void ShouldNotParsePublishTaskPayload(string hexPayload)
        {
            var bytes = Convert.FromHexString(hexPayload);

            Assert.Throws<Exception>(() => JobPayload.Deserialize(bytes));
        }

        [Theory]
        [InlineData("00:20:00", "0D0A7B0D0A2020226E616D65223A20226372616E6B222C0D0A202022636F6E646974696F6E223A2022287472756529222C0D0A20202274696D656F7574223A202230303A32303A3030222C0D0A20202261726773223A205B20222D2D7363656E6172696F2073696E676C655F717565727922205D0D0A7D0D0A")]
        public void ShouldParseTimeout(string timeSpan, string hexPayload)
        {
            var bytes = Convert.FromHexString(hexPayload);

            var payload = JobPayload.Deserialize(bytes);

            Assert.Equal(timeSpan, payload.Timeout.ToString());
        }

        [Fact]
        public void ShouldParseFilesFromPayload()
        {
            var content1 = "file-1";
            var content2 = "{\"k\":\"v\"}";
            var b64_1 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content1));
            var b64_2 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content2));

            var json = $@"{{
  ""name"": ""crank"",
  ""condition"": ""(true)"",
  ""args"": [ ""--scenario x"" ],
  ""files"": {{
    ""scenarios/benchmarks.yml"": ""{b64_1}"",
    ""scenarios/assets/payload.json"": ""{b64_2}""
  }}
}}";

            var bytes = Encoding.UTF8.GetBytes(json);
            var payload = JobPayload.Deserialize(bytes);

            Assert.NotNull(payload.Files);
            Assert.Equal(2, payload.Files.Count);
            Assert.True(payload.Files.ContainsKey("scenarios/benchmarks.yml"));
            Assert.True(payload.Files.ContainsKey("scenarios/assets/payload.json"));
            Assert.Equal(content1, Encoding.UTF8.GetString(Convert.FromBase64String(payload.Files["scenarios/benchmarks.yml"])));
            Assert.Equal(content2, Encoding.UTF8.GetString(Convert.FromBase64String(payload.Files["scenarios/assets/payload.json"])));
        }

        [Fact]
        public void MaterializeFiles_WritesFiles_AndCreatesDirectories()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "crank-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            try
            {
                var payload = new JobPayload
                {
                    Files = new Dictionary<string, string>
                    {
                        ["nested/dir/a.txt"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("A")),
                        ["b.json"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"ok\":true}")),
                    }
                };

                Program.MaterializeFiles(payload, tmp);

                var aPath = Path.Combine(tmp, "nested", "dir", "a.txt");
                var bPath = Path.Combine(tmp, "b.json");

                Assert.True(File.Exists(aPath));
                Assert.True(File.Exists(bPath));
                Assert.Equal("A", File.ReadAllText(aPath));
                Assert.Equal("{\"ok\":true}", File.ReadAllText(bPath));
            }
            finally
            {
                TryDelete(tmp);
            }
        }

        [Fact]
        public void MaterializeFiles_SkipsUnsafeAndInvalidEntries()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "crank-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            try
            {
                var outsidePath = Path.GetFullPath(Path.Combine(tmp, "..", "outside.txt"));
                if (File.Exists(outsidePath)) File.Delete(outsidePath);

                var payload = new JobPayload
                {
                    Files = new Dictionary<string, string>
                    {
                        // Unsafe traversal
                        ["../outside.txt"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("NOPE")),
                        // Invalid base64
                        ["invalid.bin"] = "***not-base64***",
                        // Valid to ensure method still processes others
                        ["safe.txt"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("SAFE"))
                    }
                };

                Program.MaterializeFiles(payload, tmp);

                // Traversal should be skipped
                Assert.False(File.Exists(outsidePath));

                // Invalid base64 should not produce a file
                Assert.False(File.Exists(Path.Combine(tmp, "invalid.bin")));

                // Safe file should exist
                var safePath = Path.Combine(tmp, "safe.txt");
                Assert.True(File.Exists(safePath));
                Assert.Equal("SAFE", File.ReadAllText(safePath));
            }
            finally
            {
                TryDelete(tmp);
            }
        }

        private static void TryDelete(string dir)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch
            {
                // best effort cleanup
            }
        }
    }
}
