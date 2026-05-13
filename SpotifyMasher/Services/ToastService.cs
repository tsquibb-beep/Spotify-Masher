using System.Diagnostics;
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

    public void Show(string message)
    {
        var settings = configService.Load().ToastSettings;
        if (!settings.Enabled) return;

        var (corner, offsetX, offsetY) = ResolvePosition(settings);

        Application.Current.Dispatcher.Invoke(() =>
        {
            _current?.ForceClose();
            _current = null;

            var toast = new ToastWindow(message, settings.DurationMs, settings.AlwaysOnTop);
            PositionToast(toast, corner, offsetX, offsetY);
            toast.Closed += (_, _) => { if (_current == toast) _current = null; };
            _current = toast;
            toast.Show();
        });
    }

    private static (string corner, int offsetX, int offsetY) ResolvePosition(ToastSettings settings)
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
                        return (rule.Corner, rule.OffsetX, rule.OffsetY);
                }
            }
        }
        catch { }

        return (settings.Corner, settings.OffsetX, settings.OffsetY);
    }

    private static void PositionToast(ToastWindow toast, string corner, int offsetX, int offsetY)
    {
        toast.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        toast.Arrange(new Rect(toast.DesiredSize));

        var w = toast.DesiredSize.Width > 0 ? toast.DesiredSize.Width : 280;
        var h = toast.DesiredSize.Height > 0 ? toast.DesiredSize.Height : 60;

        var area = SystemParameters.WorkArea;

        (toast.Left, toast.Top) = corner switch
        {
            "top-left"     => (area.Left + offsetX,         area.Top + offsetY),
            "top-right"    => (area.Right - w - offsetX,    area.Top + offsetY),
            "bottom-left"  => (area.Left + offsetX,         area.Bottom - h - offsetY),
            _              => (area.Right - w - offsetX,    area.Bottom - h - offsetY),
        };
    }
}
