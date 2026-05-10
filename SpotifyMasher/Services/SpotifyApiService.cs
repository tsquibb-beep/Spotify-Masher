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
        if (current < 0) return;
        var newVolume = Math.Clamp(current + delta, 0, 100);
        await SetVolumeAsync(newVolume);
    }

    private async Task<int> GetVolumeAsync()
    {
        var req = await BuildRequest(HttpMethod.Get, $"{BaseUrl}/me/player");
        var response = await _http.SendAsync(req);

        // 204 = no active device; anything else non-2xx is an error
        if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return -1;

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return -1;

        var json = JsonNode.Parse(body);
        return json?["device"]?["volume_percent"]?.GetValue<int>() ?? -1;
    }

    private async Task SetVolumeAsync(int volumePercent)
    {
        var req = await BuildRequest(HttpMethod.Put,
            $"{BaseUrl}/me/player/volume?volume_percent={volumePercent}");
        req.Content = new StringContent(string.Empty);
        await _http.SendAsync(req);
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string url)
    {
        var token = await _auth.GetValidTokenAsync();
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
