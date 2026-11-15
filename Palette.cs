using System.Windows.Media;

namespace MorseKeyboard;

/// <summary>Color helpers. The colors themselves live in <see cref="Themes"/>.</summary>
public static class Palette
{
    /// <summary>The header's status dot while input is off, the same in every theme.</summary>
    public static readonly SolidColorBrush OffBrush = Solid(Hex("#6B6F78"));

    public static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
