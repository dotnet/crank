using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Crank.Models.Security.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CertificateOptions"/> class.
    /// </summary>
    [TestClass]
    public class CertificateOptionsTests
    {
        private readonly string _clientId = "client-id";
        private readonly string _tenantId = "tenant-id";
        private readonly string _thumbprint = "thumbprint";
        private readonly string _path = "path";
        private readonly string _password = "password";
        private readonly bool _sniAuth = true;

        /// <summary>
        /// Tests the <see cref="CertificateOptions.CertificateOptions(string, string, string, string, string, bool)"/> constructor to ensure it correctly initializes properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_InitializesPropertiesCorrectly()
        {
            // Act
            var options = new CertificateOptions(_clientId, _tenantId, _thumbprint, _path, _password, _sniAuth);

            // Assert
            Assert.AreEqual(_clientId, options.ClientId);
            Assert.AreEqual(_tenantId, options.TenantId);
            Assert.AreEqual(_thumbprint, options.Thumbprint);
            Assert.AreEqual(_path, options.Path);
            Assert.AreEqual(_password, options.Password);
            Assert.AreEqual(_sniAuth, options.SniAuth);
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptions.CertificateOptions(string, string, string, string, string, bool)"/> constructor to ensure it correctly retrieves environment variables.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenEnvironmentVariablesAreSet_InitializesPropertiesFromEnvironmentVariables()
        {
            // Arrange
            Environment.SetEnvironmentVariable(_clientId, "env-client-id");
            Environment.SetEnvironmentVariable(_tenantId, "env-tenant-id");
            Environment.SetEnvironmentVariable(_thumbprint, "env-thumbprint");
            Environment.SetEnvironmentVariable(_path, "env-path");
            Environment.SetEnvironmentVariable(_password, "env-password");

            // Act
            var options = new CertificateOptions(_clientId, _tenantId, _thumbprint, _path, _password, _sniAuth);

            // Assert
            Assert.AreEqual("env-client-id", options.ClientId);
            Assert.AreEqual("env-tenant-id", options.TenantId);
            Assert.AreEqual("env-thumbprint", options.Thumbprint);
            Assert.AreEqual("env-path", options.Path);
            Assert.AreEqual("env-password", options.Password);
            Assert.AreEqual(_sniAuth, options.SniAuth);

            // Cleanup
            Environment.SetEnvironmentVariable(_clientId, null);
            Environment.SetEnvironmentVariable(_tenantId, null);
            Environment.SetEnvironmentVariable(_thumbprint, null);
            Environment.SetEnvironmentVariable(_path, null);
            Environment.SetEnvironmentVariable(_password, null);
        }

        /// <summary>
        /// Tests the <see cref="CertificateOptions.CertificateOptions(string, string, string, string, string, bool)"/> constructor to ensure it handles null and empty strings correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithNullOrEmptyStrings_InitializesPropertiesCorrectly()
        {
            // Act
            var options = new CertificateOptions(null, string.Empty, null, string.Empty, null, _sniAuth);

            // Assert
            Assert.IsNull(options.ClientId);
            Assert.AreEqual(string.Empty, options.TenantId);
            Assert.IsNull(options.Thumbprint);
            Assert.AreEqual(string.Empty, options.Path);
            Assert.IsNull(options.Password);
            Assert.AreEqual(_sniAuth, options.SniAuth);
        }
    }
}
