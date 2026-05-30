using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ToastDesigner;

internal static class DwmHelper
{
    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(
        IntPtr hwnd, uint dwAttribute, ref int pvAttribute, uint cbAttribute);

    private const uint DWMWA_CAPTION_COLOR = 35;
    private const uint DWMWA_TEXT_COLOR    = 36;

    public static void SetTitleBar(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int captionColor = 0x00691B2D; // midnight purple #2D1B69 as COLORREF 0x00BBGGRR
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch { }
    }
}
