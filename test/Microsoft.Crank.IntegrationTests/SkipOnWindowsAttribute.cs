using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    class SkipOnWindowsAttribute : FactAttribute
    {
        public SkipOnWindowsAttribute(string message = null) 
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = message ?? "Test ignored on Windows";
            }
        }
    }
}
