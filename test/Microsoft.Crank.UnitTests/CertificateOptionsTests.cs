// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Crank.Models.Security;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class CertificateOptionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("test-password")]
        public void LoadsPkcs12WithPrivateKey(string password)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=crank-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            var path = Path.GetTempFileName();

            try
            {
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
                var options = new CertificateOptions(null, null, null, path, password, false);

                using var loaded = options.GetClientCertificate();

                Assert.Equal(certificate.Thumbprint, loaded.Thumbprint);
                Assert.True(loaded.HasPrivateKey);
                using var loadedKey = loaded.GetRSAPrivateKey();
                var data = new byte[] { 1, 2, 3 };
                var signature = loadedKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Assert.True(key.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadsDerAndPemCertificates(bool pem)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=crank-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            var path = Path.GetTempFileName();

            try
            {
                if (pem)
                {
                    File.WriteAllText(path, certificate.ExportCertificatePem());
                }
                else
                {
                    File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
                }

                var options = new CertificateOptions(null, null, null, path, null, false);
                using var loaded = options.GetClientCertificate();

                Assert.Equal(certificate.Thumbprint, loaded.Thumbprint);
                Assert.False(loaded.HasPrivateKey);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void RejectsIncorrectPkcs12Password()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=crank-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            var path = Path.GetTempFileName();

            try
            {
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, "test-password"));
                var options = new CertificateOptions(null, null, null, path, "wrong-password", false);

                Assert.ThrowsAny<CryptographicException>(() => options.GetClientCertificate());
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
