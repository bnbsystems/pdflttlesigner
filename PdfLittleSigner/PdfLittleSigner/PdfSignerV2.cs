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
using PdfLittleSigner.Extensions;

namespace PdfLittleSigner
{

    public class PdfSignerV2 : IPdpSigner
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

        public PdfSignerV2(string iCertificatesName, string input, string output, MetaData metaData)
        {
            CertificatesName = iCertificatesName.ToUpper();
            _metaData = metaData;
            _outputPdfFileString = output;
        }

        public PdfSignerV2(string iCertificatesName, Stream input, Stream output, MetaData metaData)
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
            #region Geting Certs

            var chain = certificate != null ? 
                GetChainBouncyCastle(certificate) 
                : throw new CryptographicException("Certificate is NULL. Certificate can not be found");

            #endregion Geting Certs
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

            #region Standard Signing

            StampingProperties stampingProperties = new StampingProperties();
            PdfSigner pdfSigner = new PdfSigner(pdfReader, _outputPdfStream, stampingProperties);

            pdfSigner.SetSignDate(DateTime.Now);
            pdfSigner.SetCertificationLevel(PdfSigner.CERTIFIED_NO_CHANGES_ALLOWED);
            
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
                    var stampBytes = await stampFile.ToByteArrayAsync();
                    ImageData imageData = ImageDataFactory.Create(stampBytes);
                    signatureAppearance.SetImage(imageData);
                    signatureAppearance.SetLayer2Text(" ");
                }
                else
                {
                   signatureAppearance .SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
                    string field = certificate.GetNameInfo(X509NameType.SimpleName, false);
                    var signatureDate = DateTime.Now;
                    var layer2Text = $"Operat podpisany cyfrowo \n" +
                                     $"przez {field} \n" +
                                     $"{signatureDate}";
                    signatureAppearance.SetLayer2Text(layer2Text);
                }
            }

            #endregion

            PdfSignature dic = new PdfSignature(PdfName.Adobe_PPKMS, PdfName.Adbe_pkcs7_sha1);
            dic.SetDate(new PdfDate(pdfSigner.GetSignDate()));
            if (signatureAppearance.GetReason() != null)
                dic.SetReason(signatureAppearance.GetReason());
            if (signatureAppearance.GetLocation() != null)
                dic.SetLocation(signatureAppearance.GetLocation());
            if (signatureAppearance.GetContact() != null)
                dic.SetContact(signatureAppearance.GetContact());
       
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
            pdfSigner.SignDetached(signature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
            pdfReader.Close();
            return true;
        }

        private static Org.BouncyCastle.X509.X509Certificate[] GetChainBouncyCastle(X509Certificate2 cert)
        {
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            var certRawData = cert.RawData;
            var certificates = cp.ReadCertificate(certRawData);
            Org.BouncyCastle.X509.X509Certificate[] chain = {certificates};
            return chain;
        }
    }
}