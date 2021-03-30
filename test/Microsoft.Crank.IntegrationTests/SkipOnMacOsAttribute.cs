using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    class SkipOnMacOsAttribute : FactAttribute
    {
        public SkipOnMacOsAttribute() 
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Skip = $"Test ignored on OSX";
            }
        }
    }
}
