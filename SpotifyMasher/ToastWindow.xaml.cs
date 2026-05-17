using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SpotifyMasher.Models;

namespace SpotifyMasher;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;
    private readonly int _durationMs;
    private readonly ToastTheme _theme;
    private bool _closing;

    public ToastWindow(ToastPayload payload, int durationMs, bool alwaysOnTop, ToastTheme? theme = null)
    {
        InitializeComponent();
        Topmost = alwaysOnTop;
        Opacity = 0;
        _durationMs = durationMs;
        _theme = theme ?? new ToastTheme();

        if (payload.TrackName is not null)
            PopulateRichLayout(payload);
        else
            MessageText.Text = payload.Message;

        ApplyTheme(_theme);

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _dismissTimer.Tick += (_, _) => { _dismissTimer.Stop(); BeginDismiss(); };
    }

    private void PopulateRichLayout(ToastPayload payload)
    {
        ToastBorder.MaxWidth = 520;
        ToastBorder.MinWidth = 280;

        MessageText.Visibility = Visibility.Collapsed;
        TrackPanel.Visibility  = Visibility.Visible;

        TrackText.Text  = payload.TrackName  ?? string.Empty;
        ArtistText.Text = payload.ArtistName ?? string.Empty;
        AlbumText.Text  = payload.AlbumName  ?? string.Empty;

        if (string.IsNullOrEmpty(ArtistText.Text)) ArtistText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(AlbumText.Text))  AlbumText.Visibility  = Visibility.Collapsed;

        TrackText.FontSize  = AdaptiveSize(payload.TrackName,  defaultSize: 14,   min: 11.5);
        ArtistText.FontSize = AdaptiveSize(payload.ArtistName, defaultSize: 12.5, min: 10.5);
        AlbumText.FontSize  = AdaptiveSize(payload.AlbumName,  defaultSize: 11.5, min: 10);

        if (payload.ImageBytes is { Length: > 0 } bytes)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                AlbumArt.Source = bitmap;
                AlbumArt.Visibility = Visibility.Visible;
                ArtGap.Visibility   = Visibility.Visible;
            }
            catch { }
        }
    }

    private static double AdaptiveSize(string? text, double defaultSize, double min)
    {
        if (string.IsNullOrEmpty(text)) return defaultSize;
        return text.Length switch
        {
            > 50 => min,
            > 35 => defaultSize - 1.5,
            _ => defaultSize
        };
    }

    private void ApplyTheme(ToastTheme t)
    {
        ApplyBackground(t);
        ApplyGlow(t);
        ApplyTextColors(t);
        ApplyActionBorder(t);
    }

    private void ApplyBackground(ToastTheme t)
    {
        Color c1 = ParseColor(t.BackgroundColor1, Color.FromRgb(0x1c, 0x27, 0x48));
        Color c2 = ParseColor(t.BackgroundColor2, Color.FromRgb(0x11, 0x18, 0x32));

        ToastBorder.Background = t.BackgroundEffect switch
        {
            "Solid"       => new SolidColorBrush(c1),
            "Radial Glow" => new RadialGradientBrush(c1, c2)
                             {
                                 Center = new Point(0.5, 0.5),
                                 RadiusX = 0.7,
                                 RadiusY = 0.7,
                                 GradientOrigin = new Point(0.5, 0.5),
                             },
            "Grain"       => new SolidColorBrush(c1),
            _             => new LinearGradientBrush(c1, c2, new Point(0, 0), new Point(0, 1)),
        };

        if (t.BackgroundEffect == "Grain")
        {
            GrainOverlay.Background = NoiseHelper.GetNoiseBrush();
            GrainOverlay.Visibility = Visibility.Visible;
        }
    }

    private void ApplyGlow(ToastTheme t)
    {
        if (ToastBorder.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
            shadow.Color = ParseColor(t.GlowColor, Color.FromRgb(0x1D, 0xB9, 0x54));
    }

    private void ApplyTextColors(ToastTheme t)
    {
        var msg    = new SolidColorBrush(ParseColor(t.MessageTextColor, Colors.White));
        var artist = new SolidColorBrush(ParseColor(t.ArtistTextColor,  Color.FromRgb(0x1D, 0xB9, 0x54)));
        var album  = new SolidColorBrush(ParseColor(t.AlbumTextColor,   Color.FromRgb(0xA0, 0xA0, 0xB0)));

        MessageText.Foreground = msg;
        TrackText.Foreground   = msg;
        ArtistText.Foreground  = artist;
        AlbumText.Foreground   = album;
    }

    private void ApplyActionBorder(ToastTheme t)
    {
        CountdownBar.Visibility  = Visibility.Collapsed;
        FillCentreRect.Visibility = Visibility.Collapsed;
        BorderTraceRect.Visibility = Visibility.Collapsed;

        Color borderColor = ParseColor(t.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));

        switch (t.ActionBorderType)
        {
            case "Bottom Bar Drain":
                CountdownBar.Visibility = Visibility.Visible;
                CountdownBar.Fill = BuildSparkGradient(borderColor);
                break;

            case "Fill from Centre":
                FillCentreRect.Visibility = Visibility.Visible;
                FillCentreRect.Fill = new SolidColorBrush(borderColor);
                break;

            case "Full Border Trace":
                BorderTraceRect.Visibility = Visibility.Visible;
                BorderTraceRect.Stroke = new SolidColorBrush(borderColor);
                break;
                // perimeter + dash array set in StartFxAnimations once layout is ready
        }
    }

    // Rebuilds the countdown-bar gradient using a custom base colour, keeping the white spark tip.
    private static LinearGradientBrush BuildSparkGradient(Color c)
    {
        return new LinearGradientBrush(
            new GradientStopCollection
            {
                new(Color.FromArgb(0x66, c.R, c.G, c.B), 0),
                new(Color.FromArgb(0xFF, c.R, c.G, c.B), 0.12),
                new(Color.FromArgb(0xFF, c.R, c.G, c.B), 0.80),
                new(LightenColor(c, 0.30), 0.91),
                new(LightenColor(c, 0.70), 0.97),
                new(Colors.White, 1.0),
            },
            new Point(0, 0), new Point(1, 0));
    }

    private static Color LightenColor(Color c, double factor) =>
        Color.FromArgb(0xFF,
            (byte)(c.R + (255 - c.R) * factor),
            (byte)(c.G + (255 - c.G) * factor),
            (byte)(c.B + (255 - c.B) * factor));

    private static Color ParseColor(string hex, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return fallback; }
    }

    public new void Show()
    {
        base.Show();
        AnimateIn();
        StartFxAnimations();
        _dismissTimer.Start();
    }

    public void ForceClose()
    {
        _dismissTimer.Stop();
        _closing = true;
        Close();
    }

    private void StartFxAnimations()
    {
        // Shimmer: a narrow diagonal gleam sweeps top-left→bottom-right over 1.5 s.
        // Uses a Border (not Rectangle) so its background clips to the rounded corners.
        var sweepDuration = TimeSpan.FromMilliseconds(1500);
        var sweepDelay    = TimeSpan.FromMilliseconds(160);
        var sweepEase     = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var s1 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), -0.40);
        var s2 = new GradientStop(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF), -0.15);
        var s3 = new GradientStop(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),  0.05);
        var s4 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),  0.28);

        var shimmerBrush = new LinearGradientBrush(
            new GradientStopCollection { s1, s2, s3, s4 },
            new Point(0, 0), new Point(1, 1));
        SweepHighlight.Background = shimmerBrush;

        AnimateStop(s1, -0.40, 0.72, sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s2, -0.15, 0.97, sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s3,  0.05, 1.13, sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s4,  0.28, 1.38, sweepDuration, sweepDelay, sweepEase);

        // Action border animations
        var duration = TimeSpan.FromMilliseconds(_durationMs);

        switch (_theme.ActionBorderType)
        {
            case "Bottom Bar Drain":
                CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1.0, 0.0, duration));
                break;

            case "Fill from Centre":
                FillCentreScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.0, 1.0, duration));
                break;

            case "Full Border Trace":
                double perimeter = 2 * (ToastBorder.ActualWidth + ToastBorder.ActualHeight);
                if (perimeter < 1) perimeter = 760; // fallback if layout not yet measured
                BorderTraceRect.StrokeDashArray = [perimeter];
                BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(perimeter, 0, duration));
                break;
        }
    }

    private static void AnimateStop(GradientStop stop, double from, double to,
        TimeSpan duration, TimeSpan delay, IEasingFunction ease)
    {
        stop.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(from, to, duration)
        {
            BeginTime = delay,
            EasingFunction = ease
        });
    }

    private void AnimateIn()
    {
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(OpacityProperty, fadeIn);
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
    }

    private void BeginDismiss()
    {
        if (_closing) return;
        _closing = true;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
