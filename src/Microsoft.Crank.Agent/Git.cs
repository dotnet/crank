﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.Crank.Agent
{
    public static class Git
    {
        private static readonly TimeSpan CloneTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan CheckoutTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SubModuleTimeout = TimeSpan.FromSeconds(30);

        public static string Clone(string path, string repository, bool shallow = true, string branch = null, CancellationToken cancellationToken = default )
        {
            Log.WriteLine($"Cloning {repository} with branch '{branch}'");

            var branchParam = string.IsNullOrEmpty(branch) ? string.Empty : $"-b {branch}";

            var depth = shallow ? "--depth 1" : "";

            var result = RunGitCommand(path, $"clone -c core.longpaths=true {depth} {branchParam} {repository}", CloneTimeout, retries: 5, cancellationToken: cancellationToken);

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

        public static void Checkout(string path, string branchOrCommit, CancellationToken cancellationToken = default)
        {
            RunGitCommand(path, $"checkout {branchOrCommit}", CheckoutTimeout, retries: 5, cancellationToken: cancellationToken);
        }

        public static void InitSubModules(string path, CancellationToken cancellationToken = default)
        {
            RunGitCommand(path, $"submodule update --init", SubModuleTimeout, retries: 5, cancellationToken: cancellationToken);
        }

        private static ProcessResult RunGitCommand(string path, string command, TimeSpan? timeout, bool throwOnError = true, int retries = 0, CancellationToken cancellationToken = default)
        {
            return ProcessUtil.RetryOnException(retries, () => ProcessUtil.Run("git", command, timeout, workingDirectory: path, throwOnError: throwOnError, captureOutput: true, captureError: true, cancellationToken: cancellationToken), cancellationToken);
        }
    }
}
