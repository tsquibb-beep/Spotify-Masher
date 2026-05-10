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

        if (binding.Action != "Change Volume")
        {
            AppLogger.Log($"  → ignored (unknown action '{binding.Action}')");
            return;
        }

        if (!int.TryParse(binding.Parameter.Replace("%", "").Trim(), out int delta))
        {
            AppLogger.Log($"  → ignored (could not parse parameter '{binding.Parameter}' as int)");
            return;
        }

        AppLogger.Log($"  → calling AdjustVolumeAsync({delta})");
        HotkeyFired?.Invoke($"Volume {(delta >= 0 ? "+" : "")}{delta}%");

        _ = _spotify.AdjustVolumeAsync(delta).ContinueWith(t =>
        {
            if (t.IsFaulted)
                AppLogger.Log($"  → AdjustVolumeAsync FAILED: {t.Exception?.GetBaseException().Message}");
            else
                AppLogger.Log($"  → AdjustVolumeAsync completed OK");
        });
    }
}
