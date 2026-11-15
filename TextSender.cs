using System.Collections.Generic;
using System.Runtime.InteropServices;
using static MorseKeyboard.NativeMethods;

namespace MorseKeyboard;

/// <summary>Types characters into whatever window has the keyboard focus.</summary>
internal static class TextSender
{
    /// <param name="asKeystrokes">
    /// False: send the character itself (bypasses the IME).
    /// True: press the matching key, so an active IME sees it as romaji input.
    /// </param>
    public static void Send(char c, bool asKeystrokes)
    {
        var inputs = new List<INPUT>(6);
        if (asKeystrokes)
            AddKeystroke(inputs, c);
        else
            AddUnicode(inputs, c);
        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static void AddUnicode(List<INPUT> inputs, char c)
    {
        inputs.Add(Key(0, c, KEYEVENTF_UNICODE));
        inputs.Add(Key(0, c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
    }

    private static void AddKeystroke(List<INPUT> inputs, char c)
    {
        // Virtual-key codes for A–Z and 0–9 equal their uppercase ASCII codes.
        ushort vk = c == ' ' ? (ushort)VK_SPACE : char.ToUpperInvariant(c);
        ushort scan = (ushort)MapVirtualKey(vk, 0);
        bool shift = char.IsUpper(c);

        if (shift) inputs.Add(Key(VK_SHIFT, 0, 0));
        inputs.Add(Key(vk, scan, 0));
        inputs.Add(Key(vk, scan, KEYEVENTF_KEYUP));
        if (shift) inputs.Add(Key(VK_SHIFT, 0, KEYEVENTF_KEYUP));
    }

    private static INPUT Key(int vk, ushort scan, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = (ushort)vk, wScan = scan, dwFlags = flags } },
    };
}
