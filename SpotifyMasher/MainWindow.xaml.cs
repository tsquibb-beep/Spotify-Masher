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
    ];

    private readonly ObservableCollection<HotkeyBinding> _bindings = [];
    private bool _isAuthenticated;

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

    private void SetHotkeysVisible(bool visible)
    {
        HotkeySection.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        HotkeyCollapsedButtons.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        HotkeyExpandedButtons.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
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
}
