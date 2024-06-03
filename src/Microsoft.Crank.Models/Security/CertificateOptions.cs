using System;

namespace Microsoft.Crank.Models.Security
{
    public class CertificateOptions
    {
        public string ClientId { get; }
        public string TenantId { get; }
        public string Thumbprint { get; }
        public string Path { get; }

        public CertificateOptions(string clientId, string tenantId, string thumbprint, string path)
        {
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(clientId)))
            {
                clientId = Environment.GetEnvironmentVariable(clientId);
            }

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(tenantId)))
            {
                tenantId = Environment.GetEnvironmentVariable(tenantId);
            }

            if (!string.IsNullOrEmpty(thumbprint) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(thumbprint)))
            {
                thumbprint = Environment.GetEnvironmentVariable(thumbprint);
            }

            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(path)))
            {
                path = Environment.GetEnvironmentVariable(path);
            }

            ClientId = clientId;
            TenantId = tenantId;
            Thumbprint = thumbprint;
            Path = path;
        }
    }
}
