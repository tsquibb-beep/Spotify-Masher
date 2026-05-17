namespace SpotifyMasher.Models;

public class ToastTheme
{
    // Gradient | Solid | Radial Glow | Grain
    public string BackgroundEffect  { get; set; } = "Gradient";
    public string BackgroundColor1  { get; set; } = "#1c2748";  // top / sole colour
    public string BackgroundColor2  { get; set; } = "#111832";  // bottom / secondary colour
    public string GlowColor         { get; set; } = "#1DB954";
    public string MessageTextColor  { get; set; } = "#FFFFFF";
    public string ArtistTextColor   { get; set; } = "#1DB954";
    public string AlbumTextColor    { get; set; } = "#A0A0B0";
    // Bottom Bar Drain | Fill from Centre | Full Border Trace | None
    public string ActionBorderType  { get; set; } = "Bottom Bar Drain";
    public string ActionBorderColor { get; set; } = "#1DB954";
}
