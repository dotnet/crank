using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;

namespace Microsoft.Crank.Models.Security;

public static class CertificateOptionsExtensions
{
    public static ClientCertificateCredential GetClientCertificateCredential(this CertificateOptions certificateOptions)
    {
        var certificate = certificateOptions.GetClientCertificate();

        if (certificate == null)
        {
            return null;
        }

        return new ClientCertificateCredential(
            certificateOptions.TenantId, 
            certificateOptions.ClientId, 
            certificate, 
            new() { SendCertificateChain = certificateOptions.SniAuth }
            );
    }

    public static X509Certificate2 GetClientCertificate(this CertificateOptions certificateOptions)
    {
        if (!string.IsNullOrEmpty(certificateOptions.Path))
        {
            return new X509Certificate2(certificateOptions.Path, certificateOptions.Password);
        }

        foreach (var location in Enum.GetValues<StoreLocation>())
        {
            foreach (var name in Enum.GetValues<StoreName>())
            {
                try
                {
                    var store = new X509Store(name, location, OpenFlags.ReadOnly);

                    // Use validOnly = false in order to load self-signed certificates too. It will be the responsibility of the user to understand why the 
                    // certificate doesn't work instead of just ignoring it silently.

                    var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, certificateOptions.Thumbprint, validOnly: false).FirstOrDefault();

                    if (certificate != null)
                    {
                        return certificate;
                    }
                }
                catch (CryptographicException)
                {
                    // Skip if the store is not available on the current platform.
                    continue;
                }
            }
        }

        return null;
    }
}
