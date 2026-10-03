using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SpotifyMasher.Services;

namespace SpotifyMasher.Controls;

// Full-screen colour dropper. Freezes a screenshot of the whole virtual desktop, shows it in a
// borderless overlay with a magnifier loupe under the cursor, and returns the clicked pixel.
// Sampling uses GetCursorPos + the captured buffer (both physical pixels), so it never depends on
// WPF's DIP conversion. Esc or right-click cancels.
public class EyedropperWindow : Window
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT pt);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);

    private const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77, SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79;
    private const uint SRCCOPY = 0x00CC0020, CAPTUREBLT = 0x40000000;

    private const int LoupePixels = 11;   // odd, so there's a centre pixel
    private const int LoupeZoom   = 11;   // on-screen size of each magnified pixel

    private readonly BitmapSource _shot;
    private readonly byte[] _pixels;      // BGRA, row-major
    private readonly int _originX, _originY, _w, _h;

    private readonly Canvas _canvas = new();
    private readonly Border _loupe;
    private readonly Image _loupeImage;
    private readonly Border _loupeSwatch;
    private readonly TextBlock _loupeHex;

    public Color? PickedColor { get; private set; }

    private EyedropperWindow(BitmapSource shot, int originX, int originY)
    {
        _shot = shot;
        _w = shot.PixelWidth;
        _h = shot.PixelHeight;
        _originX = originX;
        _originY = originY;
        _pixels = new byte[_w * _h * 4];
        shot.CopyPixels(_pixels, _w * 4, 0);

        WindowStyle   = WindowStyle.None;
        ResizeMode    = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost       = true;
        Cursor        = Cursors.Cross;
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        var font = Application.Current.TryFindResource("FontLexend") as FontFamily;

        _loupeImage = new Image
        {
            Width  = LoupePixels * LoupeZoom,
            Height = LoupePixels * LoupeZoom,
            Stretch = Stretch.Fill,
        };
        RenderOptions.SetBitmapScalingMode(_loupeImage, BitmapScalingMode.NearestNeighbor);

        // Centre-pixel marker: white ring inside a black one so it shows on any colour.
        var marker = new Grid { Width = LoupeZoom + 2, Height = LoupeZoom + 2,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment   = VerticalAlignment.Center };
        marker.Children.Add(new Rectangle { Stroke = Brushes.Black, StrokeThickness = 1 });
        marker.Children.Add(new Rectangle { Stroke = Brushes.White, StrokeThickness = 1, Margin = new Thickness(1) });

        var zoomArea = new Grid { ClipToBounds = true };
        zoomArea.Children.Add(_loupeImage);
        zoomArea.Children.Add(marker);

        _loupeSwatch = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(3),
                                    BorderBrush = Brushes.White, BorderThickness = new Thickness(1),
                                    Margin = new Thickness(0, 0, 8, 0) };
        _loupeHex = new TextBlock { Foreground = Brushes.White, FontSize = 13, FontFamily = font,
                                    VerticalAlignment = VerticalAlignment.Center };

        var info = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 8, 2, 0) };
        info.Children.Add(_loupeSwatch);
        info.Children.Add(_loupeHex);

        var hint = new TextBlock { Text = "Click to pick · Esc to cancel", FontSize = 10.5, FontFamily = font,
                                   Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB0)),
                                   Margin = new Thickness(2, 4, 2, 0) };

        var stack = new StackPanel();
        stack.Children.Add(zoomArea);
        stack.Children.Add(info);
        stack.Children.Add(hint);

        _loupe = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x16, 0x21, 0x3e)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1D, 0xB9, 0x54)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(8),
            Child           = stack,
            IsHitTestVisible = false,
        };
        _canvas.Children.Add(_loupe);

        var root = new Grid();
        root.Children.Add(new Image { Source = shot, Stretch = Stretch.Fill });
        root.Children.Add(_canvas);
        Content = root;

        MouseMove += (_, e) => UpdateLoupe(e.GetPosition(_canvas));
        MouseLeftButtonDown += (_, _) => { PickedColor = SampleAtCursor(); Close(); };
        MouseRightButtonDown += (_, _) => Close();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
        Loaded += (_, _) =>
        {
            Activate();
            Focus();
            if (GetCursorPos(out _)) UpdateLoupe(Mouse.GetPosition(_canvas));
        };
    }

    // Hides `owner` (so the app isn't in the shot), captures the desktop, runs the picker, and
    // restores the owner. Returns null if cancelled or capture failed.
    public static async Task<Color?> PickAsync(Window? owner)
    {
        bool ownerWasVisible = owner?.IsVisible == true;
        if (ownerWasVisible) owner!.Hide();
        try
        {
            await Task.Delay(200);   // let DWM repaint what was under our window

            var shot = CaptureVirtualScreen(out int ox, out int oy);
            if (shot is null) return null;

            var picker = new EyedropperWindow(shot, ox, oy);
            picker.ShowDialog();
            return picker.PickedColor;
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Eyedropper failed — {ex.Message}");
            return null;
        }
        finally
        {
            if (ownerWasVisible)
            {
                owner!.Show();
                owner.Activate();
            }
        }
    }

    private static BitmapSource? CaptureVirtualScreen(out int originX, out int originY)
    {
        originX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        originY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        if (w <= 0 || h <= 0) return null;

        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc    = CreateCompatibleDC(screenDc);
        IntPtr bmp      = CreateCompatibleBitmap(screenDc, w, h);
        IntPtr old      = SelectObject(memDc, bmp);
        try
        {
            BitBlt(memDc, 0, 0, w, h, screenDc, originX, originY, SRCCOPY | CAPTUREBLT);
            SelectObject(memDc, old);

            var src = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            var bgra = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
            bgra.Freeze();
            return bgra;
        }
        finally
        {
            DeleteObject(bmp);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    // Cursor position in captured-buffer pixels, clamped to the image.
    private (int X, int Y) CursorPixel()
    {
        GetCursorPos(out var p);
        return (Math.Clamp(p.X - _originX, 0, _w - 1), Math.Clamp(p.Y - _originY, 0, _h - 1));
    }

    private Color SampleAtCursor()
    {
        var (x, y) = CursorPixel();
        int i = (y * _w + x) * 4;
        return Color.FromRgb(_pixels[i + 2], _pixels[i + 1], _pixels[i]);
    }

    private void UpdateLoupe(Point mouseDip)
    {
        var (x, y) = CursorPixel();
        const int half = LoupePixels / 2;

        // Keep the crop inside the image; near edges the marker drifts off-centre slightly, which is fine.
        int cx = Math.Clamp(x - half, 0, Math.Max(0, _w - LoupePixels));
        int cy = Math.Clamp(y - half, 0, Math.Max(0, _h - LoupePixels));
        _loupeImage.Source = new CroppedBitmap(_shot,
            new Int32Rect(cx, cy, Math.Min(LoupePixels, _w), Math.Min(LoupePixels, _h)));

        var c = SampleAtCursor();
        _loupeSwatch.Background = new SolidColorBrush(c);
        _loupeHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        // Sit below-right of the cursor; flip to the other side near the screen edges.
        _loupe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = _loupe.DesiredSize;
        double left = mouseDip.X + 24, top = mouseDip.Y + 24;
        if (left + size.Width  > ActualWidth)  left = mouseDip.X - 24 - size.Width;
        if (top  + size.Height > ActualHeight) top  = mouseDip.Y - 24 - size.Height;
        Canvas.SetLeft(_loupe, left);
        Canvas.SetTop(_loupe, top);
    }
}
