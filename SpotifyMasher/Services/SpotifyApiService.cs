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

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string url)
    {
        var token = await _auth.GetValidTokenAsync();
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
