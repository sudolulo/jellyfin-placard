using System;
using System.Reflection;
using SkiaSharp;

namespace Jellyfin.Plugin.Placard;

/// <summary>Composites a library name onto a backdrop, matching Jellyfin's default cover style.</summary>
public static class CardRenderer
{
    private const float FitWidthFraction = 0.90f;   // label spans at most this much of the width
    private const float ShadowBlurDivisor = 20f;    // blur radius   = fontSize / this
    private const float ShadowOffsetDivisor = 28f;  // shadow offset = fontSize / this
    private const int MinFontSize = 12;
    private const int JpegQuality = 92;

    private static readonly Lazy<SKTypeface> LazyTypeface = new(LoadTypeface);

    private static SKTypeface Typeface => LazyTypeface.Value;

    private static SKTypeface LoadTypeface()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Jellyfin.Plugin.Placard.Fonts.NotoSans-Bold.ttf");
        if (stream is null)
        {
            return SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);
        }

        // Copy into SKData so the typeface does not depend on the (disposed) resource stream.
        using var data = SKData.Create(stream);
        return SKTypeface.FromData(data) ?? SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold);
    }

    /// <summary>Render <paramref name="label"/> centered on the image at <paramref name="backdropPath"/>.</summary>
    /// <param name="scrim">Darkening overlay opacity, 0-255.</param>
    /// <param name="fontHeightPct">Label height as a percent of image height.</param>
    public static byte[] Render(string backdropPath, string label, int scrim, int fontHeightPct)
    {
        using var input = SKBitmap.Decode(backdropPath)
            ?? throw new InvalidOperationException($"Placard could not decode backdrop image: {backdropPath}");

        int w = input.Width;
        int h = input.Height;
        scrim = Math.Clamp(scrim, 0, 255);
        fontHeightPct = Math.Clamp(fontHeightPct, 5, 40);

        using var surface = SKSurface.Create(new SKImageInfo(w, h))
            ?? throw new InvalidOperationException($"Placard could not create a {w}x{h} drawing surface");
        var canvas = surface.Canvas;
        canvas.DrawBitmap(input, 0, 0);

        using (var scrimPaint = new SKPaint { Color = new SKColor(0, 0, 0, (byte)scrim) })
        {
            canvas.DrawRect(0, 0, w, h, scrimPaint);
        }

        using var text = new SKPaint { Typeface = Typeface, IsAntialias = true, Color = SKColors.White };

        // Shrink until the label fits within FitWidthFraction of the width.
        float size = h * fontHeightPct / 100f;
        for (; size > MinFontSize; size -= 4)
        {
            text.TextSize = size;
            if (text.MeasureText(label) <= w * FitWidthFraction)
            {
                break;
            }
        }

        text.TextSize = size;
        SKRect bounds = default;
        text.MeasureText(label, ref bounds);
        float x = ((w - bounds.Width) / 2f) - bounds.Left;
        float y = ((h - bounds.Height) / 2f) - bounds.Top;

        using (var shadow = new SKPaint
        {
            Typeface = Typeface,
            TextSize = size,
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 205),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size / ShadowBlurDivisor)
        })
        {
            canvas.DrawText(label, x, y + (size / ShadowOffsetDivisor), shadow);
        }

        canvas.DrawText(label, x, y, text);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data.ToArray();
    }
}
