using static MorseKeyboard.NativeMethods;

namespace MorseKeyboard;

/// <summary>The key that acts as the Morse key (電鍵).</summary>
public enum KeyerKey
{
    Space,
    RightCtrl,
    RightAlt,
    Muhenkan,
    Henkan,
}

public static class KeyerKeyExtensions
{
    /// <summary>Virtual-key code as reported to a low-level hook (left/right specific).</summary>
    public static int VirtualKey(this KeyerKey key) => key switch
    {
        KeyerKey.RightCtrl => VK_RCONTROL,
        KeyerKey.RightAlt => VK_RMENU,
        KeyerKey.Muhenkan => VK_NONCONVERT,
        KeyerKey.Henkan => VK_CONVERT,
        _ => VK_SPACE,
    };

    public static string DisplayName(this KeyerKey key) => key switch
    {
        KeyerKey.RightCtrl => "右Ctrl",
        KeyerKey.RightAlt => "右Alt",
        KeyerKey.Muhenkan => "無変換",
        KeyerKey.Henkan => "変換",
        _ => "スペース",
    };
}
