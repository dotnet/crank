// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Identity;

namespace Microsoft.Crank.Models.Security;

public static class ManagedIdentityOptionsExtensions
{
    /// <summary>
    /// Gets a ManagedIdentityCredential for the configured user-assigned managed identity.
    /// </summary>
    public static ManagedIdentityCredential GetManagedIdentityCredential(this ManagedIdentityOptions options)
    {
        if (options == null)
        {
            return null;
        }

        return new ManagedIdentityCredential(options.ClientId);
    }
}
