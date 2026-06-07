namespace SpotifyMasher.Models;

// Curated toast designs. Each preset is a fully-populated ToastTheme.
// "Custom" is the sentinel used when the user has hand-edited the manual controls.
//
// The Aurora family all share the "Aurora Glow" look (gradient border + blurred halo +
// drifting curtain shards) and differ only by colour scheme:
//   - AuroraGradientColors → border + glow
//   - AuroraCurtainColors  → background shards
//   - BackgroundColor1     → the solid card behind everything
// Palettes are grounded in real aurora/northern-lights colour schemes (schemecolor.com,
// color-hex.com) rather than guessed. Add new variants by copying an Aurora*() factory.
public static class ToastPresets
{
    public const string CustomName = "Aurora Custom";

    // Order here = order in the dropdown.
    public static IReadOnlyList<string> Names { get; } =
    [
        "Spotify Classic",
        "Aurora Default",
        "Aurora Jungle",
        "Aurora Twilight",
        "Aurora Lagoon",
        "Aurora Ember",
        "Aurora Frost",
    ];

    public static ToastTheme Get(string name) => name switch
    {
        "Spotify Classic" => SpotifyClassic(),
        "Aurora Default"  => AuroraDefault(),
        "Aurora Jungle"   => AuroraJungle(),
        "Aurora Twilight" => AuroraTwilight(),
        "Aurora Lagoon"   => AuroraLagoon(),
        "Aurora Ember"    => AuroraEmber(),
        "Aurora Frost"    => AuroraFrost(),
        _                 => AuroraDefault(),
    };

    private static ToastTheme SpotifyClassic() => new()
    {
        PresetName        = "Spotify Classic",
        BackgroundEffect  = "Gradient",
        BackgroundColor1  = "#1c2748",
        BackgroundColor2  = "#111832",
        GlowColor         = "#1DB954",
        MessageTextColor  = "#FFFFFF",
        ArtistTextColor   = "#1DB954",
        AlbumTextColor    = "#A0A0B0",
        ActionBorderType  = "Bottom Bar Drain",
        ActionBorderColor = "#1DB954",
        ShimmerEffect     = "Diagonal",
    };

    // ── Aurora family ──────────────────────────────────────────────────────
    // Shared chassis: solid dark card, white message text, muted album text, the Aurora Glow
    // border type, no inner shimmer. Helper keeps each variant down to just its palette.
    private static ToastTheme AuroraBase(string name, string background, string artist,
                                         string[] gradient, string[] curtains) => new()
    {
        PresetName           = name,
        BackgroundEffect     = "Solid",
        BackgroundColor1     = background,
        BackgroundColor2     = background,
        GlowColor            = gradient[0],
        MessageTextColor     = "#FFFFFF",
        ArtistTextColor      = artist,
        AlbumTextColor       = "#9AA0A6",
        ActionBorderType     = "Aurora Glow",
        ActionBorderColor    = gradient[0],
        ShimmerEffect        = "None",
        AuroraGradientColors = gradient,
        AuroraCurtainColors  = curtains,
    };

    // The original — gold / magenta / blue / cyan on near-black.
    private static ToastTheme AuroraDefault() => AuroraBase(
        "Aurora Default", background: "#14141A", artist: "#95F3D9",
        gradient: ["#FFB224", "#E34BA9", "#0072F5", "#95F3D9"],
        curtains: ["#3B82F6", "#A5B4FC", "#93C5FD", "#DDD6FE", "#60A5FA"]);

    // Greens with yellow — "Green Aurora Borealis" scheme.
    private static ToastTheme AuroraJungle() => AuroraBase(
        "Aurora Jungle", background: "#0A140A", artist: "#A3DC6F",
        gradient: ["#FFE14D", "#A3DC6F", "#3BB20A", "#00EA8D"],
        curtains: ["#B3FD00", "#62A83B", "#00EA8D", "#A3DC6F"]);

    // Purples, deep blues and pinks — "Beautiful Aurora Borealis" scheme.
    private static ToastTheme AuroraTwilight() => AuroraBase(
        "Aurora Twilight", background: "#0D0A1A", artist: "#C77DFF",
        gradient: ["#FF4FD8", "#A245DE", "#6A1D96", "#3E50B5"],
        curtains: ["#C77DFF", "#A245DE", "#5B6BE5", "#FF6FD8"]);

    // Teals, cyans and ocean blue — "Aurora Northern Lights" scheme.
    private static ToastTheme AuroraLagoon() => AuroraBase(
        "Aurora Lagoon", background: "#07151A", artist: "#5EEAD4",
        gradient: ["#0EF3C5", "#04E2B7", "#038298", "#025385"],
        curtains: ["#0EF3C5", "#5EEAD4", "#38BDF8", "#04E2B7"]);

    // Warm solar flare — gold, orange, red-pink, magenta (inventive, not a literal aurora).
    private static ToastTheme AuroraEmber() => AuroraBase(
        "Aurora Ember", background: "#160A0A", artist: "#FFB224",
        gradient: ["#FFD700", "#FF8A00", "#FF3D6E", "#E34BA9"],
        curtains: ["#FFB224", "#FF6F91", "#FFC4A3", "#FF8A5B"]);

    // Icy polar — turquoise, cyan, ice blue, sky.
    private static ToastTheme AuroraFrost() => AuroraBase(
        "Aurora Frost", background: "#0A1016", artist: "#67E8F9",
        gradient: ["#00CED1", "#37EAF9", "#A5F3FC", "#93C5FD"],
        curtains: ["#A5F3FC", "#93C5FD", "#DDEBFF", "#67E8F9"]);
}
