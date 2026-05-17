using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpotifyMasher.Controls;

public partial class ColorPickerBox : UserControl
{
    // Custom-colour slots persist for the lifetime of the app (shared across all pickers).
    private static readonly uint[] s_customColors = new uint[16];

    [StructLayout(LayoutKind.Sequential)]
    private struct CHOOSECOLOR
    {
        public int    lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public uint   rgbResult;
        public IntPtr lpCustColors;
        public uint   Flags;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
    }

    [DllImport("comdlg32.dll")]
    private static extern bool ChooseColor(ref CHOOSECOLOR cc);

    private const uint CC_RGBINIT  = 0x00000001;
    private const uint CC_FULLOPEN = 0x00000002;
    private const uint CC_ANYCOLOR = 0x00000100;

    public static readonly DependencyProperty HexValueProperty =
        DependencyProperty.Register(nameof(HexValue), typeof(string), typeof(ColorPickerBox),
            new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnHexValueChanged));

    public static readonly DependencyProperty SwatchBrushProperty =
        DependencyProperty.Register(nameof(SwatchBrush), typeof(Brush), typeof(ColorPickerBox),
            new PropertyMetadata(new SolidColorBrush(Colors.Black)));

    public string HexValue
    {
        get => (string)GetValue(HexValueProperty);
        set => SetValue(HexValueProperty, value);
    }

    public Brush SwatchBrush
    {
        get => (Brush)GetValue(SwatchBrushProperty);
        private set => SetValue(SwatchBrushProperty, value);
    }

    public ColorPickerBox()
    {
        InitializeComponent();
    }

    private static void OnHexValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPickerBox picker)
            picker.UpdateSwatchBrush();
    }

    private void UpdateSwatchBrush()
    {
        try
        {
            if (ColorConverter.ConvertFromString(HexValue) is Color color)
                SwatchBrush = new SolidColorBrush(color);
        }
        catch { }
    }

    private void Swatch_Click(object sender, MouseButtonEventArgs e)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(Window.GetWindow(this)!).Handle;
        var gc   = GCHandle.Alloc(s_customColors, GCHandleType.Pinned);
        try
        {
            var cc = new CHOOSECOLOR
            {
                lStructSize  = Marshal.SizeOf<CHOOSECOLOR>(),
                hwndOwner    = hwnd,
                rgbResult    = HexToColorRef(),
                lpCustColors = gc.AddrOfPinnedObject(),
                Flags        = CC_RGBINIT | CC_FULLOPEN | CC_ANYCOLOR,
            };

            if (ChooseColor(ref cc))
            {
                byte r = (byte)( cc.rgbResult        & 0xFF);
                byte g = (byte)((cc.rgbResult >>  8) & 0xFF);
                byte b = (byte)((cc.rgbResult >> 16) & 0xFF);
                HexValue = $"#{r:X2}{g:X2}{b:X2}";
            }
        }
        finally
        {
            gc.Free();
        }
    }

    // Converts the current HexValue to a Windows COLORREF (0x00BBGGRR).
    private uint HexToColorRef()
    {
        try
        {
            if (ColorConverter.ConvertFromString(HexValue) is Color c)
                return (uint)(c.R | (c.G << 8) | (c.B << 16));
        }
        catch { }
        return 0x00FFFFFF;
    }
}
