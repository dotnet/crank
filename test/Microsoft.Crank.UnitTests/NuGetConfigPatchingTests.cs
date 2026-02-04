// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Crank.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Crank.UnitTests
{
    public class NuGetConfigPatchingTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testDir;

        public NuGetConfigPatchingTests(ITestOutputHelper output)
        {
            _output = output;
            _testDir = Path.Combine(Path.GetTempPath(), "crank_nuget_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public void PatchNuGetConfig_NoExistingConfig_CreatesNewConfig()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var configPath = Path.Combine(appDir, "NuGet.config");
            Assert.True(File.Exists(configPath), "NuGet.config should be created");

            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            Assert.NotNull(packageSources);

            // Verify all expected sources are added
            var sources = packageSources.Elements("add").ToList();
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11-transport");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet-public");

            _output.WriteLine($"Created config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ExistingConfigWithoutMapping_AddsSources()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, "NuGet.config");
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // Original source should be preserved
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "nuget.org");

            // Crank sources should be added
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");

            // No packageSourceMapping should be added
            Assert.Null(doc.Root?.Element("packageSourceMapping"));

            _output.WriteLine($"Patched config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ExistingConfigWithPackageSourceMapping_AddsMappings()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, "NuGet.config");
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""grpc"" value=""https://grpc.example.com/nuget/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""nuget.org"">
      <package pattern=""*"" />
    </packageSource>
    <packageSource key=""grpc"">
      <package pattern=""Grpc.*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            Assert.NotNull(packageSourceMapping);

            var mappings = packageSourceMapping.Elements("packageSource").ToList();

            // Original mappings should be preserved
            Assert.Contains(mappings, m => m.Attribute("key")?.Value == "nuget.org");
            Assert.Contains(mappings, m => m.Attribute("key")?.Value == "grpc");

            // Crank source mappings should be added with wildcard patterns
            var dotnet11Mapping = mappings.FirstOrDefault(m => m.Attribute("key")?.Value == "dotnet11");
            Assert.NotNull(dotnet11Mapping);
            Assert.Contains(dotnet11Mapping.Elements("package"), p => p.Attribute("pattern")?.Value == "*");

            _output.WriteLine($"Patched config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ExistingSourceByUrl_DoesNotDuplicate()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, "NuGet.config");
            // Use same URL but different key name
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""my-dotnet11"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""my-dotnet11"">
      <package pattern=""Microsoft.*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // Should not add duplicate source with same URL
            var dotnet11Sources = sources.Where(s =>
                s.Attribute("value")?.Value == "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json").ToList();
            Assert.Single(dotnet11Sources);
            Assert.Equal("my-dotnet11", dotnet11Sources[0].Attribute("key")?.Value);

            // Should add mapping for the existing key name
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            var mappings = packageSourceMapping.Elements("packageSource").ToList();
            Assert.Contains(mappings, m => m.Attribute("key")?.Value == "my-dotnet11");

            _output.WriteLine($"Patched config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ExistingSourceByKey_DoesNotDuplicate()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, "NuGet.config");
            // Use same key but different (older) URL
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""dotnet11"" value=""https://old-feed.example.com/dotnet11/index.json"" />
  </packageSources>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // Should not add duplicate source with same key
            var dotnet11Sources = sources.Where(s => s.Attribute("key")?.Value == "dotnet11").ToList();
            Assert.Single(dotnet11Sources);
            // Original URL should be preserved
            Assert.Equal("https://old-feed.example.com/dotnet11/index.json", dotnet11Sources[0].Attribute("value")?.Value);

            _output.WriteLine($"Patched config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ConfigInParentDirectory_PatchesParentConfig()
        {
            // Arrange
            var parentDir = Path.Combine(_testDir, "parent");
            var appDir = Path.Combine(parentDir, "src", "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(parentDir, "NuGet.config");
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert - parent config should be patched
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");

            // No config should be created in app directory
            Assert.False(File.Exists(Path.Combine(appDir, "NuGet.config")));

            _output.WriteLine($"Patched parent config:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_ConfigWithClearElement_AddsSources()
        {
            // Arrange - this is the problematic case from grpc-dotnet
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, "NuGet.config");
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""dotnet7"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""nuget.org"">
      <package pattern=""*"" />
    </packageSource>
    <packageSource key=""dotnet7"">
      <package pattern=""Microsoft.*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // Clear element should still be there
            Assert.NotNull(packageSources.Element("clear"));

            // Crank sources should be added
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11-transport");

            // Mappings should be added
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            var mappings = packageSourceMapping.Elements("packageSource").ToList();
            Assert.Contains(mappings, m => m.Attribute("key")?.Value == "dotnet11");

            _output.WriteLine($"Patched config with clear:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_AllCrankSourcesAdded()
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app");
            Directory.CreateDirectory(appDir);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var configPath = Path.Combine(appDir, "NuGet.config");
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // Verify all expected sources
            var expectedSources = new[]
            {
                "dotnet9",
                "dotnet9-transport",
                "dotnet10",
                "dotnet10-transport",
                "dotnet11",
                "dotnet11-transport",
                "dotnet-public"
            };

            foreach (var expected in expectedSources)
            {
                Assert.Contains(sources, s => s.Attribute("key")?.Value == expected);
            }

            _output.WriteLine($"Config with all sources:\n{doc}");
        }

        [Theory]
        [InlineData("nuget.config")]
        [InlineData("NuGet.config")]
        [InlineData("NuGet.Config")]
        public void PatchNuGetConfig_DifferentCasings_FindsConfig(string fileName)
        {
            // Arrange
            var appDir = Path.Combine(_testDir, "app_" + fileName.Replace(".", "_"));
            Directory.CreateDirectory(appDir);

            var configPath = Path.Combine(appDir, fileName);
            var existingConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
            File.WriteAllText(configPath, existingConfig);

            // Act
            Startup.PatchNuGetConfig(appDir);

            // Assert
            var doc = XDocument.Load(configPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");

            _output.WriteLine($"Patched {fileName}:\n{doc}");
        }
    }
}
