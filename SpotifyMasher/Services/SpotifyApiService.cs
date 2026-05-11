using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace SpotifyMasher.Services;

public class SpotifyApiService
{
    private const string BaseUrl = "https://api.spotify.com/v1";

    private readonly HttpClient _http = new();
    private readonly SpotifyAuthService _auth;

    public SpotifyApiService(SpotifyAuthService auth) => _auth = auth;

    public async Task AdjustVolumeAsync(int delta)
    {
        var current = await GetVolumeAsync();
        if (current < 0)
        {
            AppLogger.Log($"AdjustVolume: GetVolume returned {current}, aborting");
            return;
        }

        var newVolume = Math.Clamp(current + delta, 0, 100);
        AppLogger.Log($"AdjustVolume: current={current} delta={delta} new={newVolume}");
        await SetVolumeAsync(newVolume);
    }

    private async Task<int> GetVolumeAsync()
    {
        AppLogger.Log("GetVolume: GET /me/player");
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player");
        var response = await _http.SendAsync(req);

        AppLogger.Log($"GetVolume: HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            AppLogger.Log("GetVolume: 204 No Content — no active Spotify device");
            return -1;
        }

        if (!response.IsSuccessStatusCode)
        {
            AppLogger.Log($"GetVolume: error response");
            return -1;
        }

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return -1;

        var json = JsonNode.Parse(body);
        var vol = json?["device"]?["volume_percent"]?.GetValue<int>() ?? -1;
        AppLogger.Log($"GetVolume: volume_percent={vol}");
        return vol;
    }

    private async Task SetVolumeAsync(int volumePercent)
    {
        AppLogger.Log($"SetVolume: PUT /me/player/volume?volume_percent={volumePercent}");
        var req = await BuildRequest(HttpMethod.Put,
            $"{BaseUrl}/me/player/volume?volume_percent={volumePercent}");
        req.Content = new StringContent(string.Empty);
        var response = await _http.SendAsync(req);
        AppLogger.Log($"SetVolume: HTTP {(int)response.StatusCode} {response.StatusCode}");
    }

    public async Task PlayPauseAsync()
    {
        AppLogger.Log("PlayPause: GET current state");
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player");
        var response = await _http.SendAsync(req);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
        {
            AppLogger.Log("PlayPause: no active device, attempting play");
            var startReq = await BuildRequest(HttpMethod.Put, $"{BaseUrl}/me/player/play");
            startReq.Content = new StringContent(string.Empty);
            await _http.SendAsync(startReq);
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(body);
        var isPlaying = json?["is_playing"]?.GetValue<bool>() ?? false;

        if (isPlaying)
        {
            AppLogger.Log("PlayPause: pausing");
            var pauseReq = await BuildRequest(HttpMethod.Put, $"{BaseUrl}/me/player/pause");
            pauseReq.Content = new StringContent(string.Empty);
            var r = await _http.SendAsync(pauseReq);
            AppLogger.Log($"PlayPause: HTTP {(int)r.StatusCode}");
        }
        else
        {
            AppLogger.Log("PlayPause: resuming");
            var playReq = await BuildRequest(HttpMethod.Put, $"{BaseUrl}/me/player/play");
            playReq.Content = new StringContent(string.Empty);
            var r = await _http.SendAsync(playReq);
            AppLogger.Log($"PlayPause: HTTP {(int)r.StatusCode}");
        }
    }

    public async Task NextTrackAsync()
    {
        AppLogger.Log("NextTrack: POST /me/player/next");
        var req = await BuildRequest(HttpMethod.Post, $"{BaseUrl}/me/player/next");
        req.Content = new StringContent(string.Empty);
        var response = await _http.SendAsync(req);
        AppLogger.Log($"NextTrack: HTTP {(int)response.StatusCode}");
    }

    public async Task PreviousTrackAsync()
    {
        AppLogger.Log("PreviousTrack: POST /me/player/previous");
        var req = await BuildRequest(HttpMethod.Post, $"{BaseUrl}/me/player/previous");
        req.Content = new StringContent(string.Empty);
        var response = await _http.SendAsync(req);
        AppLogger.Log($"PreviousTrack: HTTP {(int)response.StatusCode}");
    }

    public async Task SeekAsync(int deltaSeconds)
    {
        AppLogger.Log($"Seek: GET current position, then seek by {deltaSeconds}s");
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player");
        var response = await _http.SendAsync(req);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
        {
            AppLogger.Log("Seek: no active device, aborting");
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return;

        var json = System.Text.Json.Nodes.JsonNode.Parse(body);
        var currentMs = json?["progress_ms"]?.GetValue<long>() ?? 0;
        var durationMs = json?["item"]?["duration_ms"]?.GetValue<long>() ?? long.MaxValue;

        var newMs = Math.Clamp(currentMs + (deltaSeconds * 1000L), 0, durationMs);
        AppLogger.Log($"Seek: position {currentMs}ms → {newMs}ms");

        var seekReq = await BuildRequest(HttpMethod.Put, $"{BaseUrl}/me/player/seek?position_ms={newMs}");
        seekReq.Content = new StringContent(string.Empty);
        var seekResponse = await _http.SendAsync(seekReq);
        AppLogger.Log($"Seek: HTTP {(int)seekResponse.StatusCode}");
    }

    public async Task AddToPlaylistAsync(string parameter)
    {
        // Accept full URI "spotify:playlist:ID" or bare playlist ID
        var playlistId = parameter.StartsWith("spotify:playlist:", StringComparison.OrdinalIgnoreCase)
            ? parameter["spotify:playlist:".Length..]
            : parameter.Trim();

        if (string.IsNullOrEmpty(playlistId))
        {
            AppLogger.Log("AddToPlaylist: no playlist ID in parameter, aborting");
            return;
        }

        AppLogger.Log($"AddToPlaylist: GET currently playing");
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player/currently-playing");
        var response = await _http.SendAsync(req);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
        {
            AppLogger.Log("AddToPlaylist: nothing playing, aborting");
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(body);
        var trackUri = json?["item"]?["uri"]?.GetValue<string>();

        if (string.IsNullOrEmpty(trackUri))
        {
            AppLogger.Log("AddToPlaylist: could not get track URI");
            return;
        }

        AppLogger.Log($"AddToPlaylist: adding {trackUri} to playlist {playlistId}");
        var addReq = await BuildRequest(HttpMethod.Post, $"{BaseUrl}/playlists/{playlistId}/tracks");
        addReq.Content = new System.Net.Http.StringContent(
            $"{{\"uris\":[\"{trackUri}\"]}}", System.Text.Encoding.UTF8, "application/json");
        var addResponse = await _http.SendAsync(addReq);
        AppLogger.Log($"AddToPlaylist: HTTP {(int)addResponse.StatusCode}");
    }

    public async Task LikeCurrentTrackAsync()
    {
        AppLogger.Log("LikeTrack: GET currently playing");
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player/currently-playing");
        var response = await _http.SendAsync(req);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
        {
            AppLogger.Log("LikeTrack: nothing playing, aborting");
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return;

        var json = System.Text.Json.Nodes.JsonNode.Parse(body);
        var trackId = json?["item"]?["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(trackId))
        {
            AppLogger.Log("LikeTrack: could not get track ID");
            return;
        }

        AppLogger.Log($"LikeTrack: saving track {trackId}");
        var likeReq = await BuildRequest(HttpMethod.Put, $"{BaseUrl}/me/tracks");
        likeReq.Content = new System.Net.Http.StringContent(
            $"{{\"ids\":[\"{trackId}\"]}}", System.Text.Encoding.UTF8, "application/json");
        var likeResponse = await _http.SendAsync(likeReq);
        AppLogger.Log($"LikeTrack: HTTP {(int)likeResponse.StatusCode}");
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string url)
    {
        var token = await _auth.GetValidTokenAsync();
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
