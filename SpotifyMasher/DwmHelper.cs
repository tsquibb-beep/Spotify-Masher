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

            // Midnight purple #2D1B69 — complements the dark navy scheme without
            // clashing with the green icon. COLORREF format is 0x00BBGGRR.
            // R=0x2D, G=0x1B, B=0x69 → 0x00691B2D
            int captionColor = 0x00691B2D;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            // White title text for readability on dark purple
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch { }
    }
}
