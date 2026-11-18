using System;
using System.IO;
using Jellyfin.Plugin.Placard;
using SkiaSharp;
using Xunit;

public class CardRendererTests
{
    [Theory]
    [InlineData("Movies", 0, 20)]
    [InlineData("A very long library label that must fit on the image", 105, 40)]
    [InlineData("Movies", -100, -100)]
    [InlineData("Movies", 999, 999)]
    public void RendersJpegWithOriginalDimensionsAndChangesCenter(string label, int opacity, int size)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
        try
        {
            using var bitmap = new SKBitmap(640, 360);
            bitmap.Erase(new SKColor(80, 130, 160));
            using (var image = SKImage.FromBitmap(bitmap))
            using (var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 95))
                File.WriteAllBytes(path, jpeg.ToArray());
            byte[] result = CardRenderer.Render(path, label, opacity, size);
            Assert.Equal(0xff, result[0]);
            Assert.Equal(0xd8, result[1]);
            using var decoded = SKBitmap.Decode(result);
            Assert.NotNull(decoded);
            Assert.Equal(640, decoded.Width);
            Assert.Equal(360, decoded.Height);
            int changed = 0;
            for (int y = 130; y < 230; y++)
                for (int x = 30; x < 610; x++)
                    if (decoded.GetPixel(x, y).Red > 200) changed++;
            Assert.True(changed > 10, "Expected visible white label pixels");
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(-1, 0, 1, 5)]
    [InlineData(256, 255, 100, 40)]
    public void ClampsSettings(int opacity, int expectedOpacity, int size, int expectedSize)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
        try
        {
            using var bitmap = new SKBitmap(640, 360);
            bitmap.Erase(SKColors.CornflowerBlue);
            using (var image = SKImage.FromBitmap(bitmap))
            using (var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 95)) File.WriteAllBytes(path, jpeg.ToArray());
            Assert.Equal(CardRenderer.Render(path, "Movies", expectedOpacity, expectedSize), CardRenderer.Render(path, "Movies", opacity, size));
        }
        finally { File.Delete(path); }
    }
}
