namespace SpotifyMasher.Services;

public static class AppLogger
{
    public static event Action<string>? LineAdded;

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        LineAdded?.Invoke(line);
    }
}
