namespace SpotifyMasher.Models;

public class AppConfig
{
    public string ClientId { get; set; } = string.Empty;
    public List<HotkeyBinding> Bindings { get; set; } = [];
}
