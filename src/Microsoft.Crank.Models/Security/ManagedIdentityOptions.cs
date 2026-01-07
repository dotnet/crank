// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Azure.Identity;

namespace Microsoft.Crank.Models.Security
{
    public class ManagedIdentityOptions
    {
        /// <summary>
        /// The client ID of the user-assigned managed identity.
        /// </summary>
        public string ClientId { get; }

        public ManagedIdentityOptions(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(clientId)))
            {
                clientId = Environment.GetEnvironmentVariable(clientId) ?? clientId;
            }

            ClientId = clientId;
        }

        /// <summary>
        /// Gets a ManagedIdentityCredential for the configured user-assigned managed identity.
        /// </summary>
        public ManagedIdentityCredential GetManagedIdentityCredential()
        {
            return new ManagedIdentityCredential(ClientId);
        }
    }
}
