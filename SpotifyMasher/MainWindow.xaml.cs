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

    // Draggable real-toast handle for setting a freehand position. _dragRule is the per-process
    // rule being positioned, or null for the global position.
    private ToastWindow? _dragToast;
    private ProcessToastRule? _dragRule;

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

        LoadAllToastSettings(config.ToastSettings);

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

    private enum Panel { None, Hotkeys, Notifications }

    // Accordion: at most one panel open at a time. The other panels' entry buttons are hidden
    // while a panel is open; only the active panel's expanded (Save) buttons show.
    private void SetActivePanel(Panel panel)
    {
        if (panel != Panel.Notifications) CloseDragToast();

        HotkeySection.Visibility = panel == Panel.Hotkeys       ? Visibility.Visible : Visibility.Collapsed;
        NotifSection.Visibility  = panel == Panel.Notifications ? Visibility.Visible : Visibility.Collapsed;

        bool none = panel == Panel.None;
        HotkeyCollapsedButtons.Visibility = none ? Visibility.Visible : Visibility.Collapsed;
        NotifCollapsedButton.Visibility   = none ? Visibility.Visible : Visibility.Collapsed;

        HotkeyExpandedButtons.Visibility = panel == Panel.Hotkeys       ? Visibility.Visible : Visibility.Collapsed;
        NotifExpandedButton.Visibility   = panel == Panel.Notifications ? Visibility.Visible : Visibility.Collapsed;
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
        CloseDragToast();
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
        if (_dragToast is not null && _dragRule is null) CloseDragToast();
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

    // Notifications + Toast Style live on one page; this loads both (startup and Cancel).
    private void LoadAllToastSettings(Models.ToastSettings s)
    {
        LoadNotificationSettings(s);
        LoadStyleSettings(s.Theme);
        _loadingStyleSettings = true;   // set the slider without popping a preview
        StyleScale.Value = Math.Clamp(s.Scale, StyleScale.Minimum, StyleScale.Maximum);
        _loadingStyleSettings = false;
        SyncAppearanceEnabled();
    }

    private void CancelNotifications_Click(object sender, RoutedEventArgs e)
    {
        SetActivePanel(Panel.None);   // also closes the drag handle
        LoadAllToastSettings(App.ConfigService.Load().ToastSettings);
        AppLogger.Log("Notification changes cancelled");
    }

    private void SaveNotifications_Click(object sender, RoutedEventArgs e)
    {
        SetActivePanel(Panel.None);   // closes the drag handle — its position is already recorded

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
        s.Theme = BuildActiveTheme();
        s.Scale = StyleScale.Value;

        App.ConfigService.Save(config);
        AppLogger.Log($"Notification settings saved — pinned={s.PinnedX?.ToString("F0") ?? "no"} corner={s.Corner} enabled={s.Enabled} rules={s.ProcessRules.Count} preset={s.Theme.PresetName} scale={s.Scale:P0}");
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

    private void SetGlobalPosition_Click(object sender, RoutedEventArgs e) => OpenDragToast(null);

    private void SetProcessRulePosition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Models.ProcessToastRule rule })
            OpenDragToast(rule);
    }

    // Where the global toast would go with the current (unsaved) UI settings.
    private Services.Placement GetUiPlacement() => new(
        NotifCorner.SelectedItem?.ToString() ?? "bottom-right",
        int.TryParse(NotifOffsetX.Text, out var ox) ? ox : 20,
        int.TryParse(NotifOffsetY.Text, out var oy) ? oy : 20,
        ModeFreehand.IsChecked == true ? _pendingPinnedX : null,
        ModeFreehand.IsChecked == true ? _pendingPinnedY : null);

    // Shows the real toast (current style + size) as a drag handle. Its position is recorded live
    // as it moves; Save keeps it, Cancel throws it away.
    private void OpenDragToast(Models.ProcessToastRule? rule)
    {
        var placement = rule is null
            ? GetUiPlacement()
            : new Services.Placement(rule.Corner, rule.OffsetX, rule.OffsetY, rule.PinnedX, rule.PinnedY);

        CloseDragToast();
        _dragRule  = rule;
        _dragToast = App.ToastService.ShowDragHandle(BuildActiveTheme(), StyleScale.Value, placement);
        _dragToast.LocationChanged += DragToast_LocationChanged;
        RecordDragPosition();
    }

    // Re-opens the handle in place after a style change so it keeps matching the real toast.
    private void RefreshDragToast()
    {
        if (_dragToast is not null) OpenDragToast(_dragRule);
    }

    private void CloseDragToast()
    {
        if (_dragToast is null) return;
        _dragToast.LocationChanged -= DragToast_LocationChanged;
        App.ToastService.CloseDragHandle();
        _dragToast = null;
        _dragRule  = null;
    }

    private void DragToast_LocationChanged(object? sender, EventArgs e) => RecordDragPosition();

    private void RecordDragPosition()
    {
        if (_dragToast is null) return;
        double x = _dragToast.Left, y = _dragToast.Top;

        if (_dragRule is not null)
        {
            _dragRule.PinnedX = x;
            _dragRule.PinnedY = y;
            return;
        }

        _pendingPinnedX = x;
        _pendingPinnedY = y;
        UpdateCornerIndicator(ComputeCornerFromPosition(x, y));
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
            RefreshDragToast();
            return;
        }

        // Apply the chosen preset — this also populates the custom pickers and hides the panel.
        // Re-entrancy guard via _loadingStyleSettings stops this reverting to "Aurora Custom".
        LoadStyleSettings(Models.ToastPresets.Get(name));
        RefreshDragToast();
    }

    private void PreviewToast_Click(object sender, RoutedEventArgs e)
    {
        // Shown at the user's configured position, with the app logo as stand-in album art.
        App.ToastService.ShowPreview(BuildActiveTheme(), StyleScale.Value, GetUiPlacement());
        AppLogger.Log("Toast style preview shown");
    }

    private void SyncAppearanceEnabled()
    {
        bool enabled = NotifEnabled.IsChecked == true;
        AppearancePanel.IsEnabled = enabled;
    }

    private void ResetStyle_Click(object sender, RoutedEventArgs e)
    {
        LoadStyleSettings(Models.ToastPresets.Get(Models.ToastPresets.DefaultName));
        StyleScale.Value = 1.0;
        RefreshDragToast();
        AppLogger.Log("Toast style reset to defaults");
    }

    private void StyleScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Fires during InitializeComponent (Value="1") before the label exists.
        if (StyleScaleLabel is null) return;
        StyleScaleLabel.Text = $"{Math.Round(e.NewValue * 100)}%";

        // Live preview while adjusting — one preview window that resizes, not one per tick.
        if (!_loadingStyleSettings && NotifSection.Visibility == Visibility.Visible)
            App.ToastService.PreviewScale(BuildActiveTheme(), e.NewValue, GetUiPlacement());
    }

    private void NotifEnabled_Checked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SyncAppearanceEnabled();
    }

    private void NotifEnabled_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        SyncAppearanceEnabled();
    }
}
