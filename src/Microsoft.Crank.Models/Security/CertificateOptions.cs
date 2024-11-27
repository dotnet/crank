using System;

namespace Microsoft.Crank.Models.Security
{
    public class CertificateOptions
    {
        public string ClientId { get; }
        public string TenantId { get; }
        public string Thumbprint { get; }
        public string Path { get; }
        public string Password { get; }
        public bool SniAuth { get; }

        public CertificateOptions(string clientId, string tenantId, string thumbprint, string path, string password, bool sniAuth)
        {
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(clientId)))
            {
                clientId = Environment.GetEnvironmentVariable(clientId) ?? clientId;
            }

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(tenantId)))
            {
                tenantId = Environment.GetEnvironmentVariable(tenantId) ?? tenantId;
            }

            if (!string.IsNullOrEmpty(thumbprint) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(thumbprint)))
            {
                thumbprint = Environment.GetEnvironmentVariable(thumbprint) ?? thumbprint;
            }

            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(path)))
            {
                path = Environment.GetEnvironmentVariable(path) ?? path;
            }

            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(password)))
            {
                password = Environment.GetEnvironmentVariable(password) ?? password;
            }

            ClientId = clientId;
            TenantId = tenantId;
            Thumbprint = thumbprint;
            Path = path;
            Password = password;
            SniAuth = sniAuth;
        }
    }
}
