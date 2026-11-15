using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MorseKeyboard;
using static AssetKit.Kit;
using Path = System.IO.Path;

namespace AssetKit;

/// <summary>
/// Turns generated photos in design/raw/&lt;theme&gt;/ into theme parts: cuts them out of their plain
/// background, scales them to the design sizes, extracts the light from lit/unlit pairs, makes the rod
/// tileable and derives the shadows. Parts without a photo keep the starter art already in place.
/// See design/prompts/ for how the photos are made.
/// </summary>
internal static class Importer
{
    // Design sizes in tree units.
    private const double DitDiameter = 34;     // brass bezel, outside edge
    private const double DahLength = 62;       // brass frame, outside edge
    private const double RodThickness = 6.5;
    private const double TileLength = 90;      // rod tile; the crossfaded seam repeats this often
    private const double EmblemWidth = 66;
    private const double PlateWidth = 222;
    private static readonly Point PlateOrigin = new(16, 14);

    /// <summary>Background is the plain studio backdrop the parts were shot on.</summary>
    private const float KeyThreshold = 0.12f;

    private static readonly Vector ShadowOffset = new(3, 4.5);

    public static void Run(string rawDirectory)
    {
        string Raw(string name) => Path.Combine(rawDirectory, name + ".png");
        bool Has(string name) => File.Exists(Raw(name));

        var offsets = new Dictionary<string, Vector>
        {
            ["edge_h_shadow"] = new(2.5, 4),
            ["edge_v_shadow"] = new(2.5, 4),
        };
        double? ditRadius = null;
        Size? dahHalf = null;
        Rect? nameplate = null;

        if (Has("background")) Background(Raw("background"));
        if (Has("nameplate")) nameplate = Nameplate(Raw("nameplate"));
        if (Has("screw")) Screw(Raw("screw"));
        // Shown stretched to their place in the window; only their proportions matter.
        if (Has("tag")) Plain("tag", Raw("tag"), 118);
        if (Has("window")) Plain("window", Raw("window"), 112);
        if (Has("tape")) Plain("tape", Raw("tape"), 520);

        if (Has("dit_off"))
        {
            Lamp("dit", Raw("dit_off"), Has("dit_on") ? Raw("dit_on") : null, vertical: false, DitDiameter, onPad: 96);
            ditRadius = DitDiameter / 2;
            offsets["dit_shadow"] = ShadowOffset;
        }
        if (Has("dahh_off"))
        {
            double thickness = Lamp("dahh", Raw("dahh_off"), Has("dahh_on") ? Raw("dahh_on") : null, vertical: false, DahLength, onPad: 64);
            dahHalf = new Size(DahLength / 2, thickness / 2);
            offsets["dahh_shadow"] = ShadowOffset;
        }
        if (Has("dahv_off"))
        {
            Lamp("dahv", Raw("dahv_off"), Has("dahv_on") ? Raw("dahv_on") : null, vertical: true, DahLength, onPad: 64);
            offsets["dahv_shadow"] = ShadowOffset;
        }

        BitmapSource? rodVertical = Has("edge") ? Rod(Raw("edge")) : null;
        if (Has("root"))
        {
            Root(Raw("root"), rodVertical);
            offsets["root_shadow"] = ShadowOffset;
        }

        WriteManifest(offsets, ditRadius, dahHalf, nameplate);
    }

    /// <summary>The window background, shrunk to at most 1200 px wide (never enlarged).</summary>
    private static void Background(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int width = BitmapFrame.Create(new MemoryStream(bytes)).PixelWidth;
        var decoded = new BitmapImage();
        decoded.BeginInit();
        decoded.CacheOption = BitmapCacheOption.OnLoad;
        decoded.StreamSource = new MemoryStream(bytes);
        if (width > 1200) decoded.DecodePixelWidth = 1200;
        decoded.EndInit();
        decoded.Freeze();
        Save(decoded, Path.Combine(OutputDirectory, "background.png"));
        Made.Add(("background", decoded, false));
    }

    /// <summary>
    /// The blank plate goes into the backdrop at the top left, with its shadow. Returns where it is; the app
    /// engraves the name and version there (so the version always matches the build).
    /// </summary>
    private static Rect Nameplate(string path)
    {
        var (cut, box) = CutOut(path);
        double scale = PlateWidth / box.Width;
        double plateHeight = box.Height * scale;
        var center = new Point(PlateOrigin.X + PlateWidth / 2, PlateOrigin.Y + plateHeight / 2);

        Part("backdrop", MorseTreeCanvas.CanvasWidth, MorseTreeCanvas.CanvasHeight, false,
            Blur(Silhouette(cut, box, scale, center + ShadowOffset, 0.75), 4),
            Placed(cut, box, scale, center));
        return new Rect(PlateOrigin.X, PlateOrigin.Y, PlateWidth, plateHeight);
    }

    /// <summary>A part cut out and scaled to <paramref name="width"/> units, cropped to the object.</summary>
    private static void Plain(string name, string path, double width)
    {
        var (cut, box) = CutOut(path);
        double scale = width / box.Width;
        double height = box.Height * scale;
        Part(name, width, height, false, Placed(cut, box, scale, new Point(width / 2, height / 2)));
    }

    private static void Screw(string path)
    {
        var (cut, box) = CutOut(path);
        double scale = 12.5 / box.Width;
        var center = new Point(7.5, 7.5);
        Part("screw", 15, 15, false,
            Blur(Silhouette(cut, box, scale, center + new Vector(0.9, 1.3), 0.7), 1),
            Placed(cut, box, scale, center));
    }

    /// <summary>
    /// A lamp (dit or dah): the unlit photo becomes *_off and *_shadow, and the light the lit photo adds
    /// becomes *_on. Returns the lamp's size across its length, in units.
    /// </summary>
    private static double Lamp(string kind, string offPath, string? onPath, bool vertical, double length, double onPad)
    {
        var unlit = Img.Load(offPath);
        var mask = Pixels.ObjectMask(unlit, KeyThreshold);
        var box = Pixels.Bounds(mask, unlit.W, unlit.H);
        var cut = Pixels.Cutout(unlit, mask).ToBitmap();
        double scale = length / (vertical ? box.Height : box.Width);
        double w = box.Width * scale, h = box.Height * scale;

        Part($"{kind}_off", w + 10, h + 10, false, Placed(cut, box, scale, new Point((w + 10) / 2, (h + 10) / 2)));
        Part($"{kind}_shadow", w + 24, h + 24, false,
            Blur(Silhouette(cut, box, scale, new Point((w + 24) / 2, (h + 24) / 2), 0.8), 3.5));

        if (onPath is not null)
        {
            var lit = Img.Load(onPath);
            if (lit.W != unlit.W || lit.H != unlit.H) lit = lit.Resized(unlit.W, unlit.H);
            var shift = Pixels.Align(unlit, lit, mask, 12);
            Console.WriteLine($"  {kind}_on: lit photo shifted by ({shift.Dx}, {shift.Dy}) px");
            var light = Pixels.Emission(unlit, lit, mask, shift).ToBitmap();
            double ow = w + onPad, oh = h + onPad;
            Part($"{kind}_on", ow, oh, true, Placed(light, box, scale, new Point(ow / 2, oh / 2)));
        }

        return vertical ? w : h;
    }

    /// <summary>A crossfaded, seamlessly repeating piece of the rod (edge_h), and the same turned for edge_v.</summary>
    private static BitmapSource Rod(string path)
    {
        var img = Img.Load(path);
        var mask = Pixels.ObjectMask(img, KeyThreshold);
        var box = Pixels.Bounds(mask, img.W, img.H);
        var cut = Pixels.Cutout(img, mask);

        double scale = RodThickness / box.Height;
        int overlap = (int)Math.Round(12 / scale);
        int tile = Math.Min((int)Math.Round(TileLength / scale), (int)(box.Width * 0.7) - overlap);
        double tileUnits = tile * scale;
        int start = box.X + (box.Width - tile - overlap) / 2;
        int band = (int)Math.Round(10 / scale);
        int top = box.Y + box.Height / 2 - band / 2;

        var piece = new Img(tile, band);
        for (int y = 0; y < band; y++)
        {
            int sy = top + y;
            if (sy < 0 || sy >= img.H) continue;
            for (int x = 0; x < tile; x++)
            {
                int a = sy * img.W + start + x;
                int i = y * tile + x;
                // The first `overlap` pixels blend into what follows the tile's end, so the repeat is seamless.
                float t = x < overlap ? x / (float)overlap : 1;
                int b = x < overlap ? sy * img.W + start + x + tile : a;
                piece.R[i] = cut.R[b] + (cut.R[a] - cut.R[b]) * t;
                piece.G[i] = cut.G[b] + (cut.G[a] - cut.G[b]) * t;
                piece.B[i] = cut.B[b] + (cut.B[a] - cut.B[b]) * t;
                piece.A[i] = cut.A[b] + (cut.A[a] - cut.A[b]) * t;
            }
        }

        // The photographed rod is lighter brass than the lamps' bezels; bring it to the same warm tone.
        for (int i = 0; i < piece.R.Length; i++)
        {
            piece.R[i] *= 0.9f;
            piece.G[i] *= 0.78f;
            piece.B[i] *= 0.55f;
        }

        var horizontal = piece.ToBitmap();
        Part("edge_h", tileUnits, 10, false, Stretched(horizontal, tileUnits, 10));
        // Turned counterclockwise so the highlight on top ends up on the left, still facing the top-left light.
        var turned = new TransformedBitmap(horizontal, new RotateTransform(270));
        turned.Freeze();
        Part("edge_v", 10, tileUnits, false, Stretched(turned, 10, tileUnits));
        return turned;
    }

    /// <summary>The emblem at the root, joined to the tree's top line by a piece of rod.</summary>
    private static void Root(string path, BitmapSource? rodVertical)
    {
        const double w = 100, h = 130;
        var origin = new Point(MorseTreeCanvas.RootArtCenter.X - w / 2, MorseTreeCanvas.RootArtCenter.Y - h / 2);
        var (cut, box) = CutOut(path);
        double scale = EmblemWidth / box.Width;
        const double emblemTop = 60;
        double emblemBottom = emblemTop + box.Height * scale;
        var emblemCenter = new Point(MorseTreeCanvas.RootArtCenter.X - origin.X, emblemTop + box.Height * scale / 2 - origin.Y);

        var elements = new List<UIElement>();
        if (rodVertical is not null)
        {
            // From inside the emblem's own stem down past the junction at y = 150.
            double from = emblemBottom - 8, to = 158;
            var stem = new Rectangle { Width = RodThickness, Height = to - from, Fill = new ImageBrush(rodVertical) { Stretch = Stretch.Fill } };
            Canvas.SetLeft(stem, emblemCenter.X - RodThickness / 2);
            Canvas.SetTop(stem, from - origin.Y);
            elements.Add(stem);
        }
        elements.Add(Placed(cut, box, scale, emblemCenter));

        var off = Part("root_off", w, h, false, elements.ToArray());
        // Brass does not glow itself: the light is a halo around the emblem.
        Part("root_on", w, h, true, Blur(Tinted(off, w, h, Gray(0.55), 0.75), 7));
        Part("root_shadow", w, h, false, Blur(Tinted(off, w, h, Colors.Black, 0.75), 3));
    }

    private static void WriteManifest(Dictionary<string, Vector> offsets, double? ditRadius, Size? dahHalf, Rect? nameplate)
    {
        string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        var lines = new List<string> { "{", "  \"pixelsPerUnit\": 3," };
        if (ditRadius is double r) lines.Add($"  \"ditRadius\": {F(r)},");
        if (dahHalf is Size d) lines.Add($"  \"dahHalf\": [{F(d.Width)}, {F(d.Height)}],");
        if (nameplate is Rect p) lines.Add($"  \"nameplate\": [{F(p.X)}, {F(p.Y)}, {F(p.Width)}, {F(p.Height)}],");
        lines.Add("  \"offsets\": {");
        lines.Add(string.Join(",\n", offsets.Select(o => $"    \"{o.Key}\": [{F(o.Value.X)}, {F(o.Value.Y)}]")));
        lines.Add("  }");
        lines.Add("}");
        File.WriteAllText(Path.Combine(OutputDirectory, ThemeAssets.ManifestFileName), string.Join("\n", lines) + "\n");
    }

    // Helpers.

    private static (BitmapSource Cut, Int32Rect Box) CutOut(string path)
    {
        var img = Img.Load(path);
        var mask = Pixels.ObjectMask(img, KeyThreshold);
        return (Pixels.Cutout(img, mask).ToBitmap(), Pixels.Bounds(mask, img.W, img.H));
    }

    /// <summary>The whole photo scaled by <paramref name="scale"/> (units per pixel), its object centered on <paramref name="center"/>.</summary>
    private static Image Placed(BitmapSource source, Int32Rect box, double scale, Point center)
    {
        var image = new Image { Source = source, Width = source.PixelWidth * scale, Height = source.PixelHeight * scale, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        Canvas.SetLeft(image, center.X - (box.X + box.Width / 2.0) * scale);
        Canvas.SetTop(image, center.Y - (box.Y + box.Height / 2.0) * scale);
        return image;
    }

    /// <summary>The object's outline filled with black, for shadows.</summary>
    private static Rectangle Silhouette(BitmapSource source, Int32Rect box, double scale, Point center, double opacity)
    {
        var rect = new Rectangle
        {
            Width = source.PixelWidth * scale,
            Height = source.PixelHeight * scale,
            Fill = Brushes.Black,
            OpacityMask = new ImageBrush(source),
            Opacity = opacity,
        };
        Canvas.SetLeft(rect, center.X - (box.X + box.Width / 2.0) * scale);
        Canvas.SetTop(rect, center.Y - (box.Y + box.Height / 2.0) * scale);
        return rect;
    }

    /// <summary>A rendered part's outline filled with one color.</summary>
    private static Rectangle Tinted(BitmapSource part, double w, double h, Color color, double opacity) => new()
    {
        Width = w,
        Height = h,
        Fill = new SolidColorBrush(color),
        OpacityMask = new ImageBrush(part),
        Opacity = opacity,
    };

    private static Image Stretched(BitmapSource source, double w, double h)
    {
        var image = new Image { Source = source, Width = w, Height = h, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        return image;
    }
}
