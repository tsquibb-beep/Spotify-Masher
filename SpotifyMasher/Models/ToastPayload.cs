namespace SpotifyMasher.Models;

public record ToastPayload(
    string Message,
    byte[]? ImageBytes = null,
    string? TrackName = null,
    string? ArtistName = null,
    string? AlbumName = null);
