using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SpotifyMasher.Models;
using SpotifyMasher.Services;

namespace SpotifyMasher;

public partial class MainWindow : Window
{
    public static IReadOnlyList<string> AvailableActions { get; } =
    [
        "Play / Pause",
        "Change Volume",     // Parameter: +5 or -5 (percent)
        "Next Track",
        "Previous Track",
        "Seek",              // Parameter: +10 or -10 (seconds)
        "Add to Liked",
        "Add to Playlist",   // Parameter: playlist ID from Spotify URL
        "Show Current Track",
    ];

    public static IReadOnlyList<string> AvailableCorners { get; } =
    [
        "bottom-right",
        "bottom-left",
        "top-right",
        "top-left",
    ];

    public static IReadOnlyList<string> AvailableBackgroundEffects { get; } =
    [
        "Gradient",
        "Solid",
        "Radial Glow",
        "Grain",
    ];

    public static IReadOnlyList<string> AvailableActionBorderTypes { get; } =
    [
        "Bottom Bar Drain",
        "Fill from Centre",
        "Full Border Trace",
        "Orbiting Spark",
        "Bouncing Edge",
        "Aurora Glow",
        "None",
    ];

    public static IReadOnlyList<string> AvailableShimmerEffects { get; } =
    [
        "Diagonal",
        "Horizontal",
        "Pulse",
        "Static Gloss",
        "Slide-Through",
        "Spotlight",
        "Breathing",
        "None",
    ];

    public static IReadOnlyList<string> AvailablePresets { get; } =
        [.. Models.ToastPresets.Names, Models.ToastPresets.CustomName];

    private readonly ObservableCollection<HotkeyBinding> _bindings = [];
    private readonly ObservableCollection<ProcessToastRule> _processRules = [];
    private bool _isAuthenticated;
    private double? _pendingPinnedX, _pendingPinnedY;
    private bool _loadingSettings;
    private bool _loadingStyleSettings;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            _isAuthenticated = value;
            UpdateAuthUi();
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        HotkeyGrid.ItemsSource = _bindings;
        ProcessRuleGrid.ItemsSource = _processRules;

        AppLogger.LineAdded += line => Dispatcher.InvokeAsync(() =>
        {
            LogBox.AppendText(line + "\n");
            LogBox.ScrollToEnd();
        });

        // Use AddHandler with handledEventsToo=true so the debug toggle key fires
        // even when a child element (DataGrid, TextBox) has consumed the event.
        AddHandler(UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(HandleGlobalKey), handledEventsToo: true);

        AppLogger.Log("App started");

        var config = App.ConfigService.Load();
        if (!string.IsNullOrEmpty(config.ClientId))
            ClientIdBox.Text = config.ClientId;

        foreach (var b in config.Bindings)
            _bindings.Add(b);

        LoadNotificationSettings(config.ToastSettings);
        LoadStyleSettings(config.ToastSettings.Theme);
        SyncStyleButtonEnabled();

        AppLogger.Log($"Config loaded: ClientId={(!string.IsNullOrEmpty(config.ClientId) ? "set" : "empty")} Bindings={config.Bindings.Count}");

        IsAuthenticated = App.AuthService.IsAuthenticated;
        AppLogger.Log($"Auth state on startup: {(IsAuthenticated ? "authenticated" : "not authenticated")}");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DwmHelper.SetGreenTitleBar(this);
    }

    private void HandleGlobalKey(object sender, KeyEventArgs e)
    {
        // Ctrl+Shift+` (backtick/grave, Key.OemTilde, VK_OEM_3) toggles the debug log.
        // Use HasFlag so CapsLock or NumLock don't break the check.
        bool ctrl  = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        bool tilde = e.Key == Key.OemTilde;

        if (ctrl && shift && tilde)
        {
            DebugSection.Visibility = DebugSection.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            e.Handled = true;
        }
    }

    private void UpdateAuthUi()
    {
        if (_isAuthenticated)
        {
            AuthConnected.Visibility = Visibility.Visible;
            AuthDisconnected.Visibility = Visibility.Collapsed;
            AuthFormSection.Visibility = Visibility.Collapsed;
        }
        else
        {
            AuthConnected.Visibility = Visibility.Collapsed;
            AuthDisconnected.Visibility = Visibility.Visible;
            AuthFormSection.Visibility = Visibility.Visible;
        }
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        AuthFormSection.Visibility = Visibility.Visible;
        ClientIdBox.Focus();
    }

    private async void AuthButton_Click(object sender, RoutedEventArgs e)
    {
        var clientId = ClientIdBox.Text.Trim();
        if (string.IsNullOrEmpty(clientId))
        {
            AuthStatusText.Text = "Please enter a Client ID first.";
            return;
        }

        AuthButton.IsEnabled = false;
        AuthStatusText.Text = "Opening browser — please authorise in Spotify, then return here…";
        AppLogger.Log($"Starting auth for ClientId={clientId[..Math.Min(8, clientId.Length)]}…");

        var config = App.ConfigService.Load();
        config.ClientId = clientId;
        App.ConfigService.Save(config);

        var success = await App.AuthService.StartAuthAsync(clientId);
        AppLogger.Log($"Auth result: {(success ? "success" : "failed")}");

        AuthButton.IsEnabled = true;
        IsAuthenticated = success;

        if (!success)
            AuthStatusText.Text = "Authorisation failed or timed out. Please try again.";
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        var tokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyMasher", "tokens.json");

        if (File.Exists(tokenPath))
            File.Delete(tokenPath);

        App.HotkeyService.UnregisterAll();
        IsAuthenticated = false;
        AppLogger.Log("Disconnected and tokens deleted");
    }

    private void EditHotkeys_Click(object sender, RoutedEventArgs e) => SetHotkeysVisible(true);

    private void AddHotkey_Click(object sender, RoutedEventArgs e)
    {
        _bindings.Add(new HotkeyBinding());
        AppLogger.Log("Added new empty hotkey row");
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is HotkeyBinding binding)
        {
            _bindings.Remove(binding);
            AppLogger.Log($"Deleted hotkey row: {binding.KeysDisplay}");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        AppLogger.Log($"Save clicked — {_bindings.Count} row(s)");

        var config = App.ConfigService.Load();
        config.Bindings = [.. _bindings];
        App.ConfigService.Save(config);

        var failures = new List<string>();
        App.HotkeyService.RegistrationFailed += OnFail;
        App.HotkeyService.RegisterAll(_bindings);
        App.HotkeyService.RegistrationFailed -= OnFail;

        void OnFail(string msg) => failures.Add(msg);

        int active = _bindings.Count(b => b.Key != Key.None);

        if (failures.Count > 0)
            AppLogger.Log($"⚠ Could not register: {string.Join("; ", failures)}");
        else
            AppLogger.Log($"Hotkeys saved — {active} active.");

        SetHotkeysVisible(false);
    }

    private void SetHotkeysVisible(bool visible) =>
        SetActivePanel(visible ? Panel.Hotkeys : Panel.None);

    private enum Panel { None, Hotkeys, Notifications, Style }

    // Accordion: at most one panel open at a time. The other panels' entry buttons are hidden
    // while a panel is open; only the active panel's expanded (Save) buttons show.
    private void SetActivePanel(Panel panel)
    {
        HotkeySection.Visibility = panel == Panel.Hotkeys       ? Visibility.Visible : Visibility.Collapsed;
        NotifSection.Visibility  = panel == Panel.Notifications ? Visibility.Visible : Visibility.Collapsed;
        StyleSection.Visibility  = panel == Panel.Style         ? Visibility.Visible : Visibility.Collapsed;

        bool none = panel == Panel.None;
        HotkeyCollapsedButtons.Visibility = none ? Visibility.Visible : Visibility.Collapsed;
        NotifCollapsedButton.Visibility   = none ? Visibility.Visible : Visibility.Collapsed;
        StyleCollapsedButton.Visibility   = none ? Visibility.Visible : Visibility.Collapsed;

        HotkeyExpandedButtons.Visibility = panel == Panel.Hotkeys       ? Visibility.Visible : Visibility.Collapsed;
        NotifExpandedButton.Visibility   = panel == Panel.Notifications ? Visibility.Visible : Visibility.Collapsed;
        StyleExpandedButton.Visibility   = panel == Panel.Style         ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void ToggleDebugLog()
    {
        DebugSection.Visibility = DebugSection.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void DashboardLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var help = new HelpWindow { Owner = this };
        help.ShowDialog();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Clear();

    private void MinimiseToTray_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void ToggleNotifications_Click(object sender, RoutedEventArgs e)
    {
        bool open = NotifSection.Visibility == Visibility.Visible;
        SetActivePanel(open ? Panel.None : Panel.Notifications);
    }

    private void LoadNotificationSettings(Models.ToastSettings s)
    {
        _loadingSettings = true;
        NotifEnabled.IsChecked = s.Enabled;
        NotifCorner.SelectedItem = AvailableCorners.Contains(s.Corner) ? s.Corner : "bottom-right";
        NotifOffsetX.Text = s.OffsetX.ToString();
        NotifOffsetY.Text = s.OffsetY.ToString();
        NotifDuration.Text = s.DurationMs.ToString();
        NotifAlwaysOnTop.IsChecked = s.AlwaysOnTop;

        _pendingPinnedX = s.PinnedX;
        _pendingPinnedY = s.PinnedY;

        _processRules.Clear();
        foreach (var r in s.ProcessRules)
            _processRules.Add(r);

        bool freehand = s.PinnedX is not null;
        ModeCorner.IsChecked = !freehand;
        ModeFreehand.IsChecked = freehand;
        _loadingSettings = false;

        ApplyPositionMode(freehand);

        var indicatorCorner = freehand && s.PinnedX is double px && s.PinnedY is double py
            ? ComputeCornerFromPosition(px, py)
            : (AvailableCorners.Contains(s.Corner) ? s.Corner : "bottom-right");
        UpdateCornerIndicator(indicatorCorner);
    }

    private void ModeCorner_Checked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        _pendingPinnedX = null;
        _pendingPinnedY = null;
        ApplyPositionMode(freehand: false);
        if (NotifCorner.SelectedItem is string corner)
            UpdateCornerIndicator(corner);
    }

    private void ModeFreehand_Checked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        ApplyPositionMode(freehand: true);
        if (_pendingPinnedX is double px && _pendingPinnedY is double py)
            UpdateCornerIndicator(ComputeCornerFromPosition(px, py));
    }

    private void ApplyPositionMode(bool freehand)
    {
        CornerOffsetPanel.IsEnabled = !freehand;
        FreehandPanel.IsEnabled = freehand;
    }

    private void NotifCorner_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ModeCorner.IsChecked == true && NotifCorner.SelectedItem is string corner)
            UpdateCornerIndicator(corner);
    }

    private void UpdateCornerIndicator(string corner)
    {
        var active   = (System.Windows.Media.Brush)FindResource("BrushAccent");
        var inactive = (System.Windows.Media.Brush)FindResource("BrushBorder");
        CornerIndTL.Fill = corner == "top-left"     ? active : inactive;
        CornerIndTR.Fill = corner == "top-right"    ? active : inactive;
        CornerIndBL.Fill = corner == "bottom-left"  ? active : inactive;
        CornerIndBR.Fill = corner == "bottom-right" ? active : inactive;
    }

    private static string ComputeCornerFromPosition(double x, double y)
    {
        var area = System.Windows.SystemParameters.WorkArea;
        bool isLeft = x + 140 < area.Left + area.Width  / 2;
        bool isTop  = y + 30  < area.Top  + area.Height / 2;
        return $"{(isTop ? "top" : "bottom")}-{(isLeft ? "left" : "right")}";
    }

    private void SaveNotifications_Click(object sender, RoutedEventArgs e)
    {
        ToggleNotifications_Click(sender, e);

        var config = App.ConfigService.Load();
        var s = config.ToastSettings;

        s.Enabled = NotifEnabled.IsChecked == true;
        s.Corner = NotifCorner.SelectedItem?.ToString() ?? "bottom-right";
        s.OffsetX = int.TryParse(NotifOffsetX.Text, out var ox) ? ox : 20;
        s.OffsetY = int.TryParse(NotifOffsetY.Text, out var oy) ? oy : 20;
        s.DurationMs = int.TryParse(NotifDuration.Text, out var dur) ? Math.Max(500, dur) : 3000;
        s.AlwaysOnTop = NotifAlwaysOnTop.IsChecked == true;
        s.ProcessRules = [.. _processRules];
        s.PinnedX = _pendingPinnedX;
        s.PinnedY = _pendingPinnedY;

        App.ConfigService.Save(config);
        AppLogger.Log($"Notification settings saved — pinned={s.PinnedX?.ToString("F0") ?? "no"} corner={s.Corner} enabled={s.Enabled} rules={s.ProcessRules.Count}");
    }

    private void AddProcessRule_Click(object sender, RoutedEventArgs e)
    {
        _processRules.Add(new Models.ProcessToastRule());
    }

    private void DeleteProcessRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Models.ProcessToastRule rule)
            _processRules.Remove(rule);
    }

    private void SetGlobalPosition_Click(object sender, RoutedEventArgs e)
    {
        var (startLeft, startTop) = ResolvePickerStart(
            NotifCorner.SelectedItem?.ToString() ?? "bottom-right",
            int.TryParse(NotifOffsetX.Text, out var ox) ? ox : 20,
            int.TryParse(NotifOffsetY.Text, out var oy) ? oy : 20,
            _pendingPinnedX, _pendingPinnedY);

        var picker = new PositionPickerWindow(startLeft, startTop) { Owner = this };
        if (picker.ShowDialog() != true) return;

        _pendingPinnedX = picker.Result.X;
        _pendingPinnedY = picker.Result.Y;
        UpdateCornerIndicator(ComputeCornerFromPosition(_pendingPinnedX.Value, _pendingPinnedY.Value));
    }

    private void SetProcessRulePosition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Models.ProcessToastRule rule }) return;

        var (startLeft, startTop) = ResolvePickerStart(
            rule.Corner, rule.OffsetX, rule.OffsetY, rule.PinnedX, rule.PinnedY);

        var picker = new PositionPickerWindow(startLeft, startTop) { Owner = this };
        if (picker.ShowDialog() != true) return;

        rule.PinnedX = picker.Result.X;
        rule.PinnedY = picker.Result.Y;
    }

    private static (double Left, double Top) ResolvePickerStart(
        string corner, int offsetX, int offsetY, double? pinnedX, double? pinnedY)
    {
        if (pinnedX is double px && pinnedY is double py)
            return (px, py);

        const double w = 280, h = 60;
        var area = System.Windows.SystemParameters.WorkArea;
        return corner switch
        {
            "top-left"    => (area.Left + offsetX,        area.Top + offsetY),
            "top-right"   => (area.Right - w - offsetX,   area.Top + offsetY),
            "bottom-left" => (area.Left + offsetX,         area.Bottom - h - offsetY),
            _             => (area.Right - w - offsetX,   area.Bottom - h - offsetY),
        };
    }

    // ──────────────────────────────────────────────────────────────
    // Toast Style section
    // ──────────────────────────────────────────────────────────────

    private void LoadStyleSettings(Models.ToastTheme t)
    {
        _loadingStyleSettings = true;

        StylePreset.SelectedItem     = AvailablePresets.Contains(t.PresetName)
                                       ? t.PresetName : Models.ToastPresets.CustomName;
        StyleBgEffect.SelectedItem   = AvailableBackgroundEffects.Contains(t.BackgroundEffect)
                                       ? t.BackgroundEffect : "Gradient";
        StyleBgColor1.HexValue       = t.BackgroundColor1;
        StyleBgColor2.HexValue       = t.BackgroundColor2;
        StyleGlowColor.HexValue      = t.GlowColor;
        StyleMsgColor.HexValue       = t.MessageTextColor;
        StyleArtistColor.HexValue    = t.ArtistTextColor;
        StyleAlbumColor.HexValue     = t.AlbumTextColor;
        StyleBorderType.SelectedItem = AvailableActionBorderTypes.Contains(t.ActionBorderType)
                                       ? t.ActionBorderType : "Bottom Bar Drain";
        StyleBorderColor.HexValue    = t.ActionBorderColor;
        StyleShimmerEffect.SelectedItem = AvailableShimmerEffects.Contains(t.ShimmerEffect)
                                          ? t.ShimmerEffect : "Diagonal";

        // Aurora Custom editor — 4 gradient pickers + background. Seeded from this theme's palette
        // so switching a preset → Aurora Custom carries the colours over as a starting point.
        var grad = t.AuroraGradientColors;
        StyleAuroraC1.HexValue = grad.ElementAtOrDefault(0) ?? "#FFB224";
        StyleAuroraC2.HexValue = grad.ElementAtOrDefault(1) ?? "#E34BA9";
        StyleAuroraC3.HexValue = grad.ElementAtOrDefault(2) ?? "#0072F5";
        StyleAuroraC4.HexValue = grad.ElementAtOrDefault(3) ?? "#95F3D9";
        StyleAuroraBg.HexValue = t.BackgroundColor1;
        AuroraCustomPanel.Visibility = t.PresetName == Models.ToastPresets.CustomName
                                       ? Visibility.Visible : Visibility.Collapsed;

        _loadingStyleSettings = false;
        UpdateStyleColor2Visibility();
    }

    private void UpdateStyleColor2Visibility()
    {
        bool hideColor2 = StyleBgEffect.SelectedItem?.ToString() == "Solid";
        StyleBgColor2Label.Visibility = hideColor2 ? Visibility.Collapsed : Visibility.Visible;
        StyleBgColor2.Visibility      = hideColor2 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ToggleStyle_Click(object sender, RoutedEventArgs e)
    {
        bool open = StyleSection.Visibility == Visibility.Visible;
        SetActivePanel(open ? Panel.None : Panel.Style);
    }

    private void SaveStyle_Click(object sender, RoutedEventArgs e)
    {
        var config = App.ConfigService.Load();
        var t = BuildActiveTheme();
        config.ToastSettings.Theme = t;

        App.ConfigService.Save(config);
        AppLogger.Log($"Toast style saved — preset={t.PresetName} border={t.ActionBorderType}");

        ToggleStyle_Click(sender, e);
    }

    // The theme to preview/save. A chosen preset returns its canonical object; "Aurora Custom"
    // builds an Aurora theme from the 4 gradient pickers + background, with curtains derived
    // automatically (lightened tints of the gradient) so the editor stays simple.
    private Models.ToastTheme BuildActiveTheme()
    {
        var name = StylePreset.SelectedItem?.ToString() ?? Models.ToastPresets.CustomName;
        if (name != Models.ToastPresets.CustomName && Models.ToastPresets.Names.Contains(name))
            return Models.ToastPresets.Get(name);

        string[] grad =
        [
            StyleAuroraC1.HexValue, StyleAuroraC2.HexValue,
            StyleAuroraC3.HexValue, StyleAuroraC4.HexValue,
        ];

        return new Models.ToastTheme
        {
            PresetName           = Models.ToastPresets.CustomName,
            BackgroundEffect     = "Solid",
            BackgroundColor1     = StyleAuroraBg.HexValue,
            BackgroundColor2     = StyleAuroraBg.HexValue,
            GlowColor            = grad[0],
            MessageTextColor     = "#FFFFFF",
            ArtistTextColor      = grad[3],
            AlbumTextColor       = "#9AA0A6",
            ActionBorderType     = "Aurora Glow",
            ActionBorderColor    = grad[0],
            ShimmerEffect        = "None",
            AuroraGradientColors = grad,
            AuroraCurtainColors  = [.. grad.Select(h => Lighten(h, 0.45))],
        };
    }

    // Blend a #RRGGBB colour toward white by factor f (0 = unchanged, 1 = white) for soft curtains.
    private static string Lighten(string hex, double f)
    {
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
            byte L(byte v) => (byte)(v + (255 - v) * f);
            return $"#{L(c.R):X2}{L(c.G):X2}{L(c.B):X2}";
        }
        catch { return hex; }
    }

    private void StyleBgEffect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingStyleSettings) return;
        UpdateStyleColor2Visibility();
    }

    private void StylePreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingStyleSettings) return;

        var name = StylePreset.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(name)) return;

        if (name == Models.ToastPresets.CustomName)
        {
            // Reveal the custom editor; the pickers keep whatever palette was last loaded as a seed.
            AuroraCustomPanel.Visibility = Visibility.Visible;
            return;
        }

        // Apply the chosen preset — this also populates the custom pickers and hides the panel.
        // Re-entrancy guard via _loadingStyleSettings stops this reverting to "Aurora Custom".
        LoadStyleSettings(Models.ToastPresets.Get(name));
    }

    private void PreviewToast_Click(object sender, RoutedEventArgs e)
    {
        var theme = BuildActiveTheme();

        var config = App.ConfigService.Load();
        var payload = new Models.ToastPayload("Preview toast notification", null,
                                              "Track Name", "Artist Name", "Album Name");

        var toast = new ToastWindow(payload, config.ToastSettings.DurationMs,
                                    config.ToastSettings.AlwaysOnTop, theme)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        toast.Show();
        AppLogger.Log("Toast style preview shown");
    }

    private void SyncStyleButtonEnabled()
    {
        bool enabled = NotifEnabled.IsChecked == true;
        StyleToggleButton.IsEnabled = enabled;
        StyleToggleButton.Opacity   = enabled ? 1.0 : 0.4;
    }

    private void ResetStyle_Click(object sender, RoutedEventArgs e)
    {
        LoadStyleSettings(new Models.ToastTheme());
        AppLogger.Log("Toast style reset to defaults");
    }

    private void NotifEnabled_Checked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SyncStyleButtonEnabled();
    }

    private void NotifEnabled_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SyncStyleButtonEnabled();

        // Collapse the style section if it was open
        if (StyleSection.Visibility == Visibility.Visible)
        {
            StyleSection.Visibility       = Visibility.Collapsed;
            StyleCollapsedButton.Visibility = Visibility.Visible;
            StyleExpandedButton.Visibility  = Visibility.Collapsed;
        }
    }
}
