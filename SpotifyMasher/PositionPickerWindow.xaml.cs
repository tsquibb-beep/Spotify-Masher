using System.Windows;

namespace SpotifyMasher;

public partial class PositionPickerWindow : Window
{
    public (double X, double Y) Result { get; private set; }

    public PositionPickerWindow(double startLeft, double startTop)
    {
        InitializeComponent();
        Loaded += (_, _) => { Left = startLeft; Top = startTop; };
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => DragMove();

    private void DoneText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        Result = (Left, Top);
        DialogResult = true;
    }

    private void CancelText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        DialogResult = false;
    }
}
