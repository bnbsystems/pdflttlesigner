using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using iText.Signatures;
using Microsoft.AspNetCore.Http;
using Moq;
using PdfLittleSigner;
using Xunit;

namespace PdfLittleSignerSpecification
{
    public class PdfLittleSignerSpecification
    {
        [Fact]
        public async Task Should_not_sign_pdf_when_given_empty_output_file_path()
        {
            // Arrange
            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);

            var signReason = fixture.Create<string>();
            var contact = fixture.Create<string>();
            var location = fixture.Create<string>();
            var visible = false;
            IFormFile stamp = null;

            var fileToSign = "Data/sample.pdf";
            byte [] fileToSignBytes = await File.ReadAllBytesAsync(fileToSign);

            var pdfSigner = new PdpSigner(string.Empty, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, cert, fileToSignBytes);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Should_sign_pdf_when_given_valid_output_file_path()
        {
            // Arrange
            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);

            var signReason = fixture.Create<string>();
            var contact = fixture.Create<string>();
            var location = fixture.Create<string>();
            var visible = true;
            IFormFile stamp = null;

            var fileToSign = "Data/sample.pdf";
            byte[] fileToSignBytes = await File.ReadAllBytesAsync(fileToSign);

            var pdfSigner = new PdpSigner("output.pdf", null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, cert, fileToSignBytes);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Should_not_sign_pdf_when_given_null_output_stream()
        {
            // Arrange
            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);

            var signReason = fixture.Create<string>();
            var contact = fixture.Create<string>();
            var location = fixture.Create<string>();
            var visible = false;
            IFormFile stamp = null;

            var fileToSign = "Data/sample.pdf";
            byte[] fileToSignBytes = await File.ReadAllBytesAsync(fileToSign);

            Stream fs = null;
            var pdfSigner = new PdpSigner(fs, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, cert, fileToSignBytes);

            // Assert
            result.Should().BeFalse();
        }


        [Fact]
        public async Task Should_sign_pdf_when_given_valid_output_stream()
        {
            // Arrange

            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);

            var signReason = fixture.Create<string>();
            var contact = fixture.Create<string>();
            var location = fixture.Create<string>();
            var visible = false;
            var stampFileMock = new Mock<IFormFile>();

            var fileToSign = "Data/sample.pdf";
            byte[] fileToSignBytes = await File.ReadAllBytesAsync(fileToSign);

            await using FileStream fs = new FileStream("output.pdf", FileMode.OpenOrCreate, FileAccess.Write);
            var pdfSigner = new PdpSigner(fs, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stampFileMock.Object, cert, fileToSignBytes);

            // Assert
            result.Should().BeTrue();
        }

        
        [Fact]
        public void Should_create_external_signature()
        {
            // Arrange
            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);

            PdpSigner pdfSigner = new("output.pdf", null);
            MethodInfo? methodInfo = typeof(PdpSigner).GetMethod("CreateExternalSignature", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { cert };

            // Act
            var result = methodInfo?.Invoke(pdfSigner, parameters);

            // Assert
            result.Should().NotBeNull();
            if (result != null)
            {
                var resultObj = (IExternalSignature)result;
                resultObj.GetEncryptionAlgorithm().Should().NotBeNull();
                resultObj.GetHashAlgorithm().Should().NotBeNull();
            }
        }

        [Fact]
        public void Should_not_create_external_signature_when_there_is_no_rsa_private_key_in_certificate()
        {
            // Arrange
            var fixture = new Fixture();

            var commonName = fixture.Create<string>();
            var cert = X509CertificateTestingUtils.GenerateX509Certificate2WithEcdsaKey(commonName);

            PdpSigner pdfSigner = new("output.pdf", null);
            MethodInfo? methodInfo = typeof(PdpSigner).GetMethod("CreateExternalSignature", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { cert };

            // Act - Assert
            var result = Assert.Throws<TargetInvocationException>(() => methodInfo?.Invoke(pdfSigner, parameters));
            result.InnerException.Should().NotBeNull();
            result.InnerException.Should().BeOfType<CryptographicException>();
        }
    }
}