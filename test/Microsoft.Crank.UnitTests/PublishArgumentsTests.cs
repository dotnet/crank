using Microsoft.Crank.Agent;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class PublishArgumentsTests
    {
        [Theory]
        [InlineData("6.0.428")]
        [InlineData("5.0.408")]
        [InlineData("3.1.426")]
        public void GetPublishArguments_OmitsDisableBuildServersBeforeSdk7(string sdkVersion)
        {
            var arguments = Startup.GetPublishArguments("hello.csproj", "published", "--framework net6.0 ", sdkVersion);

            Assert.Equal("publish hello.csproj -c Release -o published --framework net6.0 ", arguments);
        }

        [Theory]
        [InlineData("7.0.100")]
        [InlineData("8.0.416")]
        [InlineData("10.0.100")]
        [InlineData("11.0.100-rtm.26480.113")]
        [InlineData(null)]
        [InlineData("")]
        public void GetPublishArguments_IncludesDisableBuildServersFromSdk7(string sdkVersion)
        {
            var arguments = Startup.GetPublishArguments("hello.csproj", "published", "--framework net8.0 ", sdkVersion);

            Assert.Equal("publish hello.csproj -c Release --disable-build-servers -o published --framework net8.0 ", arguments);
        }
    }
}
