using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace PdfLittleSigner
{

    public class PdfSigner : IPdfSigner
    {
        #region Properties

        private readonly string _outputPdfFileString = "";
        private Stream _outputPdfStream;
        private readonly Size _imageSize = new(248, 99);
        private readonly ILogger<PdfSigner> _logger;
        private string[] _unsupportedImageFormats = { ".png" };
        private const int _stampMargin = 5;

        #endregion

        #region Constructors

        public PdfSigner(string output, ILogger<PdfSigner> logger)
        {
            _outputPdfFileString = output;
            _logger = logger ?? new NullLogger<PdfSigner>();
        }

        public PdfSigner(Stream output, ILogger<PdfSigner> logger)
        {
            _outputPdfStream = output;
            _logger = logger ?? new NullLogger<PdfSigner>();
        }

        #endregion

        public async Task<bool> Sign(
            string iSignReason,
            string iSignContact,
            string iSignLocation,
            bool visible,
            IFormFile stampFile,
            X509Certificate2 certificate,
            byte[] fileToSign)
        {

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

            _logger.Log(LogLevel.Information, "Reading pdf file...");
            await using Stream inputPdfSigner = new MemoryStream(fileToSign);
            using PdfReader singerPdfReader = new(inputPdfSigner);

            try
            {
                var pdfSigner = GetPdfSigner(singerPdfReader);

                _logger.Log(LogLevel.Information, "Setting signature appearance...");
                await using Stream inputPdfDocument = new MemoryStream(fileToSign);
                using PdfReader documentPdfReader = new PdfReader(inputPdfDocument);
                using PdfDocument pdfDocument = new PdfDocument(documentPdfReader);
                var pageSize = pdfDocument.GetFirstPage().GetPageSize();
                await ConfigureSignatureAppearance(pageSize, iSignReason, iSignContact, iSignLocation, visible, stampFile,
                    certificate, pdfSigner);

                _logger.Log(LogLevel.Information, "Loading private key and signature data...");
                var signature = CreateExternalSignature(certificate);

                _logger.Log(LogLevel.Information, "Signing pdf...");
                pdfSigner.SignDetached(signature, chain, null, null, null, 0, iText.Signatures.PdfSigner.CryptoStandard.CMS);
            }
            catch (IOException)
            {
                return false;
            }
            finally
            {
                await _outputPdfStream.DisposeAsync();
            }

            _logger.Log(LogLevel.Information, "Signing pdf successful.");
            return true;
        }

        private IExternalSignature CreateExternalSignature(X509Certificate2 certificate)
        {
            var hashAlgorithm = DigestAlgorithms.SHA256;
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

            IExternalSignature signature = new PrivateKeySignature(privateKey, hashAlgorithm);
            return signature;
        }

        private async Task ConfigureSignatureAppearance(Rectangle pageSize, string iSignReason, string iSignContact, string iSignLocation,
            bool visible, IFormFile stampFile, X509Certificate2 certificate, iText.Signatures.PdfSigner pdfSigner)
        {
            var signatureAppearance = pdfSigner.GetSignatureAppearance();
            signatureAppearance
                .SetReason(iSignReason)
                .SetContact(iSignContact)
                .SetLocation(iSignLocation);

            if (visible)
            {
                var signatureLocationX = pageSize.GetWidth() - _imageSize.Width - _stampMargin;
                var signatureLocationY = pageSize.GetHeight() - _imageSize.Height - _stampMargin;
                signatureAppearance.SetPageRect(new Rectangle(signatureLocationX, signatureLocationY, _imageSize.Width, _imageSize.Height));

                if (stampFile != null)
                {
                    var stampImageExtension = Path.GetExtension(stampFile.FileName);
                    if (Array.Exists(_unsupportedImageFormats, element => element.Equals(stampImageExtension)))
                    {
                        throw new Exception($"{stampImageExtension} image files are not supported");
                    }

                    signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.GRAPHIC);
                    var stampBytes = await FormFileToByteArrayAsync(stampFile);
                    ImageData imageData = ImageDataFactory.Create(stampBytes);
                    signatureAppearance.SetSignatureGraphic(imageData);
                    signatureAppearance.SetLayer2Text(" ");
                }
                else
                {
                    signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
                    string field = certificate.GetNameInfo(X509NameType.SimpleName, false);
                    var signatureDate = DateTime.Now;
                    var layer2Text = "Operat podpisany cyfrowo \n" +
                                     $"przez {field} \n" +
                                     $"{signatureDate}";
                    signatureAppearance.SetLayer2Text(layer2Text);
                }
            }
        }

        private iText.Signatures.PdfSigner GetPdfSigner(PdfReader pdfReader)
        {
            StampingProperties stampingProperties = new StampingProperties();
            iText.Signatures.PdfSigner pdfSigner = new(pdfReader, _outputPdfStream, stampingProperties);
            pdfSigner.SetSignDate(DateTime.Now);
            pdfSigner.SetCertificationLevel(iText.Signatures.PdfSigner.CERTIFIED_NO_CHANGES_ALLOWED);

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

        private async Task<byte[]> FormFileToByteArrayAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            return stream.ToArray();
        }
    }
}