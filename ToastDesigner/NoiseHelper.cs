using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ToastDesigner;

internal static class NoiseHelper
{
    internal static ImageBrush GetNoiseBrush(double scale, Color tint, double intensity)
    {
        int sz = Math.Max(16, Math.Min(512, (int)scale));
        var bmp    = new WriteableBitmap(sz, sz, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[sz * sz * 4];
        var rng    = new Random(42);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte v        = (byte)rng.Next(256);
            pixels[i]     = (byte)(tint.B * v / 255);  // B
            pixels[i + 1] = (byte)(tint.G * v / 255);  // G
            pixels[i + 2] = (byte)(tint.R * v / 255);  // R
            pixels[i + 3] = v;                          // alpha = noise density
        }

        bmp.WritePixels(new Int32Rect(0, 0, sz, sz), pixels, sz * 4, 0);
        bmp.Freeze();

        return new ImageBrush(bmp)
        {
            TileMode      = TileMode.Tile,
            Viewport      = new Rect(0, 0, sz, sz),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity       = intensity,
        };
    }
}
