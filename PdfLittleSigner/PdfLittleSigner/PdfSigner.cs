using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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

namespace PdfLittleSigner
{

    public class PdpSigner : IPdpSigner
    {
        #region Properties

        private readonly string _outputPdfFileString = "";
        private Stream _outputPdfStream;
        private readonly Size _imageSize = new(248, 99);
        private readonly Size _imageLocation = new(60, 100);
        private readonly ILogger<PdpSigner> _logger;

        #endregion

        #region Constructors

        public PdpSigner(string output, ILogger<PdpSigner> logger)
        {
            _outputPdfFileString = output;
            _logger = logger ?? new NullLogger<PdpSigner>();
        }

        public PdpSigner(Stream output, ILogger<PdpSigner> logger)
        {
            _outputPdfStream = output;
            _logger = logger ?? new NullLogger<PdpSigner>();
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

            _logger.Log(LogLevel.Information, "Reading pdf file...");

            await using Stream inputPdfFile = new MemoryStream(fileToSign);
            PdfReader pdfReader = new(inputPdfFile);

            _logger.Log(LogLevel.Information, "Creating output stream...");

            if (_outputPdfStream == null && !string.IsNullOrEmpty(_outputPdfFileString))
            {
                _outputPdfStream = new FileStream(_outputPdfFileString, FileMode.OpenOrCreate, FileAccess.Write);
            }
            
            if (_outputPdfStream == null)
            {
                return false;
            }

            try
            {
                var pdfSigner = GetPdfSigner(pdfReader);

                _logger.Log(LogLevel.Information, "Setting signature appearance...");
                await ConfigureSignatureAppearance(iSignReason, iSignContact, iSignLocation, visible, stampFile,
                    certificate, pdfSigner);

                _logger.Log(LogLevel.Information, "Loading private key and signature data...");
                var signature = CreateExternalSignature(certificate);

                _logger.Log(LogLevel.Information, "Signing pdf...");
                pdfSigner.SignDetached(signature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
            }
            catch (IOException ioe)
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
            var hashAlgorithm = DigestAlgorithms.SHA1;
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

        private async Task ConfigureSignatureAppearance(string iSignReason, string iSignContact, string iSignLocation,
            bool visible, IFormFile stampFile, X509Certificate2 certificate, PdfSigner pdfSigner)
        {
            var signatureAppearance = pdfSigner.GetSignatureAppearance();
            signatureAppearance
                .SetReason(iSignReason)
                .SetContact(iSignContact)
                .SetLocation(iSignLocation);


            if (visible)
            {
                signatureAppearance.SetPageRect(new iText.Kernel.Geom.Rectangle(_imageLocation.Width,
                    _imageLocation.Height, _imageSize.Width, _imageSize.Height));

                if (stampFile != null)
                {
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

        private PdfSigner GetPdfSigner(PdfReader pdfReader)
        {
            StampingProperties stampingProperties = new StampingProperties();

            PdfSigner pdfSigner = new(pdfReader, _outputPdfStream, stampingProperties);
            pdfSigner.SetSignDate(DateTime.Now);
            pdfSigner.SetCertificationLevel(PdfSigner.CERTIFIED_NO_CHANGES_ALLOWED);
            
            return pdfSigner;
        }

        private Org.BouncyCastle.X509.X509Certificate[] GetChain(X509Certificate2 cert)
        {
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            var certRawData = cert.RawData;
            var certificates = cp.ReadCertificate(certRawData);
            Org.BouncyCastle.X509.X509Certificate[] chain = {certificates};
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