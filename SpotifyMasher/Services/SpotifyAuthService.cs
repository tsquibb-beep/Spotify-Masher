using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpotifyMasher.Models;

namespace SpotifyMasher.Services;

public class SpotifyAuthService
{
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";
    private const string AuthEndpoint = "https://accounts.spotify.com/authorize";
    // Spotify receives this (no trailing slash — must match dashboard exactly)
    private const string RedirectUri = "http://127.0.0.1:5001/callback";
    // HttpListener requires a trailing slash on its prefix
    private const string ListenerPrefix = "http://127.0.0.1:5001/callback/";
    private const string Scopes = "user-read-playback-state user-modify-playback-state user-library-modify";

    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpotifyMasher", "tokens.json");

    private readonly HttpClient _http = new();
    private TokenData? _tokens;

    public bool IsAuthenticated => _tokens != null && !string.IsNullOrEmpty(_tokens.RefreshToken);

    public void LoadStoredTokens()
    {
        if (!File.Exists(TokenPath)) return;
        try
        {
            var json = File.ReadAllText(TokenPath);
            _tokens = JsonSerializer.Deserialize<TokenData>(json);
        }
        catch { _tokens = null; }
    }

    public async Task<string> GetValidTokenAsync()
    {
        if (_tokens == null)
            throw new InvalidOperationException("Not authenticated.");

        if (DateTime.UtcNow >= _tokens.ExpiresAt.AddSeconds(-30))
            await RefreshAccessTokenAsync(_tokens.RefreshToken);

        return _tokens!.AccessToken;
    }

    public async Task<bool> StartAuthAsync(string clientId)
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N")[..8];

        var authUrl = $"{AuthEndpoint}" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(Scopes)}" +
            $"&code_challenge_method=S256" +
            $"&code_challenge={codeChallenge}" +
            $"&state={state}";

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var code = await ListenForCallbackAsync(state);
        if (string.IsNullOrEmpty(code)) return false;

        return await ExchangeCodeAsync(clientId, code, codeVerifier);
    }

    private async Task<string?> ListenForCallbackAsync(string expectedState)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(ListenerPrefix);
        listener.Start();

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            var context = await listener.GetContextAsync().WaitAsync(cts.Token);
            var query = context.Request.QueryString;

            var html = "<html><body style='font-family:Segoe UI;background:#1a1a2e;color:#fff;text-align:center;padding-top:80px'>" +
                       "<h2 style='color:#1DB954'>Authorised!</h2><p>You can close this tab and return to Spotify Masher.</p></body></html>";
            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();

            if (query["state"] != expectedState || query["error"] != null) return null;
            return query["code"];
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task<bool> ExchangeCodeAsync(string clientId, string code, string codeVerifier)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = codeVerifier,
        });

        var response = await _http.PostAsync(TokenEndpoint, body);
        if (!response.IsSuccessStatusCode) return false;

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        if (json == null) return false;

        _tokens = new TokenData
        {
            AccessToken = json["access_token"]!.GetValue<string>(),
            RefreshToken = json["refresh_token"]!.GetValue<string>(),
            ExpiresAt = DateTime.UtcNow.AddSeconds(json["expires_in"]!.GetValue<int>()),
        };
        PersistTokens();
        return true;
    }

    private async Task RefreshAccessTokenAsync(string refreshToken)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = LoadClientId(),
        });

        var response = await _http.PostAsync(TokenEndpoint, body);
        response.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        _tokens!.AccessToken = json["access_token"]!.GetValue<string>();
        _tokens!.ExpiresAt = DateTime.UtcNow.AddSeconds(json["expires_in"]!.GetValue<int>());

        // Spotify sometimes rotates the refresh token
        var newRefresh = json["refresh_token"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(newRefresh))
            _tokens!.RefreshToken = newRefresh;

        PersistTokens();
    }

    private void PersistTokens()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenPath)!);
        File.WriteAllText(TokenPath, JsonSerializer.Serialize(_tokens, new JsonSerializerOptions { WriteIndented = true }));
    }

    private string LoadClientId()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyMasher", "config.json");
        if (!File.Exists(configPath)) return string.Empty;
        var json = JsonNode.Parse(File.ReadAllText(configPath));
        return json?["ClientId"]?.GetValue<string>() ?? string.Empty;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
