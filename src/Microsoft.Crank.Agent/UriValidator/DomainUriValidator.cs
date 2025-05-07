using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.AntiSSRF;

namespace Microsoft.Crank.Agent.UriValidator
{
    public class DomainUriValidator(ICollection<string> allowedDomains) : IUriValidator
    {
        private readonly string[] _allowedDomains = allowedDomains?.ToArray() ?? [];

        public bool IsValid(Uri uri)
        {
            if (uri == null)
            {
                return false;
            }
            if (_allowedDomains.Length == 0)
            {
                return true;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }
            if (!URIValidate.InDomain(uri, _allowedDomains))
            {
                return false;
            }

            return true;
        }
    }
}
