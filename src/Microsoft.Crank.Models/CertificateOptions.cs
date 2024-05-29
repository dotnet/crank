using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crank.Models
{
    public class CertificateOptions
    {
        public string ClientId { get; }
        public string TenantId { get; }
        public string Thumbprint { get; }
        public string Path { get; }

        public CertificateOptions(string clientId, string tenantId, string thumbprint, string path)
        {
            ClientId = clientId;
            TenantId = tenantId;
            Thumbprint = thumbprint;
            Path = path;
        }
    }
}
