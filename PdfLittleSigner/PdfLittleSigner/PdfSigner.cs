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
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace PdfLittleSigner
{

    public class PdpSigner : IPdpSigner
    {
        public string CertificatesName { get; }
        public StoreName StoredName { get; set; }
        public StoreLocation StoredLocation { get; set; }
        public Stream OutputPdfStream { get; }
        public DateTime SignDate { get; set; }

        private string _outputPdfFileString = "";
        private Stream _outputPdfStream;
        private MetaData _metaData;
        public Size ImageSize = new Size(248, 99);
        public Size ImageLocation = new Size(60, 100);

        #region Constructors

        public PdpSigner(string iCertificatesName, string input, string output, MetaData metaData)
        {
            CertificatesName = iCertificatesName.ToUpper();
            _metaData = metaData;
            _outputPdfFileString = output;
        }

        public PdpSigner(string iCertificatesName, Stream input, Stream output, MetaData metaData)
        {
            CertificatesName = iCertificatesName.ToUpper();
            _metaData = metaData;
            _outputPdfStream = output;
        }

        #endregion

        public async Task<bool> Sign(string iSignReason,
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


            Stream inputPdfFile = new MemoryStream(fileToSign);
            PdfReader pdfReader = new PdfReader(inputPdfFile);


            if (_outputPdfStream == null && !string.IsNullOrEmpty(_outputPdfFileString))
            {
                _outputPdfStream = new FileStream(_outputPdfFileString, FileMode.OpenOrCreate, FileAccess.Write);
            }

            if (_outputPdfStream == null)
            {
                return false;
            }



            var pdfSigner = GetPdfSigner(pdfReader);
            await ConfigureSignatureAppearance(iSignReason, iSignContact, iSignLocation, visible, stampFile, certificate, pdfSigner);
            var signature = CreateExternalSignature(certificate);

            pdfSigner.SignDetached(signature, chain, null, null, null, 0, iText.Signatures.PdfSigner.CryptoStandard.CMS);
            pdfReader.Close();

            return true;
        }

        private IExternalSignature CreateExternalSignature(X509Certificate2 certificate)
        {
            var hashAlgorithm = DigestAlgorithms.SHA1;
            var rsa = certificate.GetRSAPrivateKey();
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
            bool visible, IFormFile stampFile, X509Certificate2 certificate, iText.Signatures.PdfSigner pdfSigner)
        {
            var signatureAppearance = pdfSigner.GetSignatureAppearance();
            signatureAppearance
                .SetReason(iSignReason)
                .SetContact(iSignContact)
                .SetLocation(iSignLocation);


            if (visible)
            {
                signatureAppearance.SetPageRect(new iText.Kernel.Geom.Rectangle(ImageLocation.Width,
                    ImageLocation.Height, ImageSize.Width, ImageSize.Height));

                if (stampFile != null)
                {
                    signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.GRAPHIC);
                    var stampBytes = await FormFileToByteArrayAsync(stampFile);
                    ImageData imageData = ImageDataFactory.Create(stampBytes);
                    signatureAppearance.SetImage(imageData);
                    signatureAppearance.SetLayer2Text(" ");
                }
                else
                {
                    signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
                    string field = certificate.GetNameInfo(X509NameType.SimpleName, false);
                    var signatureDate = DateTime.Now;
                    var layer2Text = $"Operat podpisany cyfrowo \n" +
                                     $"przez {field} \n" +
                                     $"{signatureDate}";
                    signatureAppearance.SetLayer2Text(layer2Text);
                }
            }
        }

        private iText.Signatures.PdfSigner GetPdfSigner(PdfReader pdfReader)
        {
            StampingProperties stampingProperties = new StampingProperties();

            iText.Signatures.PdfSigner pdfSigner = new iText.Signatures.PdfSigner(pdfReader, _outputPdfStream, stampingProperties);
            pdfSigner.SetSignDate(DateTime.Now);
            pdfSigner.SetCertificationLevel(iText.Signatures.PdfSigner.CERTIFIED_NO_CHANGES_ALLOWED);
            
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