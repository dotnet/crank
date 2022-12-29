using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    class SkipOnMacOsAttribute : FactAttribute
    {
        public SkipOnMacOsAttribute(string message = null) 
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Skip = message ?? "Test ignored on OSX";
            }
        }
    }
}
