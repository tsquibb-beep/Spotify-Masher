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
        CountdownBar.Visibility    = Visibility.Collapsed;
        FillCentreRect.Visibility  = Visibility.Collapsed;
        BorderTraceRect.Visibility = Visibility.Collapsed;
        TopEdgeRect.Visibility     = Visibility.Collapsed;
        AuroraGlow.Visibility      = Visibility.Collapsed;

        Color borderColor = ParseColor(t.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));

        switch (t.ActionBorderType)
        {
            case "Aurora Glow":
                ApplyAuroraGlow();
                break;

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
                // perimeter + dash array set in StartFxAnimations once layout is ready
                break;

            case "Orbiting Spark":
                BorderTraceRect.Visibility = Visibility.Visible;
                BorderTraceRect.Stroke = new SolidColorBrush(borderColor);
                // dash array + animation set in StartFxAnimations once layout is ready
                break;

            case "Bouncing Edge":
                TopEdgeRect.Visibility = Visibility.Visible;
                // fill + animation set in StartFxAnimations once layout is ready
                break;
        }
    }

    // Aurora Glow — a blurred, animated multi-colour gradient halo behind the toast.
    // WPF recreation of the LobeHub "Get Started" button: linear-gradient(-45deg) of four
    // colours, oversized + drifting (background-position animation) + blur(0.5em) + opacity 0.5.
    private const double AuroraPad = 16;       // transparent room around the toast for the blur to bleed
    private LinearGradientBrush? _auroraBrush;
    private LinearGradientBrush? _auroraBgBrush;

    // One drift period for the curtains — shifting both gradient points by this vector loops seamlessly.
    private static readonly Vector AuroraBgDrift = new(0.32, 0.19);

    private static readonly string[] AuroraGradientFallback = ["#FFB224", "#E34BA9", "#0072F5", "#95F3D9"];
    private static readonly string[] AuroraCurtainFallback  = ["#3B82F6", "#A5B4FC", "#93C5FD", "#DDD6FE", "#60A5FA"];

    // Evenly-spaced gradient (offsets 0, 1/n, 2/n …) with the first colour repeated at 1.0,
    // so a SpreadMethod=Repeat scroll tiles with no seam.
    private static GradientStopCollection BuildLoopStops(string[]? colors, string[] fallback)
    {
        string[] c = colors is { Length: >= 2 } ? colors : fallback;
        var stops = new GradientStopCollection();
        for (int i = 0; i < c.Length; i++)
            stops.Add(new GradientStop(ParseColor(c[i], Colors.Magenta), (double)i / c.Length));
        stops.Add(new GradientStop(ParseColor(c[0], Colors.Magenta), 1.0));
        return stops;
    }

    // Curtain shards: transparent gap, colours spread across 0.10–0.70, transparent tail — the
    // baked-in alpha mask that replaces the CSS black-stripe + difference-blend trick.
    private static GradientStopCollection BuildCurtainStops(string[]? colors, string[] fallback)
    {
        string[] c = colors is { Length: >= 1 } ? colors : fallback;
        const double lo = 0.10, hi = 0.70;
        var stops = new GradientStopCollection { new(Colors.Transparent, 0.0) };
        for (int i = 0; i < c.Length; i++)
        {
            double off = c.Length == 1 ? (lo + hi) / 2 : lo + (hi - lo) * i / (c.Length - 1);
            stops.Add(new GradientStop(ParseColor(c[i], Colors.SkyBlue), off));
        }
        stops.Add(new GradientStop(Colors.Transparent, 0.85));
        stops.Add(new GradientStop(Colors.Transparent, 1.0));
        return stops;
    }

    private void ApplyAuroraGlow()
    {
        // Pad the toast so the blurred halo has room inside the window.
        ToastBorder.Margin = new Thickness(AuroraPad);
        AuroraGlow.Margin  = new Thickness(AuroraPad);
        AuroraGlow.CornerRadius = ToastBorder.CornerRadius;

        // The halo IS the glow here — drop the single-colour DropShadow so they don't fight.
        ToastBorder.Effect = null;

        // Palette-driven (per theme). Colours spread evenly with the first repeated at 1.0 so the
        // SpreadMethod=Repeat scroll tiles seamlessly.
        _auroraBrush = new LinearGradientBrush
        {
            StartPoint   = new Point(0, 0),
            EndPoint     = new Point(1, 1),     // -45° diagonal
            SpreadMethod = GradientSpreadMethod.Repeat,
            GradientStops = BuildLoopStops(_theme.AuroraGradientColors,
                AuroraGradientFallback),
        };

        AuroraGlow.Background = _auroraBrush;
        AuroraGlow.Opacity    = 0.5;
        AuroraGlow.Visibility = Visibility.Visible;

        // Same brush instance on the crisp border — the blur lives on the halo element only,
        // so the border stays sharp and animates in perfect sync with the glow.
        ToastBorder.BorderBrush     = _auroraBrush;
        ToastBorder.BorderThickness = new Thickness(1);

        ApplyAuroraCurtains();
    }

    // Aurora curtains — the Aceternity "Aurora Background": drifting ~100° colour bands behind
    // the text. WPF approximation of two repeating-linear-gradients + mix-blend-mode:difference —
    // the black stripe-mask is baked into alpha (transparent gaps) so no blend shader is needed.
    private void ApplyAuroraCurtains()
    {
        _auroraBgBrush = new LinearGradientBrush
        {
            StartPoint   = new Point(0, 0),
            EndPoint     = new Point(AuroraBgDrift.X, AuroraBgDrift.Y),  // ~100° (near-horizontal, tilted)
            SpreadMethod = GradientSpreadMethod.Repeat,
            GradientStops = BuildCurtainStops(_theme.AuroraCurtainColors, AuroraCurtainFallback),
        };

        AuroraBg.Background = _auroraBgBrush;
        AuroraBg.Opacity    = 0.08;   // subtle — lets the dark card dominate, like the reference
        AuroraBg.Visibility = Visibility.Visible;

        // Vertical falloff — the website's shards fade out toward the top/bottom edges rather
        // than running as uniform streaks. An OpacityMask tapers the curtains at both ends.
        AuroraBg.OpacityMask = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint   = new Point(0, 1),
            GradientStops = new GradientStopCollection
            {
                new(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.00),  // top: full — shards anchored here
                new(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.50),
                new(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1.00),  // bottom: faded out — shards trail off
            },
        };
    }

    private void StartAuroraAnimation()
    {
        if (_auroraBrush is null) return;

        // Scroll the gradient by exactly one colour period (the full StartPoint→EndPoint vector)
        // so it loops with no visible seam — the WPF analog of the background-position drift.
        var dur = new Duration(TimeSpan.FromSeconds(5));
        _auroraBrush.BeginAnimation(LinearGradientBrush.StartPointProperty,
            new PointAnimation(new Point(0, 0), new Point(1, 1), dur) { RepeatBehavior = RepeatBehavior.Forever });
        _auroraBrush.BeginAnimation(LinearGradientBrush.EndPointProperty,
            new PointAnimation(new Point(1, 1), new Point(2, 2), dur) { RepeatBehavior = RepeatBehavior.Forever });

        // Curtains drift slowly sideways — shift both points by one full period so it loops seamlessly.
        if (_auroraBgBrush is not null)
        {
            var bgDur = new Duration(TimeSpan.FromSeconds(10));
            Point s0 = _auroraBgBrush.StartPoint, e0 = _auroraBgBrush.EndPoint;
            _auroraBgBrush.BeginAnimation(LinearGradientBrush.StartPointProperty,
                new PointAnimation(s0, s0 + AuroraBgDrift, bgDur) { RepeatBehavior = RepeatBehavior.Forever });
            _auroraBgBrush.BeginAnimation(LinearGradientBrush.EndPointProperty,
                new PointAnimation(e0, e0 + AuroraBgDrift, bgDur) { RepeatBehavior = RepeatBehavior.Forever });
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
        StartShimmer();
        StartActionBorderAnimation();
    }

    private void StartShimmer()
    {
        var duration = TimeSpan.FromMilliseconds(1500);
        var delay    = TimeSpan.FromMilliseconds(160);
        var ease     = new CubicEase { EasingMode = EasingMode.EaseInOut };

        switch (_theme.ShimmerEffect)
        {
            case "Diagonal":
                RunSweepShimmer(new Point(0, 0), new Point(1, 1), duration, delay, ease);
                break;

            case "Horizontal":
                RunSweepShimmer(new Point(0, 0.5), new Point(1, 0.5), duration, delay, ease);
                break;

            case "Pulse":
                RunPulseShimmer(duration);
                break;

            case "Static Gloss":
                SweepHighlight.Visibility = Visibility.Visible;
                SweepHighlight.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Colors.Transparent,                         0.00),
                        new(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF),    0.45),
                        new(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF),    0.55),
                        new(Colors.Transparent,                         1.00),
                    },
                    new Point(0, 0), new Point(1, 1));
                break;

            case "Slide-Through":
                RunSweepShimmer(new Point(0, 0.5), new Point(1, 0.5),
                    TimeSpan.FromMilliseconds(700), TimeSpan.FromMilliseconds(400),
                    new QuinticEase { EasingMode = EasingMode.EaseIn },
                    peakAlpha: 0x5A);
                break;

            case "Spotlight":
                var radial = new RadialGradientBrush
                {
                    Center         = new Point(-0.2, 0.5),
                    GradientOrigin = new Point(-0.2, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.8,
                    GradientStops = new GradientStopCollection
                    {
                        new(Color.FromArgb(0x3C, 0xFF, 0xFF, 0xFF), 0.0),
                        new(Colors.Transparent,                       1.0),
                    }
                };
                SweepHighlight.Visibility = Visibility.Visible;
                SweepHighlight.Background = radial;
                var ptAnim = new PointAnimation(
                    new Point(-0.2, 0.5), new Point(1.2, 0.5),
                    TimeSpan.FromSeconds(3))
                {
                    BeginTime = TimeSpan.FromMilliseconds(300)
                };
                radial.BeginAnimation(RadialGradientBrush.CenterProperty, ptAnim);
                radial.BeginAnimation(RadialGradientBrush.GradientOriginProperty,
                    ptAnim.Clone() as PointAnimation);
                break;

            case "Breathing":
                if (ToastBorder.Background is LinearGradientBrush lgb && lgb.GradientStops.Count >= 2)
                {
                    AnimateStop(lgb.GradientStops[0], 0.0, 0.3,
                        TimeSpan.FromSeconds(3), TimeSpan.Zero,
                        new SineEase(), autoReverse: true);
                    AnimateStop(lgb.GradientStops[^1], 1.0, 0.7,
                        TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(150),
                        new SineEase(), autoReverse: true);
                }
                break;

            default: // "None"
                SweepHighlight.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void RunSweepShimmer(Point start, Point end, TimeSpan duration, TimeSpan delay,
        IEasingFunction ease, byte peakAlpha = 0x18)
    {
        var s1 = new GradientStop(Color.FromArgb(0x00,       0xFF, 0xFF, 0xFF), -0.40);
        var s2 = new GradientStop(Color.FromArgb(peakAlpha,  0xFF, 0xFF, 0xFF), -0.15);
        var s3 = new GradientStop(Color.FromArgb(peakAlpha,  0xFF, 0xFF, 0xFF),  0.05);
        var s4 = new GradientStop(Color.FromArgb(0x00,       0xFF, 0xFF, 0xFF),  0.28);

        SweepHighlight.Background = new LinearGradientBrush(
            new GradientStopCollection { s1, s2, s3, s4 }, start, end);

        AnimateStop(s1, -0.40, 0.72, duration, delay, ease);
        AnimateStop(s2, -0.15, 0.97, duration, delay, ease);
        AnimateStop(s3,  0.05, 1.13, duration, delay, ease);
        AnimateStop(s4,  0.28, 1.38, duration, delay, ease);
    }

    private void RunPulseShimmer(TimeSpan duration)
    {
        SweepHighlight.Background = new SolidColorBrush(Colors.White);
        SweepHighlight.Opacity = 0;

        var pulse = new DoubleAnimationUsingKeyFrames
        {
            BeginTime = TimeSpan.FromMilliseconds(100),
            Duration  = new Duration(duration),
        };
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,  KeyTime.FromPercent(0.0)));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.08, KeyTime.FromPercent(0.35),
            new SineEase { EasingMode = EasingMode.EaseOut }));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,  KeyTime.FromPercent(0.75),
            new SineEase { EasingMode = EasingMode.EaseIn }));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.0,  KeyTime.FromPercent(1.0)));
        SweepHighlight.BeginAnimation(OpacityProperty, pulse);
    }

    private void StartActionBorderAnimation()
    {
        var duration = TimeSpan.FromMilliseconds(_durationMs);

        switch (_theme.ActionBorderType)
        {
            case "Aurora Glow":
                StartAuroraAnimation();
                break;

            case "Bottom Bar Drain":
                CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1.0, 0.0, duration));
                break;

            case "Fill from Centre":
                FillCentreScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.0, 1.0, duration));
                break;

            case "Full Border Trace":
            {
                double perimeter = 2 * (ToastBorder.ActualWidth + ToastBorder.ActualHeight);
                if (perimeter < 1) perimeter = 760;
                BorderTraceRect.StrokeDashArray = new DoubleCollection([perimeter]);
                BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(perimeter, 0, duration));
                break;
            }

            case "Orbiting Spark":
            {
                double perimeter = 2 * (ToastBorder.ActualWidth + ToastBorder.ActualHeight);
                if (perimeter < 1) perimeter = 760;
                double sparkLen = Math.Min(50, perimeter * 0.12);
                BorderTraceRect.StrokeThickness = 2;
                BorderTraceRect.StrokeDashArray = new DoubleCollection([sparkLen, perimeter - sparkLen]);
                BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(perimeter, 0, TimeSpan.FromSeconds(2.5))
                    {
                        RepeatBehavior = RepeatBehavior.Forever
                    });
                break;
            }

            case "Bouncing Edge":
            {
                Color bc = ParseColor(_theme.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));
                const double half = 0.3;
                var bounceBrush = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Colors.Transparent, 0.0),
                        new(bc,                 0.45),
                        new(bc,                 0.55),
                        new(Colors.Transparent, 1.0),
                    },
                    new Point(0, 0), new Point(1, 0));
                TopEdgeRect.Fill = bounceBrush;

                var bounceDur = TimeSpan.FromSeconds(1.4);
                var bounceEase = new SineEase();
                AnimateStop(bounceBrush.GradientStops[0], -half * 2,       1.0,            bounceDur, TimeSpan.Zero, bounceEase, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[1], 0.0 - half,      1.0 + 0,        bounceDur, TimeSpan.Zero, bounceEase, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[2], 0.0 + half,      1.0 + half,     bounceDur, TimeSpan.Zero, bounceEase, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[3], 0.0 + half * 2,  1.0 + half * 2, bounceDur, TimeSpan.Zero, bounceEase, autoReverse: true);
                break;
            }
        }
    }

    private static void AnimateStop(GradientStop stop, double from, double to,
        TimeSpan duration, TimeSpan delay, IEasingFunction? ease, bool autoReverse = false)
    {
        stop.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(from, to, duration)
        {
            BeginTime      = delay,
            EasingFunction = ease,
            AutoReverse    = autoReverse,
            RepeatBehavior = autoReverse ? RepeatBehavior.Forever : default,
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
