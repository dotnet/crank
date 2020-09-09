// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.RegressionBot
{
    public class BotOptions
    {
        public BotOptions(
            long repositoryId, 
            string accessToken, 
            string username, 
            string appKey,
            string appId,
            long installId,
            string connectionstring,
            string[] source,
            bool debug)
        {
            RepositoryId = repositoryId;
            AccessToken = accessToken;
            Username = username;
            AppKey = appKey;
            AppId = appId;
            InstallId = installId;
            ConnectionString = connectionstring;
            Source = source;
            Debug = debug;
        }

        public long RepositoryId { get; }
        public string AccessToken { get; }
        public string Username { get; }
        public string AppKey { get; }
        public string AppId { get; }
        public long InstallId { get; }
        public string ConnectionString { get; }
        public string[] Source { get; }
        public bool Debug { get; }

        public void Validate()
        {
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

                if (String.IsNullOrEmpty(ConnectionString))
                {
                    throw new ArgumentException("ConnectionString argument is missing");
                }
            }
        }
    }
}
