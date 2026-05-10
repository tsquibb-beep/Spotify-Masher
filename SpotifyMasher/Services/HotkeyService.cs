using NHotkey.Wpf;
using SpotifyMasher.Models;

namespace SpotifyMasher.Services;

public class HotkeyService
{
    private readonly SpotifyApiService _spotify;
    private readonly List<string> _registeredNames = [];

    public event Action<string>? HotkeyFired;
    public event Action<string>? RegistrationFailed;

    public HotkeyService(SpotifyApiService spotify) => _spotify = spotify;

    public void RegisterAll(IEnumerable<HotkeyBinding> bindings)
    {
        UnregisterAll();

        int index = 0;
        foreach (var binding in bindings)
        {
            AppLogger.Log($"RegisterAll: processing binding Key={binding.Key} Modifiers={binding.Modifiers} Action='{binding.Action}' Param='{binding.Parameter}'");

            if (binding.Key == System.Windows.Input.Key.None)
            {
                AppLogger.Log("  → skipped (Key is None)");
                continue;
            }

            var name = $"hotkey_{index++}";
            var captured = binding;

            try
            {
                HotkeyManager.Current.AddOrReplace(name, binding.Key, binding.Modifiers,
                    (_, _) => HandleHotkey(captured));
                _registeredNames.Add(name);
                AppLogger.Log($"  → registered as '{name}' OK");
            }
            catch (Exception ex)
            {
                var msg = $"{binding.KeysDisplay} — {ex.Message}";
                AppLogger.Log($"  → FAILED: {msg}");
                RegistrationFailed?.Invoke(msg);
            }
        }

        AppLogger.Log($"RegisterAll complete: {_registeredNames.Count} registered");
    }

    public void UnregisterAll()
    {
        foreach (var name in _registeredNames)
        {
            try { HotkeyManager.Current.Remove(name); }
            catch { }
        }
        if (_registeredNames.Count > 0)
            AppLogger.Log($"Unregistered {_registeredNames.Count} hotkeys");
        _registeredNames.Clear();
    }

    private void HandleHotkey(HotkeyBinding binding)
    {
        AppLogger.Log($"Hotkey FIRED: Key={binding.Key} Modifiers={binding.Modifiers} Action='{binding.Action}' Param='{binding.Parameter}'");
        HotkeyFired?.Invoke(binding.Action);

        Task task = binding.Action switch
        {
            "Play / Pause"  => _spotify.PlayPauseAsync(),
            "Change Volume" => HandleChangeVolume(binding.Parameter),
            "Next Track"    => _spotify.NextTrackAsync(),
            "Previous Track"=> _spotify.PreviousTrackAsync(),
            "Seek"          => HandleSeek(binding.Parameter),
            "Add to Liked"  => _spotify.LikeCurrentTrackAsync(),
            _ => LogUnknown(binding.Action)
        };

        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                AppLogger.Log($"  → action FAILED: {t.Exception?.GetBaseException().Message}");
        });
    }

    private Task HandleChangeVolume(string parameter)
    {
        if (!int.TryParse(parameter.Replace("%", "").Trim(), out int delta))
        {
            AppLogger.Log($"  → ignored (could not parse '{parameter}' as int)");
            return Task.CompletedTask;
        }
        AppLogger.Log($"  → AdjustVolumeAsync({delta})");
        return _spotify.AdjustVolumeAsync(delta);
    }

    private Task HandleSeek(string parameter)
    {
        if (!int.TryParse(parameter.Replace("s", "").Trim(), out int seconds))
        {
            AppLogger.Log($"  → ignored (could not parse '{parameter}' as int seconds)");
            return Task.CompletedTask;
        }
        AppLogger.Log($"  → SeekAsync({seconds}s)");
        return _spotify.SeekAsync(seconds);
    }

    private static Task LogUnknown(string action)
    {
        AppLogger.Log($"  → ignored (unknown action '{action}')");
        return Task.CompletedTask;
    }
}
