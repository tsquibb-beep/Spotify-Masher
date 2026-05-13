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
}

public class ProcessToastRule : INotifyPropertyChanged
{
    private string _processName = string.Empty;
    private string _corner = "top-right";
    private int _offsetX = 20;
    private int _offsetY = 20;

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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
