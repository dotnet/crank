using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Crank.IntegrationTests;

public class ConfigurationTests
{
    [Fact]
    public async Task LoadConfigurationAsync_ShouldNotDuplicateImports()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "crank-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a shared profile file that will be imported multiple times
            var sharedProfilePath = Path.Combine(tempDir, "shared-profile.yml");
            File.WriteAllText(sharedProfilePath, @"
profiles:
  test-profile:
    agents:
      test-agent:
        endpoints:
          - http://localhost:5001
");

            // Create first config that imports the shared profile
            var config1Path = Path.Combine(tempDir, "config1.yml");
            File.WriteAllText(config1Path, $@"
imports:
  - {sharedProfilePath}
");

            // Create second config that also imports the shared profile
            var config2Path = Path.Combine(tempDir, "config2.yml");
            File.WriteAllText(config2Path, $@"
imports:
  - {sharedProfilePath}
");

            // Create main config that imports both
            var mainConfigPath = Path.Combine(tempDir, "main.yml");
            File.WriteAllText(mainConfigPath, $@"
imports:
  - {config1Path}
  - {config2Path}
");

            var configuration = await Controller.Program.LoadConfigurationAsync(mainConfigPath);
            Assert.NotNull(configuration);

            // Verify the shared profile exists
            Assert.True(configuration["profiles"]?["test-profile"] != null);

            // Get the endpoints array from the test-agent
            var endpoints = configuration["profiles"]?["test-profile"]?["agents"]?["test-agent"]?["endpoints"] as JArray;

            Assert.NotNull(endpoints);

            // The key assertion: endpoints should NOT be duplicated
            // If the bug exists, we'd have 2 or more identical endpoints
            Assert.Single(endpoints);
            Assert.Equal("http://localhost:5001", endpoints[0].ToString());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithNestedImports_ShouldLoadEachImportOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "crank-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a base profile
            var basePath = Path.Combine(tempDir, "base.yml");
            File.WriteAllText(basePath, @"
profiles:
  base-profile:
    agents:
      agent1:
        endpoints:
          - http://localhost:5001
");

            // Create intermediate config A that imports base
            var configAPath = Path.Combine(tempDir, "configA.yml");
            File.WriteAllText(configAPath, $@"
imports:
  - {basePath}

profiles:
  profile-a:
    agents:
      agent2:
        endpoints:
          - http://localhost:5002
");

            // Create intermediate config B that also imports base
            var configBPath = Path.Combine(tempDir, "configB.yml");
            File.WriteAllText(configBPath, $@"
imports:
  - {basePath}

profiles:
  profile-b:
    agents:
      agent3:
        endpoints:
          - http://localhost:5003
");

            // Create main config that imports both A and B (which both import base)
            var mainConfigPath = Path.Combine(tempDir, "main.yml");
            File.WriteAllText(mainConfigPath, $@"
imports:
  - {configAPath}
  - {configBPath}
");

            var configuration = await Controller.Program.LoadConfigurationAsync(mainConfigPath);
            Assert.NotNull(configuration);

            // Verify all three profiles exist
            Assert.True(configuration["profiles"]?["base-profile"] != null);
            Assert.True(configuration["profiles"]?["profile-a"] != null);
            Assert.True(configuration["profiles"]?["profile-b"] != null);

            // Verify base-profile endpoints are not duplicated
            var baseEndpoints = configuration["profiles"]?["base-profile"]?["agents"]?["agent1"]?["endpoints"] as JArray;
            Assert.NotNull(baseEndpoints);
            Assert.Single(baseEndpoints);

            // Verify other profiles
            var profileAEndpoints = configuration["profiles"]?["profile-a"]?["agents"]?["agent2"]?["endpoints"] as JArray;
            Assert.NotNull(profileAEndpoints);
            Assert.Single(profileAEndpoints);

            var profileBEndpoints = configuration["profiles"]?["profile-b"]?["agents"]?["agent3"]?["endpoints"] as JArray;
            Assert.NotNull(profileBEndpoints);
            Assert.Single(profileBEndpoints);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task LoadConfigurationAsync_SameImportInMultipleFiles_ShouldMergeOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "crank-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Simulate the real-world scenario from the bug report
            // where azure.profile.yml is imported by both ci.profile.yml and another config
            var azureProfilePath = Path.Combine(tempDir, "azure.profile.yml");
            File.WriteAllText(azureProfilePath, @"
profiles:
  idna-intel-lin:
    agents:
      app:
        endpoints:
          - http://asp-perf-lin:5001
      load:
        endpoints:
          - http://asp-perf-load:5001
");

            var ciProfilePath = Path.Combine(tempDir, "ci.profile.yml");
            File.WriteAllText(ciProfilePath, $@"
imports:
  - {azureProfilePath}
");

            var mainConfigPath = Path.Combine(tempDir, "main.yml");
            File.WriteAllText(mainConfigPath, $@"
imports:
  - {ciProfilePath}
  - {azureProfilePath}
");

            var configuration = await Controller.Program.LoadConfigurationAsync(mainConfigPath);
            Assert.NotNull(configuration);

            var profile = configuration["profiles"]?["idna-intel-lin"];
            Assert.NotNull(profile);

            // Check app agent endpoints
            var appEndpoints = profile["agents"]?["app"]?["endpoints"] as JArray;
            Assert.NotNull(appEndpoints);
            Assert.Single(appEndpoints); // Should NOT be duplicated
            Assert.Equal("http://asp-perf-lin:5001", appEndpoints[0].ToString());

            // Check load agent endpoints
            var loadEndpoints = profile["agents"]?["load"]?["endpoints"] as JArray;
            Assert.NotNull(loadEndpoints);
            Assert.Single(loadEndpoints); // Should NOT be duplicated
            Assert.Equal("http://asp-perf-load:5001", loadEndpoints[0].ToString());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}