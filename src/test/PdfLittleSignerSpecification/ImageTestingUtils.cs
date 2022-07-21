using SkiaSharp;

namespace PdfLittleSignerSpecification
{
    public class ImageTestingUtils
    {
        public static byte[] GenerateSingleColorRectangle(int width, int height, string hexColor, int jpgQuality)
        {
            using SKBitmap bitmap = new SKBitmap(width, height);
            var color = SKColor.Parse(hexColor);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bitmap.SetPixel(x, y, color);
                }
            }
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, jpgQuality);

            return data.ToArray();
        }
    }
}
