using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace PdfLittleSigner
{
    public class PdfSigner : IPdfSigner, IDisposable
    {
        private readonly string _outputPdfFileString;
        private Stream _outputPdfStream;
        private readonly ILogger<PdfSigner> _logger;

        public const string SignDateFormat = "dd.MM.yyyy HH:mm:ss";

        public PdfSigner(string output, ILogger<PdfSigner> logger = null)
        {
            _outputPdfFileString = output;
            _logger = logger ?? new NullLogger<PdfSigner>();
        }

        public PdfSigner(Stream output, ILogger<PdfSigner> logger = null)
        {
            _outputPdfStream = output;
            _logger = logger ?? new NullLogger<PdfSigner>();
        }

        public async Task<bool> Sign(
            string iSignReason,
            string iSignContact,
            string iSignLocation,
            bool visible,
            INamedImage stampFile,
            X509Certificate2 certificate,
            byte[] fileToSign,
            string signatureCreator = "",
            string imageText = "",
            bool addSignDateToImageText = true,
            int pageNumber = 1)
        {

            imageText = imageText ?? "";

            var chain = certificate != null ?
                GetChain(certificate)
                : throw new CryptographicException("Certificate is NULL. Certificate can not be found");

            _logger.Log(LogLevel.Information, "Creating output stream...");
            if (_outputPdfStream == null && !string.IsNullOrEmpty(_outputPdfFileString))
            {
                _outputPdfStream = new FileStream(_outputPdfFileString, FileMode.OpenOrCreate, FileAccess.Write);
            }

            if (_outputPdfStream == null)
            {
                return false;
            }

            await using Stream inputPdfSigner = new MemoryStream(fileToSign);
            using PdfReader singerPdfReader = new(inputPdfSigner);

            try
            {
                await using Stream inputPdfDocument = new MemoryStream(fileToSign);
                using PdfReader documentPdfReader = new PdfReader(inputPdfDocument);
                using PdfDocument pdfDocument = new PdfDocument(documentPdfReader);
                var pageSize = pdfDocument.GetFirstPage().GetPageSize();
                bool useAppendMode = IsDocumentSigned(pdfDocument);
                var pdfSigner = GetPdfSigner(singerPdfReader, useAppendMode);

                if (addSignDateToImageText)
                {
                    imageText += pdfSigner.GetSignDate().ToString(SignDateFormat, CultureInfo.CurrentCulture);
                }

                ConfigureSignatureAppearance(pageSize, iSignReason, iSignContact, iSignLocation, visible, stampFile,
                    certificate, pdfSigner, signatureCreator, imageText, pageNumber);
                var signature = CreateExternalSignature(certificate);

                _logger.Log(LogLevel.Information, "Signing pdf by using detached mode,");
                pdfSigner.SignDetached(signature, chain, null, null, null, 0, iText.Signatures.PdfSigner.CryptoStandard.CMS);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Sign method");
                return false;
            }

            finally
            {
                await _outputPdfStream.DisposeAsync();
            }
            return true;
        }

        public IExternalSignature CreateExternalSignature(X509Certificate2 certificate)
        {
            _logger.Log(LogLevel.Information, "Loading private key and signature data...");

            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new CryptographicException("Certificate does not contain RSA private key");
            }
            var parameters = rsa.ExportParameters(true);

            var modulus = new BigInteger(1, parameters.Modulus);
            var exponent = new BigInteger(1, parameters.Exponent);
            var d = new BigInteger(1, parameters.D);
            var p = new BigInteger(1, parameters.P);
            var q = new BigInteger(1, parameters.Q);
            var dp = new BigInteger(1, parameters.DP);
            var dq = new BigInteger(1, parameters.DQ);
            var inverseQ = new BigInteger(1, parameters.InverseQ);
            var privateKey = new RsaPrivateCrtKeyParameters(
                modulus,
                exponent,
                d,
                p,
                q,
                dp,
                dq,
                inverseQ);

            IExternalSignature signature = new PrivateKeySignature(privateKey, DigestAlgorithms.SHA256);
            return signature;
        }

        public void ConfigureSignatureAppearance(Rectangle pageSize, string iSignReason, string iSignContact, string iSignLocation,
            bool visible, INamedImage stampFile, X509Certificate2 certificate, iText.Signatures.PdfSigner pdfSigner, string signatureCreator,
            string layer2Text, int pageNumber)
        {
            _logger.Log(LogLevel.Information, "Setting signature appearance...");

            var signatureAppearance = pdfSigner.GetSignatureAppearance();
            signatureAppearance
                .SetReason(iSignReason)
                .SetContact(iSignContact)
                .SetLocation(iSignLocation)
                .SetPageNumber(pageNumber)
                ;

            if (!string.IsNullOrEmpty(signatureCreator))
            {
                signatureAppearance.SetSignatureCreator(signatureCreator);
            }

            if (visible)
            {
                SignatureImage signatureImage = new SignatureImage();
                signatureImage.SetImage(pageSize, signatureAppearance, stampFile, layer2Text);
            }
        }

        public iText.Signatures.PdfSigner GetPdfSigner(PdfReader pdfReader, bool useAppendMode)
        {
            StampingProperties stampingProperties = new();
            if (useAppendMode)
            {
                stampingProperties.UseAppendMode();
            }
            iText.Signatures.PdfSigner pdfSigner = new(pdfReader, _outputPdfStream, stampingProperties);
            pdfSigner.SetSignDate(DateTime.UtcNow);
            pdfSigner.SetCertificationLevel(useAppendMode
                ? iText.Signatures.PdfSigner.NOT_CERTIFIED
                : iText.Signatures.PdfSigner.CERTIFIED_FORM_FILLING);

            return pdfSigner;
        }

        private Org.BouncyCastle.X509.X509Certificate[] GetChain(X509Certificate2 cert)
        {
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            var certRawData = cert.RawData;
            var certificates = cp.ReadCertificate(certRawData);
            Org.BouncyCastle.X509.X509Certificate[] chain = { certificates };

            return chain;
        }

        private bool IsDocumentSigned(PdfDocument document)
        {
            SignatureUtil signatureUtil = new SignatureUtil(document);
            var signatureNames = signatureUtil.GetSignatureNames();

            return signatureNames.Count > 0;
        }

        public void Dispose()
        {
            if (_outputPdfStream != null)
            {
                _outputPdfStream.Dispose();
                _outputPdfStream = null;
            }
        }
    }
}