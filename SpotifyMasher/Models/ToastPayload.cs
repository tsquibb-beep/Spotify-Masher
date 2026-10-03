namespace SpotifyMasher.Models;

public record ToastPayload(
    string Message,
    byte[]? ImageBytes = null,
    string? TrackName = null,
    string? ArtistName = null,
    string? AlbumName = null,
    string? Heading = null);   // optional status line above the track (e.g. "⏸ Paused")
