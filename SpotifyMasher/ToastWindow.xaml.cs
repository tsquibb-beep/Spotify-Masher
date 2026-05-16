using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SpotifyMasher.Models;

namespace SpotifyMasher;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;
    private readonly int _durationMs;
    private bool _closing;

    public ToastWindow(ToastPayload payload, int durationMs, bool alwaysOnTop)
    {
        InitializeComponent();
        Topmost = alwaysOnTop;
        Opacity = 0;
        _durationMs = durationMs;

        if (payload.TrackName is not null)
            PopulateRichLayout(payload);
        else
            MessageText.Text = payload.Message;

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
        // Shimmer: a white gleam sweeps left→right over 1.5 s, starting after the fade-in.
        // GradientStop offsets begin off the left edge and travel to off the right edge.
        var sweepDuration = TimeSpan.FromMilliseconds(1500);
        var sweepDelay    = TimeSpan.FromMilliseconds(160);
        var sweepEase     = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var s1 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), -0.55);
        var s2 = new GradientStop(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF), -0.2);
        var s3 = new GradientStop(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF),  0.0);
        var s4 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),  0.45);

        var shimmerBrush = new LinearGradientBrush(
            new GradientStopCollection { s1, s2, s3, s4 },
            new Point(0, 0), new Point(1, 0));
        SweepHighlight.Fill = shimmerBrush;

        AnimateStop(s1, -0.55, 0.55,  sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s2, -0.20, 0.90,  sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s3,  0.00, 1.10,  sweepDuration, sweepDelay, sweepEase);
        AnimateStop(s4,  0.45, 1.55,  sweepDuration, sweepDelay, sweepEase);

        // Countdown bar: ScaleX 1→0 (origin at left), drains right-to-left over full duration.
        var countdown = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(_durationMs));
        CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty, countdown);
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
