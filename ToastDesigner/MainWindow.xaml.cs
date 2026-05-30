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
        BgColor1Picker.ColorChanged     += (_, _) => OnThemeChanged();
        BgColor2Picker.ColorChanged     += (_, _) => OnThemeChanged();
        BorderColorPicker.ColorChanged  += (_, _) => OnThemeChanged();
        GlowColorPicker.ColorChanged    += (_, _) => OnThemeChanged();
        ShadowColorPicker.ColorChanged  += (_, _) => OnThemeChanged();
        TrackColorPicker.ColorChanged   += (_, _) => OnThemeChanged();
        ArtistColorPicker.ColorChanged  += (_, _) => OnThemeChanged();
        AlbumColorPicker.ColorChanged   += (_, _) => OnThemeChanged();

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
    }

    private void ReadControlsIntoTheme()
    {
        // Background
        _theme.BackgroundEffect    = ComboText(BgEffectCombo);
        _theme.BackgroundColor1    = BgColor1Picker.HexValue;
        _theme.BackgroundColor2    = BgColor2Picker.HexValue;
        _theme.BackgroundImagePath = BgImagePathBox.Text;
        _theme.BackgroundOpacity   = BgOpacitySlider.Value;
        _theme.ShimmerEffect       = ComboText(ShimmerCombo);

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
    }

    private void LoadThemeIntoControls(DesignerTheme t)
    {
        _loading = true;
        try
        {
            // Background
            SelectComboItem(BgEffectCombo, t.BackgroundEffect);
            BgColor1Picker.HexValue  = t.BackgroundColor1;
            BgColor2Picker.HexValue  = t.BackgroundColor2;
            BgImagePathBox.Text      = t.BackgroundImagePath;
            BgOpacitySlider.Value    = t.BackgroundOpacity;
            SelectComboItem(ShimmerCombo, t.ShimmerEffect);

            // Border
            SelectComboItem(BorderAnimCombo, t.ActionBorderType);
            BorderColorPicker.HexValue    = t.ActionBorderColor;
            BorderThicknessSlider.Value   = t.BorderThickness;
            CornerRadiusSlider.Value      = t.CornerRadius;
            BorderOpacitySlider.Value     = t.BorderOpacity;
            GlowCheck.IsChecked          = t.BorderGlowEnabled;
            GlowColorPicker.HexValue     = t.BorderGlowColor;
            GlowIntensitySlider.Value    = t.GlowIntensity;
            ShadowColorPicker.HexValue   = t.GlowColor;

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

            // Refresh dependent visibility
            RefreshBgImageRowVisibility();
            RefreshGlowControlsEnabled();
            RefreshSliderValueLabels();
        }
        finally { _loading = false; }
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void BgEffectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshBgImageRowVisibility();
        OnThemeChanged();
    }

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => OnThemeChanged();

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshSliderValueLabels();
        OnThemeChanged();
    }

    private void TextBox_Changed(object sender, TextChangedEventArgs e)
        => OnThemeChanged();

    private void SampleData_Changed(object sender, TextChangedEventArgs e)
        => UpdatePreview();

    private void GlowCheck_Changed(object sender, RoutedEventArgs e)
    {
        RefreshGlowControlsEnabled();
        OnThemeChanged();
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
        if (dlg.ShowDialog() == true)
        {
            BgImagePathBox.Text    = dlg.FileName;
            _theme.BackgroundImagePath = dlg.FileName;
            OnThemeChanged();
        }
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
            var bmp = new BitmapImage(new Uri(dlg.FileName));
            SampleArtThumb.Source = bmp;
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

    private void RefreshBgImageRowVisibility()
    {
        bool isImage = ComboText(BgEffectCombo) == "Image";
        BgImageRow.Visibility   = isImage ? Visibility.Visible : Visibility.Collapsed;
        BgImageLabel.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshGlowControlsEnabled()
    {
        bool on = GlowCheck.IsChecked == true;
        GlowColorPicker.IsEnabled    = on;
        GlowIntensitySlider.IsEnabled = on;
    }

    private void RefreshSliderValueLabels()
    {
        BgOpacityValue.Text         = $"{BgOpacitySlider.Value:F2}";
        BorderThicknessValue.Text   = $"{BorderThicknessSlider.Value:F1}";
        CornerRadiusValue.Text      = $"{CornerRadiusSlider.Value:F0}";
        BorderOpacityValue.Text     = $"{BorderOpacitySlider.Value:F2}";
        GlowIntensityValue.Text     = $"{GlowIntensitySlider.Value:F0}";
        TrackSizeValue.Text         = $"{TrackSizeSlider.Value:F1}";
        TrackOpacityValue.Text      = $"{TrackOpacitySlider.Value:F2}";
        ArtistSizeValue.Text        = $"{ArtistSizeSlider.Value:F1}";
        ArtistOpacityValue.Text     = $"{ArtistOpacitySlider.Value:F2}";
        AlbumSizeValue.Text         = $"{AlbumSizeSlider.Value:F1}";
        AlbumOpacityValue.Text      = $"{AlbumOpacitySlider.Value:F2}";
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
