using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MorseKeyboard;

/// <summary>How one lighting state looks: the light's color, how much it fills the node, and its glow.</summary>
public sealed record LookSpec(Color Color, byte FillAlpha, Color Stroke, double Blur, double Opacity)
{
    private SolidColorBrush? _brush;

    /// <summary>The light's color as a brush, for symbols and text.</summary>
    public SolidColorBrush Brush => _brush ??= Palette.Solid(Color);
}

/// <summary>How the digits 0–9 are shown under the tree.</summary>
public enum DigitStyle
{
    /// <summary>A bordered chip per digit.</summary>
    Chips,
    /// <summary>A riveted brass tag per digit, engraved like the nameplate (tag.png).</summary>
    Tags,
}

/// <summary>How the strip with the current code, the candidate letter and the history is shown.</summary>
public enum StripStyle
{
    /// <summary>A bordered panel.</summary>
    Panel,
    /// <summary>Paper tape printed in ink (tape.png), the candidate in a glass window (window.png).</summary>
    Tape,
}

/// <summary>A complete visual design for the overlay window. See <see cref="Themes"/> for the built-in ones.</summary>
public sealed class Theme
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    // Window frame and header.
    /// <summary>Created on first use, since the textured ones take a moment to generate.</summary>
    public required Func<Brush> FrameBackground { get; init; }
    public required Brush FrameBorder { get; init; }
    public double FrameBorderThickness { get; init; } = 1;
    public double CornerRadius { get; init; } = 14;
    /// <summary>Decoration drawn over the whole frame (screws, mounting holes).</summary>
    public Func<UIElement>? FrameOverlay { get; init; }
    /// <summary>Distance of the header controls from the frame's sides (more when corners are decorated).</summary>
    public double HeaderInset { get; init; } = 10;
    public required Brush HeaderText { get; init; }
    public required Brush PillBackground { get; init; }
    public required Brush PillBorder { get; init; }
    public required Brush PillText { get; init; }

    // Tree.
    public required Brush EdgeBrush { get; init; }
    public double EdgeThickness { get; init; } = 5;
    public required Brush NodeFill { get; init; }
    public required Brush NodeStroke { get; init; }
    public double NodeStrokeThickness { get; init; } = 3;
    public double DitRadius { get; init; } = 15;
    /// <summary>Half size of a horizontal dah; vertical ones are rotated.</summary>
    public Size DahHalf { get; init; } = new(29, 11);
    public double DahCorner { get; init; } = 7;
    /// <summary>Size of a node's light relative to the node (above 1 for halos around small stars).</summary>
    public double GlowScale { get; init; } = 1;
    /// <summary>Static effect on unlit nodes (e.g. the faint glow of a star).</summary>
    public Effect? NodeEffect { get; init; }
    /// <summary>Extra visuals on top of each unlit node: center, is dah, half size.</summary>
    public Func<Point, bool, Size, IEnumerable<UIElement>>? NodeDecoration { get; init; }
    /// <summary>Visuals behind the tree in canvas coordinates (680 x 760).</summary>
    public Func<IEnumerable<UIElement>>? Backdrop { get; init; }

    public required FontFamily LabelFont { get; init; }
    public FontWeight LabelWeight { get; init; } = FontWeights.SemiBold;
    public FontStyle LabelStyle { get; init; } = FontStyles.Normal;
    public double LabelSize { get; init; } = 27;
    public required Color LabelIdle { get; init; }
    public Effect? LabelEffect { get; init; }
    /// <summary>Draws the letters as engraved and filled (the fill is the label color) instead of plain text.</summary>
    public Engraving? LabelEngraving { get; init; }

    /// <summary>
    /// Where the backdrop has a blank nameplate (tree units); the app engraves its name and version there.
    /// Artwork can move it with "nameplate" in theme.json.
    /// </summary>
    public Rect? NameplateArea { get; init; }
    public Engraving? NameplateEngraving { get; init; }
    public Color NameplateFill { get; init; } = Colors.Black;
    public FontFamily? NameplateFont { get; init; }

    /// <summary>The key at the root of the tree; its stem must cross (333, 150).</summary>
    public string RootGeometry { get; init; } = "M 301,66 L 365,66 L 333,110 Z M 314,77 L 352,77 M 333,77 L 333,162";
    public Brush? RootFill { get; init; }
    public required Brush RootStroke { get; init; }
    public double RootStrokeThickness { get; init; } = 4;
    public Effect? RootEffect { get; init; }
    /// <summary>Whether the root's light fills its shape (for solid roots) or only traces its outline.</summary>
    public bool RootLightFills { get; init; }

    // Lighting states.
    /// <summary>Entered so far.</summary>
    public required LookSpec Path { get; init; }
    /// <summary>The deepest node entered so far.</summary>
    public required LookSpec Current { get; init; }
    /// <summary>Where releasing the key right now would go.</summary>
    public required LookSpec Pending { get; init; }
    /// <summary>Decoded and sent.</summary>
    public required LookSpec Success { get; init; }
    /// <summary>Not a known code.</summary>
    public required LookSpec Error { get; init; }

    // Digit chips.
    public DigitStyle DigitStyle { get; init; } = DigitStyle.Chips;
    public required Brush ChipBackground { get; init; }
    public required Color ChipBorderIdle { get; init; }
    public required Color SymbolIdle { get; init; }
    public double ChipCorner { get; init; } = 10;

    // Status strip.
    public StripStyle StripStyle { get; init; } = StripStyle.Panel;
    /// <summary>Printing on the paper tape (tape style).</summary>
    public Color TapeInk { get; init; } = Color.FromRgb(0x1E, 0x1A, 0x24);
    public Color TapeErrorInk { get; init; } = Color.FromRgb(0xA8, 0x24, 0x18);
    public required Brush StripBackground { get; init; }
    public required Brush StripBorder { get; init; }
    public double StripCorner { get; init; } = 12;
    public required FontFamily HistoryFont { get; init; }
    public required Brush HistoryText { get; init; }
    public required Brush HistoryBright { get; init; }
    public required FontFamily CandidateFont { get; init; }
}
