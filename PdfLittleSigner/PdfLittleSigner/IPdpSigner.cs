using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace PdfLittleSigner;

public interface IPdpSigner
{
    string CertificatesName { get; }
    StoreName StoredName { get; set; }
    StoreLocation StoredLocation { get; set; }
    Stream OutputPdfStream { get; }
    DateTime SignDate { get; set; }
    bool Sign(string iSignReason, string iSignContact, string iSignLocation, bool visible, string iImageString);
}