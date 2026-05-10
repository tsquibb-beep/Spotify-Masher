using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SpotifyMasher.Models;

namespace SpotifyMasher.Controls;

/// <summary>
/// Read-only TextBox that captures a key combo and updates a bound HotkeyBinding.
/// </summary>
public class HotkeyBox : TextBox
{
    public static readonly DependencyProperty BindingTargetProperty =
        DependencyProperty.Register(nameof(BindingTarget), typeof(HotkeyBinding),
            typeof(HotkeyBox), new PropertyMetadata(null, OnBindingTargetChanged));

    public HotkeyBinding? BindingTarget
    {
        get => (HotkeyBinding?)GetValue(BindingTargetProperty);
        set => SetValue(BindingTargetProperty, value);
    }

    public HotkeyBox()
    {
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        Cursor = Cursors.Arrow;
        Text = "Click and press keys…";
    }

    private static void OnBindingTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyBox box && e.NewValue is HotkeyBinding binding && !string.IsNullOrEmpty(binding.KeysDisplay))
            box.Text = binding.KeysDisplay;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore lone modifier presses
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var modifiers = Keyboard.Modifiers;
        var display = BuildDisplay(modifiers, key);

        Text = display;

        if (BindingTarget != null)
        {
            BindingTarget.Modifiers = modifiers;
            BindingTarget.Key = key;
            BindingTarget.KeysDisplay = display;
        }
    }

    private static string BuildDisplay(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
