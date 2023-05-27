using Microsoft.AspNetCore.Http;
using System.IO;

namespace PdfLittleSigner
{
    public record class NamedImage : INamedImage
    {
        public string Name { get; set; }

        public byte[] Data { get; set; }

        public static NamedImage FromFormFile(IFormFile formFile)
        {
            using var stream = new MemoryStream();
            formFile.CopyTo(stream);

            return new NamedImage
            {
                Name = formFile.FileName,
                Data = stream.ToArray()
            };

        }

    }
}
