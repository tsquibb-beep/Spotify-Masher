using System.Windows;

namespace SpotifyMasher;

public partial class PositionPickerWindow : Window
{
    public (string Corner, int OffsetX, int OffsetY) Result { get; private set; }

    public PositionPickerWindow(string corner, int offsetX, int offsetY)
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceAtCornerOffset(corner, offsetX, offsetY);
    }

    private void PlaceAtCornerOffset(string corner, int offsetX, int offsetY)
    {
        UpdateLayout();
        var w = ActualWidth > 0 ? ActualWidth : 280;
        var h = ActualHeight > 0 ? ActualHeight : 100;
        var area = SystemParameters.WorkArea;

        (Left, Top) = corner switch
        {
            "top-left"    => (area.Left + offsetX,          area.Top + offsetY),
            "top-right"   => (area.Right - w - offsetX,     area.Top + offsetY),
            "bottom-left" => (area.Left + offsetX,           area.Bottom - h - offsetY),
            _             => (area.Right - w - offsetX,     area.Bottom - h - offsetY),
        };
    }

    private void DragStrip_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => DragMove();

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ComputeResult();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private (string Corner, int OffsetX, int OffsetY) ComputeResult()
    {
        var area = SystemParameters.WorkArea;
        var w = ActualWidth;
        var h = ActualHeight;

        var centerX = Left + w / 2;
        var centerY = Top  + h / 2;

        var useLeft = centerX < area.Left + area.Width  / 2;
        var useTop  = centerY < area.Top  + area.Height / 2;

        var corner = (useTop, useLeft) switch
        {
            (true,  true)  => "top-left",
            (true,  false) => "top-right",
            (false, true)  => "bottom-left",
            _              => "bottom-right",
        };

        var offsetX = (int)Math.Max(0, useLeft ? Left - area.Left : area.Right  - Left - w);
        var offsetY = (int)Math.Max(0, useTop  ? Top  - area.Top  : area.Bottom - Top  - h);

        return (corner, offsetX, offsetY);
    }
}
