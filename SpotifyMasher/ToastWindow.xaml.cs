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
    private bool _closing;

    public ToastWindow(ToastPayload payload, int durationMs, bool alwaysOnTop)
    {
        InitializeComponent();
        Topmost = alwaysOnTop;
        Opacity = 0;

        if (payload.TrackName is not null)
            PopulateRichLayout(payload);
        else
            MessageText.Text = payload.Message;

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _dismissTimer.Tick += (_, _) => { _dismissTimer.Stop(); BeginDismiss(); };
    }

    private void PopulateRichLayout(ToastPayload payload)
    {
        // Widen the border for the rich layout
        ToastBorder.MaxWidth = 520;
        ToastBorder.MinWidth = 280;

        MessageText.Visibility = Visibility.Collapsed;
        TrackPanel.Visibility = Visibility.Visible;

        TrackText.Text   = payload.TrackName ?? string.Empty;
        ArtistText.Text  = payload.ArtistName ?? string.Empty;
        AlbumText.Text   = payload.AlbumName ?? string.Empty;

        // Hide empty lines so the panel doesn't leave a gap
        if (string.IsNullOrEmpty(ArtistText.Text)) ArtistText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(AlbumText.Text))  AlbumText.Visibility  = Visibility.Collapsed;

        // Adaptive font sizes — reduce for long strings, TextTrimming handles the rest
        TrackText.FontSize  = AdaptiveSize(payload.TrackName,  defaultSize: 14, min: 11.5);
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

    // Reduce font size for longer strings; TextTrimming handles anything that still overflows.
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
        _dismissTimer.Start();
    }

    public void ForceClose()
    {
        _dismissTimer.Stop();
        _closing = true;
        Close();
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
