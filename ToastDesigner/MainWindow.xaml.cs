using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ToastDesigner.Models;

namespace ToastDesigner;

public partial class MainWindow : Window
{
    private DesignerTheme _theme    = new();
    private bool          _loading  = false;
    private bool          _animating = true;
    private byte[]?       _sampleArtBytes;

    public MainWindow() => InitializeComponent();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DwmHelper.SetTitleBar(this);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Wire up ColorPickerBox change events
        BgColor1Picker.ColorChanged       += (_, _) => OnThemeChanged();
        BgColor2Picker.ColorChanged       += (_, _) => OnThemeChanged();
        BorderColorPicker.ColorChanged    += (_, _) => OnThemeChanged();
        GlowColorPicker.ColorChanged      += (_, _) => OnThemeChanged();
        ShadowColorPicker.ColorChanged    += (_, _) => OnThemeChanged();
        TrackColorPicker.ColorChanged     += (_, _) => OnThemeChanged();
        ArtistColorPicker.ColorChanged    += (_, _) => OnThemeChanged();
        AlbumColorPicker.ColorChanged     += (_, _) => OnThemeChanged();
        GrainTintPicker.ColorChanged      += (_, _) => OnThemeChanged();
        ShimmerColorPicker.ColorChanged   += (_, _) => OnThemeChanged();

        LoadThemeIntoControls(_theme);
        UpdatePreview();
    }

    // ── Core update pipeline ─────────────────────────────────────────────────

    private void OnThemeChanged()
    {
        if (_loading) return;
        ReadControlsIntoTheme();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Preview.SetSampleData(
            SampleTrackBox.Text,
            SampleArtistBox.Text,
            SampleAlbumBox.Text,
            _sampleArtBytes);
        Preview.ApplyTheme(_theme, _animating);
        Preview.SetLayerVisibility(
            ShowBgCheck.IsChecked     == true,
            ShowBorderCheck.IsChecked == true,
            ShowTextCheck.IsChecked   == true,
            ShowShimmerCheck.IsChecked == true);
    }

    private void ReadControlsIntoTheme()
    {
        // Background
        _theme.BackgroundEffect    = ComboText(BgEffectCombo);
        _theme.BackgroundColor1    = BgColor1Picker.HexValue;
        _theme.BackgroundColor2    = BgColor2Picker.HexValue;
        _theme.BackgroundImagePath = BgImagePathBox.Text;
        _theme.EffectOpacity       = EffectOpacitySlider.Value;
        _theme.BackgroundOpacity   = BgOpacitySlider.Value;
        _theme.GradientAngle       = GradAngleSlider.Value;
        _theme.GradientSharpness   = GradSharpSlider.Value;
        _theme.GradientIsRadial    = RadialCheck.IsChecked == true;
        _theme.RadialCenterX       = RadCXSlider.Value;
        _theme.RadialCenterY       = RadCYSlider.Value;
        _theme.RadialRadiusX       = RadRXSlider.Value;
        _theme.RadialRadiusY       = RadRYSlider.Value;
        _theme.GrainTintColor      = GrainTintPicker.HexValue;
        _theme.GrainScale          = GrainScaleSlider.Value;
        _theme.GrainIntensity      = GrainIntensitySlider.Value;

        // Border
        _theme.ActionBorderType  = ComboText(BorderAnimCombo);
        _theme.ActionBorderColor = BorderColorPicker.HexValue;
        _theme.BorderThickness   = BorderThicknessSlider.Value;
        _theme.CornerRadius      = CornerRadiusSlider.Value;
        _theme.BorderOpacity     = BorderOpacitySlider.Value;
        _theme.BorderGlowEnabled = GlowCheck.IsChecked == true;
        _theme.BorderGlowColor   = GlowColorPicker.HexValue;
        _theme.GlowIntensity     = GlowIntensitySlider.Value;
        _theme.GlowColor         = ShadowColorPicker.HexValue;

        // Text — Track
        _theme.MessageTextColor = TrackColorPicker.HexValue;
        _theme.TrackFontSize    = TrackSizeSlider.Value;
        _theme.TrackFontWeight  = ComboText(TrackWeightCombo);
        _theme.TrackPrefix      = TrackPrefixBox.Text;
        _theme.TrackSuffix      = TrackSuffixBox.Text;
        _theme.TrackTextOpacity = TrackOpacitySlider.Value;

        // Text — Artist
        _theme.ArtistTextColor   = ArtistColorPicker.HexValue;
        _theme.ArtistFontSize    = ArtistSizeSlider.Value;
        _theme.ArtistFontWeight  = ComboText(ArtistWeightCombo);
        _theme.ArtistPrefix      = ArtistPrefixBox.Text;
        _theme.ArtistSuffix      = ArtistSuffixBox.Text;
        _theme.ArtistTextOpacity = ArtistOpacitySlider.Value;

        // Text — Album
        _theme.AlbumTextColor   = AlbumColorPicker.HexValue;
        _theme.AlbumFontSize    = AlbumSizeSlider.Value;
        _theme.AlbumFontWeight  = ComboText(AlbumWeightCombo);
        _theme.AlbumPrefix      = AlbumPrefixBox.Text;
        _theme.AlbumSuffix      = AlbumSuffixBox.Text;
        _theme.AlbumTextOpacity = AlbumOpacitySlider.Value;

        // Shimmer
        _theme.ShimmerShape           = ComboText(ShimmerShapeCombo);
        _theme.ShimmerWidthFraction   = ShimmerWidthSlider.Value;
        _theme.ShimmerHeightFraction  = ShimmerHeightSlider.Value;
        _theme.ShimmerColor           = ShimmerColorPicker.HexValue;
        _theme.ShimmerTransparency    = ShimmerTransparencySlider.Value;
        _theme.ShimmerBlur            = ShimmerBlurSlider.Value;
        _theme.ShimmerRotation        = ShimmerRotationSlider.Value;
        _theme.ShimmerDirectionPreset = ComboText(ShimmerDirectionCombo);
        _theme.ShimmerDirectionAngle  = ShimmerDirAngleSlider.Value;
        _theme.ShimmerSpeed           = ShimmerSpeedSlider.Value;
        _theme.ToastDuration          = ToastDurationSlider.Value;
    }

    private void LoadThemeIntoControls(DesignerTheme t)
    {
        _loading = true;
        try
        {
            // Background
            SelectComboItem(BgEffectCombo, t.BackgroundEffect);
            BgColor1Picker.HexValue    = t.BackgroundColor1;
            BgColor2Picker.HexValue    = t.BackgroundColor2;
            BgImagePathBox.Text        = t.BackgroundImagePath;
            EffectOpacitySlider.Value  = t.EffectOpacity;
            BgOpacitySlider.Value      = t.BackgroundOpacity;
            GradAngleSlider.Value      = t.GradientAngle;
            GradSharpSlider.Value      = t.GradientSharpness;
            RadialCheck.IsChecked      = t.GradientIsRadial;
            RadCXSlider.Value          = t.RadialCenterX;
            RadCYSlider.Value          = t.RadialCenterY;
            RadRXSlider.Value          = t.RadialRadiusX;
            RadRYSlider.Value          = t.RadialRadiusY;
            GrainTintPicker.HexValue   = t.GrainTintColor;
            GrainScaleSlider.Value     = t.GrainScale;
            GrainIntensitySlider.Value = t.GrainIntensity;

            // Border
            SelectComboItem(BorderAnimCombo, t.ActionBorderType);
            BorderColorPicker.HexValue    = t.ActionBorderColor;
            BorderThicknessSlider.Value   = t.BorderThickness;
            CornerRadiusSlider.Value      = t.CornerRadius;
            BorderOpacitySlider.Value     = t.BorderOpacity;
            GlowCheck.IsChecked           = t.BorderGlowEnabled;
            GlowColorPicker.HexValue      = t.BorderGlowColor;
            GlowIntensitySlider.Value     = t.GlowIntensity;
            ShadowColorPicker.HexValue    = t.GlowColor;

            // Text — Track
            TrackColorPicker.HexValue  = t.MessageTextColor;
            TrackSizeSlider.Value      = t.TrackFontSize;
            SelectComboItem(TrackWeightCombo, t.TrackFontWeight);
            TrackPrefixBox.Text        = t.TrackPrefix;
            TrackSuffixBox.Text        = t.TrackSuffix;
            TrackOpacitySlider.Value   = t.TrackTextOpacity;

            // Text — Artist
            ArtistColorPicker.HexValue  = t.ArtistTextColor;
            ArtistSizeSlider.Value      = t.ArtistFontSize;
            SelectComboItem(ArtistWeightCombo, t.ArtistFontWeight);
            ArtistPrefixBox.Text        = t.ArtistPrefix;
            ArtistSuffixBox.Text        = t.ArtistSuffix;
            ArtistOpacitySlider.Value   = t.ArtistTextOpacity;

            // Text — Album
            AlbumColorPicker.HexValue  = t.AlbumTextColor;
            AlbumSizeSlider.Value      = t.AlbumFontSize;
            SelectComboItem(AlbumWeightCombo, t.AlbumFontWeight);
            AlbumPrefixBox.Text        = t.AlbumPrefix;
            AlbumSuffixBox.Text        = t.AlbumSuffix;
            AlbumOpacitySlider.Value   = t.AlbumTextOpacity;

            // Shimmer
            SelectComboItem(ShimmerShapeCombo, t.ShimmerShape);
            ShimmerWidthSlider.Value        = t.ShimmerWidthFraction;
            ShimmerHeightSlider.Value       = t.ShimmerHeightFraction;
            ShimmerColorPicker.HexValue     = t.ShimmerColor;
            ShimmerTransparencySlider.Value = t.ShimmerTransparency;
            ShimmerBlurSlider.Value         = t.ShimmerBlur;
            ShimmerRotationSlider.Value     = t.ShimmerRotation;
            SelectComboItem(ShimmerDirectionCombo, t.ShimmerDirectionPreset);
            ShimmerDirAngleSlider.Value     = t.ShimmerDirectionAngle;
            ShimmerSpeedSlider.Value        = t.ShimmerSpeed;
            ToastDurationSlider.Value       = t.ToastDuration;

            RefreshBgControlVisibility();
            RefreshGlowControlsEnabled();
            RefreshRadialPanelVisibility();
            RefreshCustomDirPanelVisibility();
            RefreshSliderValueLabels();
        }
        finally { _loading = false; }
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void BgEffectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshBgControlVisibility();
        OnThemeChanged();
    }

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        OnThemeChanged();
    }

    private void ShimmerDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshCustomDirPanelVisibility();
        OnThemeChanged();
    }

    private void RadialCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshRadialPanelVisibility();
        OnThemeChanged();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        RefreshSliderValueLabels();
        OnThemeChanged();
    }

    private void TextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        OnThemeChanged();
    }

    private void SampleData_Changed(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        UpdatePreview();
    }

    private void GlowCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshGlowControlsEnabled();
        OnThemeChanged();
    }

    private void LayerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        Preview.SetLayerVisibility(
            ShowBgCheck.IsChecked      == true,
            ShowBorderCheck.IsChecked  == true,
            ShowTextCheck.IsChecked    == true,
            ShowShimmerCheck.IsChecked == true);
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        _animating = !_animating;
        PlayPauseBtn.Content = _animating ? "⏸ Pause" : "▶ Play";
        if (_animating)
            Preview.StartAnimations();
        else
            Preview.StopAnimations();
    }

    // ── File browse / image handlers ─────────────────────────────────────────

    private void BrowseBgImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select background image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*",
        };
        if (dlg.ShowDialog() != true) return;
        BgImagePathBox.Text        = dlg.FileName;
        _theme.BackgroundImagePath = dlg.FileName;
        OnThemeChanged();
    }

    private void BrowseArt_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select album art",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files|*.*",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _sampleArtBytes = File.ReadAllBytes(dlg.FileName);
            SampleArtThumb.Source = new BitmapImage(new Uri(dlg.FileName));
        }
        catch { _sampleArtBytes = null; }

        UpdatePreview();
    }

    private void ClearArt_Click(object sender, RoutedEventArgs e)
    {
        _sampleArtBytes       = null;
        SampleArtThumb.Source = null;
        UpdatePreview();
    }

    // ── Footer button handlers ───────────────────────────────────────────────

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        ReadControlsIntoTheme();
        Clipboard.SetText(_theme.ExportToMasherJson());
        ExportBtn.Content = "✓ Copied!";
        var timer = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) => { ExportBtn.Content = "Copy Masher JSON"; timer.Stop(); };
        timer.Start();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ReadControlsIntoTheme();
        var dlg = new SaveFileDialog
        {
            Title      = "Save theme",
            Filter     = "Theme JSON|*.json|All files|*.*",
            DefaultExt = ".json",
            FileName   = "my-toast-theme.json",
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, _theme.ToJson());
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Load theme",
            Filter = "Theme JSON|*.json|All files|*.*",
        };
        if (dlg.ShowDialog() != true) return;

        var loaded = DesignerTheme.FromJson(File.ReadAllText(dlg.FileName));
        if (loaded is null)
        {
            MessageBox.Show("Could not parse the theme file.", "Toast Designer",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _theme = loaded;
        LoadThemeIntoControls(_theme);
        UpdatePreview();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _theme = new DesignerTheme();
        LoadThemeIntoControls(_theme);
        UpdatePreview();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RefreshBgControlVisibility()
    {
        string effect = ComboText(BgEffectCombo);

        bool isGradient = effect == "Gradient";
        bool isGrain    = effect == "Grain";
        bool isImage    = effect == "Image";

        // Color 2 only needed for Gradient
        Color2Row.Visibility = isGradient ? Visibility.Visible : Visibility.Collapsed;

        // Gradient angle/sharpness/radial controls
        GradientPanel.Visibility = isGradient ? Visibility.Visible : Visibility.Collapsed;

        // Grain controls
        GrainPanel.Visibility = isGrain ? Visibility.Visible : Visibility.Collapsed;

        // Image path
        ImagePathRow.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshRadialPanelVisibility()
    {
        bool radial = RadialCheck.IsChecked == true;
        RadialPanel.Visibility = radial ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshCustomDirPanelVisibility()
    {
        bool custom = ComboText(ShimmerDirectionCombo) == "Custom";
        CustomDirLabel.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        CustomDirPanel.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshGlowControlsEnabled()
    {
        bool on = GlowCheck.IsChecked == true;
        GlowColorPicker.IsEnabled    = on;
        GlowIntensitySlider.IsEnabled = on;
    }

    private void RefreshSliderValueLabels()
    {
        // Background
        EffectOpacityValue.Text  = $"{EffectOpacitySlider.Value:F2}";
        BgOpacityValue.Text      = $"{BgOpacitySlider.Value:F2}";
        GradAngleValue.Text      = $"{GradAngleSlider.Value:F0}°";
        GradSharpValue.Text      = $"{GradSharpSlider.Value:F2}";
        RadCXValue.Text          = $"{RadCXSlider.Value:F2}";
        RadCYValue.Text          = $"{RadCYSlider.Value:F2}";
        RadRXValue.Text          = $"{RadRXSlider.Value:F2}";
        RadRYValue.Text          = $"{RadRYSlider.Value:F2}";
        GrainScaleValue.Text     = $"{GrainScaleSlider.Value:F0}";
        GrainIntensityValue.Text = $"{GrainIntensitySlider.Value:F2}";

        // Border
        BorderThicknessValue.Text = $"{BorderThicknessSlider.Value:F1}";
        CornerRadiusValue.Text    = $"{CornerRadiusSlider.Value:F0}";
        BorderOpacityValue.Text   = $"{BorderOpacitySlider.Value:F2}";
        GlowIntensityValue.Text   = $"{GlowIntensitySlider.Value:F0}";

        // Text
        TrackSizeValue.Text   = $"{TrackSizeSlider.Value:F1}";
        TrackOpacityValue.Text = $"{TrackOpacitySlider.Value:F2}";
        ArtistSizeValue.Text  = $"{ArtistSizeSlider.Value:F1}";
        ArtistOpacityValue.Text = $"{ArtistOpacitySlider.Value:F2}";
        AlbumSizeValue.Text   = $"{AlbumSizeSlider.Value:F1}";
        AlbumOpacityValue.Text = $"{AlbumOpacitySlider.Value:F2}";

        // Shimmer
        ShimmerWidthValue.Text        = $"{ShimmerWidthSlider.Value:F2}";
        ShimmerHeightValue.Text       = $"{ShimmerHeightSlider.Value:F2}";
        ShimmerTransparencyValue.Text = $"{ShimmerTransparencySlider.Value:F2}";
        ShimmerBlurValue.Text         = $"{ShimmerBlurSlider.Value:F0}";
        ShimmerRotationValue.Text     = $"{ShimmerRotationSlider.Value:F0}°";
        ShimmerDirAngleValue.Text     = $"{ShimmerDirAngleSlider.Value:F0}°";
        ShimmerSpeedValue.Text        = $"{ShimmerSpeedSlider.Value:F1}s";
        ToastDurationValue.Text       = $"{ToastDurationSlider.Value:F1}s";
    }

    private static string ComboText(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item)
            return item.Content?.ToString() ?? string.Empty;
        return combo.SelectedItem?.ToString() ?? string.Empty;
    }

    private static void SelectComboItem(ComboBox combo, string value)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (item.Content?.ToString() == value)
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }
}
