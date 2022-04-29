using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Signatures;
using PdfLttleSigner;

namespace PdfLittleSigner
{

    public class PdfSignerV2 : IPdpSigner
    {
        public string CertificatesName { get; }
        public StoreName StoredName { get; set; }
        public StoreLocation StoredLocation { get; set; }
        public Stream OutputPdfStream { get; }
        public DateTime SignDate { get; set; }

        private string _inputPdfFileString = "";
        private string _outputPdfFileString = "";
        private Stream _inputPdfStream;
        private Stream _outputPdfStream;
        private MetaData _metaData;
        public Size ImageSize = new Size(248, 99);
        public Size ImageLocation = new Size(60, 100);

        #region Constructors

        public PdfSignerV2(string iCertificatesName, string input, string output, MetaData metaData)
        {
            CertificatesName = iCertificatesName.ToUpper();
            _metaData = metaData;
            _inputPdfFileString = input;
            _outputPdfFileString = output;
        }

        public PdfSignerV2(string iCertificatesName, Stream input, Stream output, MetaData metaData)
        {
            CertificatesName = iCertificatesName.ToUpper();
            _metaData = metaData;
            _inputPdfStream = input;
            _outputPdfStream = output;
        }

        #endregion

        public bool Sign(string iSignReason, string iSignContact, string iSignLocation, bool visible,
            string iImageString)
        {
            string vCertificatesPath = "CN=" + CertificatesName;

            #region Geting Certs

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            StorePermission sp = new StorePermission(PermissionState.Unrestricted);
            sp.Flags = StorePermissionFlags.OpenStore;
            sp.Assert();


            var cert = GetX509Certificate2(store, vCertificatesPath);
            if (cert == null)
            {
                throw new CryptographicException("Certificate is NULL. Certificate can not be found");
            }

            var chain = GetChainBouncyCastle(cert);

            #endregion Geting Certs

            PdfReader pdfReader = string.IsNullOrEmpty(_inputPdfFileString)
                ? new PdfReader(_inputPdfStream)
                : new PdfReader(_inputPdfFileString);


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

            pdfSigner.SetSignDate(SignDate);

            var signatureAppearance = pdfSigner.GetSignatureAppearance();
            signatureAppearance
                .SetReason(iSignReason)
                .SetContact(iSignContact)
                .SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
            if (visible)
            {
                signatureAppearance.SetPageRect(new iText.Kernel.Geom.Rectangle(ImageLocation.Width,
                    ImageLocation.Height, ImageSize.Width, ImageSize.Height));

                if (File.Exists(iImageString))
                {
                    ImageData imageData = ImageDataFactory.Create(iImageString);
                    signatureAppearance.SetImage(imageData);
                }
            }

            #endregion

            PdfSignature dic = new PdfSignature(PdfName.Adobe_PPKMS, PdfName.Adbe_pkcs7_sha1);
            dic.SetDate(new PdfDate(pdfSigner.GetSignDate()));
            if (signatureAppearance.GetReason() != null)
                dic.SetReason(signatureAppearance.GetReason());
            if (signatureAppearance.GetLocation() != null)
                dic.SetLocation(signatureAppearance.GetLocation());

            string hashAlgorithm = DigestAlgorithms.SHA256;

            pdfSigner.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
            return true;
        }

        private static X509Certificate2 GetX509Certificate2(X509Store store, string vCertificatesPath)
        {
            store.Open(OpenFlags.MaxAllowed);

            X509Certificate2 cert =
                store.Certificates.FirstOrDefault(x => x.Subject.ToUpper().Contains(vCertificatesPath));

            store.Close();
            return cert;
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