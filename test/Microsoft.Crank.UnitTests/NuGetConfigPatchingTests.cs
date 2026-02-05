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
            // The global NuGet.config file created by crank may be ignored if the local project has 
            // a custom one with a <clear /> statement. This test ensures we patch the local config
            // so sources are available even with <clear />.
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
            var allElements = packageSources.Elements().ToList();
            var sources = packageSources.Elements("add").ToList();

            // Clear element should still be there
            Assert.NotNull(packageSources.Element("clear"));

            // Crank sources should be added
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11-transport");

            // CRITICAL: Verify sources are added AFTER the <clear /> element
            // If sources were added before <clear />, they would be cleared and unavailable
            var clearIndex = allElements.FindIndex(e => e.Name.LocalName == "clear");
            var dotnet11Index = allElements.FindIndex(e => 
                e.Name.LocalName == "add" && e.Attribute("key")?.Value == "dotnet11");
            Assert.True(dotnet11Index > clearIndex, 
                $"dotnet11 source (index {dotnet11Index}) should be after <clear /> (index {clearIndex})");

            // Mappings should be added
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            var mappings = packageSourceMapping.Elements("packageSource").ToList();
            Assert.Contains(mappings, m => m.Attribute("key")?.Value == "dotnet11");

            _output.WriteLine($"Patched config with clear:\n{doc}");
        }

        [Fact]
        public void PatchNuGetConfig_GlobalConfigIgnoredByClear_LocalConfigPatched()
        {
            // This test simulates the exact scenario from the comment:
            // "The global NuGet.config file created by crank may be ignored if the local project has 
            // a custom one with a <clear /> statement."
            //
            // Previously, crank created a global NuGet.config at the temp root, but local configs
            // with <clear /> would ignore it. Now we patch the local config directly.

            // Arrange - simulate a repo structure with a local config that clears global sources
            var repoRoot = Path.Combine(_testDir, "repo");
            var srcDir = Path.Combine(repoRoot, "src", "MyApp");
            Directory.CreateDirectory(srcDir);

            // This is like grpc-dotnet's NuGet.config - it clears all parent sources
            var localConfigPath = Path.Combine(repoRoot, "NuGet.config");
            var localConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""nuget.org"">
      <package pattern=""*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";
            File.WriteAllText(localConfigPath, localConfig);

            // Act - patch from the app directory (like crank would do)
            Startup.PatchNuGetConfig(srcDir);

            // Assert - the local config should now have crank sources
            var doc = XDocument.Load(localConfigPath);
            var packageSources = doc.Root?.Element("packageSources");
            var sources = packageSources.Elements("add").ToList();

            // All crank sources should be in the local config
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet11-transport");
            Assert.Contains(sources, s => s.Attribute("key")?.Value == "dotnet-public");

            // packageSourceMapping should have wildcard mappings for crank sources
            var packageSourceMapping = doc.Root?.Element("packageSourceMapping");
            var mappings = packageSourceMapping.Elements("packageSource").ToList();
            
            var dotnet11Mapping = mappings.FirstOrDefault(m => m.Attribute("key")?.Value == "dotnet11");
            Assert.NotNull(dotnet11Mapping);
            Assert.Contains(dotnet11Mapping.Elements("package"), 
                p => p.Attribute("pattern")?.Value == "*");

            _output.WriteLine($"Local config after patching:\n{doc}");
            _output.WriteLine("\nThis config now has crank sources that won't be cleared, " +
                "and packageSourceMapping entries so sources aren't ignored.");
        }

        [Fact]
        public void PatchNuGetConfig_NestedConfigWithClear_PatchesNearestConfig()
        {
            // This test verifies that when there's a NuGet.config hierarchy where a subfolder
            // has its own config with <clear />, we patch the nearest (subfolder) config.
            // This is critical because the subfolder's <clear /> would ignore sources from
            // the parent config, so we must add sources to the subfolder config.

            // Arrange - create a hierarchy:
            // repo/
            //   NuGet.config (parent - has sources)
            //   src/
            //     project/
            //       NuGet.config (child - has <clear /> and packageSourceMapping)

            var repoRoot = Path.Combine(_testDir, "repo");
            var projectDir = Path.Combine(repoRoot, "src", "project");
            Directory.CreateDirectory(projectDir);

            // Parent config at repo root
            var parentConfigPath = Path.Combine(repoRoot, "NuGet.config");
            var parentConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""company-feed"" value=""https://company.example.com/nuget/v3/index.json"" />
  </packageSources>
</configuration>";
            File.WriteAllText(parentConfigPath, parentConfig);

            // Child config in project folder with <clear /> - this ignores parent sources
            var childConfigPath = Path.Combine(projectDir, "NuGet.config");
            var childConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""project-feed"" value=""https://project.example.com/nuget/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""nuget.org"">
      <package pattern=""*"" />
    </packageSource>
    <packageSource key=""project-feed"">
      <package pattern=""Project.*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";
            File.WriteAllText(childConfigPath, childConfig);

            // Act - patch from the project directory
            Startup.PatchNuGetConfig(projectDir);

            // Assert - the CHILD config (nearest) should be patched, not the parent
            var childDoc = XDocument.Load(childConfigPath);
            var childPackageSources = childDoc.Root?.Element("packageSources");
            var childSources = childPackageSources.Elements("add").ToList();

            // Crank sources should be in the child config
            Assert.Contains(childSources, s => s.Attribute("key")?.Value == "dotnet11");
            Assert.Contains(childSources, s => s.Attribute("key")?.Value == "dotnet11-transport");

            // Verify sources are added AFTER <clear />
            var allElements = childPackageSources.Elements().ToList();
            var clearIndex = allElements.FindIndex(e => e.Name.LocalName == "clear");
            var dotnet11Index = allElements.FindIndex(e => 
                e.Name.LocalName == "add" && e.Attribute("key")?.Value == "dotnet11");
            Assert.True(dotnet11Index > clearIndex, 
                "Crank sources must be added after <clear /> to not be cleared");

            // packageSourceMapping should have mappings for crank sources
            var childMapping = childDoc.Root?.Element("packageSourceMapping");
            var childMappings = childMapping.Elements("packageSource").ToList();
            Assert.Contains(childMappings, m => m.Attribute("key")?.Value == "dotnet11");

            // Parent config should NOT be modified (we patch the nearest config only)
            var parentDoc = XDocument.Load(parentConfigPath);
            var parentPackageSources = parentDoc.Root?.Element("packageSources");
            var parentSources = parentPackageSources.Elements("add").ToList();
            Assert.DoesNotContain(parentSources, s => s.Attribute("key")?.Value == "dotnet11");

            _output.WriteLine($"Child config (patched):\n{childDoc}");
            _output.WriteLine($"\nParent config (unchanged):\n{parentDoc}");
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
