using System;
using System.Linq;
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

        return new ClientCertificateCredential(certificateOptions.TenantId, certificateOptions.ClientId, certificate);
    }

    public static X509Certificate2 GetClientCertificate(this CertificateOptions certificateOptions)
    {
        if (!string.IsNullOrEmpty(certificateOptions.Path))
        {
            return new X509Certificate2(certificateOptions.Path);
        }

        foreach (var name in Enum.GetValues<StoreName>())
        {
            var store = new X509Store(name, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, certificateOptions.Thumbprint, true).FirstOrDefault();

            if (certificate != null)
            {
                return certificate;
            }
        }

        return null;
    }
}
