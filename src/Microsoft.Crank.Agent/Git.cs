// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Agent
{
    public static class Git
    {
        private static readonly TimeSpan CloneTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan CheckoutTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SubModuleTimeout = TimeSpan.FromSeconds(30);

        public static async Task<string> CloneAsync(string path, string repository, bool shallow = true, string branch = null, CancellationToken cancellationToken = default)
        {
            Log.WriteLine($"Cloning {repository} with branch '{branch}'");

            var branchParam = string.IsNullOrEmpty(branch) ? string.Empty : $"-b {branch}";

            var depth = shallow ? "--depth 1" : "";

            var result = await RunGitCommandAsync(path, $"clone -c core.longpaths=true {depth} {branchParam} {repository}", CloneTimeout, retries: 5, cancellationToken: cancellationToken);

            var match = Regex.Match(result.StandardError, @"'(.*)'");
            if (match.Success && match.Groups.Count == 2)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new InvalidOperationException("Could not parse directory from 'git clone' standard error");
            }
        }

        public static Task CheckoutAsync(string path, string branchOrCommit, CancellationToken cancellationToken = default)
        {
            return RunGitCommandAsync(path, $"checkout {branchOrCommit}", CheckoutTimeout, retries: 5, cancellationToken: cancellationToken);
        }

        public static Task InitSubModulesAsync(string path, CancellationToken cancellationToken = default)
        {
            return RunGitCommandAsync(path, $"submodule update --init", SubModuleTimeout, retries: 5, cancellationToken: cancellationToken);
        }

        private static Task<ProcessResult> RunGitCommandAsync(string path, string command, TimeSpan? timeout, bool throwOnError = true, int retries = 0, CancellationToken cancellationToken = default)
        {
            return ProcessUtil.RetryOnExceptionAsync(retries, () => ProcessUtil.RunAsync("git", command, timeout, workingDirectory: path, throwOnError: throwOnError, captureOutput: true, captureError: true, cancellationToken: cancellationToken));
        }
    }
}
