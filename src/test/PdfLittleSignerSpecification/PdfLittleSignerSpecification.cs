using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using iText.Signatures;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using PdfSigner = PdfLittleSigner.PdfSigner;

namespace PdfLittleSignerSpecification
{
    public class PdfLittleSignerSpecification
    {
        readonly string commonName;
        readonly X509Certificate2 rsaCert;
        readonly X509Certificate2 ecdsaCert;
        readonly string signReason;
        readonly string contact;
        readonly string location;
        readonly byte[] fileToSignBytes;

        public PdfLittleSignerSpecification()
        {
            var fixture = new Fixture();

            commonName = fixture.Create<string>();
            rsaCert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);
            ecdsaCert = X509CertificateTestingUtils.GenerateX509Certificate2WithEcdsaKey(commonName);

            signReason = fixture.Create<string>();
            contact = fixture.Create<string>();
            location = fixture.Create<string>();

            var fileToSign = "Data/sample.pdf";
            fileToSignBytes = File.ReadAllBytes(fileToSign);
        }

        [Fact]
        public async Task Should_not_sign_pdf_when_given_empty_output_file_path()
        {
            // Arrange
            var visible = false;
            IFormFile? stamp = null;
            var pdfSigner = new PdfSigner(string.Empty, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, rsaCert, fileToSignBytes);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Should_sign_pdf_when_given_valid_output_file_path()
        {
            // Arrange
            var visible = true;
            IFormFile? stamp = null;
            var pdfSigner = new PdfSigner("output.pdf", null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, rsaCert, fileToSignBytes);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Should_not_sign_pdf_when_given_null_output_stream()
        {
            // Arrange
            var visible = false;
            IFormFile? stamp = null;

            Stream? fs = null;
            var pdfSigner = new PdfSigner(fs, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stamp, rsaCert, fileToSignBytes);

            // Assert
            result.Should().BeFalse();
        }


        [Fact]
        public async Task Should_sign_pdf_when_given_valid_output_stream()
        {
            // Arrange
            var stampFileMock = new Mock<IFormFile>();
            var visible = false;


            var fileToSign = "Data/sample.pdf";
            byte[] fileToSignBytes = await File.ReadAllBytesAsync(fileToSign);

            await using FileStream fs = new FileStream("output.pdf", FileMode.OpenOrCreate, FileAccess.Write);
            var pdfSigner = new PdfSigner(fs, null);

            // Act
            var result = await pdfSigner.Sign(signReason, contact, location, visible, stampFileMock.Object, rsaCert, fileToSignBytes);

            // Assert
            result.Should().BeTrue();
        }


        [Fact]
        public void Should_create_external_signature()
        {
            // Arrange
            PdfSigner pdfSigner = new("output.pdf", null);
            MethodInfo? methodInfo = typeof(PdfSigner).GetMethod("CreateExternalSignature", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { rsaCert };

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
            PdfSigner pdfSigner = new("output.pdf", null);
            MethodInfo? methodInfo = typeof(PdfSigner).GetMethod("CreateExternalSignature", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { ecdsaCert };

            // Act - Assert
            var result = Assert.Throws<TargetInvocationException>(() => methodInfo?.Invoke(pdfSigner, parameters));
            result.InnerException.Should().NotBeNull();
            result.InnerException.Should().BeOfType<CryptographicException>();
        }
    }
}