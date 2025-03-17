using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Microsoft.Crank.Models.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Crank.Models.Security.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CertificateOptionsExtensions"/> class.
    /// </summary>
    [TestClass]
    public class CertificateOptionsExtensionsTests
    {
        private readonly Mock<CertificateOptions> _mockCertificateOptions;

        public CertificateOptionsExtensionsTests()
        {
            _mockCertificateOptions = new Mock<CertificateOptions>();
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptionsExtensions.GetClientCertificateCredential(CertificateOptions)"/> method to ensure it returns a valid ClientCertificateCredential.
        /// </summary>
        [TestMethod]
        public void GetClientCertificateCredential_ValidCertificate_ReturnsClientCertificateCredential()
        {
            // Arrange
            var certificate = new X509Certificate2();
            _mockCertificateOptions.Setup(m => m.GetClientCertificate()).Returns(certificate);
            _mockCertificateOptions.Setup(m => m.TenantId).Returns("tenantId");
            _mockCertificateOptions.Setup(m => m.ClientId).Returns("clientId");
            _mockCertificateOptions.Setup(m => m.SniAuth).Returns(true);

            // Act
            var result = CertificateOptionsExtensions.GetClientCertificateCredential(_mockCertificateOptions.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ClientCertificateCredential));
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptionsExtensions.GetClientCertificateCredential(CertificateOptions)"/> method to ensure it returns null when the certificate is null.
        /// </summary>
        [TestMethod]
        public void GetClientCertificateCredential_NullCertificate_ReturnsNull()
        {
            // Arrange
            _mockCertificateOptions.Setup(m => m.GetClientCertificate()).Returns((X509Certificate2)null);

            // Act
            var result = CertificateOptionsExtensions.GetClientCertificateCredential(_mockCertificateOptions.Object);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptionsExtensions.GetClientCertificate(CertificateOptions)"/> method to ensure it returns a valid X509Certificate2 when a valid path is provided.
        /// </summary>
        [TestMethod]
        public void GetClientCertificate_ValidPath_ReturnsX509Certificate2()
        {
            // Arrange
            _mockCertificateOptions.Setup(m => m.Path).Returns("validPath");
            _mockCertificateOptions.Setup(m => m.Password).Returns("password");

            // Act
            var result = CertificateOptionsExtensions.GetClientCertificate(_mockCertificateOptions.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(X509Certificate2));
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptionsExtensions.GetClientCertificate(CertificateOptions)"/> method to ensure it returns null when no certificate is found.
        /// </summary>
        [TestMethod]
        public void GetClientCertificate_NoCertificateFound_ReturnsNull()
        {
            // Arrange
            _mockCertificateOptions.Setup(m => m.Path).Returns((string)null);
            _mockCertificateOptions.Setup(m => m.Thumbprint).Returns("invalidThumbprint");

            // Act
            var result = CertificateOptionsExtensions.GetClientCertificate(_mockCertificateOptions.Object);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptionsExtensions.GetClientCertificate(CertificateOptions)"/> method to ensure it handles CryptographicException and continues searching.
        /// </summary>
        [TestMethod]
        public void GetClientCertificate_CryptographicException_ContinuesSearching()
        {
            // Arrange
            _mockCertificateOptions.Setup(m => m.Path).Returns((string)null);
            _mockCertificateOptions.Setup(m => m.Thumbprint).Returns("thumbprint");

            // Act
            var result = CertificateOptionsExtensions.GetClientCertificate(_mockCertificateOptions.Object);

            // Assert
            Assert.IsNull(result);
        }
    }
}
