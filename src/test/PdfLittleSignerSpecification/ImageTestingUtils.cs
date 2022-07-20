using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PdfLittleSignerSpecification
{
    public class ImageTestingUtils
    {
        public static byte[] GenerateSingleColorRectangle(int width, int height, Color color)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics gfx = Graphics.FromImage(bmp))
            using (SolidBrush brush = new SolidBrush(color))
            {
                gfx.FillRectangle(brush, 0, 0, width, height);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }
}
