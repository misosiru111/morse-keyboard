using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MorseKeyboard;

/// <summary>
/// Optional PNG artwork for a theme, loaded from Assets/Themes/&lt;theme id&gt;/. Each file replaces one part
/// of the code-drawn theme; parts without a file keep being drawn in code. The file names and the
/// coordinate system are documented in design/ASSETS.md.
/// </summary>
public sealed class ThemeAssets
{
    public const string ManifestFileName = "theme.json";

    private readonly Dictionary<string, BitmapSource> _images;
    private readonly Dictionary<string, Vector> _offsets;
    private readonly Dictionary<(string Name, Color Color), BitmapSource> _tinted = new();

    private ThemeAssets(string directory, Dictionary<string, BitmapSource> images, Dictionary<string, Vector> offsets,
        double pixelsPerUnit, double? ditRadius, Size? dahHalf, Rect? nameplateArea)
    {
        NameplateArea = nameplateArea;
        Directory = directory;
        _images = images;
        _offsets = offsets;
        PixelsPerUnit = pixelsPerUnit;
        DitRadius = ditRadius;
        DahHalf = dahHalf;
    }

    public string Directory { get; }

    /// <summary>Image pixels per design unit (the tree canvas is 680 x 760 units). 3 by default.</summary>
    public double PixelsPerUnit { get; }

    /// <summary>Node sizes matching the artwork, overriding the theme's (they place the letters around nodes).</summary>
    public double? DitRadius { get; }
    public Size? DahHalf { get; }

    /// <summary>Where the backdrop art has its blank nameplate (tree units), overriding the theme's.</summary>
    public Rect? NameplateArea { get; }

    /// <summary>Loads every PNG in <paramref name="directory"/>; null when the folder has none.</summary>
    public static ThemeAssets? Load(string directory)
    {
        if (!System.IO.Directory.Exists(directory)) return null;

        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in System.IO.Directory.EnumerateFiles(directory, "*.png"))
        {
            var image = TryLoadImage(file);
            if (image is not null) images[Path.GetFileNameWithoutExtension(file)] = image;
        }
        if (images.Count == 0) return null;

        var offsets = new Dictionary<string, Vector>(StringComparer.OrdinalIgnoreCase);
        double pixelsPerUnit = 3;
        double? ditRadius = null;
        Size? dahHalf = null;
        Rect? nameplateArea = null;
        string manifest = Path.Combine(directory, ManifestFileName);
        if (File.Exists(manifest))
        {
            try
            {
                var m = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifest), ManifestOptions);
                if (m?.PixelsPerUnit > 0) pixelsPerUnit = m.PixelsPerUnit;
                if (m?.DitRadius > 0) ditRadius = m.DitRadius;
                if (m?.DahHalf is { Length: 2 } half) dahHalf = new Size(half[0], half[1]);
                if (m?.Nameplate is { Length: 4 } plate) nameplateArea = new Rect(plate[0], plate[1], plate[2], plate[3]);
                foreach (var (name, xy) in m?.Offsets ?? new())
                    if (xy.Length == 2) offsets[name] = new Vector(xy[0], xy[1]);
            }
            catch (Exception)
            {
                // A half-saved manifest while editing; the next save reloads it.
            }
        }

        return new ThemeAssets(directory, images, offsets, pixelsPerUnit, ditRadius, dahHalf, nameplateArea);
    }

    /// <summary>Changes whenever a file in the folder is added, removed or saved (used for live reloading).</summary>
    public static string Signature(string directory)
    {
        if (!System.IO.Directory.Exists(directory)) return "";
        return string.Join("|", new DirectoryInfo(directory).EnumerateFiles()
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => $"{f.Name}:{f.Length}:{f.LastWriteTimeUtc.Ticks}"));
    }

    public BitmapSource? Get(string name) => _images.GetValueOrDefault(name);

    /// <summary>
    /// The vertical variant if drawn, else the horizontal one turned 90° counterclockwise, so a highlight on
    /// top ends up on the left and still faces the top-left light (right for rods; lamps should get their own art).
    /// </summary>
    public BitmapSource? GetVertical(string verticalName, string horizontalName)
    {
        if (Get(verticalName) is { } v) return v;
        if (Get(horizontalName) is not { } h) return null;
        string key = verticalName + "@rotated";
        if (_images.TryGetValue(key, out var cached)) return cached;
        var rotated = new TransformedBitmap(h, new RotateTransform(270));
        rotated.Freeze();
        _images[key] = rotated;
        return rotated;
    }

    public Vector OffsetOf(string name) => _offsets.GetValueOrDefault(name);

    /// <summary>Size of an image in design units.</summary>
    public Size SizeOf(BitmapSource image) => new(image.PixelWidth / PixelsPerUnit, image.PixelHeight / PixelsPerUnit);

    /// <summary>
    /// The lit version of a part for one state: &lt;name&gt;_&lt;state&gt;.png if drawn, otherwise
    /// <paramref name="grayscale"/> colored with <see cref="Tint"/>.
    /// </summary>
    public BitmapSource Lit(string name, BitmapSource grayscale, string state, Color color)
    {
        if (Get($"{name}_{state}") is { } explicitImage) return explicitImage;
        var key = (name, color);
        if (!_tinted.TryGetValue(key, out var tinted))
            _tinted[key] = tinted = Tint(grayscale, color);
        return tinted;
    }

    /// <summary>
    /// Colors grayscale light art: black stays black, 50% gray becomes <paramref name="color"/>,
    /// and white becomes white, so a bright core reads as white-hot with a colored falloff. Alpha is kept.
    /// </summary>
    public static BitmapSource Tint(BitmapSource grayscale, Color color)
    {
        var source = new FormatConvertedBitmap(grayscale, PixelFormats.Bgra32, null, 0);
        int width = source.PixelWidth, height = source.PixelHeight, stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            double l = (0.2126 * pixels[i + 2] + 0.7152 * pixels[i + 1] + 0.0722 * pixels[i]) / 255.0;
            pixels[i + 2] = Channel(color.R, l);
            pixels[i + 1] = Channel(color.G, l);
            pixels[i] = Channel(color.B, l);
        }

        var result = BitmapSource.Create(width, height, grayscale.DpiX, grayscale.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    private static byte Channel(byte tint, double luminance)
    {
        double c = tint / 255.0;
        double value = luminance <= 0.5 ? c * luminance * 2 : c + (1 - c) * (luminance - 0.5) * 2;
        return (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }

    private static BitmapSource? TryLoadImage(string path)
    {
        try
        {
            // Read into memory so the file is not kept locked while it is being edited.
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            // (A stream source is never cached, and IgnoreImageCache would throw without a UriSource.)
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(File.ReadAllBytes(path));
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception)
        {
            // Unreadable or half-written; skipped until it is saved again.
            return null;
        }
    }

    private static readonly JsonSerializerOptions ManifestOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class Manifest
    {
        public double PixelsPerUnit { get; set; }
        public double? DitRadius { get; set; }
        public double[]? DahHalf { get; set; }
        public double[]? Nameplate { get; set; }
        public Dictionary<string, double[]> Offsets { get; set; } = new();
    }
}
