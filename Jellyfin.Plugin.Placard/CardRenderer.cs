using System.Reflection;
using SkiaSharp;

namespace Jellyfin.Plugin.Placard;

/// <summary>Composites a library name onto a backdrop, matching Jellyfin's default cover style.</summary>
public static class CardRenderer
{
    private static SKTypeface? _typeface;

    private static SKTypeface Typeface
    {
        get
        {
            if (_typeface != null)
            {
                return _typeface;
            }

            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream("Jellyfin.Plugin.Placard.Fonts.NotoSans-Bold.ttf");
            _typeface = s != null
                ? SKTypeface.FromStream(s)
                : SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);
            return _typeface;
        }
    }

    /// <summary>Render <paramref name="label"/> centered on the image at <paramref name="backdropPath"/>.</summary>
    public static byte[] Render(string backdropPath, string label, int scrim, int fontHeightPct)
    {
        using var input = SKBitmap.Decode(backdropPath);
        return RenderBitmap(input, label, scrim, fontHeightPct);
    }

    /// <summary>Generate SMPTE color bars and label them (for Live TV, which has no media backdrop).</summary>
    public static byte[] RenderColorBars(string label, int scrim)
    {
        const int w = 1920;
        const int h = 1080;
        using var bmp = new SKBitmap(w, h);
        using (var canvas = new SKCanvas(bmp))
        {
            var cols = new[]
            {
                new SKColor(192, 192, 192), new SKColor(192, 192, 0), new SKColor(0, 192, 192),
                new SKColor(0, 192, 0), new SKColor(192, 0, 192), new SKColor(192, 0, 0), new SKColor(0, 0, 192)
            };
            float bw = (float)w / cols.Length;
            for (int i = 0; i < cols.Length; i++)
            {
                using var p = new SKPaint { Color = cols[i] };
                canvas.DrawRect(i * bw, 0, bw + 1, h, p);
            }
        }

        return RenderBitmap(bmp, label, scrim, 20);
    }

    private static byte[] RenderBitmap(SKBitmap input, string label, int scrim, int fontHeightPct)
    {
        int w = input.Width;
        int h = input.Height;
        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(input, 0, 0);

        using (var scrimPaint = new SKPaint { Color = new SKColor(0, 0, 0, (byte)scrim) })
        {
            canvas.DrawRect(0, 0, w, h, scrimPaint);
        }

        float size = h * fontHeightPct / 100f;
        using var measure = new SKPaint { Typeface = Typeface, IsAntialias = true };
        for (; size > 12; size -= 4)
        {
            measure.TextSize = size;
            if (measure.MeasureText(label) <= w * 0.90f)
            {
                break;
            }
        }

        measure.TextSize = size;
        SKRect bounds = default;
        measure.MeasureText(label, ref bounds);
        float x = ((w - bounds.Width) / 2f) - bounds.Left;
        float y = ((h - bounds.Height) / 2f) - bounds.Top;

        using (var shadow = new SKPaint
        {
            Typeface = Typeface,
            TextSize = size,
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 205),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size / 20f)
        })
        {
            canvas.DrawText(label, x, y + (size / 28f), shadow);
        }

        using (var text = new SKPaint
        {
            Typeface = Typeface,
            TextSize = size,
            IsAntialias = true,
            Color = SKColors.White
        })
        {
            canvas.DrawText(label, x, y, text);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 92);
        return data.ToArray();
    }
}
