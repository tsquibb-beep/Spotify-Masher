using NHotkey;
using NHotkey.Wpf;
using SpotifyMasher.Models;

namespace SpotifyMasher.Services;

public class HotkeyService
{
    private readonly SpotifyApiService _spotify;
    private readonly List<string> _registeredNames = [];

    public HotkeyService(SpotifyApiService spotify) => _spotify = spotify;

    public void RegisterAll(IEnumerable<HotkeyBinding> bindings)
    {
        UnregisterAll();

        int index = 0;
        foreach (var binding in bindings)
        {
            if (binding.Key == System.Windows.Input.Key.None) continue;

            var name = $"hotkey_{index++}";
            var captured = binding;

            try
            {
                HotkeyManager.Current.AddOrReplace(name, binding.Key, binding.Modifiers,
                    (_, _) => HandleHotkey(captured));
                _registeredNames.Add(name);
            }
            catch
            {
                // Hotkey already registered by another app — skip silently
            }
        }
    }

    public void UnregisterAll()
    {
        foreach (var name in _registeredNames)
        {
            try { HotkeyManager.Current.Remove(name); }
            catch { }
        }
        _registeredNames.Clear();
    }

    private void HandleHotkey(HotkeyBinding binding)
    {
        if (binding.Action != "ChangeVolume") return;

        if (!int.TryParse(binding.Parameter.Replace("%", "").Trim(), out int delta))
            return;

        _ = _spotify.AdjustVolumeAsync(delta);
    }
}
