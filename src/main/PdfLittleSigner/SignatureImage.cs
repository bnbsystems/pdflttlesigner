using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Signatures;
using SkiaSharp;
using System.Drawing;
using System.IO;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace PdfLittleSigner
{

    public class SignatureImage
    {
        public Size ImageSize { get; set; } = new(150, 150);
        public readonly SKFilterQuality ImageResizeQuality = SKFilterQuality.High;
        public readonly PdfFont Font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.CP1250);
        public readonly float FontSize = 12;
        public readonly int JpegEncodingImageQuality = 75;
        public int SignatureMarginRight { get; set; } = 20;
        public int SignatureMarginBottom { get; set; } = 150;

        public SKBitmap ResizeImage(SKBitmap stampBitmap)
        {
            return stampBitmap.Resize(new SKSizeI(ImageSize.Width, ImageSize.Height), ImageResizeQuality);
        }

        public SKBitmap RemoveTransparentBackgorund(SKBitmap bitmap)
        {
            using SKBitmap adjustedBitmap = new(bitmap.Width, bitmap.Height);
            using (SKCanvas canvas = new(adjustedBitmap))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(bitmap, 0, 0);
            };

            return adjustedBitmap.Copy();
        }
        public byte[] SKBitmapToByteArray(SKBitmap bitmap)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegEncodingImageQuality);

            return data.ToArray();
        }

        public void CalculateSignatureLocation(Rectangle pageSize, out float signatureLocationX, out float signatureLocationY)
        {
            var remainingWidth = pageSize.GetWidth() - ImageSize.Width - SignatureMarginRight;
            var remainingHeight = pageSize.GetHeight() - ImageSize.Height - SignatureMarginBottom;
            signatureLocationX = remainingWidth >= 0 ? remainingWidth : 0;
            signatureLocationY = remainingHeight >= 0 ? SignatureMarginBottom : 0;
        }

        public void SetImage(Rectangle pageSize, PdfSignatureAppearance signatureAppearance, INamedImage stampFile, string layer2Text)
        {
            float signatureLocationX, signatureLocationY;
            CalculateSignatureLocation(pageSize, out signatureLocationX, out signatureLocationY);
            signatureAppearance.SetPageRect(new Rectangle(signatureLocationX, signatureLocationY, ImageSize.Width, ImageSize.Height));

            if (stampFile != null)
            {
                signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.GRAPHIC);

                using var stampBitmap = SKBitmap.Decode(stampFile.Data);
                using SKBitmap resizedStampBitmap = ResizeImage(stampBitmap);

                var stampImageExtension = Path.GetExtension(stampFile.Name);
                byte[] resizedStampBytes;
                if (stampImageExtension.ToLower().Equals(".png"))
                {
                    using var adjustedStampBitmap = RemoveTransparentBackgorund(resizedStampBitmap);
                    resizedStampBytes = SKBitmapToByteArray(adjustedStampBitmap);
                }
                else
                {
                    resizedStampBytes = SKBitmapToByteArray(resizedStampBitmap);
                }

                ImageData imageData = ImageDataFactory.Create(resizedStampBytes);
                signatureAppearance.SetSignatureGraphic(imageData);
                signatureAppearance.SetLayer2Text(" ");
            }
            else
            {
                signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
                signatureAppearance.SetLayer2Font(Font);
                signatureAppearance.SetLayer2FontSize(FontSize);
                signatureAppearance.SetLayer2Text(layer2Text);
            }
        }
    }
}
