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
        MessageText.Text = payload.Message;
        Topmost = alwaysOnTop;
        Opacity = 0;

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
                ArtGap.Visibility = Visibility.Visible;
            }
            catch { }
        }

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _dismissTimer.Tick += (_, _) => { _dismissTimer.Stop(); BeginDismiss(); };
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
