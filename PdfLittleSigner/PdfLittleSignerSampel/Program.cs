using PdfLttleSigner;

namespace PdfLittleSignerSampel
{
    class Program
    {
        static void Main(string[] args)
        {
            MetaData metadata = new MetaData();
            metadata.Author = "me";
            PdpSigner signer = new PdpSigner("CertyficateName", "file.pdf", "file_signed.pdf", metadata);
            bool signed = signer.Sign("reasonToSign", "Contact", "PhysicalLocation", true, "image.jpg");
        }
    }
}