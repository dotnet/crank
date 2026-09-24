// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class ConfigurationValidationTests
    {
        [Theory]
        [InlineData("controller", "jobs: {}\n", "jobs: invalid\n", "/jobs")]
        [InlineData("pull-request", "benchmarks: {}\n", "benchmarks: invalid\n", "/benchmarks")]
        [InlineData("regression", "sources: []\n", "sources: invalid\n", "/sources")]
        public async Task YamlConfigurationsAreValidated(string application, string valid, string invalid, string errorLocation)
        {
            var filename = Path.Combine(Path.GetTempPath(), $"crank-schema-{Guid.NewGuid():N}.yml");

            try
            {
                await File.WriteAllTextAsync(filename, valid);
                await LoadConfiguration(application, filename);

                await File.WriteAllTextAsync(filename, invalid);
                var exception = await Record.ExceptionAsync(() => LoadConfiguration(application, filename));

                Assert.NotNull(exception);
                Assert.Contains("Invalid configuration file", exception.Message);
                Assert.Contains(errorLocation, exception.Message);
                Assert.Contains("type", exception.Message);
                Assert.Contains("Debug file created", exception.Message);
            }
            finally
            {
                File.Delete(filename);
            }
        }

        private static async Task LoadConfiguration(string application, string filename)
        {
            switch (application)
            {
                case "controller":
                    await Controller.Program.LoadConfigurationAsync(filename);
                    break;
                case "pull-request":
                    await PullRequestBot.Program.LoadConfigurationAsync(filename);
                    break;
                case "regression":
                    await RegressionBot.Program.LoadConfigurationAsync(filename);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(application));
            }
        }
    }
}
