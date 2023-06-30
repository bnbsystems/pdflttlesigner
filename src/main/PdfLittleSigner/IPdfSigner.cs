using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PdfLittleSigner
{

    public interface IPdfSigner
    {
        Task<bool> Sign(string iSignReason,
            string iSignContact,
            string iSignLocation,
            bool visible,
            INamedImage stampFile,
            X509Certificate2 certificate,
            byte[] fileToSign,
            string signatureCreator = "",
            string imageText = "",
            bool addSignDateToImageText = true,
            int pageNumber = 1);
    }
}