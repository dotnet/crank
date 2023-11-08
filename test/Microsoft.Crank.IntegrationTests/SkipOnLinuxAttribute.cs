using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    class SkipOnLinuxAttribute : FactAttribute
    {
        public SkipOnLinuxAttribute(string message = null) 
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = message ?? "Test ignored on Linux";
            }
        }
    }
}
