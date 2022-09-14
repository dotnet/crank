// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Crank.PullRequestBot
{
    public class BotOptions
    {
        public string Workspace { get; set; } = Path.GetTempPath();
        public int Limit { get; set; } = 0;
        public int Age { get; set; } = 60;
        public string Benchmarks { get; set; } = "";
        public string Profiles { get; set; } = "";
        public string Components { get; set; } = "";
        public string PullRequest { get; set; }
        public string Repository { get; set; }
        public bool PublishResults { get; set; } = false;
        public string AccessToken { get; set; }
        public string AppKey { get; set; }
        public string AppId { get; set; }
        public long InstallId { get; set; }
        public string Config { get; set; }
        public bool Debug { get; set; }
        public Uri GitHubBaseUrl { get; set; }
        public string Arguments { get; set; }

        public void Validate()
        {
            if (!Debug)
            {
                if (string.IsNullOrEmpty(Repository) && string.IsNullOrEmpty(PullRequest))
                {
                    throw new ArgumentException("--repository or --pull-request is required");
                }

                else if (!string.IsNullOrEmpty(AppKey))
                {
                    if(string.IsNullOrEmpty(AppId))
                    {
                        throw new ArgumentException("GitHubAppId argument is missing");
                    }

                    if (InstallId == 0)
                    {
                        throw new ArgumentException("GitHubInstallationId argument is missing");
                    }
                }
            }
        }
    }
}
