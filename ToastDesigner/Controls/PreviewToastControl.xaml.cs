using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ToastDesigner.Models;

namespace ToastDesigner.Controls;

public partial class PreviewToastControl : System.Windows.Controls.UserControl
{
    private DesignerTheme _theme = new();
    private bool          _animating;

    public PreviewToastControl() => InitializeComponent();

    // ── Public API ──────────────────────────────────────────────────────────────

    public void ApplyTheme(DesignerTheme t, bool animate = true)
    {
        _theme     = t;
        _animating = animate;

        StopAnimations();
        ApplyStructure(t);

        if (animate) StartAnimations();
    }

    public void SetSampleData(string track, string artist, string album, byte[]? artBytes)
    {
        TrackText.Text  = string.IsNullOrEmpty(_theme.TrackPrefix + _theme.TrackSuffix)
                          ? track
                          : $"{_theme.TrackPrefix}{track}{_theme.TrackSuffix}";
        ArtistText.Text = string.IsNullOrEmpty(_theme.ArtistPrefix + _theme.ArtistSuffix)
                          ? artist
                          : $"{_theme.ArtistPrefix}{artist}{_theme.ArtistSuffix}";
        AlbumText.Text  = string.IsNullOrEmpty(_theme.AlbumPrefix + _theme.AlbumSuffix)
                          ? album
                          : $"{_theme.AlbumPrefix}{album}{_theme.AlbumSuffix}";

        if (artBytes is { Length: > 0 })
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource  = new MemoryStream(artBytes);
                bmp.CacheOption   = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                AlbumArt.Source       = bmp;
                AlbumArt.Visibility   = Visibility.Visible;
                ArtGap.Visibility     = Visibility.Visible;
            }
            catch { SetNoArt(); }
        }
        else
        {
            SetNoArt();
        }
    }

    public void StartAnimations()
    {
        _animating = true;
        StartShimmer();
        StartActionBorderAnimation();
    }

    public void StopAnimations()
    {
        _animating = false;

        SweepHighlight.BeginAnimation(OpacityProperty, null);
        CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        FillCentreScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty, null);

        if (ToastBorder.Background is LinearGradientBrush lgb)
        {
            foreach (var stop in lgb.GradientStops)
                stop.BeginAnimation(GradientStop.OffsetProperty, null);
        }

        // Clear any looping shimmer brush animations on SweepHighlight
        if (SweepHighlight.Background is LinearGradientBrush shb)
        {
            foreach (var stop in shb.GradientStops)
                stop.BeginAnimation(GradientStop.OffsetProperty, null);
        }

        if (TopEdgeRect.Fill is LinearGradientBrush teb)
        {
            foreach (var stop in teb.GradientStops)
                stop.BeginAnimation(GradientStop.OffsetProperty, null);
        }
    }

    // ── Visual structure ────────────────────────────────────────────────────────

    private void ApplyStructure(DesignerTheme t)
    {
        // Border geometry
        var cr = new CornerRadius(t.CornerRadius);
        ToastBorder.CornerRadius  = cr;
        GrainOverlay.CornerRadius = new CornerRadius(Math.Max(0, t.CornerRadius - 1));
        SweepHighlight.CornerRadius = new CornerRadius(Math.Max(0, t.CornerRadius - 1));
        ToastBorder.BorderThickness = new Thickness(t.BorderThickness);
        ToastBorder.Opacity       = t.BackgroundOpacity;

        double traceR = Math.Max(0, t.CornerRadius - 1);
        BorderTraceRect.RadiusX = traceR;
        BorderTraceRect.RadiusY = traceR;

        // Border colour and opacity
        var borderColor = ParseColor(t.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));
        ToastBorder.BorderBrush  = new SolidColorBrush(borderColor) { Opacity = t.BorderOpacity };

        // Glow (DropShadowEffect)
        if (ToastBorder.Effect is DropShadowEffect shadow)
        {
            shadow.Color     = ParseColor(t.BorderGlowEnabled ? t.BorderGlowColor : "#000000",
                                          Color.FromRgb(0x1D, 0xB9, 0x54));
            shadow.BlurRadius = t.BorderGlowEnabled ? t.GlowIntensity : 0;
            shadow.Opacity    = t.BorderGlowEnabled ? 0.55 : 0;
        }

        // Background
        ApplyBackground(t);

        // Text colours and styling
        ApplyTextStyles(t);

        // Action border visibility
        ApplyActionBorder(t);
    }

    private void ApplyBackground(DesignerTheme t)
    {
        Color c1 = ParseColor(t.BackgroundColor1, Color.FromRgb(0x1c, 0x27, 0x48));
        Color c2 = ParseColor(t.BackgroundColor2, Color.FromRgb(0x11, 0x18, 0x32));

        GrainOverlay.Visibility = Visibility.Collapsed;

        switch (t.BackgroundEffect)
        {
            case "Solid":
                ToastBorder.Background = new SolidColorBrush(c1);
                break;

            case "Radial Glow":
                ToastBorder.Background = new RadialGradientBrush(c1, c2)
                {
                    Center         = new Point(0.5, 0.5),
                    RadiusX        = 0.7,
                    RadiusY        = 0.7,
                    GradientOrigin = new Point(0.5, 0.5),
                };
                break;

            case "Grain":
                ToastBorder.Background  = new SolidColorBrush(c1);
                GrainOverlay.Background = NoiseHelper.GetNoiseBrush();
                GrainOverlay.Visibility = Visibility.Visible;
                break;

            case "Image":
                if (!string.IsNullOrWhiteSpace(t.BackgroundImagePath) && File.Exists(t.BackgroundImagePath))
                {
                    try
                    {
                        var bmp = new BitmapImage(new Uri(t.BackgroundImagePath));
                        ToastBorder.Background = new ImageBrush(bmp)
                        {
                            Stretch   = Stretch.UniformToFill,
                            TileMode  = TileMode.None,
                        };
                    }
                    catch { ToastBorder.Background = new SolidColorBrush(c1); }
                }
                else
                {
                    // Checkerboard placeholder when no image is set
                    ToastBorder.Background = BuildCheckerBrush();
                }
                break;

            default: // Gradient
                ToastBorder.Background = new LinearGradientBrush(c1, c2, new Point(0, 0), new Point(0, 1));
                break;
        }
    }

    private static ImageBrush BuildCheckerBrush()
    {
        const int sz = 16;
        var bmp    = new WriteableBitmap(sz * 2, sz * 2, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[sz * 2 * sz * 2 * 4];
        for (int y = 0; y < sz * 2; y++)
        {
            for (int x = 0; x < sz * 2; x++)
            {
                bool light = (x / sz + y / sz) % 2 == 0;
                int  i     = (y * sz * 2 + x) * 4;
                byte v     = light ? (byte)0x44 : (byte)0x22;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = v;
                pixels[i + 3] = 0xFF;
            }
        }
        bmp.WritePixels(new Int32Rect(0, 0, sz * 2, sz * 2), pixels, sz * 2 * 4, 0);
        bmp.Freeze();
        return new ImageBrush(bmp)
        {
            TileMode      = TileMode.Tile,
            Viewport      = new Rect(0, 0, sz * 2, sz * 2),
            ViewportUnits = BrushMappingMode.Absolute,
        };
    }

    private void ApplyTextStyles(DesignerTheme t)
    {
        TrackText.Foreground  = new SolidColorBrush(ParseColor(t.MessageTextColor, Colors.White))  { Opacity = t.TrackTextOpacity };
        ArtistText.Foreground = new SolidColorBrush(ParseColor(t.ArtistTextColor, Color.FromRgb(0x1D, 0xB9, 0x54))) { Opacity = t.ArtistTextOpacity };
        AlbumText.Foreground  = new SolidColorBrush(ParseColor(t.AlbumTextColor,  Color.FromRgb(0xA0, 0xA0, 0xB0))) { Opacity = t.AlbumTextOpacity };

        TrackText.FontSize  = t.TrackFontSize;
        ArtistText.FontSize = t.ArtistFontSize;
        AlbumText.FontSize  = t.AlbumFontSize;

        TrackText.FontWeight  = ParseFontWeight(t.TrackFontWeight);
        ArtistText.FontWeight = ParseFontWeight(t.ArtistFontWeight);
        AlbumText.FontWeight  = ParseFontWeight(t.AlbumFontWeight);
    }

    private void ApplyActionBorder(DesignerTheme t)
    {
        CountdownBar.Visibility    = Visibility.Collapsed;
        FillCentreRect.Visibility  = Visibility.Collapsed;
        BorderTraceRect.Visibility = Visibility.Collapsed;
        TopEdgeRect.Visibility     = Visibility.Collapsed;

        Color bc = ParseColor(t.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));

        switch (t.ActionBorderType)
        {
            case "Bottom Bar Drain":
                CountdownBar.Visibility = Visibility.Visible;
                CountdownBar.Fill       = BuildSparkGradient(bc);
                break;

            case "Fill from Centre":
                FillCentreRect.Visibility = Visibility.Visible;
                FillCentreRect.Fill       = new SolidColorBrush(bc);
                break;

            case "Full Border Trace":
                BorderTraceRect.Visibility     = Visibility.Visible;
                BorderTraceRect.Stroke         = new SolidColorBrush(bc);
                BorderTraceRect.StrokeThickness = t.BorderThickness + 0.5;
                break;

            case "Orbiting Spark":
                BorderTraceRect.Visibility     = Visibility.Visible;
                BorderTraceRect.Stroke         = new SolidColorBrush(bc);
                BorderTraceRect.StrokeThickness = t.BorderThickness + 0.5;
                break;

            case "Bouncing Edge":
                TopEdgeRect.Visibility = Visibility.Visible;
                break;
        }
    }

    // ── Animations ──────────────────────────────────────────────────────────────

    private void StartShimmer()
    {
        // All shimmers loop in the designer so the effect is always visible
        var duration = TimeSpan.FromMilliseconds(1500);
        var delay    = TimeSpan.FromMilliseconds(100);
        var ease     = new CubicEase { EasingMode = EasingMode.EaseInOut };

        SweepHighlight.Visibility = Visibility.Visible;

        switch (_theme.ShimmerEffect)
        {
            case "Diagonal":
                RunSweepShimmer(new Point(0, 0), new Point(1, 1), duration, delay, ease);
                break;

            case "Horizontal":
                RunSweepShimmer(new Point(0, 0.5), new Point(1, 0.5), duration, delay, ease);
                break;

            case "Pulse":
                RunPulseShimmer(TimeSpan.FromMilliseconds(1800));
                break;

            case "Static Gloss":
                SweepHighlight.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Colors.Transparent,                      0.00),
                        new(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF), 0.45),
                        new(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF), 0.55),
                        new(Colors.Transparent,                      1.00),
                    },
                    new Point(0, 0), new Point(1, 1));
                break;

            case "Slide-Through":
                RunSweepShimmer(new Point(0, 0.5), new Point(1, 0.5),
                    TimeSpan.FromMilliseconds(700), TimeSpan.FromMilliseconds(200),
                    new QuinticEase { EasingMode = EasingMode.EaseIn },
                    peakAlpha: 0x5A);
                break;

            case "Spotlight":
            {
                var radial = new RadialGradientBrush
                {
                    Center         = new Point(-0.2, 0.5),
                    GradientOrigin = new Point(-0.2, 0.5),
                    RadiusX = 0.5, RadiusY = 0.8,
                    GradientStops = new GradientStopCollection
                    {
                        new(Color.FromArgb(0x3C, 0xFF, 0xFF, 0xFF), 0.0),
                        new(Colors.Transparent,                       1.0),
                    }
                };
                SweepHighlight.Background = radial;
                var ptAnim = new PointAnimation(new Point(-0.2, 0.5), new Point(1.2, 0.5),
                    TimeSpan.FromSeconds(3))
                {
                    BeginTime       = TimeSpan.FromMilliseconds(200),
                    RepeatBehavior  = RepeatBehavior.Forever,
                };
                radial.BeginAnimation(RadialGradientBrush.CenterProperty,         ptAnim);
                radial.BeginAnimation(RadialGradientBrush.GradientOriginProperty,  ptAnim.Clone());
                break;
            }

            case "Breathing":
                if (ToastBorder.Background is LinearGradientBrush lgb && lgb.GradientStops.Count >= 2)
                {
                    AnimateStop(lgb.GradientStops[0],  0.0, 0.3, TimeSpan.FromSeconds(3), TimeSpan.Zero,                      new SineEase(), autoReverse: true);
                    AnimateStop(lgb.GradientStops[^1], 1.0, 0.7, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(150), new SineEase(), autoReverse: true);
                }
                SweepHighlight.Visibility = Visibility.Collapsed;
                break;

            default: // None
                SweepHighlight.Visibility  = Visibility.Collapsed;
                SweepHighlight.Background  = null;
                break;
        }
    }

    private void RunSweepShimmer(Point start, Point end, TimeSpan duration, TimeSpan delay,
        IEasingFunction ease, byte peakAlpha = 0x18)
    {
        var s1 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), -0.40);
        var s2 = new GradientStop(Color.FromArgb(peakAlpha, 0xFF, 0xFF, 0xFF), -0.15);
        var s3 = new GradientStop(Color.FromArgb(peakAlpha, 0xFF, 0xFF, 0xFF),  0.05);
        var s4 = new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),  0.28);

        SweepHighlight.Background = new LinearGradientBrush(
            new GradientStopCollection { s1, s2, s3, s4 }, start, end);

        AnimateStop(s1, -0.40, 0.72, duration, delay, ease, repeat: true);
        AnimateStop(s2, -0.15, 0.97, duration, delay, ease, repeat: true);
        AnimateStop(s3,  0.05, 1.13, duration, delay, ease, repeat: true);
        AnimateStop(s4,  0.28, 1.38, duration, delay, ease, repeat: true);
    }

    private void RunPulseShimmer(TimeSpan duration)
    {
        SweepHighlight.Background = new SolidColorBrush(Colors.White);
        SweepHighlight.Opacity    = 0;

        var pulse = new DoubleAnimationUsingKeyFrames
        {
            BeginTime      = TimeSpan.FromMilliseconds(100),
            Duration       = new Duration(duration),
            RepeatBehavior = RepeatBehavior.Forever,
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
        // All animations loop in the designer — use AutoReverse+Forever for fill/drain types,
        // and RepeatBehavior.Forever for trace/spark types (already loop-native).
        const double cycleMs = 3500.0;
        var cycleDur = TimeSpan.FromMilliseconds(cycleMs);

        switch (_theme.ActionBorderType)
        {
            case "Bottom Bar Drain":
                CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1.0, 0.0, cycleDur)
                    {
                        AutoReverse    = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                    });
                break;

            case "Fill from Centre":
                FillCentreScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.0, 1.0, cycleDur)
                    {
                        AutoReverse    = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                    });
                break;

            case "Full Border Trace":
            {
                // Use an estimated perimeter (updated after layout if possible)
                double perimeter = ActualWidth > 0
                    ? 2 * (ActualWidth + ActualHeight)
                    : 760;
                BorderTraceRect.StrokeDashArray = new DoubleCollection([perimeter]);
                BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(perimeter, 0, cycleDur)
                    {
                        AutoReverse    = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                    });
                break;
            }

            case "Orbiting Spark":
            {
                double perimeter = ActualWidth > 0
                    ? 2 * (ActualWidth + ActualHeight)
                    : 760;
                double sparkLen = Math.Min(50, perimeter * 0.12);
                BorderTraceRect.StrokeDashArray = new DoubleCollection([sparkLen, perimeter - sparkLen]);
                BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(perimeter, 0, TimeSpan.FromSeconds(2.5))
                    {
                        RepeatBehavior = RepeatBehavior.Forever,
                    });
                break;
            }

            case "Bouncing Edge":
            {
                Color bc    = ParseColor(_theme.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));
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

                var ease = new SineEase();
                var dur  = TimeSpan.FromSeconds(1.4);
                AnimateStop(bounceBrush.GradientStops[0], -half * 2,      1.0,            dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[1], 0.0 - half,     1.0,            dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[2], 0.0 + half,     1.0 + half,     dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[3], 0.0 + half * 2, 1.0 + half * 2, dur, TimeSpan.Zero, ease, autoReverse: true);
                break;
            }
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void AnimateStop(GradientStop stop, double from, double to,
        TimeSpan duration, TimeSpan delay, IEasingFunction? ease,
        bool autoReverse = false, bool repeat = false)
    {
        stop.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(from, to, duration)
        {
            BeginTime      = delay,
            EasingFunction = ease,
            AutoReverse    = autoReverse,
            RepeatBehavior = (autoReverse || repeat) ? RepeatBehavior.Forever : default,
        });
    }

    private static LinearGradientBrush BuildSparkGradient(Color c) =>
        new LinearGradientBrush(
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

    private static Color LightenColor(Color c, double f) =>
        Color.FromArgb(0xFF,
            (byte)(c.R + (255 - c.R) * f),
            (byte)(c.G + (255 - c.G) * f),
            (byte)(c.B + (255 - c.B) * f));

    private static Color ParseColor(string hex, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return fallback; }
    }

    private static FontWeight ParseFontWeight(string name) =>
        name switch
        {
            "Thin"       => FontWeights.Thin,
            "Light"      => FontWeights.Light,
            "Regular"    => FontWeights.Regular,
            "Medium"     => FontWeights.Medium,
            "SemiBold"   => FontWeights.SemiBold,
            "Bold"       => FontWeights.Bold,
            "ExtraBold"  => FontWeights.ExtraBold,
            "Black"      => FontWeights.Black,
            _            => FontWeights.Normal,
        };

    private void SetNoArt()
    {
        AlbumArt.Source     = null;
        AlbumArt.Visibility = Visibility.Collapsed;
        ArtGap.Visibility   = Visibility.Collapsed;
    }
}
