using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PdfLittleSigner.Extensions;

public static class FileExtensions
{
    public static async Task<byte[]> ToByteArrayAsync(this IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return stream.ToArray();
    }

    public static byte[] ToByteArray(this IFormFile file)
    {
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        return stream.ToArray();
    }

}