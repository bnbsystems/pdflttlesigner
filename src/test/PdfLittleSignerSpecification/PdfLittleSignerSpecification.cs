using System.Drawing;
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
using Rectangle = iText.Kernel.Geom.Rectangle;
using SkiaSharp;

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

        [Theory]
        [InlineData(600, 800, 150, 150, 5, 445, 645)]
        [InlineData(100, 100, 150, 150, 5, 0, 0)]
        public void Should_calculate_signature_location(float pageWidth, float pageHeight, int imageWidth, int imageHeight, int signatureMargin, float expectedX, float expectedY)
        {
            // Arrange
            PdfSigner pdfSigner = new("output.pdf", null);
            pdfSigner.ImageSize = new Size(imageWidth, imageHeight);
            pdfSigner.SignatureMargin = signatureMargin;
            MethodInfo? methodInfo = typeof(PdfSigner).GetMethod("CalculateSignatureLocation", BindingFlags.NonPublic | BindingFlags.Instance);
            Rectangle pageSize = new Rectangle(pageWidth, pageHeight);
            object[] parameters = { pageSize, null, null };
            float precision = 0.001F;

            // Act
            methodInfo?.Invoke(pdfSigner, parameters);
            var signatureLocationX = (float)parameters[1];
            var signatureLocationY = (float)parameters[2];

            // Assert
            signatureLocationX.Should().BeApproximately(expectedX, precision);
            signatureLocationY.Should().BeApproximately(expectedY, precision);
        }

        [Theory]
        [InlineData(100, 100, 150, 150)]
        [InlineData(121, 145, 150, 150)]
        [InlineData(300, 300, 100, 100)]
        [InlineData(316, 212, 100, 100)]
        public void Should_resize_image(int width, int height, int widthAfter, int heightAfter)
        {
            // Arrange
            byte[] imageBytes = ImageTestingUtils.GenerateSingleColorRectangle(width, height, "#e2656d", 75);
            PdfSigner pdfSigner = new("output.pdf", null);
            pdfSigner.ImageSize = new Size(widthAfter, heightAfter);
            MethodInfo? methodInfo = typeof(PdfSigner).GetMethod("ResizeImage", BindingFlags.NonPublic | BindingFlags.Instance);
            using var stampBitmap = SKBitmap.Decode(imageBytes);
            object[] parameters = { stampBitmap };

            // Act
            using SKBitmap? resizedBitmap = methodInfo?.Invoke(pdfSigner, parameters) as SKBitmap;

            // Assert
            resizedBitmap.Should().NotBeNull();
            resizedBitmap.Width.Should().Be(widthAfter);
            resizedBitmap.Height.Should().Be(heightAfter);
        }
    }
}