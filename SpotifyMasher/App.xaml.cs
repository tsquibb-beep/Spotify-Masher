using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using SpotifyMasher.Services;

namespace SpotifyMasher;

public partial class App : Application
{
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;

    internal static SpotifyAuthService AuthService { get; } = new();
    internal static SpotifyApiService ApiService { get; } = new(AuthService);
    internal static HotkeyService HotkeyService { get; } = new(ApiService);
    internal static ConfigService ConfigService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single-instance guard
        _mutex = new Mutex(true, "SpotifyMasherSingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Spotify Masher is already running.", "Spotify Masher",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        AuthService.LoadStoredTokens();

        var config = ConfigService.Load();
        if (config.Bindings.Count > 0)
            HotkeyService.RegisterAll(config.Bindings);

        // Start minimised to tray — show window only on double-click
        var window = new MainWindow();
        MainWindow = window;
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e) => ShowMainWindow();

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        HotkeyService.UnregisterAll();
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        HotkeyService.UnregisterAll();
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
