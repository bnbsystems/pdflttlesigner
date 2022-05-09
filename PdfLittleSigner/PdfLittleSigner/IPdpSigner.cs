using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PdfLittleSigner;

public interface IPdpSigner
{
    Task<bool> Sign(string iSignReason,
        string iSignContact,
        string iSignLocation,
        bool visible,
        IFormFile stampFile,
        X509Certificate2 certificate,
        byte[] fileToSign);
}