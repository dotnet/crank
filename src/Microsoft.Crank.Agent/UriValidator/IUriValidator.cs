using System;

namespace Microsoft.Crank.Agent.UriValidator
{
    public interface IUriValidator
    {
        public bool IsValid(Uri uri);
    }
}
