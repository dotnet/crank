using System.Runtime.InteropServices;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="EnvironmentData"/> class.
    /// </summary>
    [TestClass]
    public class EnvironmentDataTests
    {
        private readonly EnvironmentData _environmentData;

        public EnvironmentDataTests()
        {
            _environmentData = new EnvironmentData();
        }

        /// <summary>
        /// Tests the <see cref="EnvironmentData.Platform"/> property to ensure it returns the correct platform string.
        /// </summary>
        [TestMethod]
        [DataRow(OSPlatform.Windows, "windows")]
        [DataRow(OSPlatform.Linux, "linux")]
        [DataRow(OSPlatform.OSX, "osx")]
        [DataRow(null, "other")]
        public void Platform_WhenCalled_ReturnsCorrectPlatformString(OSPlatform? osPlatform, string expectedPlatform)
        {
            // Arrange
            if (osPlatform.HasValue)
            {
                MockRuntimeInformation(osPlatform.Value);
            }

            // Act
            string actualPlatform = _environmentData.Platform;

            // Assert
            Assert.AreEqual(expectedPlatform, actualPlatform,
                $"Expected platform: {expectedPlatform}, Actual platform: {actualPlatform}");
        }

        /// <summary>
        /// Tests the <see cref="EnvironmentData.Architecture"/> property to ensure it returns the correct architecture string.
        /// </summary>
        [TestMethod]
        [DataRow(Architecture.X86, "X86")]
        [DataRow(Architecture.X64, "X64")]
        [DataRow(Architecture.Arm, "Arm")]
        [DataRow(Architecture.Arm64, "Arm64")]
        public void Architecture_WhenCalled_ReturnsCorrectArchitectureString(Architecture architecture, string expectedArchitecture)
        {
            // Arrange
            MockRuntimeInformation(architecture);

            // Act
            string actualArchitecture = _environmentData.Architecture;

            // Assert
            Assert.AreEqual(expectedArchitecture, actualArchitecture,
                $"Expected architecture: {expectedArchitecture}, Actual architecture: {actualArchitecture}");
        }

        private void MockRuntimeInformation(OSPlatform osPlatform)
        {
            // Mocking RuntimeInformation.IsOSPlatform method
            var runtimeInformationMock = new Mock<IRuntimeInformation>();
            runtimeInformationMock.Setup(ri => ri.IsOSPlatform(OSPlatform.Windows)).Returns(osPlatform == OSPlatform.Windows);
            runtimeInformationMock.Setup(ri => ri.IsOSPlatform(OSPlatform.Linux)).Returns(osPlatform == OSPlatform.Linux);
            runtimeInformationMock.Setup(ri => ri.IsOSPlatform(OSPlatform.OSX)).Returns(osPlatform == OSPlatform.OSX);
            RuntimeInformationWrapper.RuntimeInformation = runtimeInformationMock.Object;
        }

        private void MockRuntimeInformation(Architecture architecture)
        {
            // Mocking RuntimeInformation.OSArchitecture property
            var runtimeInformationMock = new Mock<IRuntimeInformation>();
            runtimeInformationMock.Setup(ri => ri.OSArchitecture).Returns(architecture);
            RuntimeInformationWrapper.RuntimeInformation = runtimeInformationMock.Object;
        }
    }

    public interface IRuntimeInformation
    {
        bool IsOSPlatform(OSPlatform osPlatform);
        Architecture OSArchitecture { get; }
    }

    public static class RuntimeInformationWrapper
    {
        public static IRuntimeInformation RuntimeInformation { get; set; } = new DefaultRuntimeInformation();

        private class DefaultRuntimeInformation : IRuntimeInformation
        {
            public bool IsOSPlatform(OSPlatform osPlatform) => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(osPlatform);
            public Architecture OSArchitecture => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        }
    }
}

