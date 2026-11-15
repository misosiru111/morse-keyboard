using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MorseKeyboard;

/// <summary>
/// Procedurally generated frame backgrounds, so the themes ship without third-party image assets.
/// The brushes are generated once at the overlay's default proportions and stretched to fill the window;
/// <see cref="Render"/> makes larger copies, e.g. as a starting point for a theme's background.png.
/// </summary>
public static class Textures
{
    public enum Kind { Wood, Olive, Night, Pcb }

    private const int DefaultWidth = 540;
    private const int DefaultHeight = 880;

    private static Brush? _wood, _olive, _night, _pcb;

    /// <summary>Dark walnut planks with vertical grain.</summary>
    public static Brush Wood => _wood ??= ToBrush(Render(Kind.Wood, DefaultWidth, DefaultHeight));

    /// <summary>Olive drab enamel on steel: mottled, with a fine speckle.</summary>
    public static Brush Olive => _olive ??= ToBrush(Render(Kind.Olive, DefaultWidth, DefaultHeight));

    /// <summary>Night sky: deep blue gradient, faint nebula and a scatter of stars.</summary>
    public static Brush Night => _night ??= ToBrush(Render(Kind.Night, DefaultWidth, DefaultHeight));

    /// <summary>Green solder mask over glass-fiber weave.</summary>
    public static Brush Pcb => _pcb ??= ToBrush(Render(Kind.Pcb, DefaultWidth, DefaultHeight));

    /// <summary>Generates a texture at any size; detail scales with the size so it looks the same, only sharper.</summary>
    public static BitmapSource Render(Kind kind, int width, int height)
    {
        double scale = width / (double)DefaultWidth; // pixel-sized details grow with the image
        Func<double, double, (double, double, double)> pixel = kind switch
        {
            Kind.Wood => (u, v) => WoodPixel(u, v, width, scale),
            Kind.Olive => (u, v) => OlivePixel(u, v, width, height, scale),
            Kind.Night => (u, v) => NightPixel(u, v, width, height, scale),
            _ => (u, v) => PcbPixel(u, v, width, height, scale),
        };

        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            double v = (double)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                var (r, g, b) = pixel((double)x / (width - 1), v);
                int i = (y * width + x) * 4;
                pixels[i] = ToByte(b);
                pixels[i + 1] = ToByte(g);
                pixels[i + 2] = ToByte(r);
                pixels[i + 3] = 255;
            }
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static (double, double, double) WoodPixel(double u, double v, int width, double scale)
    {
        // Grain rings warped by low-frequency noise, plus fine fibers along the grain.
        double warp = Fbm(u * 3, v * 0.7, 11, 4) * 5.0;
        double rings = 0.5 + 0.5 * Math.Sin((u * 26 + warp) * Math.PI * 2);
        double fibers = Fbm(u * 120, v * 3, 12, 3);
        double blotch = Fbm(u * 6, v * 4, 13, 3);
        double shade = 0.45 * Math.Pow(rings, 2.5) + 0.35 * fibers + 0.2 * blotch;

        var c = Lerp(Rgb(0x2B, 0x16, 0x0A), Rgb(0x6E, 0x42, 0x22), shade);

        // Plank seams.
        foreach (double seam in new[] { 0.34, 0.69 })
        {
            double d = Math.Abs(u - seam) * width / scale;
            if (d < 1.6) c = Scale(c, 0.45 + 0.3 * d / 1.6);
        }
        return Scale(c, Vignette(u, v, 0.35));
    }

    private static (double, double, double) OlivePixel(double u, double v, int width, int height, double scale)
    {
        double mottle = Fbm(u * 4, v * 4, 21, 4);
        double speckle = Hash((int)(u * width / scale), (int)(v * height / scale), 22);
        var c = Scale(Rgb(0x4E, 0x55, 0x2E), 0.86 + 0.24 * mottle + 0.06 * (speckle - 0.5));
        return Scale(c, Vignette(u, v, 0.3));
    }

    private static (double, double, double) NightPixel(double u, double v, int width, int height, double scale)
    {
        var c = Lerp(Rgb(0x06, 0x0A, 0x1E), Rgb(0x15, 0x1C, 0x48), v);

        // A faint diagonal band of nebula.
        double band = Math.Exp(-Math.Pow((u - v * 0.7 - 0.05) * 3.2, 2));
        double cloud = Fbm(u * 4, v * 4, 31, 5);
        double nebula = band * Math.Max(0, cloud - 0.35) * 0.9;
        c = Add(c, Rgb(0x6A, 0x5A, 0xB8), nebula * 0.5);

        // Stars: sparse cells, each holding at most one star of random brightness.
        const int cell = 9;
        double px = u * width / scale, py = v * height / scale;
        int cx = (int)(px / cell), cy = (int)(py / cell);
        if (Hash(cx, cy, 32) < 0.42)
        {
            double sx = cx * cell + Hash(cx, cy, 33) * cell;
            double sy = cy * cell + Hash(cx, cy, 34) * cell;
            double brightness = Math.Pow(Hash(cx, cy, 35), 6);
            double radius = 0.55 + brightness * 1.4;
            double d = Math.Sqrt((px - sx) * (px - sx) + (py - sy) * (py - sy));
            double glow = Math.Exp(-(d * d) / (radius * radius)) * (0.25 + 0.75 * brightness + 0.15);
            var tint = Hash(cx, cy, 36) < 0.3 ? Rgb(0xFF, 0xE6, 0xC0) : Rgb(0xD6, 0xE2, 0xFF);
            c = Add(c, tint, Math.Min(1, glow));
        }
        return c;
    }

    private static (double, double, double) PcbPixel(double u, double v, int width, int height, double scale)
    {
        double x = u * width / scale, y = v * height / scale;
        double weave = Math.Sin(x * 0.85) * Math.Sin(y * 0.85);
        double mottle = Fbm(u * 5, v * 5, 41, 3);
        var c = Scale(Rgb(0x0E, 0x4A, 0x2C), 0.9 + 0.025 * weave + 0.16 * mottle);
        return Scale(c, Vignette(u, v, 0.25));
    }

    private static Brush ToBrush(BitmapSource bitmap)
    {
        var brush = new ImageBrush(bitmap) { Stretch = Stretch.Fill };
        brush.Freeze();
        return brush;
    }

    // Colors are 0..1 tuples.

    private static (double, double, double) Rgb(int r, int g, int b) => (r / 255.0, g / 255.0, b / 255.0);

    private static (double, double, double) Lerp((double R, double G, double B) a, (double R, double G, double B) b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    private static (double, double, double) Scale((double R, double G, double B) c, double k) => (c.R * k, c.G * k, c.B * k);

    private static (double, double, double) Add((double R, double G, double B) c, (double R, double G, double B) light, double k) =>
        (c.R + light.R * k, c.G + light.G * k, c.B + light.B * k);

    private static double Vignette(double u, double v, double strength)
    {
        double dx = u - 0.5, dy = v - 0.5;
        return 1 - strength * Math.Min(1, (dx * dx + dy * dy) * 2.2);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);

    // Value noise.

    private static double Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (double)0xFFFFFF;
        }
    }

    private static double Noise(double x, double y, int seed)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        double xf = x - xi, yf = y - yi;
        double sx = xf * xf * (3 - 2 * xf), sy = yf * yf * (3 - 2 * yf);
        double a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed);
        double c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
        return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
    }

    /// <summary>Fractal noise in 0..1.</summary>
    private static double Fbm(double x, double y, int seed, int octaves)
    {
        double sum = 0, amplitude = 0.5, norm = 0;
        for (int i = 0; i < octaves; i++)
        {
            sum += Noise(x, y, seed + i * 101) * amplitude;
            norm += amplitude;
            x *= 2.03;
            y *= 2.03;
            amplitude *= 0.5;
        }
        return sum / norm;
    }
}
