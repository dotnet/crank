// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace Microsoft.Crank.RegressionBot
{
    public class GitHubHelper
    {
        private static GitHubClient _githubClient;
        static ProductHeaderValue ClientHeader = new ProductHeaderValue("crank-regression-bot");
        static Credentials _credentials;

        static readonly TimeSpan GitHubJwtTimeout = TimeSpan.FromMinutes(5);

        public static Credentials GetCredentialsForUser(BotOptions options)
        {
            return _credentials = new Credentials(options.AccessToken);
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
            return _credentials = new Credentials(installationToken.Token, AuthenticationType.Bearer);
        }

        public static GitHubClient GetClient()
        {
            if (_githubClient == null)
            {
                _githubClient = new GitHubClient(ClientHeader);
                _githubClient.Credentials = _credentials;
            }

            return _githubClient;
        }
    }
}
