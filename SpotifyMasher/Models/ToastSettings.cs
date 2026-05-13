using System.ComponentModel;

namespace SpotifyMasher.Models;

public class ToastSettings
{
    public bool Enabled { get; set; } = true;
    public string Corner { get; set; } = "bottom-right";
    public int OffsetX { get; set; } = 20;
    public int OffsetY { get; set; } = 20;
    public int DurationMs { get; set; } = 3000;
    public bool AlwaysOnTop { get; set; } = false;
    public List<ProcessToastRule> ProcessRules { get; set; } = [];

    // Set by the drag picker — used directly instead of corner+offset when non-null
    public double? PinnedX { get; set; } = null;
    public double? PinnedY { get; set; } = null;
}

public class ProcessToastRule : INotifyPropertyChanged
{
    private string _processName = string.Empty;
    private string _corner = "top-right";
    private int _offsetX = 20;
    private int _offsetY = 20;
    private double? _pinnedX;
    private double? _pinnedY;

    public string ProcessName
    {
        get => _processName;
        set { _processName = value; OnPropertyChanged(nameof(ProcessName)); }
    }

    public string Corner
    {
        get => _corner;
        set { _corner = value; OnPropertyChanged(nameof(Corner)); }
    }

    public int OffsetX
    {
        get => _offsetX;
        set { _offsetX = value; OnPropertyChanged(nameof(OffsetX)); }
    }

    public int OffsetY
    {
        get => _offsetY;
        set { _offsetY = value; OnPropertyChanged(nameof(OffsetY)); }
    }

    public double? PinnedX
    {
        get => _pinnedX;
        set { _pinnedX = value; OnPropertyChanged(nameof(PinnedX)); }
    }

    public double? PinnedY
    {
        get => _pinnedY;
        set { _pinnedY = value; OnPropertyChanged(nameof(PinnedY)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
