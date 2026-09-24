// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class BotCommandLineTests
    {
        [Fact]
        public async Task PullRequestBotPreservesDefaultsAndExitCode()
        {
            PullRequestBot.BotOptions options = null;
            var command = PullRequestBot.Program.CreateCommand(value =>
            {
                options = value;
                return Task.FromResult(42);
            });

            Assert.Equal(42, await command.Parse("--config config.yml").InvokeAsync());
            Assert.Equal("config.yml", options.Config);
            Assert.Equal(Path.GetTempPath(), options.Workspace);
            Assert.Equal(60, options.Age);
            Assert.Equal(0, options.Limit);
            Assert.Equal("", options.Benchmarks);
            Assert.Equal("", options.Profiles);
            Assert.Equal("", options.Components);
            Assert.False(options.PublishResults);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PullRequestBotBindsAllOptions(bool publishResults)
        {
            PullRequestBot.BotOptions options = null;
            var command = PullRequestBot.Program.CreateCommand(value =>
            {
                options = value;
                return Task.FromResult(0);
            });

            var result = command.Parse(new[]
            {
                "--config", "config.yml", "--workspace", "work space",
                "--benchmarks", "plaintext", "--profiles", "local", "--components", "runtime",
                "--limit", "2", "--age", "15", "--repository", "dotnet/aspnetcore",
                "--pull-request", "123", "--publish-results", publishResults.ToString(),
                "--access-token", "test-token", "--app-key", "test-key", "--app-id", "456",
                "--install-id", "789", "--github-base-url", "https://github.example",
                "--arguments", "--property name=value", "--external-log-uri", "https://logs.example/run"
            });

            Assert.Empty(result.Errors);
            Assert.Equal(0, await result.InvokeAsync());
            Assert.Equal("config.yml", options.Config);
            Assert.Equal("work space", options.Workspace);
            Assert.Equal("plaintext", options.Benchmarks);
            Assert.Equal("local", options.Profiles);
            Assert.Equal("runtime", options.Components);
            Assert.Equal(2, options.Limit);
            Assert.Equal(15, options.Age);
            Assert.Equal("dotnet/aspnetcore", options.Repository);
            Assert.Equal("123", options.PullRequest);
            Assert.Equal(publishResults, options.PublishResults);
            Assert.Equal("test-token", options.AccessToken);
            Assert.Equal("test-key", options.AppKey);
            Assert.Equal("456", options.AppId);
            Assert.Equal(789, options.InstallId);
            Assert.Equal(new Uri("https://github.example"), options.GitHubBaseUrl);
            Assert.Equal("--property name=value", options.Arguments);
            Assert.Equal(new Uri("https://logs.example/run"), options.ExternalLogUri);
        }

        [Theory]
        [InlineData("")]
        [InlineData("--config")]
        [InlineData("--config config.yml --publish-results")]
        [InlineData("--config config.yml --publish-results invalid")]
        [InlineData("--config config.yml --age invalid")]
        [InlineData("--config config.yml --github-base-url invalid")]
        [InlineData("--config config.yml --external-log-uri invalid")]
        [InlineData("--config config.yml --github-base-url")]
        public async Task PullRequestBotRejectsInvalidArguments(string args)
        {
            var invoked = false;
            var command = PullRequestBot.Program.CreateCommand(_ =>
            {
                invoked = true;
                return Task.FromResult(0);
            });
            var result = command.Parse(args);

            Assert.NotEmpty(result.Errors);
            Assert.NotEqual(0, await result.InvokeAsync());
            Assert.False(invoked);
        }

        [Fact]
        public async Task RegressionBotBindsOptionsAndRepeatedConfigurations()
        {
            RegressionBot.BotOptions options = null;
            var command = RegressionBot.Program.CreateCommand(value =>
            {
                options = value;
                return Task.FromResult(42);
            });

            var result = command.Parse(new[]
            {
                "--repository-id", "123", "--access-token", "test-token", "--username", "test-bot",
                "--app-key", "test-key", "--app-id", "456", "--install-id", "789",
                "--connectionstring", "SQL_CONNECTION_STRING", "--config", "first.yml",
                "--config", "second.yml", "--debug", "--verbose", "--read-only"
            });

            Assert.Empty(result.Errors);
            Assert.Equal(42, await result.InvokeAsync());
            Assert.Equal(123, options.RepositoryId);
            Assert.Equal("test-token", options.AccessToken);
            Assert.Equal("test-bot", options.Username);
            Assert.Equal("test-key", options.AppKey);
            Assert.Equal("456", options.AppId);
            Assert.Equal(789, options.InstallId);
            Assert.Equal("SQL_CONNECTION_STRING", options.ConnectionString);
            Assert.Equal(new[] { "first.yml", "second.yml" }, options.Config);
            Assert.True(options.Debug);
            Assert.True(options.Verbose);
            Assert.True(options.ReadOnly);
        }

        [Fact]
        public async Task RegressionBotFlagsDefaultToFalse()
        {
            RegressionBot.BotOptions options = null;
            var command = RegressionBot.Program.CreateCommand(value =>
            {
                options = value;
                return Task.FromResult(0);
            });

            Assert.Equal(0, await command.Parse("--config config.yml --connectionstring SQL_CONNECTION_STRING").InvokeAsync());
            Assert.False(options.Debug);
            Assert.False(options.Verbose);
            Assert.False(options.ReadOnly);
        }

        [Theory]
        [InlineData("")]
        [InlineData("--config config.yml")]
        [InlineData("--connectionstring SQL_CONNECTION_STRING")]
        [InlineData("--connectionstring SQL_CONNECTION_STRING --config")]
        public async Task RegressionBotRequiresConnectionStringAndConfiguration(string args)
        {
            var invoked = false;
            var command = RegressionBot.Program.CreateCommand(_ =>
            {
                invoked = true;
                return Task.FromResult(0);
            });
            var result = command.Parse(args);

            Assert.NotEmpty(result.Errors);
            Assert.NotEqual(0, await result.InvokeAsync());
            Assert.False(invoked);
        }
    }
}
