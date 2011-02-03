using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using iTextSharp.text.pdf;

namespace PdfLttleSigner
{
    public class PdpSigner
    {
        private BaseFont bFontTimesRoman = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

        #region Fileds And Properties

        public string CertificatesName { get; private set; }

        private StoreName _storedName = StoreName.My;
        private StoreLocation _storedLocation = StoreLocation.CurrentUser;

        public StoreName StoredName
        {
            get { return _storedName; }
            set { _storedName = value; }
        }

        public StoreLocation StoredLocation
        {
            get { return _storedLocation; }
            set { _storedLocation = value; }
        }

        public Size ImageSize = new Size(248, 99);
        public Size ImageLocation = new Size(60, 100);

        private string inputPdfFileString = "";
        private string outputPdfFileString = "";
        private Stream inputPdfStream;
        private Stream outputPdfStream;

        public Stream OutputPdfStream
        {
            get { return outputPdfStream; }
        }

        private MetaData settingMetadata;

        internal MetaData SettingMetadata
        {
            get { return settingMetadata; }
            set { settingMetadata = value; }
        }

        public DateTime SignDate
        { get; set; }

        #endregion Fileds And Properties

        #region Constructors

        public PdpSigner(string iCertificatesName, string input, string output, MetaData md)
        {
            CertificatesName = iCertificatesName.ToUpper();
            this.settingMetadata = md;

            this.inputPdfFileString = input;
            this.outputPdfFileString = output;
        }

        public PdpSigner(string iCertificatesName, Stream input, Stream output, MetaData md)
        {
            CertificatesName = iCertificatesName.ToUpper();
            this.settingMetadata = md;

            this.inputPdfStream = input;
            this.outputPdfStream = output;
        }

        #endregion Constructors

        private static byte[] SignMsg(Byte[] iMsg, X509Certificate2 iSignerCert, bool detached)
        {
            ContentInfo vContentInfo = new ContentInfo(iMsg);
            SignedCms vSignedCms = new SignedCms(vContentInfo, detached);
            CmsSigner vCmsSigner = new CmsSigner(iSignerCert);
            vSignedCms.ComputeSignature(vCmsSigner, false);
            return vSignedCms.Encode();
        }

        public bool Sign(string iSignReason, string iSignContact, string iSignLocation, bool visible, string iImageString)
        {
            string vCertificatesPath = "CN=" + CertificatesName;

            #region Geting Certs

            X509Store store = new X509Store(_storedName, _storedLocation);
            StorePermission sp = new StorePermission(PermissionState.Unrestricted);
            sp.Flags = StorePermissionFlags.OpenStore;
            sp.Assert();
            store.Open(OpenFlags.MaxAllowed);
            X509Certificate2 cert = null;
            int i = 0;
            while ((i < store.Certificates.Count) && (cert == null))
            {
                if (store.Certificates[i].Subject.ToUpper().Contains(vCertificatesPath))
                    cert = store.Certificates[i];
                else
                    i++;
            }
            store.Close();
            if (cert == null)
            {
                throw new CryptographicException("Certificate is NULL. Certificate can not be found");
            }
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            var cerRawData = cert.RawData;
            var certificates = cp.ReadCertificate(cerRawData);
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { certificates };

            var chainFirst = GetChainBouncyCastle(cert);

            #endregion Geting Certs

            PdfReader reader = null;
            if (string.IsNullOrEmpty(inputPdfFileString))
            {
                reader = new PdfReader(inputPdfStream);
            }
            else
            {
                reader = new PdfReader(this.inputPdfFileString);
            }
            if (outputPdfStream == null && string.IsNullOrEmpty(outputPdfFileString) == false)
            {
                outputPdfStream = new FileStream(this.outputPdfFileString, FileMode.OpenOrCreate, FileAccess.Write);
            }
            if (reader != null && outputPdfStream != null)
            {
                #region Standard Signing

                PdfStamper vStamper = PdfStamper.CreateSignature(reader, outputPdfStream, '\0', null, false);
                vStamper.MoreInfo = this.settingMetadata.GetMetaDataHashtable();
                vStamper.XmpMetadata = this.settingMetadata.GetStreamedMetaData();

                PdfSignatureAppearance vSignatureAppearance = vStamper.SignatureAppearance;
                vSignatureAppearance.SetCrypto(null, chain, null, PdfSignatureAppearance.SELF_SIGNED);
                vSignatureAppearance.SignDate = SignDate;
                vSignatureAppearance.Reason = iSignReason;
                vSignatureAppearance.Contact = iSignContact;
                vSignatureAppearance.Location = iSignLocation;
                vSignatureAppearance.Acro6Layers = true;
                vSignatureAppearance.Render = PdfSignatureAppearance.SignatureRender.Description;
                if (visible)
                {
                    vSignatureAppearance.SetVisibleSignature(
                        new iTextSharp.text.Rectangle(ImageLocation.Width, ImageLocation.Height, ImageLocation.Width + ImageSize.Width, ImageLocation.Height + ImageSize.Height),
                                                                1, null);
                    if (File.Exists(iImageString))
                    {
                        iTextSharp.text.Image vImage = iTextSharp.text.Image.GetInstance(iImageString);
                        vSignatureAppearance.Image = vImage;
                    }
                }
                vSignatureAppearance.SetExternalDigest(new byte[128], new byte[20], "RSA");

                #endregion Standard Signing

                #region Self Signed Mode

                PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                dic.Date = new PdfDate(vSignatureAppearance.SignDate);
                var vName = PdfPKCS7.GetSubjectFields(chain[0]).GetField("CN");
                dic.Name = vName;
                if (vSignatureAppearance.Reason != null)
                    dic.Reason = vSignatureAppearance.Reason;
                if (vSignatureAppearance.Location != null)
                    dic.Location = vSignatureAppearance.Location;
                vSignatureAppearance.CryptoDictionary = dic;

                int csize = 4000;
                Dictionary<PdfName, int> exc = new Dictionary<PdfName, int>();
                exc[PdfName.CONTENTS] = csize * 2 + 2;
                vSignatureAppearance.PreClose(new Hashtable(exc));

                HashAlgorithm sha = new SHA1CryptoServiceProvider();

                Stream s = vSignatureAppearance.RangeStream;
                int read = 0;
                byte[] buff = new byte[8192];
                while ((read = s.Read(buff, 0, 8192)) > 0)
                    sha.TransformBlock(buff, 0, read, buff, 0);
                sha.TransformFinalBlock(buff, 0, 0);
                byte[] pk = SignMsg(sha.Hash, cert, false);
                byte[] outc = new byte[csize];
                PdfDictionary dic2 = new PdfDictionary();
                Array.Copy(pk, 0, outc, 0, pk.Length);
                dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
                vSignatureAppearance.Close(dic2);

                #endregion Self Signed Mode

                if (vSignatureAppearance.IsPreClosed() == false)
                {
                    vStamper.Close();
                }
                reader.Close();
                return true;
            }
            return false;
        }

        private static Org.BouncyCastle.X509.X509Certificate[] GetChainBouncyCastle(X509Certificate2 cer)
        {
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            var cerRawData = cer.RawData;
            var certificates = cp.ReadCertificate(cerRawData);
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { certificates };
            return chain;
        }
    }
}