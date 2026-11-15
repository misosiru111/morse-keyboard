using System.Collections.Generic;
using System.Linq;

namespace MorseKeyboard;

/// <summary>International Morse code for A–Z and 0–9. '.' is a dit, '-' is a dah.</summary>
public static class MorseTable
{
    public static readonly IReadOnlyList<(char Char, string Code)> Letters = new[]
    {
        ('A', ".-"),   ('B', "-..."), ('C', "-.-."), ('D', "-.."),  ('E', "."),    ('F', "..-."),
        ('G', "--."),  ('H', "...."), ('I', ".."),   ('J', ".---"), ('K', "-.-"),  ('L', ".-.."),
        ('M', "--"),   ('N', "-."),   ('O', "---"),  ('P', ".--."), ('Q', "--.-"), ('R', ".-."),
        ('S', "..."),  ('T', "-"),    ('U', "..-"),  ('V', "...-"), ('W', ".--"),  ('X', "-..-"),
        ('Y', "-.--"), ('Z', "--.."),
    };

    /// <summary>Digits in keyboard order (1…9, 0), which is also the order of their code pattern.</summary>
    public static readonly IReadOnlyList<(char Char, string Code)> Digits = new[]
    {
        ('1', ".----"), ('2', "..---"), ('3', "...--"), ('4', "....-"), ('5', "....."),
        ('6', "-...."), ('7', "--..."), ('8', "---.."), ('9', "----."), ('0', "-----"),
    };

    private static readonly Dictionary<string, char> ByCode =
        Letters.Concat(Digits).ToDictionary(e => e.Code, e => e.Char);

    private static readonly HashSet<string> Prefixes = BuildPrefixes();

    public static bool TryDecode(string code, out char c) => ByCode.TryGetValue(code, out c);

    /// <summary>True if <paramref name="code"/> is the start of at least one known character (the empty code included).</summary>
    public static bool IsValidPrefix(string code) => Prefixes.Contains(code);

    private static HashSet<string> BuildPrefixes()
    {
        var set = new HashSet<string> { "" };
        foreach (var code in ByCode.Keys)
            for (int i = 1; i <= code.Length; i++)
                set.Add(code[..i]);
        return set;
    }
}
