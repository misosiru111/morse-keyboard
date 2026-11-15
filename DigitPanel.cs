using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MorseKeyboard;

/// <summary>
/// Digits are five elements long, too deep for the tree, so they get a chip (or a brass tag) each.
/// The part of each code that matches the input so far lights up.
/// </summary>
public sealed class DigitPanel : UniformGrid
{
    /// <param name="Symbols">Fill of each dot/dash; lit by changing its color.</param>
    /// <param name="LabelIdle">Color of the digit when unlit.</param>
    /// <param name="SymbolIdle">Color of the dots and dashes when unlit.</param>
    /// <param name="SymbolViews">Tags only: the dot/dash elements, which glow when lit (a light's color alone is close to brass).</param>
    /// <param name="LabelView">Tags only: the digit, which glows when its whole code is entered.</param>
    private sealed record Chip(char Digit, string Code, FrameworkElement Root, SolidColorBrush BorderBrush,
        SolidColorBrush LabelBrush, SolidColorBrush[] Symbols, Color LabelIdle, Color SymbolIdle,
        UIElement[]? SymbolViews = null, UIElement? LabelView = null);

    private const double TagWidth = 118;

    private static readonly Duration FlashDuration = TimeSpan.FromMilliseconds(750);

    /// <summary>Engraving for tags when the theme has no nameplate engraving.</summary>
    private static readonly Engraving DefaultTagEngraving = new(1.0, Color.FromArgb(0x70, 0, 0, 0),
        Color.FromArgb(0x60, 0xE8, 0xC8, 0x8A), Color.FromArgb(0xD8, 0xFF, 0xF4, 0xD2));

    private readonly List<Chip> _chips = new();
    private readonly Dictionary<Color, Effect> _glows = new();
    private Theme _theme = Themes.Classic;

    public DigitPanel()
    {
        Rows = 2;
        Columns = 5;
        Width = MorseTreeCanvas.CanvasWidth;
        IsHitTestVisible = false;
        ApplyTheme(Themes.Classic, null);
    }

    /// <summary>Rebuilds the chips for <paramref name="theme"/>; call <see cref="Render"/> afterwards.</summary>
    public void ApplyTheme(Theme theme, ThemeAssets? assets)
    {
        _theme = theme;
        _chips.Clear();
        Children.Clear();
        foreach (var (digit, code) in MorseTable.Digits)
        {
            var chip = theme.DigitStyle == DigitStyle.Tags ? BuildTag(digit, code, assets) : BuildChip(digit, code);
            _chips.Add(chip);
            Children.Add(chip.Root);
        }
    }

    public void Render(string code, char? pending, bool invalid)
    {
        string probe = code + pending;
        bool tags = _theme.DigitStyle == DigitStyle.Tags;

        foreach (var chip in _chips)
        {
            bool codeMatch = !invalid && chip.Code.StartsWith(code, StringComparison.Ordinal);
            bool probeMatch = codeMatch && chip.Code.StartsWith(probe, StringComparison.Ordinal);

            // Brass tags stay readable when dimmed less.
            chip.Root.Opacity = probe.Length == 0 || probeMatch ? 1 : codeMatch ? 0.6 : tags ? 0.45 : 0.28;

            for (int i = 0; i < chip.Symbols.Length; i++)
            {
                Color? lit =
                    codeMatch && i < code.Length ? _theme.Path.Color
                    : probeMatch && pending is not null && i == code.Length ? _theme.Pending.Color
                    : null;
                if (chip.SymbolViews is { } views)
                {
                    chip.Symbols[i].Color = lit is Color c ? WhiteHot(c) : chip.SymbolIdle;
                    views[i].Effect = lit is Color g ? Glow(g) : null;
                }
                else
                {
                    chip.Symbols[i].Color = lit ?? chip.SymbolIdle;
                }
            }

            Color? accent =
                codeMatch && pending is null && code == chip.Code ? _theme.Path.Color
                : probeMatch && pending is not null && probe == chip.Code ? _theme.Pending.Color
                : null;

            if (accent is Color color)
            {
                chip.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                chip.LabelBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                chip.BorderBrush.Color = color;
                chip.LabelBrush.Color = chip.LabelView is null ? color : WhiteHot(color);
                if (chip.LabelView is { } label) label.Effect = Glow(color);
            }
            else
            {
                chip.BorderBrush.Color = _theme.ChipBorderIdle;
                chip.LabelBrush.Color = chip.LabelIdle;
                if (chip.LabelView is { } label) label.Effect = null;
            }
        }
    }

    public void Flash(char digit)
    {
        var chip = _chips.FirstOrDefault(c => c.Digit == digit);
        if (chip is null) return;

        chip.BorderBrush.Color = _theme.ChipBorderIdle;
        chip.LabelBrush.Color = chip.LabelIdle;
        chip.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(_theme.Success.Color, _theme.ChipBorderIdle, FlashDuration) { FillBehavior = FillBehavior.Stop });
        chip.LabelBrush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(_theme.Success.Color, chip.LabelIdle, FlashDuration) { FillBehavior = FillBehavior.Stop });
    }

    private Chip BuildChip(char digit, string code)
    {
        var borderBrush = new SolidColorBrush(_theme.ChipBorderIdle);
        var labelBrush = new SolidColorBrush(_theme.LabelIdle);
        var symbolBrushes = code.Select(_ => new SolidColorBrush(_theme.SymbolIdle)).ToArray();

        var symbolRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        };
        for (int i = 0; i < code.Length; i++)
        {
            Shape s = code[i] == '.'
                ? new Ellipse { Width = 9, Height = 9 }
                : new Rectangle { Width = 17, Height = 9, RadiusX = 4, RadiusY = 4 };
            s.Fill = symbolBrushes[i];
            s.Margin = new Thickness(2, 0, 2, 0);
            s.VerticalAlignment = VerticalAlignment.Center;
            symbolRow.Children.Add(s);
        }

        var label = new TextBlock
        {
            Text = digit.ToString(),
            FontFamily = _theme.LabelFont,
            FontSize = 26,
            FontWeight = _theme.LabelWeight,
            FontStyle = _theme.LabelStyle,
            Foreground = labelBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var root = new Border
        {
            CornerRadius = new CornerRadius(_theme.ChipCorner),
            BorderThickness = new Thickness(2),
            BorderBrush = borderBrush,
            Background = _theme.ChipBackground,
            Margin = new Thickness(6),
            Padding = new Thickness(4, 2, 4, 10),
            Child = new StackPanel { Children = { label, symbolRow } },
        };

        return new Chip(digit, code, root, borderBrush, labelBrush, symbolBrushes, _theme.LabelIdle, _theme.SymbolIdle);
    }

    /// <summary>A riveted brass tag with the digit and its code engraved and filled with enamel.</summary>
    private Chip BuildTag(char digit, string code, ThemeAssets? assets)
    {
        var engraving = _theme.NameplateEngraving ?? DefaultTagEngraving;
        var enamel = _theme.NameplateFill;
        var family = _theme.NameplateFont ?? _theme.LabelFont;
        var labelBrush = new SolidColorBrush(enamel);
        var symbolBrushes = code.Select(_ => new SolidColorBrush(enamel)).ToArray();

        var art = assets?.Get("tag");
        double height = art is null ? 58 : Math.Clamp(TagWidth * art.PixelHeight / art.PixelWidth, 44, 80);

        var symbolRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0),
        };
        var symbolEngraving = engraving with { Depth = engraving.Depth * 0.8 };
        var symbolViews = new UIElement[code.Length];
        for (int i = 0; i < code.Length; i++)
        {
            // Small enough that five dashes stay clear of the rivets at the tag's ends.
            Geometry shape = code[i] == '.'
                ? new EllipseGeometry(new Rect(0, 0, 7, 7))
                : new RectangleGeometry(new Rect(0, 0, 12, 7), 2.5, 2.5);
            symbolViews[i] = new EngravedText(shape, symbolBrushes[i], symbolEngraving)
            {
                Margin = new Thickness(1.5, 0, 1.5, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            symbolRow.Children.Add(symbolViews[i]);
        }

        var label = new EngravedText(digit.ToString(), family, 22, FontWeights.Bold, FontStyles.Normal, labelBrush, engraving)
            { HorizontalAlignment = HorizontalAlignment.Center };
        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { label, symbolRow },
        };

        UIElement plate = art is not null
            ? new Image { Source = art, Stretch = Stretch.Fill }
            : DrawnTag();
        plate.Effect = TagShadow();
        RenderOptions.SetBitmapScalingMode(plate, BitmapScalingMode.HighQuality);

        var root = new Grid
        {
            Width = TagWidth,
            Height = height,
            Margin = new Thickness(0, 5, 0, 5),
            Children = { plate, content },
        };

        // Tags have no border to light; the brush exists so the chip logic stays the same.
        return new Chip(digit, code, root, new SolidColorBrush(), labelBrush, symbolBrushes, enamel, enamel, symbolViews, label);
    }

    /// <summary>A light's color pushed toward white, for a glowing core that reads on brass.</summary>
    private static Color WhiteHot(Color c) =>
        Color.FromRgb((byte)(c.R + (255 - c.R) * 0.6), (byte)(c.G + (255 - c.G) * 0.6), (byte)(c.B + (255 - c.B) * 0.6));

    private Effect Glow(Color color)
    {
        if (!_glows.TryGetValue(color, out var glow))
        {
            var effect = new DropShadowEffect { Color = color, ShadowDepth = 0, BlurRadius = 12, Opacity = 1 };
            effect.Freeze();
            _glows[color] = glow = effect;
        }
        return glow;
    }

    /// <summary>A brass tag drawn in code, used until tag.png artwork exists.</summary>
    private static UIElement DrawnTag()
    {
        var brass = new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromRgb(0xF4, 0xDB, 0x98), 0),
            new(Color.FromRgb(0xC9, 0x9E, 0x4C), 0.45),
            new(Color.FromRgb(0x8A, 0x63, 0x24), 1),
        }, new Point(0.3, 0), new Point(0.7, 1));
        var grid = new Grid();
        grid.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(5),
            Background = brass,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x32, 0x0E)),
            BorderThickness = new Thickness(1.2),
        });
        foreach (var align in new[] { HorizontalAlignment.Left, HorizontalAlignment.Right })
        {
            grid.Children.Add(new Ellipse
            {
                Width = 7,
                Height = 7,
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(7, 0, 7, 0),
                Fill = new RadialGradientBrush(Color.FromRgb(0xFF, 0xF2, 0xC6), Color.FromRgb(0x6A, 0x48, 0x14))
                    { GradientOrigin = new Point(0.35, 0.3) },
            });
        }
        return grid;
    }

    private static Effect TagShadow()
    {
        var shadow = new DropShadowEffect { Color = Colors.Black, Direction = 315, ShadowDepth = 3, BlurRadius = 7, Opacity = 0.65 };
        shadow.Freeze();
        return shadow;
    }
}
