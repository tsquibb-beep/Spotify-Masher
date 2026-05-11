using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SpotifyMasher;

internal static class DwmHelper
{
    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(
        IntPtr hwnd, uint dwAttribute, ref int pvAttribute, uint cbAttribute);

    private const uint DWMWA_CAPTION_COLOR = 35;
    private const uint DWMWA_TEXT_COLOR = 36;

    public static void SetGreenTitleBar(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Spotify green #1DB954 → COLORREF is BGR, not RGB: 0x0054B91D
            int captionColor = 0x0054B91D;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            // White title text so "Spotify Masher" is readable on green
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch { }
    }
}
