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

public class ProcessToastRule
{
    public string ProcessName { get; set; } = string.Empty;
    public string Corner { get; set; } = "top-right";
    public int OffsetX { get; set; } = 20;
    public int OffsetY { get; set; } = 20;
}
