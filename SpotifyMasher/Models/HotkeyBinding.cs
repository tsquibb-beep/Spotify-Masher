using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace SpotifyMasher.Models;

public class HotkeyBinding : INotifyPropertyChanged
{
    private string _keysDisplay = string.Empty;
    private string _action = "Change Volume";
    private string _parameter = "+5";

    public string KeysDisplay
    {
        get => _keysDisplay;
        set { _keysDisplay = value; OnPropertyChanged(nameof(KeysDisplay)); }
    }

    public string Action
    {
        get => _action;
        set { _action = value; OnPropertyChanged(nameof(Action)); }
    }

    public string Parameter
    {
        get => _parameter;
        set { _parameter = value; OnPropertyChanged(nameof(Parameter)); }
    }

    [JsonIgnore]
    public ModifierKeys Modifiers { get; set; }

    [JsonIgnore]
    public Key Key { get; set; }

    // Serialised as ints so we don't need WPF on the JSON model
    public int ModifiersValue
    {
        get => (int)Modifiers;
        set => Modifiers = (ModifierKeys)value;
    }

    public int KeyValue
    {
        get => (int)Key;
        set => Key = (Key)value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
