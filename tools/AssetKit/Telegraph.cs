using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using MorseKeyboard;
using static AssetKit.Kit;

namespace AssetKit;

/// <summary>
/// Starter art for 電信局: brass-rimmed glass bulbs and tubes on walnut, brass rods, an engraved nameplate.
/// Light comes from the top-left; shadows fall to the bottom-right (offsets in theme.json).
/// </summary>
internal static class Telegraph
{
    // Node sizes match the code-drawn theme so labels stay where they are.
    private const double DitRadius = 15;
    private const double DahLength = 58;
    private const double DahThickness = 22;

    private static readonly Color Rim = Hex("#24160A");

    private static (Color, double)[] BrassBevel => new[]
    {
        (Hex("#FFF2C6"), 0.0), (Hex("#EBC877"), 0.25), (Hex("#B88A38"), 0.6), (Hex("#6A4814"), 1.0),
    };

    private static (Color, double)[] BrassRecess => new[]
    {
        (Hex("#4C320C"), 0.0), (Hex("#8A6224"), 0.45), (Hex("#E9C982"), 1.0),
    };

    public const string Manifest = """
        {
          "pixelsPerUnit": 3,
          "nameplate": [20, 26, 252, 50],
          "offsets": {
            "dit_shadow": [3, 4.5],
            "dahh_shadow": [3, 4.5],
            "dahv_shadow": [3, 4.5],
            "root_shadow": [3, 4.5],
            "edge_h_shadow": [2.5, 4],
            "edge_v_shadow": [2.5, 4]
          }
        }
        """;

    public static void Make()
    {
        Background();
        Backdrop();
        Screw();

        DitOff();
        DitOn();
        Part("dit_shadow", 60, 60, false, Blur(Circle(30, 30, DitRadius + 1.8, new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))), 3.5));

        foreach (bool vertical in new[] { false, true })
        {
            DahOff(vertical);
            DahOn(vertical);
            DahShadow(vertical);
        }

        Light();
        Edges();
        Root();

        File.WriteAllText(System.IO.Path.Combine(OutputDirectory, ThemeAssets.ManifestFileName), Manifest);
    }

    /// <summary>Walnut with a soft top-left light and darkened, beveled edges.</summary>
    private static void Background()
    {
        const int w = 1080, h = 1760;
        var wood = Textures.Render(Textures.Kind.Wood, w, h);
        var canvas = new Canvas { Width = w, Height = h };
        canvas.Children.Add(new Image { Source = wood, Width = w, Height = h });
        canvas.Children.Add(new Rectangle
        {
            Width = w, Height = h,
            Fill = Radial(new Point(0.22, 0.12), (Color.FromArgb(70, 255, 214, 150), 0), (Color.FromArgb(0, 255, 214, 150), 0.75)),
        });
        canvas.Children.Add(new Rectangle
        {
            Width = w, Height = h,
            Fill = new LinearGradientBrush(Stops((Color.FromArgb(150, 0, 0, 0), 0), (Color.FromArgb(0, 0, 0, 0), 0.035),
                (Color.FromArgb(0, 0, 0, 0), 0.965), (Color.FromArgb(170, 0, 0, 0), 1)), new Point(0, 0), new Point(1, 0)),
        });
        canvas.Children.Add(new Rectangle
        {
            Width = w, Height = h,
            Fill = new LinearGradientBrush(Stops((Color.FromArgb(60, 255, 230, 180), 0), (Color.FromArgb(0, 0, 0, 0), 0.02),
                (Color.FromArgb(0, 0, 0, 0), 0.975), (Color.FromArgb(180, 0, 0, 0), 1)), new Point(0, 0), new Point(0, 1)),
        });
        var bitmap = Rasterize(canvas, w, h, 1);
        Save(bitmap, System.IO.Path.Combine(OutputDirectory, "background.png"));
        Made.Add(("background", bitmap, false));
    }

    /// <summary>The brass nameplate (blank), drawn in tree coordinates (680 x 760).</summary>
    private static void Backdrop()
    {
        const double x = 20, y = 26, w = 252, h = 50;
        var label = new Canvas();

        var plate = new Grid { Width = w, Height = h };
        plate.Children.Add(new Rectangle { RadiusX = 4, RadiusY = 4, Fill = FromTopLeft(w, h, BrassBevel) });
        plate.Children.Add(new Rectangle
        {
            Margin = new Thickness(4.5), RadiusX = 2.5, RadiusY = 2.5,
            Stroke = new SolidColorBrush(Color.FromArgb(200, 0x6A, 0x48, 0x14)), StrokeThickness = 0.9,
        });
        plate.Children.Add(new Rectangle
        {
            Margin = new Thickness(5.4, 5.4, 3.6, 3.6), RadiusX = 2.5, RadiusY = 2.5,
            Stroke = new SolidColorBrush(Color.FromArgb(130, 255, 240, 192)), StrokeThickness = 0.7,
        });
        // Left blank: the app engraves the name and version (theme.json "nameplate").
        Canvas.SetLeft(plate, x);
        Canvas.SetTop(plate, y);

        Part("backdrop", MorseTreeCanvas.CanvasWidth, MorseTreeCanvas.CanvasHeight, false,
            Blur(Box(x + w / 2 + 3, y + h / 2 + 4.5, w, h, 4, new SolidColorBrush(Color.FromArgb(170, 0, 0, 0))), 4),
            Box(x + w / 2, y + h / 2, w + 2, h + 2, 5, new SolidColorBrush(Rim)),
            plate,
            Rivet(x + 7.5, y + h / 2), Rivet(x + w - 7.5, y + h / 2));
    }

    private static UIElement Rivet(double cx, double cy)
    {
        var group = new Canvas();
        group.Children.Add(Blur(Circle(cx + 0.8, cy + 1.1, 3.0, new SolidColorBrush(Color.FromArgb(150, 0, 0, 0))), 0.8));
        group.Children.Add(Circle(cx, cy, 3.0, Radial(new Point(0.35, 0.3), (Hex("#FFF6D6"), 0), (Hex("#C99A48"), 0.5), (Hex("#5E3F10"), 1))));
        return group;
    }

    /// <summary>A slotted brass screw with its own shadow, for the frame corners.</summary>
    private static void Screw()
    {
        const double c = 7;
        var slot = Rotate(Box(c, c, 8.4, 1.5, 0.5, new SolidColorBrush(Rim)), 32);
        var slotLight = Rotate(Box(c + 0.45, c + 0.75, 8, 0.5, 0.25, new SolidColorBrush(Color.FromArgb(140, 255, 240, 200))), 32);
        Part("screw", 15, 15, false,
            Blur(Circle(c + 0.9, c + 1.3, 5.8, new SolidColorBrush(Color.FromArgb(170, 0, 0, 0))), 1.1),
            Circle(c, c, 5.9, new SolidColorBrush(Rim)),
            Circle(c, c, 5.3, Radial(new Point(0.35, 0.3), (Hex("#FFF4CC"), 0), (Hex("#D9B060"), 0.45), (Hex("#7A5418"), 1))),
            slotLight, slot);
    }

    private static void DitOff()
    {
        const double c = 22, r = DitRadius;
        Part("dit_off", 44, 44, false,
            Circle(c, c, r + 2.2, new SolidColorBrush(Rim)),
            Circle(c, c, r + 1.4, FromTopLeft((r + 1.4) * 2, (r + 1.4) * 2, BrassBevel)),
            Circle(c, c, r - 1.6, FromTopLeft((r - 1.6) * 2, (r - 1.6) * 2, BrassRecess)),
            Circle(c, c, r - 2.8, Radial(new Point(0.42, 0.38), (Hex("#7A5222"), 0), (Hex("#3E240B"), 0.55), (Hex("#170C03"), 1))),
            Filament(c, c, new SolidColorBrush(Hex("#B08A4C")), 0.9),
            Circle(c, c, r - 2.8, Radial((Color.FromArgb(0, 0, 0, 0), 0.6), (Color.FromArgb(120, 0, 0, 0), 1))),
            Specular(c, c, 0.9));
    }

    private static void DitOn()
    {
        const double c = 65, r = DitRadius;
        Part("dit_on", 130, 130, true,
            Circle(c, c, 64, Radial((Gray(0.5, 0.6), 0), (Gray(0.5, 0.32), 0.25), (Gray(0.5, 0.12), 0.55), (Gray(0.5, 0), 1))),
            Circle(c, c, r + 4, Radial((Gray(0.75, 0.55), 0), (Gray(0.55, 0.35), 0.7), (Gray(0.5, 0), 1))),
            Circle(c, c, r - 2.8, Radial(new Point(0.45, 0.42), (Gray(1), 0), (Gray(0.9), 0.3), (Gray(0.66), 0.75), (Gray(0.52), 1))),
            Blur(Filament(c, c, new SolidColorBrush(Gray(0.95, 0.8)), 3), 1.2),
            Filament(c, c, new SolidColorBrush(Gray(1)), 1.2),
            Specular(c, c, 0.5));
    }

    /// <summary>A zigzag filament on two support wires, centered in a dit.</summary>
    private static Path Filament(double cx, double cy, Brush brush, double width)
    {
        string P(double x, double y) => $"{cx + x:0.##},{cy + y:0.##}";
        return Stroke($"M {P(-5.5, 8)} L {P(-4.5, 1.5)} L {P(-3, -1.5)} L {P(-1.5, 1.5)} L {P(0, -1.5)} L {P(1.5, 1.5)} " +
                      $"L {P(3, -1.5)} L {P(4.5, 1.5)} L {P(5.5, 8)}", brush, width);
    }

    /// <summary>The window reflection on the glass, top-left, plus a faint bounce bottom-right.</summary>
    private static UIElement Specular(double cx, double cy, double strength)
    {
        var group = new Canvas();
        group.Children.Add(Rotate(Oval(cx - 5.5, cy - 6.5, 7.5, 3.6,
            Radial((Color.FromArgb((byte)(230 * strength), 255, 255, 255), 0), (Color.FromArgb(0, 255, 255, 255), 1))), -35));
        group.Children.Add(Rotate(Oval(cx + 5.5, cy + 6, 3, 1.6,
            Radial((Color.FromArgb((byte)(70 * strength), 255, 255, 255), 0), (Color.FromArgb(0, 255, 255, 255), 1))), -35));
        return group;
    }

    // Dahs are glass tubes in a brass frame. Drawn per orientation so the light stays top-left.

    private static (double W, double H) Size(bool vertical, double along, double across) => vertical ? (across, along) : (along, across);

    private static void DahOff(bool vertical)
    {
        var (cw, ch) = Size(vertical, 72, 36);
        double cx = cw / 2, cy = ch / 2;
        var (bw, bh) = Size(vertical, DahLength, DahThickness);
        var (rw, rh) = Size(vertical, DahLength - 6, DahThickness - 6);
        var (gw, gh) = Size(vertical, DahLength - 8, DahThickness - 8.4);
        var (sw, sh) = Size(vertical, DahLength - 18, 2.2);
        var (sx, sy) = vertical ? (cx - 2.4, cy) : (cx, cy - 2.4);
        double reach = DahLength / 2 - 9;
        string wire = vertical
            ? $"M {cx + 0.5:0.##},{cy - reach:0.##} L {cx + 0.5:0.##},{cy + reach:0.##}"
            : $"M {cx - reach:0.##},{cy + 0.5:0.##} L {cx + reach:0.##},{cy + 0.5:0.##}";

        Part(vertical ? "dahv_off" : "dahh_off", cw, ch, false,
            Box(cx, cy, bw + 2.4, bh + 2.4, 8.2, new SolidColorBrush(Rim)),
            Box(cx, cy, bw, bh, 7, FromTopLeft(bw, bh, BrassBevel)),
            Box(cx, cy, rw, rh, 5, FromTopLeft(rw, rh, BrassRecess)),
            Box(cx, cy, gw, gh, 4.2, Across(gw, gh, (Hex("#5A3A14"), 0), (Hex("#7A5222"), 0.25), (Hex("#3E240B"), 0.6), (Hex("#160B03"), 1))),
            Stroke(wire, new SolidColorBrush(Hex("#B08A4C")), 0.9),
            Box(cx, cy, gw, gh, 4.2, Across(gw, gh, (Color.FromArgb(90, 0, 0, 0), 0), (Color.FromArgb(0, 0, 0, 0), 0.3),
                (Color.FromArgb(0, 0, 0, 0), 0.7), (Color.FromArgb(120, 0, 0, 0), 1))),
            Box(sx, sy, sw, sh, 1.1, Along(sw, sh, (Color.FromArgb(0, 255, 255, 255), 0), (Color.FromArgb(200, 255, 255, 255), 0.2),
                (Color.FromArgb(200, 255, 255, 255), 0.6), (Color.FromArgb(0, 255, 255, 255), 1))));
    }

    private static void DahOn(bool vertical)
    {
        var (cw, ch) = Size(vertical, 150, 110);
        double cx = cw / 2, cy = ch / 2;
        var (bw, bh) = Size(vertical, DahLength + 8, DahThickness + 8);
        var (gw, gh) = Size(vertical, DahLength - 8, DahThickness - 8.4);
        double reach = DahLength / 2 - 9;
        string wire = vertical
            ? $"M {cx:0.##},{cy - reach:0.##} L {cx:0.##},{cy + reach:0.##}"
            : $"M {cx - reach:0.##},{cy:0.##} L {cx + reach:0.##},{cy:0.##}";

        Part(vertical ? "dahv_on" : "dahh_on", cw, ch, true,
            Oval(cx, cy, cw - 4, ch - 4, Radial((Gray(0.5, 0.6), 0), (Gray(0.5, 0.3), 0.3), (Gray(0.5, 0.1), 0.6), (Gray(0.5, 0), 1))),
            Blur(Box(cx, cy, bw, bh, 11, new SolidColorBrush(Gray(0.62, 0.45))), 3),
            Box(cx, cy, gw, gh, 4.2, Across(gw, gh, (Gray(0.55), 0), (Gray(0.92), 0.4), (Gray(1), 0.5), (Gray(0.92), 0.6), (Gray(0.55), 1))),
            Blur(Stroke(wire, new SolidColorBrush(Gray(0.95, 0.8)), 3), 1.2),
            Stroke(wire, new SolidColorBrush(Gray(1)), 1.2));
    }

    private static void DahShadow(bool vertical)
    {
        var (cw, ch) = Size(vertical, DahLength + 24, DahThickness + 24);
        var (bw, bh) = Size(vertical, DahLength + 2.4, DahThickness + 2.4);
        Part(vertical ? "dahv_shadow" : "dahh_shadow", cw, ch, false,
            Blur(Box(cw / 2, ch / 2, bw, bh, 8.2, new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))), 3.5));
    }

    /// <summary>The warm spill a lit bulb throws onto the wood around it.</summary>
    private static void Light()
    {
        Part("light", 200, 200, true,
            Circle(100, 100, 100, Radial((Gray(0.5, 0.5), 0), (Gray(0.5, 0.28), 0.3), (Gray(0.5, 0.1), 0.6), (Gray(0.5, 0), 1))));
    }

    /// <summary>Brass rods (6 units thick), their shadows and their lit glow, as tiles repeated along the edge.</summary>
    private static void Edges()
    {
        const double tile = 20;
        var rod = new[] { (Hex("#4A300A"), 0.0), (Hex("#F6DE9C"), 0.18), (Hex("#D8B262"), 0.4), (Hex("#9A6E26"), 0.75), (Hex("#3A2608"), 1.0) };
        var shadow = new[]
        {
            (Color.FromArgb(0, 0, 0, 0), 0.0), (Color.FromArgb(34, 0, 0, 0), 0.3), (Color.FromArgb(128, 0, 0, 0), 0.5),
            (Color.FromArgb(34, 0, 0, 0), 0.7), (Color.FromArgb(0, 0, 0, 0), 1.0),
        };
        var glow = new[]
        {
            (Gray(0.5, 0), 0.0), (Gray(0.5, 0.18), 0.3), (Gray(0.7, 0.85), 0.42), (Gray(0.95, 1), 0.5),
            (Gray(0.7, 0.85), 0.58), (Gray(0.5, 0.18), 0.7), (Gray(0.5, 0), 1.0),
        };

        Part("edge_h", tile, 10, false, Box(tile / 2, 5, tile, 6, 0, Across(tile, 6, rod)));
        Part("edge_v", 10, tile, false, Box(5, tile / 2, 6, tile, 0, Across(6, tile, rod)));
        Part("edge_h_shadow", tile, 18, false, Box(tile / 2, 9, tile, 18, 0, new LinearGradientBrush(Stops(shadow), new Point(0, 0), new Point(0, 1))));
        Part("edge_v_shadow", 18, tile, false, Box(9, tile / 2, 18, tile, 0, new LinearGradientBrush(Stops(shadow), new Point(0, 0), new Point(1, 0))));
        Part("edge_h_on", tile, 24, true, Box(tile / 2, 12, tile, 24, 0, new LinearGradientBrush(Stops(glow), new Point(0, 0), new Point(0, 1))));
        Part("edge_v_on", 24, tile, true, Box(12, tile / 2, 24, tile, 0, new LinearGradientBrush(Stops(glow), new Point(0, 0), new Point(1, 0))));
    }

    /// <summary>The antenna at the root, centered on MorseTreeCanvas.RootArtCenter (333, 111).</summary>
    private static void Root()
    {
        const double w = 100, h = 130;
        double ox = MorseTreeCanvas.RootArtCenter.X - w / 2, oy = MorseTreeCanvas.RootArtCenter.Y - h / 2;
        string P(double x, double y) => $"{x - ox:0.##},{y - oy:0.##}";
        string lines = $"M {P(301, 66)} L {P(365, 66)} L {P(333, 110)} Z M {P(314, 77)} L {P(352, 77)} M {P(333, 77)} L {P(333, 162)}";
        string triangle = $"M {P(301, 66)} L {P(365, 66)} L {P(333, 110)} Z";

        var inlay = new Path { Data = Geometry.Parse(triangle), Fill = Radial((Hex("#3A2410"), 0), (Hex("#160C04"), 1)) };
        Part("root_off", w, h, false,
            inlay,
            Stroke(lines, new SolidColorBrush(Rim), 7),
            Stroke(lines, new SolidColorBrush(Hex("#6A4814")), 4.6, 0.7, 0.7),
            Stroke(lines, new SolidColorBrush(Hex("#C99A48")), 3.8),
            Stroke(lines, new SolidColorBrush(Color.FromArgb(215, 255, 240, 192)), 1.2, -0.7, -0.7));

        var glowFill = new Path { Data = Geometry.Parse(triangle), Fill = new SolidColorBrush(Gray(0.5, 0.25)) };
        Part("root_on", w, h, true,
            glowFill,
            Blur(Stroke(lines, new SolidColorBrush(Gray(0.5, 0.5)), 16), 6),
            Stroke(lines, new SolidColorBrush(Gray(0.85)), 4),
            Stroke(lines, new SolidColorBrush(Gray(1)), 1.6));

        var shadowFill = new Path { Data = Geometry.Parse(triangle), Fill = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)) };
        var shadow = new Canvas { Children = { shadowFill, Stroke(lines, new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), 6) } };
        Part("root_shadow", w, h, false, Blur(shadow, 3));
    }
}
