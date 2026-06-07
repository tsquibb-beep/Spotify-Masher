namespace SpotifyMasher.Models;

// Named presets live in ToastPresets. PresetName records which preset is selected
// ("Custom" once the user hand-edits the manual controls). LoadStyleSettings(ToastTheme)
// in MainWindow is the "apply preset" entry point.
public class ToastTheme
{
    public string PresetName { get; set; } = "Spotify Classic";

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
    // Diagonal | Horizontal | Pulse | None
    public string ShimmerEffect     { get; set; } = "Diagonal";

    // ── Aurora palette (used only by the "Aurora Glow" action border look) ──
    // Border + glow gradient: any number of colours (4 looks best); the renderer repeats the
    // first colour to close the loop seamlessly.
    public string[] AuroraGradientColors { get; set; } = ["#FFB224", "#E34BA9", "#0072F5", "#95F3D9"];
    // Drifting background "curtain" shards behind the text — soft/pastel colours read best.
    public string[] AuroraCurtainColors  { get; set; } = ["#3B82F6", "#A5B4FC", "#93C5FD", "#DDD6FE", "#60A5FA"];
}
