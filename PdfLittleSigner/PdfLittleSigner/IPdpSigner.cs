using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PdfLittleSigner;

public interface IPdpSigner
{
    string CertificatesName { get; }
    StoreName StoredName { get; set; }
    StoreLocation StoredLocation { get; set; }
    Stream OutputPdfStream { get; }
    DateTime SignDate { get; set; }
    Task<bool> Sign(string iSignReason,
        string iSignContact,
        string iSignLocation,
        bool visible,
        string iImageString,
        IFormFile stampFile,
        X509Certificate2 certificate,
        byte[] fileToSign);
}