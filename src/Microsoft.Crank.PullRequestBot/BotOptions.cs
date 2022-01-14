// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.PullRequestBot
{
    public class BotOptions
    {
        public long RepositoryId { get; set; }
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public string AppKey { get; set; }
        public string AppId { get; set; }
        public long InstallId { get; set; }
        public string Config { get; set; }
        public bool Debug { get; set; }
        public bool Verbose { get; set; }
        public bool ReadOnly { get; set; }

        public void Validate()
        {
            return;

            if (!Debug)
            {
                if (RepositoryId == 0)
                {
                    throw new ArgumentException("RepositoryId argument is missing or invalid");
                }

                if (String.IsNullOrEmpty(AccessToken) && String.IsNullOrEmpty(AppKey))
                {
                    throw new ArgumentException("AccessToken or GitHubAppKey is required");
                }
                else if (!String.IsNullOrEmpty(AppKey))
                {
                    if(String.IsNullOrEmpty(AppId))
                    {
                        throw new ArgumentException("GitHubAppId argument is missing");
                    }

                    if (InstallId == 0)
                    {
                        throw new ArgumentException("GitHubInstallationId argument is missing");
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(Username))
                    {
                        throw new ArgumentException("Username argument is missing");
                    }
                }

            }
        }
    }
}
