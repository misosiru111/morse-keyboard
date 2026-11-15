using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace MorseKeyboard;

/// <summary>The built-in designs. Only system fonts and generated textures are used, so all of them can ship.</summary>
public static class Themes
{
    public static readonly Theme Classic = new()
    {
        Id = "classic",
        Name = "クラシック",
        FrameBackground = () => Frozen(new LinearGradientBrush(Hex("#30323A"), Hex("#1E1F25"), 90)),
        FrameBorder = Solid("#3A3D46"),
        HeaderText = Solid("#8D929C"),
        PillBackground = Solid("#24262C"),
        PillBorder = Solid("#3A3D46"),
        PillText = Solid("#E6E8EC"),

        EdgeBrush = Solid("#4A4D55"),
        NodeFill = Frozen(new LinearGradientBrush(Hex("#0C0D10"), Hex("#2B2D33"), 90)),
        NodeStroke = Frozen(new LinearGradientBrush(Hex("#E4E7EC"), Hex("#767A83"), 90)),
        LabelFont = new FontFamily("Segoe UI"),
        LabelIdle = Hex("#C3C7CF"),
        RootFill = Frozen(new LinearGradientBrush(Hex("#0C0D10"), Hex("#2B2D33"), 90)),
        RootStroke = Frozen(new LinearGradientBrush(Hex("#E4E7EC"), Hex("#767A83"), 90)),

        Path = new(Hex("#FFB13B"), 200, Hex("#FFD98A"), 16, 0.9),
        Current = new(Hex("#FFD98A"), 255, Hex("#FFF5DE"), 32, 1.0),
        Pending = new(Hex("#4CCFFF"), 90, Hex("#4CCFFF"), 22, 1.0),
        Success = new(Hex("#5CF29A"), 220, Hex("#D9FFE8"), 28, 1.0),
        Error = new(Hex("#FF5A5A"), 200, Hex("#FFA3A3"), 20, 0.95),

        ChipBackground = Solid("#18191E"),
        ChipBorderIdle = Hex("#30333B"),
        SymbolIdle = Hex("#575B65"),
        StripBackground = Solid("#15161A"),
        StripBorder = Solid("#2E3138"),
        HistoryFont = new FontFamily("Consolas"),
        HistoryText = Solid("#7E838D"),
        HistoryBright = Solid("#F1F2F4"),
        CandidateFont = new FontFamily("Segoe UI"),
    };

    /// <summary>電信局: walnut, brass fittings and incandescent lamps.</summary>
    public static readonly Theme Telegraph = new()
    {
        Id = "telegraph",
        Name = "電信局",
        FrameBackground = () => Textures.Wood,
        FrameBorder = Solid("#1A0D05"),
        FrameBorderThickness = 2,
        CornerRadius = 10,
        HeaderInset = 26,
        FrameOverlay = () => Screws(Hex("#F4DC9A"), Hex("#7A5A22")),
        HeaderText = Solid("#D9C49A"),
        PillBackground = Solid("#CC1E1208"),
        PillBorder = Solid("#9A7636"),
        PillText = Solid("#F0DDB0"),

        EdgeBrush = Solid("#A07D3F"),
        EdgeThickness = 6,
        NodeFill = Frozen(new RadialGradientBrush(Hex("#5E3D18"), Hex("#1C1006")) { GradientOrigin = new Point(0.4, 0.35) }),
        NodeStroke = Frozen(new LinearGradientBrush(Hex("#F6DC96"), Hex("#80601F"), 90)),
        NodeStrokeThickness = 3.5,
        NodeDecoration = GlassHighlight,
        Backdrop = TelegraphPlate,
        LabelFont = new FontFamily("Georgia"),
        LabelWeight = FontWeights.Bold,
        // Letters cut into the walnut and filled with gilt; lit letters take the light's color.
        // Gold, set off from the wood by a soft dark halo; lit letters differ by hue (amber, blue, green, red).
        LabelIdle = Hex("#CDAE68"),
        LabelEffect = Halo(),
        LabelEngraving = new Engraving(1.6, Hex("#A0140A03"), Hex("#70FFF4D6"), Hex("#55FFE6B8"),
            Frozen(new LinearGradientBrush(Stops((Hex("#38FFFFFF"), 0), (Hex("#00FFFFFF"), 0.5), (Hex("#30000000"), 1)), 90))),
        // A brass plate engraved and filled with black enamel.
        NameplateArea = new Rect(20, 26, 252, 50),
        NameplateEngraving = new Engraving(1.2, Hex("#70000000"), Hex("#60E8C88A"), Hex("#D8FFF4D2")),
        NameplateFill = Hex("#120A03"),
        NameplateFont = new FontFamily("Georgia"),
        DigitStyle = DigitStyle.Tags,
        StripStyle = StripStyle.Tape,
        RootFill = Frozen(new RadialGradientBrush(Hex("#5E3D18"), Hex("#1C1006"))),
        RootStroke = Frozen(new LinearGradientBrush(Hex("#F6DC96"), Hex("#80601F"), 90)),

        Path = new(Hex("#FFA83D"), 210, Hex("#FFD48A"), 20, 0.92),
        Current = new(Hex("#FFD98A"), 255, Hex("#FFF6DD"), 34, 1.0),
        Pending = new(Hex("#8FC4FF"), 110, Hex("#BFDDFF"), 22, 1.0),
        Success = new(Hex("#7CE38B"), 220, Hex("#D8FFE0"), 28, 1.0),
        Error = new(Hex("#FF5A3C"), 210, Hex("#FFB09C"), 22, 0.95),

        ChipBackground = Solid("#C8241609"),
        ChipBorderIdle = Hex("#7A5A2A"),
        SymbolIdle = Hex("#6E5634"),
        ChipCorner = 6,
        StripBackground = Solid("#E6170E06"),
        StripBorder = Solid("#8A6A30"),
        StripCorner = 6,
        HistoryFont = new FontFamily("Courier New"),
        HistoryText = Solid("#B8A27A"),
        HistoryBright = Solid("#F5E6C0"),
        CandidateFont = new FontFamily("Georgia"),
    };

    /// <summary>軍用無線機: olive drab enamel, stencil lettering, dial lamps.</summary>
    public static readonly Theme Military = new()
    {
        Id = "military",
        Name = "軍用無線機",
        FrameBackground = () => Textures.Olive,
        FrameBorder = Solid("#23271A"),
        FrameBorderThickness = 2,
        CornerRadius = 6,
        HeaderInset = 26,
        FrameOverlay = () => Screws(Hex("#D9DBD0"), Hex("#4C4F44")),
        HeaderText = Solid("#D6D1B4"),
        PillBackground = Solid("#2B2F1C"),
        PillBorder = Solid("#8C907C"),
        PillText = Solid("#EDE8CF"),

        EdgeBrush = Solid("#2C301E"),
        EdgeThickness = 6,
        NodeFill = Frozen(new RadialGradientBrush(Hex("#2A2C22"), Hex("#0E0F0A")) { GradientOrigin = new Point(0.4, 0.35) }),
        NodeStroke = Frozen(new LinearGradientBrush(Hex("#D2D4C8"), Hex("#5E6155"), 90)),
        DahCorner = 3,
        NodeDecoration = LampLens,
        Backdrop = MilitaryStencil,
        LabelFont = new FontFamily("Bahnschrift"),
        LabelWeight = FontWeights.Bold,
        LabelIdle = Hex("#E8E3C6"),
        RootFill = Solid("#16170F"),
        RootStroke = Frozen(new LinearGradientBrush(Hex("#D2D4C8"), Hex("#5E6155"), 90)),

        Path = new(Hex("#FF9A1F"), 210, Hex("#FFC870"), 18, 0.92),
        Current = new(Hex("#FFC85A"), 255, Hex("#FFF0CC"), 30, 1.0),
        Pending = new(Hex("#E8F4FF"), 120, Hex("#FFFFFF"), 20, 1.0),
        Success = new(Hex("#8DFF5A"), 220, Hex("#E2FFD0"), 26, 1.0),
        Error = new(Hex("#FF3B2B"), 210, Hex("#FF9F95"), 20, 0.95),

        ChipBackground = Solid("#262A18"),
        ChipBorderIdle = Hex("#5F6450"),
        SymbolIdle = Hex("#6B705A"),
        ChipCorner = 3,
        StripBackground = Solid("#10120C"),
        StripBorder = Solid("#5F6450"),
        StripCorner = 3,
        HistoryFont = new FontFamily("Consolas"),
        HistoryText = Solid("#A9A58C"),
        HistoryBright = Solid("#F0EBD0"),
        CandidateFont = new FontFamily("Bahnschrift"),
    };

    /// <summary>星座: the tree as a constellation on a night sky.</summary>
    public static readonly Theme Constellation = new()
    {
        Id = "constellation",
        Name = "星座",
        FrameBackground = () => Textures.Night,
        FrameBorder = Solid("#2A3566"),
        CornerRadius = 16,
        HeaderText = Solid("#8F9CCB"),
        PillBackground = Solid("#10173A"),
        PillBorder = Solid("#34427A"),
        PillText = Solid("#DDE4FF"),

        EdgeBrush = Solid("#55A8B8FF"),
        EdgeThickness = 1.6,
        NodeFill = Frozen(new RadialGradientBrush(Hex("#FFFFFF"), Hex("#AFC2FF"))),
        NodeStroke = Brushes.Transparent,
        NodeStrokeThickness = 0,
        DitRadius = 7,
        DahHalf = new Size(17, 4.5),
        DahCorner = 4.5,
        GlowScale = 1.6,
        NodeEffect = Glow(Hex("#C8D6FF"), 14),
        Backdrop = CelestialGrid,
        LabelFont = new FontFamily("Georgia"),
        LabelWeight = FontWeights.Normal,
        LabelStyle = FontStyles.Italic,
        LabelSize = 25,
        LabelIdle = Hex("#A9B7E8"),
        RootGeometry = "M 333,116 C 336,144 339,147 367,150 C 339,153 336,156 333,184 C 330,156 327,153 299,150 C 327,147 330,144 333,116 Z",
        RootFill = Frozen(new RadialGradientBrush(Hex("#FFFFFF"), Hex("#B9C9FF"))),
        RootStroke = Brushes.Transparent,
        RootStrokeThickness = 0,
        RootEffect = Glow(Hex("#C8D6FF"), 22),
        RootLightFills = true,

        Path = new(Hex("#FFD36B"), 170, Hex("#FFE9B0"), 22, 0.95),
        Current = new(Hex("#FFF6DA"), 230, Hex("#FFFFFF"), 36, 1.0),
        Pending = new(Hex("#7FD8FF"), 110, Hex("#C5EEFF"), 26, 1.0),
        Success = new(Hex("#9DFFC8"), 200, Hex("#E6FFF1"), 30, 1.0),
        Error = new(Hex("#FF6B81"), 190, Hex("#FFB3C0"), 24, 0.95),

        ChipBackground = Solid("#A00D1433"),
        ChipBorderIdle = Hex("#2C3A72"),
        SymbolIdle = Hex("#46558F"),
        ChipCorner = 14,
        StripBackground = Solid("#B0080C22"),
        StripBorder = Solid("#2C3A72"),
        StripCorner = 16,
        HistoryFont = new FontFamily("Georgia"),
        HistoryText = Solid("#8F9CCB"),
        HistoryBright = Solid("#FFFFFF"),
        CandidateFont = new FontFamily("Georgia"),
    };

    /// <summary>回路基板: solder mask, gold pads and traces, LEDs.</summary>
    public static readonly Theme Circuit = new()
    {
        Id = "circuit",
        Name = "回路基板",
        FrameBackground = () => Textures.Pcb,
        FrameBorder = Solid("#0A2A1A"),
        FrameBorderThickness = 2,
        CornerRadius = 12,
        HeaderInset = 30,
        FrameOverlay = MountingHoles,
        HeaderText = Solid("#CFE6D6"),
        PillBackground = Solid("#0A2E1C"),
        PillBorder = Solid("#C9A04E"),
        PillText = Solid("#F2F5EE"),

        EdgeBrush = Solid("#B8913F"),
        EdgeThickness = 6,
        NodeFill = Frozen(new LinearGradientBrush(Hex("#EFD27E"), Hex("#A27A2C"), 90)),
        NodeStroke = Solid("#F6E2A6"),
        NodeStrokeThickness = 1.5,
        DitRadius = 14,
        DahHalf = new Size(27, 11),
        DahCorner = 3,
        NodeDecoration = DrillHole,
        Backdrop = CircuitSilkscreen,
        LabelFont = new FontFamily("Consolas"),
        LabelWeight = FontWeights.Bold,
        LabelIdle = Hex("#F0F4EC"),
        RootFill = Brushes.Transparent,
        RootStroke = Solid("#F0F4EC"),
        RootStrokeThickness = 3,

        Path = new(Hex("#FFA51E"), 230, Hex("#FFD27A"), 18, 0.95),
        Current = new(Hex("#FFD86B"), 255, Hex("#FFF6D8"), 30, 1.0),
        Pending = new(Hex("#3FA9FF"), 200, Hex("#A9D8FF"), 22, 1.0),
        Success = new(Hex("#C3FF4A"), 230, Hex("#F0FFD0"), 26, 1.0),
        Error = new(Hex("#FF3434"), 230, Hex("#FFA0A0"), 20, 0.95),

        ChipBackground = Solid("#1B1D1C"),
        ChipBorderIdle = Hex("#9FB5A6"),
        SymbolIdle = Hex("#4C6656"),
        ChipCorner = 3,
        StripBackground = Solid("#050807"),
        StripBorder = Solid("#C9A04E"),
        StripCorner = 4,
        HistoryFont = new FontFamily("Consolas"),
        HistoryText = Solid("#6FAF8A"),
        HistoryBright = Solid("#D8FFE6"),
        CandidateFont = new FontFamily("Consolas"),
    };

    public static readonly IReadOnlyList<Theme> All = new[] { Classic, Telegraph, Military, Constellation, Circuit };

    public static Theme Get(string? id) => All.FirstOrDefault(t => t.Id == id) ?? Classic;

    // Decorations.

    /// <summary>Four slotted screws in the frame corners.</summary>
    private static UIElement Screws(Color light, Color dark)
    {
        var grid = new Grid { IsHitTestVisible = false };
        var corners = new[]
        {
            (HorizontalAlignment.Left, VerticalAlignment.Top, 35.0),
            (HorizontalAlignment.Right, VerticalAlignment.Top, -20.0),
            (HorizontalAlignment.Left, VerticalAlignment.Bottom, 70.0),
            (HorizontalAlignment.Right, VerticalAlignment.Bottom, 10.0),
        };
        foreach (var (h, v, angle) in corners)
        {
            var screw = new Grid { Width = 11, Height = 11, HorizontalAlignment = h, VerticalAlignment = v, Margin = new Thickness(8) };
            screw.Children.Add(new Ellipse
            {
                Fill = new RadialGradientBrush(light, dark) { GradientOrigin = new Point(0.35, 0.3) },
                Stroke = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                StrokeThickness = 0.8,
            });
            screw.Children.Add(new Rectangle
            {
                Width = 8,
                Height = 1.6,
                Fill = new SolidColorBrush(Color.FromArgb(200, 20, 14, 6)),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(angle),
            });
            grid.Children.Add(screw);
        }
        return grid;
    }

    /// <summary>Plated mounting holes in the board corners.</summary>
    private static UIElement MountingHoles()
    {
        var grid = new Grid { IsHitTestVisible = false };
        foreach (var (h, v) in new[]
        {
            (HorizontalAlignment.Left, VerticalAlignment.Top), (HorizontalAlignment.Right, VerticalAlignment.Top),
            (HorizontalAlignment.Left, VerticalAlignment.Bottom), (HorizontalAlignment.Right, VerticalAlignment.Bottom),
        })
        {
            grid.Children.Add(new Ellipse
            {
                Width = 15,
                Height = 15,
                HorizontalAlignment = h,
                VerticalAlignment = v,
                Margin = new Thickness(8),
                Fill = Solid("#071A10"),
                Stroke = Solid("#C9A04E"),
                StrokeThickness = 3,
            });
        }
        return grid;
    }

    /// <summary>A small reflection that makes an unlit dit read as a glass bulb.</summary>
    private static IEnumerable<UIElement> GlassHighlight(Point center, bool dah, Size half)
    {
        if (dah) yield break;
        var shine = new Ellipse { Width = 9, Height = 5, Fill = Solid("#55FFF4D8"), RenderTransform = new RotateTransform(-30) };
        Canvas.SetLeft(shine, center.X - 8);
        Canvas.SetTop(shine, center.Y - 9);
        yield return shine;
    }

    /// <summary>The lens ring of an unlit dial lamp.</summary>
    private static IEnumerable<UIElement> LampLens(Point center, bool dah, Size half)
    {
        if (dah) yield break;
        var lens = new Ellipse { Width = 14, Height = 14, Stroke = Solid("#444836"), StrokeThickness = 1.5 };
        Canvas.SetLeft(lens, center.X - 7);
        Canvas.SetTop(lens, center.Y - 7);
        yield return lens;
    }

    /// <summary>The plated hole through a through-hole pad.</summary>
    private static IEnumerable<UIElement> DrillHole(Point center, bool dah, Size half)
    {
        if (dah) yield break;
        var hole = new Ellipse { Width = 10, Height = 10, Fill = Solid("#0A1F14") };
        Canvas.SetLeft(hole, center.X - 5);
        Canvas.SetTop(hole, center.Y - 5);
        yield return hole;
    }

    private static IEnumerable<UIElement> TelegraphPlate()
    {
        var plateBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(Hex("#EED08A"), 0),
                new GradientStop(Hex("#B88F42"), 0.55),
                new GradientStop(Hex("#D9B868"), 1),
            },
        };
        var plate = new Border
        {
            Width = 252,
            Height = 50,
            CornerRadius = new CornerRadius(4),
            Background = plateBrush,
            BorderBrush = Solid("#5E4418"),
            BorderThickness = new Thickness(1.5),
        };
        Canvas.SetLeft(plate, 20);
        Canvas.SetTop(plate, 26);
        yield return plate;

        foreach (double x in new[] { 28.0, 254.0 })
        {
            var rivet = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = new RadialGradientBrush(Hex("#FFF0C0"), Hex("#7A5A22")) { GradientOrigin = new Point(0.35, 0.3) },
            };
            Canvas.SetLeft(rivet, x);
            Canvas.SetTop(rivet, 47.5);
            yield return rivet;
        }
    }

    private static IEnumerable<UIElement> MilitaryStencil()
    {
        yield return Text("SIGNAL TRAINER", 22, 16, "Bahnschrift", 27, "#E9E4C9", FontWeights.Bold, FontStretches.Condensed);
        yield return Text("TYPE M-1     SER. 0451", 24, 52, "Bahnschrift", 14, "#C8C3A6", FontWeights.SemiBold, FontStretches.Condensed);
        // Painted band along the top edge of the tree, like a panel stripe.
        var band = new Rectangle { Width = 120, Height = 6, Fill = Solid("#B8E9E4C9") };
        Canvas.SetLeft(band, 540);
        Canvas.SetTop(band, 30);
        yield return band;
        var band2 = new Rectangle { Width = 120, Height = 2, Fill = Solid("#B8E9E4C9") };
        Canvas.SetLeft(band2, 540);
        Canvas.SetTop(band2, 42);
        yield return band2;
    }

    private static IEnumerable<UIElement> CelestialGrid()
    {
        var line = Solid("#1EFFFFFF");
        foreach (double r in new[] { 820.0, 980.0, 1140.0 })
        {
            var arc = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Stroke = line,
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 3, 5 },
            };
            Canvas.SetLeft(arc, 340 - r);
            Canvas.SetTop(arc, -360 - r);
            yield return arc;
        }
        foreach (double x in new[] { 120.0, 560.0 })
        {
            yield return new Line
            {
                X1 = 340, Y1 = -360, X2 = x, Y2 = 760,
                Stroke = line,
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 3, 5 },
            };
        }
        yield return Text("CONSTELLATIO MORSE", 22, 26, "Georgia", 17, "#8C9AD0", FontWeights.Normal, FontStretches.Normal, FontStyles.Italic);
    }

    private static IEnumerable<UIElement> CircuitSilkscreen()
    {
        yield return Text("MORSE-KB", 22, 16, "Consolas", 26, "#EEF2EA", FontWeights.Bold, FontStretches.Normal);
        yield return Text("REV 1.0", 24, 52, "Consolas", 14, "#C9D6CC", FontWeights.Normal, FontStretches.Normal);

        // Copper under the solder mask: a few traces and vias in the empty corners.
        var copper = Solid("#1E7A4B");
        var traces = new[]
        {
            "M 560,505 L 650,505 L 650,745",
            "M 585,540 L 625,540 L 625,700 L 600,725 L 520,725",
            "M 20,560 L 60,560 L 80,580 L 80,595",
            "M 20,690 L 110,690 L 130,710 L 130,745",
            "M 440,30 L 520,30 L 540,50 L 660,50",
            "M 470,70 L 640,70",
        };
        foreach (string data in traces)
        {
            yield return new Path
            {
                Data = Geometry.Parse(data),
                Stroke = copper,
                StrokeThickness = 4,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
        }
        foreach (var (x, y) in new[] { (560.0, 505.0), (585.0, 540.0), (520.0, 725.0), (80.0, 595.0), (130.0, 745.0), (440.0, 30.0), (470.0, 70.0), (640.0, 70.0) })
        {
            var via = new Ellipse { Width = 11, Height = 11, Stroke = copper, StrokeThickness = 2.5, Fill = Solid("#0A2A1A") };
            Canvas.SetLeft(via, x - 5.5);
            Canvas.SetTop(via, y - 5.5);
            yield return via;
        }
    }

    private static TextBlock Text(string text, double x, double y, string font, double size, string color,
        FontWeight weight, FontStretch stretch, FontStyle? style = null)
    {
        var block = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(font),
            FontSize = size,
            FontWeight = weight,
            FontStretch = stretch,
            FontStyle = style ?? FontStyles.Normal,
            Foreground = Solid(color),
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        return block;
    }

    /// <summary>A soft dark patch behind text so it reads on busy wood grain.</summary>
    private static Effect Halo()
    {
        var effect = new DropShadowEffect { Color = Colors.Black, ShadowDepth = 0, BlurRadius = 10, Opacity = 0.9 };
        effect.Freeze();
        return effect;
    }

    private static Effect Glow(Color color, double blur)
    {
        var effect = new DropShadowEffect { Color = color, ShadowDepth = 0, BlurRadius = blur, Opacity = 1 };
        effect.Freeze();
        return effect;
    }

    private static Color Hex(string hex) => Palette.Hex(hex);

    private static SolidColorBrush Solid(string hex) => Palette.Solid(Palette.Hex(hex));

    private static GradientStopCollection Stops(params (Color Color, double Offset)[] stops)
    {
        var collection = new GradientStopCollection();
        foreach (var (c, o) in stops) collection.Add(new GradientStop(c, o));
        return collection;
    }

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
