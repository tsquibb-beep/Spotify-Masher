using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpotifyMasher;

internal static class NoiseHelper
{
    private static ImageBrush? _cached;

    internal static ImageBrush GetNoiseBrush()
    {
        _cached ??= Generate();
        return _cached;
    }

    private static ImageBrush Generate()
    {
        const int size = 64;
        var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[size * size * 4];
        var rng = new Random(42);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte v = (byte)rng.Next(256);
            pixels[i]     = v;
            pixels[i + 1] = v;
            pixels[i + 2] = v;
            pixels[i + 3] = 0xFF;
        }

        bmp.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        bmp.Freeze();

        return new ImageBrush(bmp)
        {
            TileMode    = TileMode.Tile,
            Viewport    = new Rect(0, 0, 1, 1),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            Opacity     = 0.05,
        };
    }
}
