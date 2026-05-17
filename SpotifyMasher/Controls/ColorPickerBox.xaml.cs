using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpotifyMasher.Controls;

public partial class ColorPickerBox : UserControl
{
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
        HexInput.SelectAll();
        HexInput.Focus();
    }
}
