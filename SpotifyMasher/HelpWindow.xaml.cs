using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace SpotifyMasher;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();

        var versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "version.txt");
        if (File.Exists(versionPath))
            VersionText.Text = "v" + File.ReadAllText(versionPath).Trim();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DwmHelper.SetGreenTitleBar(this);
    }

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
