using AutoFixture;
using FluentAssertions;
using iText.Kernel.Pdf;
using iText.Signatures;
using PdfLittleSigner;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using PdfSigner = PdfLittleSigner.PdfSigner;

namespace PdfLittleSignerSpecification
{
    public class PdfLittleSignerSpecification : IDisposable
    {
        readonly string commonName;
        readonly X509Certificate2 rsaCert;
        readonly X509Certificate2 ecdsaCert;
        const string signReason = "Signed Reason";
        readonly string contact;
        readonly string location;
        readonly byte[] fileToSignBytes;
        const string fileToSign = "Data/sample.pdf";
        readonly byte[] alreadySignedFileBytes;
        const string alreadySignedFile = "Data/signed_sample.pdf";
        readonly string fileOutput;
        readonly bool visible = false;

        PdfSigner pdfSigner;
        public PdfLittleSignerSpecification()
        {
            var fixture = new Fixture();

            commonName = fixture.Create<string>();
            rsaCert = X509CertificateTestingUtils.GenerateX509Certificate2WithRsaKey(commonName);
            ecdsaCert = X509CertificateTestingUtils.GenerateX509Certificate2WithEcdsaKey(commonName); // no RSA private key

            contact = fixture.Create<string>();
            location = fixture.Create<string>();
            fileOutput = "output_" + location + ".pdf";

            fileToSignBytes = File.ReadAllBytes(fileToSign);
            alreadySignedFileBytes = File.ReadAllBytes(alreadySignedFile);
            pdfSigner = new PdfSigner(fileOutput);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Should_not_sign_pdf_when_given_empty_output_file_path(string output)
        {
            var pdfSigner = new PdfSigner(output);
            var result = await pdfSigner.Sign(signReason, contact, location, visible, null, rsaCert, fileToSignBytes);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Should_not_sign_pdf_when_given_null_output_stream()
        {
            Stream? fs = null;
            var pdfSigner = new PdfSigner(fs);
            var result = await pdfSigner.Sign(signReason, contact, location, visible, null, rsaCert, fileToSignBytes);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_sign_pdf_when_given_valid_output_file_path(bool shouldbevisible)
        {
            var result = await pdfSigner.Sign(signReason, contact, location, shouldbevisible, null, rsaCert, fileToSignBytes);
            result.Should().BeTrue();
            ValidateIfDocumentIsSigned(1);
        }


        private void ValidateIfDocumentIsSigned(int expectedSignatureCount)
        {
            using PdfReader reader = new(fileOutput);
            using PdfDocument document = new PdfDocument(reader);
            SignatureUtil signatureUtil = new SignatureUtil(document);
            var signatureNames = signatureUtil.GetSignatureNames();
            signatureNames.Count.Should().Be(expectedSignatureCount);
            var signatureName = signatureNames.Last();
            var signature = signatureUtil.GetSignature(signatureName);

            var dateStr = signature.GetDate().ToString();
            dateStr.Should().NotBeEmpty();
            DateTime signedDateTime = DDateTimeParser.ToDateTimeFromDString(dateStr);
            signedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));

            signature.GetReason().Should().BeEquivalentTo(signReason);
            signature.GetLocation().Should().BeEquivalentTo(location);
            signature.GetContents().ToString().Should().NotBeEmpty();

            var name = signature.GetName();
            var cert = signature.GetCert();
            signature.GetSubFilter().GetValue().ToString().Should().Contain("adbe.pkcs7.detached");

            var pdfObject = signature.GetPdfObject();
            pdfObject.ToString().Should().Contain(contact);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Should_sign_document_which_was_already_signed_before(bool visible)
        {
            var result = await SignWithDefaultTestConfiguration(visible, alreadySignedFileBytes);

            result.Should().BeTrue();
            ValidateIfDocumentIsSigned(2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Should_sign_pdf_when_given_valid_output_stream(bool visible)
        {
            var result = await SignWithDefaultTestConfiguration(visible, fileToSignBytes);

            result.Should().BeTrue();
            ValidateIfDocumentIsSigned(1);
        }

        private async Task<bool> SignWithDefaultTestConfiguration(bool visible, byte[] fileBytes)
        {
            using var m = new MemoryStream();
            using StreamReader streamRead = new StreamReader("Data/valid.jpg");
            streamRead.BaseStream.CopyTo(m);

            var namedImage = new NamedImage { Name = "valid.jpg", Data = m.ToArray() };

            await using FileStream fs = new FileStream(fileOutput, FileMode.OpenOrCreate, FileAccess.Write);
            var pdfSigner = new PdfSigner(fs);

            string field = rsaCert.GetNameInfo(X509NameType.SimpleName, false);
            var imageText = "Operat podpisany cyfrowo \n" + $"przez {field} \n";

            return await pdfSigner.Sign(signReason, contact, location, visible, namedImage, rsaCert, fileBytes, imageText: imageText);
        }

        [Fact]
        public void Should_create_external_signature()
        {
            var result = pdfSigner.CreateExternalSignature(rsaCert);
            result.Should().NotBeNull();
            result.GetEncryptionAlgorithm().Should().Be("RSA");
            result.GetHashAlgorithm().Should().Be("SHA256");
        }


        [Fact]
        public void Should_not_create_external_signature_when_there_is_no_rsa_private_key_in_certificate()
        {
            Assert.Throws<CryptographicException>(() => pdfSigner.CreateExternalSignature(ecdsaCert));
        }


        public void Dispose()
        {
            if (File.Exists(fileOutput))
            {
                File.Delete(fileOutput);
            }
        }
    }
}