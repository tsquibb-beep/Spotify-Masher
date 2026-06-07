using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ToastDesigner.Models;

namespace ToastDesigner.Controls;

public partial class PreviewToastControl : System.Windows.Controls.UserControl
{
    private DesignerTheme _theme     = new();
    private bool          _animating;
    private bool          _grainActive;
    private Brush         _appliedBorderBrush = Brushes.Transparent;

    // Layer visibility state
    private bool _showBackground = true;
    private bool _showBorder     = true;
    private bool _showText       = true;
    private bool _showShimmer    = true;

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
                bmp.StreamSource = new MemoryStream(artBytes);
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                AlbumArt.Source     = bmp;
                AlbumArt.Visibility = Visibility.Visible;
                ArtGap.Visibility   = Visibility.Visible;
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

        // Stop shimmer movement
        ShimmerContainer.BeginAnimation(Canvas.LeftProperty, null);
        ShimmerContainer.BeginAnimation(Canvas.TopProperty,  null);

        // Stop action border animations
        CountdownScale.BeginAnimation(ScaleTransform.ScaleXProperty,  null);
        FillCentreScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BorderTraceRect.BeginAnimation(Shape.StrokeDashOffsetProperty, null);

        if (TopEdgeRect.Fill is LinearGradientBrush teb)
            foreach (var stop in teb.GradientStops)
                stop.BeginAnimation(GradientStop.OffsetProperty, null);
    }

    public void SetLayerVisibility(bool background, bool border, bool text, bool shimmer)
    {
        _showBackground = background;
        _showBorder     = border;
        _showText       = text;
        _showShimmer    = shimmer;

        BgLayer.Visibility      = background ? Visibility.Visible : Visibility.Collapsed;
        GrainOverlay.Visibility = background && _grainActive ? Visibility.Visible : Visibility.Collapsed;

        ToastBorder.BorderBrush = border ? _appliedBorderBrush : Brushes.Transparent;
        BorderLayer.Visibility  = border ? Visibility.Visible : Visibility.Collapsed;

        ContentGrid.Visibility  = text    ? Visibility.Visible : Visibility.Collapsed;
        ShimmerCanvas.Visibility = shimmer ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Visual structure ────────────────────────────────────────────────────────

    private void ApplyStructure(DesignerTheme t)
    {
        var cr = new CornerRadius(t.CornerRadius);
        ToastBorder.CornerRadius  = cr;
        ToastBorder.BorderThickness = new Thickness(t.BorderThickness);
        ToastBorder.Opacity       = t.BackgroundOpacity;

        double traceR = Math.Max(0, t.CornerRadius - 1);
        BorderTraceRect.RadiusX = traceR;
        BorderTraceRect.RadiusY = traceR;

        var borderColor = ParseColor(t.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));
        _appliedBorderBrush = new SolidColorBrush(borderColor) { Opacity = t.BorderOpacity };
        ToastBorder.BorderBrush = _showBorder ? _appliedBorderBrush : Brushes.Transparent;

        if (ToastBorder.Effect is DropShadowEffect shadow)
        {
            shadow.Color      = ParseColor(t.BorderGlowEnabled ? t.BorderGlowColor : "#000000",
                                           Color.FromRgb(0x1D, 0xB9, 0x54));
            shadow.BlurRadius = t.BorderGlowEnabled ? t.GlowIntensity : 0;
            shadow.Opacity    = t.BorderGlowEnabled ? 0.55 : 0;
        }

        ApplyBackground(t);
        ApplyTextStyles(t);
        ApplyActionBorder(t);
        SetupShimmerAppearance(t);

        // Sync layer visibility flags
        SetLayerVisibility(_showBackground, _showBorder, _showText, _showShimmer);
    }

    private void ApplyBackground(DesignerTheme t)
    {
        Color c1 = ParseColor(t.BackgroundColor1, Color.FromRgb(0x1c, 0x27, 0x48));
        Color c2 = ParseColor(t.BackgroundColor2, Color.FromRgb(0x11, 0x18, 0x32));

        _grainActive = false;
        GrainOverlay.Visibility = Visibility.Collapsed;

        switch (t.BackgroundEffect)
        {
            case "Solid":
                BgLayer.Background = new SolidColorBrush(c1) { Opacity = t.EffectOpacity };
                break;

            case "Grain":
            {
                BgLayer.Background = new SolidColorBrush(c1);
                Color tint = ParseColor(t.GrainTintColor, Colors.White);
                GrainOverlay.Background = NoiseHelper.GetNoiseBrush(t.GrainScale, tint, t.GrainIntensity * t.EffectOpacity);
                _grainActive = true;
                GrainOverlay.Visibility = _showBackground ? Visibility.Visible : Visibility.Collapsed;
                break;
            }

            case "Image":
                if (!string.IsNullOrWhiteSpace(t.BackgroundImagePath) && File.Exists(t.BackgroundImagePath))
                {
                    try
                    {
                        var bmp = new BitmapImage(new Uri(t.BackgroundImagePath));
                        BgLayer.Background = new ImageBrush(bmp)
                        {
                            Stretch   = Stretch.UniformToFill,
                            TileMode  = TileMode.None,
                            Opacity   = t.EffectOpacity,
                        };
                    }
                    catch { BgLayer.Background = new SolidColorBrush(c1); }
                }
                else
                {
                    BgLayer.Background = BuildCheckerBrush();
                }
                break;

            default: // Gradient
                BgLayer.Background = BuildGradient(c1, c2, t) ;
                break;
        }
    }

    private static Brush BuildGradient(Color c1, Color c2, DesignerTheme t)
    {
        if (t.GradientIsRadial)
        {
            return new RadialGradientBrush
            {
                Center         = new Point(t.RadialCenterX, t.RadialCenterY),
                GradientOrigin = new Point(t.RadialCenterX, t.RadialCenterY),
                RadiusX        = t.RadialRadiusX,
                RadiusY        = t.RadialRadiusY,
                GradientStops  = BuildSharpStops(c1, c2, t.GradientSharpness),
                Opacity        = t.EffectOpacity,
            };
        }

        // Linear gradient with angle
        double rad  = t.GradientAngle * Math.PI / 180;
        double dx   = Math.Cos(rad);
        double dy   = Math.Sin(rad);
        var startPt = new Point(0.5 - dx * 0.5, 0.5 - dy * 0.5);
        var endPt   = new Point(0.5 + dx * 0.5, 0.5 + dy * 0.5);

        return new LinearGradientBrush
        {
            StartPoint    = startPt,
            EndPoint      = endPt,
            GradientStops = BuildSharpStops(c1, c2, t.GradientSharpness),
            Opacity       = t.EffectOpacity,
        };
    }

    private static GradientStopCollection BuildSharpStops(Color c1, Color c2, double sharpness)
    {
        // sharpness 0 = soft gradient; 1 = near-hard line at centre
        double edge = sharpness * 0.499;
        return new GradientStopCollection
        {
            new(c1, 0.0),
            new(c1, edge),
            new(c2, 1.0 - edge),
            new(c2, 1.0),
        };
    }

    private static ImageBrush BuildCheckerBrush()
    {
        const int sz = 16;
        var bmp    = new WriteableBitmap(sz * 2, sz * 2, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[sz * 2 * sz * 2 * 4];
        for (int y = 0; y < sz * 2; y++)
        for (int x = 0; x < sz * 2; x++)
        {
            bool light = (x / sz + y / sz) % 2 == 0;
            int  i     = (y * sz * 2 + x) * 4;
            byte v     = light ? (byte)0x44 : (byte)0x22;
            pixels[i] = pixels[i + 1] = pixels[i + 2] = v;
            pixels[i + 3] = 0xFF;
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
        TrackText.Foreground  = new SolidColorBrush(ParseColor(t.MessageTextColor, Colors.White))
                                    { Opacity = t.TrackTextOpacity };
        ArtistText.Foreground = new SolidColorBrush(ParseColor(t.ArtistTextColor,  Color.FromRgb(0x1D, 0xB9, 0x54)))
                                    { Opacity = t.ArtistTextOpacity };
        AlbumText.Foreground  = new SolidColorBrush(ParseColor(t.AlbumTextColor,   Color.FromRgb(0xA0, 0xA0, 0xB0)))
                                    { Opacity = t.AlbumTextOpacity };

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
                BorderTraceRect.Visibility      = Visibility.Visible;
                BorderTraceRect.Stroke          = new SolidColorBrush(bc);
                BorderTraceRect.StrokeThickness = t.BorderThickness + 0.5;
                break;

            case "Orbiting Spark":
                BorderTraceRect.Visibility      = Visibility.Visible;
                BorderTraceRect.Stroke          = new SolidColorBrush(bc);
                BorderTraceRect.StrokeThickness = t.BorderThickness + 0.5;
                break;

            case "Bouncing Edge":
                TopEdgeRect.Visibility = Visibility.Visible;
                break;
        }
    }

    // ── Shimmer appearance (static setup) ──────────────────────────────────────

    private void SetupShimmerAppearance(DesignerTheme t)
    {
        Color sc    = ParseColor(t.ShimmerColor, Colors.White);
        byte  alpha = (byte)Math.Round((1.0 - t.ShimmerTransparency) * 255);
        var   fill  = new SolidColorBrush(Color.FromArgb(alpha, sc.R, sc.G, sc.B));
        ShimmerShapePath.Fill = fill;

        ShimmerShapePath.Data = BuildShapeGeometry(t.ShimmerShape);
        ShimmerRotTx.Angle    = t.ShimmerRotation;

        if (t.ShimmerBlur > 0.5)
            ShimmerContainer.Effect = new BlurEffect { Radius = t.ShimmerBlur };
        else
            ShimmerContainer.Effect = null;
    }

    // ── Animations ──────────────────────────────────────────────────────────────

    private void StartShimmer()
    {
        double cw = ShimmerCanvas.ActualWidth  > 0 ? ShimmerCanvas.ActualWidth  : 400;
        double ch = ShimmerCanvas.ActualHeight > 0 ? ShimmerCanvas.ActualHeight : 100;

        double sw = cw * _theme.ShimmerWidthFraction;
        double sh = ch * _theme.ShimmerHeightFraction;

        ShimmerContainer.Width  = sw;
        ShimmerContainer.Height = sh;

        double centreX = (cw - sw) / 2;
        double centreY = (ch - sh) / 2;
        double speed   = Math.Max(0.1, _theme.ShimmerSpeed);
        var    dur     = TimeSpan.FromSeconds(speed);

        // Stop any prior movement
        ShimmerContainer.BeginAnimation(Canvas.LeftProperty, null);
        ShimmerContainer.BeginAnimation(Canvas.TopProperty,  null);

        switch (_theme.ShimmerDirectionPreset)
        {
            case "Left to Right":
                Canvas.SetLeft(ShimmerContainer, -sw);
                Canvas.SetTop(ShimmerContainer,  centreY);
                ShimmerContainer.BeginAnimation(Canvas.LeftProperty,
                    new DoubleAnimation(-sw, cw, dur) { RepeatBehavior = RepeatBehavior.Forever });
                break;

            case "Right to Left":
                Canvas.SetLeft(ShimmerContainer, cw);
                Canvas.SetTop(ShimmerContainer,  centreY);
                ShimmerContainer.BeginAnimation(Canvas.LeftProperty,
                    new DoubleAnimation(cw, -sw, dur) { RepeatBehavior = RepeatBehavior.Forever });
                break;

            case "Top to Bottom":
                Canvas.SetLeft(ShimmerContainer, centreX);
                Canvas.SetTop(ShimmerContainer,  -sh);
                ShimmerContainer.BeginAnimation(Canvas.TopProperty,
                    new DoubleAnimation(-sh, ch, dur) { RepeatBehavior = RepeatBehavior.Forever });
                break;

            case "Bottom to Top":
                Canvas.SetLeft(ShimmerContainer, centreX);
                Canvas.SetTop(ShimmerContainer,  ch);
                ShimmerContainer.BeginAnimation(Canvas.TopProperty,
                    new DoubleAnimation(ch, -sh, dur) { RepeatBehavior = RepeatBehavior.Forever });
                break;

            case "Static":
                Canvas.SetLeft(ShimmerContainer, centreX);
                Canvas.SetTop(ShimmerContainer,  centreY);
                break;

            case "Custom":
            {
                double rad  = _theme.ShimmerDirectionAngle * Math.PI / 180;
                double dx   = Math.Cos(rad);
                double dy   = Math.Sin(rad);
                double dist = Math.Sqrt(cw * cw + ch * ch) + Math.Max(sw, sh);

                Canvas.SetLeft(ShimmerContainer, centreX - dx * dist);
                Canvas.SetTop(ShimmerContainer,  centreY - dy * dist);

                ShimmerContainer.BeginAnimation(Canvas.LeftProperty,
                    new DoubleAnimation(centreX - dx * dist, centreX + dx * dist, dur)
                        { RepeatBehavior = RepeatBehavior.Forever });
                ShimmerContainer.BeginAnimation(Canvas.TopProperty,
                    new DoubleAnimation(centreY - dy * dist, centreY + dy * dist, dur)
                        { RepeatBehavior = RepeatBehavior.Forever });
                break;
            }

            default:
                Canvas.SetLeft(ShimmerContainer, centreX);
                Canvas.SetTop(ShimmerContainer,  centreY);
                break;
        }
    }

    private void StartActionBorderAnimation()
    {
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
                double perimeter = ActualWidth > 0 ? 2 * (ActualWidth + ActualHeight) : 760;
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
                double perimeter = ActualWidth > 0 ? 2 * (ActualWidth + ActualHeight) : 760;
                double sparkLen  = Math.Min(50, perimeter * 0.12);
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
                Color bc        = ParseColor(_theme.ActionBorderColor, Color.FromRgb(0x1D, 0xB9, 0x54));
                const double h  = 0.3;
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
                AnimateStop(bounceBrush.GradientStops[0], -h * 2,      1.0,        dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[1], 0.0 - h,     1.0,        dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[2], 0.0 + h,     1.0 + h,    dur, TimeSpan.Zero, ease, autoReverse: true);
                AnimateStop(bounceBrush.GradientStops[3], 0.0 + h * 2, 1.0 + h * 2, dur, TimeSpan.Zero, ease, autoReverse: true);
                break;
            }
        }
    }

    // ── Shape geometry ──────────────────────────────────────────────────────────

    private static Geometry BuildShapeGeometry(string shape) => shape switch
    {
        "Circle"   => new EllipseGeometry(new Point(50, 50), 50, 50),
        "Triangle" => BuildTriangle(),
        "Star"     => BuildStar(),
        _          => new RectangleGeometry(new Rect(0, 0, 100, 100)),
    };

    private static PathGeometry BuildTriangle()
    {
        var fig = new PathFigure(new Point(50, 0),
        [
            new LineSegment(new Point(100, 100), true),
            new LineSegment(new Point(0,   100), true),
        ], closed: true);
        return new PathGeometry([fig]);
    }

    private static PathGeometry BuildStar()
    {
        var pts = new List<Point>();
        for (int i = 0; i < 10; i++)
        {
            double angle = (i * 36 - 90) * Math.PI / 180;
            double r     = i % 2 == 0 ? 50 : 20;
            pts.Add(new Point(50 + r * Math.Cos(angle), 50 + r * Math.Sin(angle)));
        }
        var fig = new PathFigure(pts[0],
            pts.Skip(1).Select(p => (PathSegment)new LineSegment(p, true)), closed: true);
        return new PathGeometry([fig]);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

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

    private static FontWeight ParseFontWeight(string name) => name switch
    {
        "Thin"      => FontWeights.Thin,
        "Light"     => FontWeights.Light,
        "Regular"   => FontWeights.Regular,
        "Medium"    => FontWeights.Medium,
        "SemiBold"  => FontWeights.SemiBold,
        "Bold"      => FontWeights.Bold,
        "ExtraBold" => FontWeights.ExtraBold,
        "Black"     => FontWeights.Black,
        _           => FontWeights.Normal,
    };

    private void SetNoArt()
    {
        AlbumArt.Source     = null;
        AlbumArt.Visibility = Visibility.Collapsed;
        ArtGap.Visibility   = Visibility.Collapsed;
    }
}
