using System.Text.Json;

namespace ToastDesigner.Models;

public class DesignerTheme
{
    // ── Fields matching SpotifyMasher ToastTheme exactly (for JSON export compat) ──
    public string BackgroundEffect  { get; set; } = "Gradient";
    public string BackgroundColor1  { get; set; } = "#1c2748";
    public string BackgroundColor2  { get; set; } = "#111832";
    public string GlowColor         { get; set; } = "#1DB954";
    public string MessageTextColor  { get; set; } = "#FFFFFF";
    public string ArtistTextColor   { get; set; } = "#1DB954";
    public string AlbumTextColor    { get; set; } = "#A0A0B0";
    public string ActionBorderType  { get; set; } = "Bottom Bar Drain";
    public string ActionBorderColor { get; set; } = "#1DB954";
    public string ShimmerEffect     { get; set; } = "None";  // kept for Masher export compat

    // ── Border ──
    public double BorderThickness   { get; set; } = 1.5;
    public double CornerRadius      { get; set; } = 10.0;
    public double GlowIntensity     { get; set; } = 16.0;
    public bool   BorderGlowEnabled { get; set; } = true;
    public string BorderGlowColor   { get; set; } = "#1DB954";
    public double BorderOpacity     { get; set; } = 1.0;

    // ── Background ──
    public double BackgroundOpacity  { get; set; } = 1.0;   // whole toast opacity
    public double EffectOpacity      { get; set; } = 1.0;   // bg effect layer opacity only
    public double GradientAngle      { get; set; } = 90.0;  // degrees: 0=L→R, 90=T→B
    public double GradientSharpness  { get; set; } = 0.0;   // 0=soft blend, 1=hard edge
    public bool   GradientIsRadial   { get; set; } = false;
    public double RadialCenterX      { get; set; } = 0.5;
    public double RadialCenterY      { get; set; } = 0.5;
    public double RadialRadiusX      { get; set; } = 0.7;
    public double RadialRadiusY      { get; set; } = 0.7;
    public double GrainScale         { get; set; } = 64.0;  // tile size in pixels
    public double GrainIntensity     { get; set; } = 0.15;  // grain opacity
    public string GrainTintColor     { get; set; } = "#FFFFFF";
    public string BackgroundImagePath { get; set; } = "";

    // ── Text ──
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

    // ── Shimmer ──
    public string ShimmerShape           { get; set; } = "Rectangle";
    public double ShimmerWidthFraction   { get; set; } = 0.25;  // fraction of toast width
    public double ShimmerHeightFraction  { get; set; } = 1.5;   // fraction of toast height
    public string ShimmerColor           { get; set; } = "#FFFFFF";
    public double ShimmerTransparency    { get; set; } = 0.85;  // 0=opaque, 1=invisible
    public double ShimmerBlur            { get; set; } = 12.0;  // BlurEffect.BlurRadius
    public double ShimmerRotation        { get; set; } = 0.0;   // degrees
    public string ShimmerDirectionPreset { get; set; } = "Left to Right";
    public double ShimmerDirectionAngle  { get; set; } = 0.0;   // degrees (for Custom)
    public double ShimmerSpeed           { get; set; } = 2.0;   // seconds per pass
    public double ToastDuration          { get; set; } = 4.0;   // total toast display time (s)

    // ── Serialisation ──
    private static readonly JsonSerializerOptions s_opts = new() { WriteIndented = true };

    public string ToJson() => JsonSerializer.Serialize(this, s_opts);

    public static DesignerTheme? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<DesignerTheme>(json, s_opts); }
        catch { return null; }
    }

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
