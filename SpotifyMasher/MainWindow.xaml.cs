using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpotifyMasher.Models;
using SpotifyMasher.Services;

namespace SpotifyMasher;

public partial class MainWindow : Window
{
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

    private void UpdateAuthUi()
    {
        if (_isAuthenticated)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x1D, 0xB9, 0x54));
            StatusLabel.Text = "Connected";
            AuthStatusText.Text = "Authorised — your hotkeys are active.";
            AuthButton.Visibility = Visibility.Collapsed;
            ClientIdBox.IsReadOnly = true;
            DisconnectButton.Visibility = Visibility.Visible;
            SaveButton.IsEnabled = true;
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x66));
            StatusLabel.Text = "Not connected";
            AuthStatusText.Text = "Enter your Spotify Client ID from developer.spotify.com/dashboard";
            AuthButton.Visibility = Visibility.Visible;
            ClientIdBox.IsReadOnly = false;
            DisconnectButton.Visibility = Visibility.Collapsed;
            SaveButton.IsEnabled = false;
        }
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

        int active = _bindings.Count(b => b.Key != System.Windows.Input.Key.None);

        if (failures.Count > 0)
            AuthStatusText.Text = $"⚠ Could not register: {string.Join("; ", failures)}";
        else
            AuthStatusText.Text = $"Hotkeys saved — {active} active.";
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Clear();

    private void MinimiseToTray_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
