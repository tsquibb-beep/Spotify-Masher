using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToastDesigner.Models;

public class DesignerTheme
{
    // ── Fields matching SpotifyMasher ToastTheme exactly (for JSON export compat) ──
    public string BackgroundEffect  { get; set; } = "Gradient";
    public string BackgroundColor1  { get; set; } = "#1c2748";
    public string BackgroundColor2  { get; set; } = "#111832";
    public string GlowColor         { get; set; } = "#1DB954";   // outer shadow colour
    public string MessageTextColor  { get; set; } = "#FFFFFF";
    public string ArtistTextColor   { get; set; } = "#1DB954";
    public string AlbumTextColor    { get; set; } = "#A0A0B0";
    public string ActionBorderType  { get; set; } = "Bottom Bar Drain";
    public string ActionBorderColor { get; set; } = "#1DB954";
    public string ShimmerEffect     { get; set; } = "Diagonal";

    // ── Extended designer-only fields ──
    public double BorderThickness   { get; set; } = 1.5;
    public double CornerRadius      { get; set; } = 10.0;
    public double GlowIntensity     { get; set; } = 16.0;   // DropShadowEffect.BlurRadius
    public bool   BorderGlowEnabled { get; set; } = true;
    public string BorderGlowColor   { get; set; } = "#1DB954";
    public double BackgroundOpacity { get; set; } = 1.0;
    public double BorderOpacity     { get; set; } = 1.0;
    public double TrackTextOpacity  { get; set; } = 1.0;
    public double ArtistTextOpacity { get; set; } = 1.0;
    public double AlbumTextOpacity  { get; set; } = 1.0;
    public double TrackFontSize     { get; set; } = 14.0;
    public double ArtistFontSize    { get; set; } = 12.5;
    public double AlbumFontSize     { get; set; } = 11.5;
    public string TrackFontWeight   { get; set; } = "Normal";
    public string ArtistFontWeight  { get; set; } = "Bold";
    public string AlbumFontWeight   { get; set; } = "Normal";
    public string TrackPrefix       { get; set; } = "";
    public string TrackSuffix       { get; set; } = "";
    public string ArtistPrefix      { get; set; } = "";
    public string ArtistSuffix      { get; set; } = "";
    public string AlbumPrefix       { get; set; } = "";
    public string AlbumSuffix       { get; set; } = "";
    public string BackgroundImagePath { get; set; } = "";

    // ── Serialisation ──

    private static readonly JsonSerializerOptions s_opts = new() { WriteIndented = true };

    public string ToJson() => JsonSerializer.Serialize(this, s_opts);

    public static DesignerTheme? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<DesignerTheme>(json, s_opts); }
        catch { return null; }
    }

    // Exports only the fields that SpotifyMasher's ToastTheme understands,
    // so the result can be pasted straight into config.json under "Theme".
    public string ExportToMasherJson()
    {
        var masher = new
        {
            BackgroundEffect,
            BackgroundColor1,
            BackgroundColor2,
            GlowColor,
            MessageTextColor,
            ArtistTextColor,
            AlbumTextColor,
            ActionBorderType,
            ActionBorderColor,
            ShimmerEffect,
        };
        return JsonSerializer.Serialize(masher, s_opts);
    }
}
