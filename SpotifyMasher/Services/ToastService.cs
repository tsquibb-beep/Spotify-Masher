using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using SpotifyMasher.Models;

namespace SpotifyMasher.Services;

public class ToastService(ConfigService configService)
{
    private ToastWindow? _current;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public void Show(ToastPayload payload)
    {
        var settings = configService.Load().ToastSettings;
        if (!settings.Enabled) return;

        var (corner, offsetX, offsetY, pinnedX, pinnedY) = ResolvePosition(settings);

        Application.Current.Dispatcher.Invoke(() =>
        {
            _current?.ForceClose();
            _current = null;

            var toast = new ToastWindow(payload, settings.DurationMs, settings.AlwaysOnTop, settings.Theme,
                                        settings.Scale);
            PositionToast(toast, corner, offsetX, offsetY, pinnedX, pinnedY);
            toast.Closed += (_, _) => { if (_current == toast) _current = null; };
            _current = toast;
            toast.Show();
        });
    }

    // Shows a one-off preview of the given theme at the user's configured position, using the
    // app logo as stand-in album art. Ignores the Enabled flag (the user explicitly asked for it).
    public void ShowPreview(ToastTheme theme, double scale)
    {
        var settings = configService.Load().ToastSettings;
        var (corner, offsetX, offsetY, pinnedX, pinnedY) = ResolvePosition(settings);

        Application.Current.Dispatcher.Invoke(() =>
        {
            _current?.ForceClose();
            _current = null;

            var payload = new ToastPayload("Preview toast notification", LoadLogoBytes(),
                                           "Track Name", "Artist Name", "Album Name", Heading: "▶ Playing");
            var toast = new ToastWindow(payload, settings.DurationMs, settings.AlwaysOnTop, theme, scale)
            {
                IsPreview = true,
            };
            PositionToast(toast, corner, offsetX, offsetY, pinnedX, pinnedY);
            toast.Closed += (_, _) => { if (_current == toast) _current = null; };
            _current = toast;
            toast.Show();
        });
    }

    // Live resize for the Toast Size slider: grows/shrinks the preview already on screen (and keeps it
    // alive a few more seconds) rather than spawning a new one per slider tick. Opens one if none is up.
    public void PreviewScale(ToastTheme theme, double scale)
    {
        bool resized = Application.Current.Dispatcher.Invoke(() =>
        {
            if (_current is not { IsPreview: true, IsDismissing: false } toast) return false;

            toast.SetScale(scale);
            toast.UpdateLayout();
            var (corner, offsetX, offsetY, pinnedX, pinnedY) = ResolvePosition(configService.Load().ToastSettings);
            PositionToast(toast, corner, offsetX, offsetY, pinnedX, pinnedY);
            toast.KeepAlive();
            return true;
        });

        if (!resized) ShowPreview(theme, scale);
    }

    private static byte[]? LoadLogoBytes()
    {
        try
        {
            var sri = Application.GetResourceStream(
                new Uri("pack://application:,,,/Resources/fuspotify256.png"));
            if (sri is null) return null;
            using var ms = new MemoryStream();
            sri.Stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch { return null; }
    }

    private static (string corner, int offsetX, int offsetY, double? pinnedX, double? pinnedY)
        ResolvePosition(ToastSettings settings)
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                var proc = Process.GetProcessById((int)pid);
                var exeName = proc.ProcessName + ".exe";

                foreach (var rule in settings.ProcessRules)
                {
                    if (rule.ProcessName.Equals(exeName, StringComparison.OrdinalIgnoreCase))
                        return (rule.Corner, rule.OffsetX, rule.OffsetY, rule.PinnedX, rule.PinnedY);
                }
            }
        }
        catch { }

        return (settings.Corner, settings.OffsetX, settings.OffsetY, settings.PinnedX, settings.PinnedY);
    }

    private static void PositionToast(ToastWindow toast, string corner, int offsetX, int offsetY,
        double? pinnedX, double? pinnedY)
    {
        toast.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        toast.Arrange(new Rect(toast.DesiredSize));

        var w = toast.DesiredSize.Width  > 0 ? toast.DesiredSize.Width  : 280;
        var h = toast.DesiredSize.Height > 0 ? toast.DesiredSize.Height : 60;

        if (pinnedX is double px && pinnedY is double py)
        {
            // The pin is the top-left anchor, so a scaled-up toast grows right/down — nudge it back
            // inside the virtual desktop if that would push it off the edge.
            toast.Left = Math.Max(SystemParameters.VirtualScreenLeft,
                Math.Min(px, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - w));
            toast.Top  = Math.Max(SystemParameters.VirtualScreenTop,
                Math.Min(py, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - h));
            return;
        }

        var area = SystemParameters.WorkArea;

        (toast.Left, toast.Top) = corner switch
        {
            "top-left"    => (area.Left + offsetX,        area.Top + offsetY),
            "top-right"   => (area.Right - w - offsetX,   area.Top + offsetY),
            "bottom-left" => (area.Left + offsetX,         area.Bottom - h - offsetY),
            _             => (area.Right - w - offsetX,   area.Bottom - h - offsetY),
        };
    }
}
