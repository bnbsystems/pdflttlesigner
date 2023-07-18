using FluentAssertions;
using PdfLittleSigner;
using SkiaSharp;
using System.Drawing;
using System.Linq;
using Xunit;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace PdfLittleSignerSpecification
{
    public class SignatureImageSpecification
    {
        SignatureImage signatureImage;
        public SignatureImageSpecification()
        {
            signatureImage = new SignatureImage();
        }


        [Theory]
        [InlineData(600, 800, 150, 150, 5, 445, 5)]
        [InlineData(600, 800, 150, 150, 0, 450, 0)]
        [InlineData(600, 800, 120, 160, 200, 280, 200)]
        [InlineData(100, 100, 150, 150, 5, 0, 0)]
        [InlineData(100, 100, 150, 150, 0, 0, 0)]
        [InlineData(100, 100, 110, 110, 90, 0, 0)]
        public void Should_calculate_signature_location(float pageWidth, float pageHeight, int imageWidth, int imageHeight, int signatureMargin, float expectedX, float expectedY)
        {
            float precision = 0.001F;

            signatureImage.ImageSize = new Size(imageWidth, imageHeight);
            signatureImage.SignatureMarginBottom = signatureMargin;
            signatureImage.SignatureMarginRight = signatureMargin;

            Rectangle pageSize = new Rectangle(pageWidth, pageHeight);


            signatureImage.CalculateSignatureLocation(pageSize, out float signatureLocationX, out float signatureLocationY);

            signatureLocationX.Should().BeApproximately(expectedX, precision);
            signatureLocationY.Should().BeApproximately(expectedY, precision);
        }

        [Theory]
        [InlineData(100, 100, 150, 150)]
        [InlineData(121, 145, 150, 150)]
        [InlineData(300, 300, 100, 100)]
        [InlineData(316, 212, 100, 100)]
        [InlineData(316, 212, 400, 400)]
        [InlineData(100, 100, 50, 50)]
        public void Should_resize_image(int width, int height, int widthAfter, int heightAfter)
        {

            signatureImage.ImageSize = new Size(widthAfter, heightAfter);
            byte[] imageBytes = ImageTestingUtils.GenerateSingleColorRectangle(width, height, "#FF0000", 75);

            using var stampBitmap = SKBitmap.Decode(imageBytes);
            using SKBitmap? resizedBitmap = signatureImage.ResizeImage(stampBitmap);

            resizedBitmap.Should().NotBeNull();
            resizedBitmap.Width.Should().Be(widthAfter);
            resizedBitmap.Height.Should().Be(heightAfter);
            resizedBitmap.Pixels.Select(x => x.ToString().Should().BeEquivalentTo("#FF0000"));
        }

        [Fact]
        public void Should_RemoveTransparentBackgorund()
        {
            using var stampBitmap = SKBitmap.Decode("./Data/vx.png");
            var beforeRemove = stampBitmap.Pixels.Count(x => x == SKColors.White).Should().Be(0);
            var removedBackgraound = signatureImage.RemoveTransparentBackgorund(stampBitmap);
            var afterRemove = removedBackgraound.Pixels.Count(x => x == SKColors.White).Should().BeGreaterThan(0);

            removedBackgraound.Width.Should().Be(stampBitmap.Width);
            removedBackgraound.Height.Should().Be(stampBitmap.Height);
        }

        [Fact]
        public void Should_map_to_byteArray()
        {
            using var stampBitmap = SKBitmap.Decode("./Data/valid.jpg");
            signatureImage.SKBitmapToByteArray(stampBitmap).Should().NotBeNullOrEmpty();
        }
    }
}
