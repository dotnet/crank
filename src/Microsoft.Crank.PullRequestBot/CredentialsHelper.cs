// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.PullRequestBot
{
    public class GitHubHelper
    {
        static ProductHeaderValue ClientHeader = new ProductHeaderValue("crank-pullrequest-bot");

        static readonly TimeSpan GitHubJwtTimeout = TimeSpan.FromMinutes(5);

        public static async Task<Credentials> GetCredentialsAsync(BotOptions options)
        {
            if (!String.IsNullOrEmpty(options.AppKey))
            {
                return await GetCredentialsForAppAsync(options);
            }
            else if (!String.IsNullOrEmpty(options.AccessToken))
            {
                return GetCredentialsForUser(options);
            }
            else
            {
                return await GetCredentialsFromStore();
            }
        }
        public static Credentials GetCredentialsForUser(BotOptions options)
        {
            return new Credentials(options.AccessToken);
        }

        private static RsaSecurityKey GetRsaSecurityKeyFromPemKey(string keyText)
        {
            using var rsa = RSA.Create();

            var keyBytes = Convert.FromBase64String(keyText);

            rsa.ImportRSAPrivateKey(keyBytes, out _);

            return new RsaSecurityKey(rsa.ExportParameters(true));
        }

        public static async Task<Credentials> GetCredentialsForAppAsync(BotOptions options)
        {
            var creds = new SigningCredentials(GetRsaSecurityKeyFromPemKey(options.AppKey), SecurityAlgorithms.RsaSha256);

            var jwtToken = new JwtSecurityToken(
                new JwtHeader(creds),
                new JwtPayload(
                    issuer: options.AppId,
                    issuedAt: DateTime.Now,
                    expires: DateTime.Now.Add(GitHubJwtTimeout),
                    audience: null,
                    claims: null,
                    notBefore: null));

            var jwtTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            var initClient = new GitHubClient(ClientHeader)
            {
                Credentials = new Credentials(jwtTokenString, AuthenticationType.Bearer),
            };

            var installationToken = await initClient.GitHubApps.CreateInstallationToken(options.InstallId);
            return new Credentials(installationToken.Token, AuthenticationType.Bearer);
        }

        public static async Task<Credentials> GetCredentialsFromStore()
        {
            // echo url=https://github.com/git/git.git | git credential fill

            // protocol=https
            // host=github.com
            // username=[PLACEHOLDER] # actual value is "Personal Access Token"
            // password=[PLACEHOLDER]

            var presult = await ProcessUtil.RunAsync(ProcessUtil.GetScriptHost(), "/c echo url=https://github.com/git/git.git | git credential fill", captureOutput: true);

            var match = Regex.Match(presult.StandardOutput, "password=(.*)");

            if (match.Success)
            {
                return new Credentials(match.Groups[1].Value.Trim());
            }

            return null;
        }

        public static GitHubClient CreateClient(Credentials credentials)
        {
            var githubClient = new GitHubClient(ClientHeader);
            githubClient.Credentials = credentials;

            return githubClient;
        }
    }
}
