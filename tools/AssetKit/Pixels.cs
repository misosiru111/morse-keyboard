using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AssetKit;

/// <summary>A straight (non-premultiplied) RGBA image with channels in 0..1, for cleaning up generated photos.</summary>
internal sealed class Img
{
    public Img(int width, int height)
    {
        W = width;
        H = height;
        R = new float[width * height];
        G = new float[width * height];
        B = new float[width * height];
        A = new float[width * height];
    }

    public int W { get; }
    public int H { get; }
    public float[] R { get; }
    public float[] G { get; }
    public float[] B { get; }
    public float[] A { get; }

    public static Img Load(string path)
    {
        var decoded = new BitmapImage();
        decoded.BeginInit();
        decoded.CacheOption = BitmapCacheOption.OnLoad;
        decoded.StreamSource = new MemoryStream(File.ReadAllBytes(path));
        decoded.EndInit();
        var source = new FormatConvertedBitmap(decoded, PixelFormats.Bgra32, null, 0);
        var img = new Img(source.PixelWidth, source.PixelHeight);
        var px = new byte[img.W * img.H * 4];
        source.CopyPixels(px, img.W * 4, 0);
        for (int i = 0; i < img.W * img.H; i++)
        {
            img.B[i] = px[i * 4] / 255f;
            img.G[i] = px[i * 4 + 1] / 255f;
            img.R[i] = px[i * 4 + 2] / 255f;
            img.A[i] = px[i * 4 + 3] / 255f;
        }
        return img;
    }

    public BitmapSource ToBitmap()
    {
        var px = new byte[W * H * 4];
        for (int i = 0; i < W * H; i++)
        {
            px[i * 4] = ToByte(B[i]);
            px[i * 4 + 1] = ToByte(G[i]);
            px[i * 4 + 2] = ToByte(R[i]);
            px[i * 4 + 3] = ToByte(A[i]);
        }
        var bitmap = BitmapSource.Create(W, H, 96, 96, PixelFormats.Bgra32, null, px, W * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public float Luma(int i) => 0.2126f * R[i] + 0.7152f * G[i] + 0.0722f * B[i];

    /// <summary>Bilinear resample (used to match a slightly differently sized edit to its original).</summary>
    public Img Resized(int width, int height)
    {
        var result = new Img(width, height);
        for (int y = 0; y < height; y++)
        {
            float sy = (y + 0.5f) * H / height - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float sx = (x + 0.5f) * W / width - 0.5f;
                int i = y * width + x;
                result.R[i] = Sample(R, sx, sy);
                result.G[i] = Sample(G, sx, sy);
                result.B[i] = Sample(B, sx, sy);
                result.A[i] = Sample(A, sx, sy);
            }
        }
        return result;
    }

    public float Sample(float[] channel, float x, float y)
    {
        x = Math.Clamp(x, 0, W - 1);
        y = Math.Clamp(y, 0, H - 1);
        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, W - 1), y1 = Math.Min(y0 + 1, H - 1);
        float fx = x - x0, fy = y - y0;
        float top = channel[y0 * W + x0] * (1 - fx) + channel[y0 * W + x1] * fx;
        float bottom = channel[y1 * W + x0] * (1 - fx) + channel[y1 * W + x1] * fx;
        return top * (1 - fy) + bottom * fy;
    }

    private static byte ToByte(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255), 0, 255);
}

internal static class Pixels
{
    /// <summary>
    /// The object's mask (1 = object): background is everything connected to the image border whose color is
    /// within <paramref name="threshold"/> of the border's typical color, or is a colorless darker gray
    /// (the soft shadow the object casts on the backdrop), or continues the backdrop's gentle gradient
    /// (a colorless pixel almost identical to its background neighbor). Bright spots inside the object
    /// (glass reflections) are kept because they are not connected to the border.
    /// </summary>
    public static float[] ObjectMask(Img img, float threshold)
    {
        var (br, bg, bb) = BorderColor(img);
        int w = img.W, h = img.H;
        var background = new bool[w * h];
        var queue = new Queue<int>();

        float backdropLuma = 0.2126f * br + 0.7152f * bg + 0.0722f * bb;
        bool Near(int i)
        {
            if (Math.Max(Math.Abs(img.R[i] - br), Math.Max(Math.Abs(img.G[i] - bg), Math.Abs(img.B[i] - bb))) < threshold)
                return true;
            float saturation = Math.Max(img.R[i], Math.Max(img.G[i], img.B[i])) - Math.Min(img.R[i], Math.Min(img.G[i], img.B[i]));
            float luma = img.Luma(i);
            return saturation < 0.06f && luma < backdropLuma + 0.03f && luma > backdropLuma - 0.45f;
        }
        bool Continues(int i, int from)
        {
            float step = Math.Max(Math.Abs(img.R[i] - img.R[from]), Math.Max(Math.Abs(img.G[i] - img.G[from]), Math.Abs(img.B[i] - img.B[from])));
            float saturation = Math.Max(img.R[i], Math.Max(img.G[i], img.B[i])) - Math.Min(img.R[i], Math.Min(img.G[i], img.B[i]));
            return step < 0.012f && saturation < 0.06f && Math.Abs(img.Luma(i) - backdropLuma) < 0.3f;
        }
        void Visit(int i, int from)
        {
            if (background[i] || !(Near(i) || (from >= 0 && Continues(i, from)))) return;
            background[i] = true;
            queue.Enqueue(i);
        }

        for (int x = 0; x < w; x++) { Visit(x, -1); Visit((h - 1) * w + x, -1); }
        for (int y = 0; y < h; y++) { Visit(y * w, -1); Visit(y * w + w - 1, -1); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % w, y = i / w;
            if (x > 0) Visit(i - 1, i);
            if (x < w - 1) Visit(i + 1, i);
            if (y > 0) Visit(i - w, i);
            if (y < h - 1) Visit(i + w, i);
        }

        var mask = new float[w * h];
        for (int i = 0; i < mask.Length; i++) mask[i] = background[i] ? 0 : 1;
        DropSpecks(mask, w, h);
        return mask;
    }

    /// <summary>
    /// Removes small islands (dust, noise in the backdrop) so they cannot stretch the object's bounds:
    /// keeps only pieces at least 5% the size of the largest.
    /// </summary>
    private static void DropSpecks(float[] mask, int w, int h)
    {
        var label = new int[mask.Length];
        var sizes = new List<int> { 0 };
        var queue = new Queue<int>();
        for (int start = 0; start < mask.Length; start++)
        {
            if (mask[start] < 0.5f || label[start] != 0) continue;
            int id = sizes.Count, size = 0;
            label[start] = id;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                size++;
                int x = i % w, y = i / w;
                foreach (int n in new[] { x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1 })
                {
                    if (n < 0 || mask[n] < 0.5f || label[n] != 0) continue;
                    label[n] = id;
                    queue.Enqueue(n);
                }
            }
            sizes.Add(size);
        }

        int largest = sizes.Max();
        for (int i = 0; i < mask.Length; i++)
            if (label[i] != 0 && sizes[label[i]] < largest * 0.05) mask[i] = 0;
    }

    /// <summary>The object cut out with a soft, slightly pulled-in edge so no background fringe remains.</summary>
    public static Img Cutout(Img img, float[] mask)
    {
        var alpha = Blur(Erode(mask, img.W, img.H), img.W, img.H, 1.2f);
        var result = new Img(img.W, img.H);
        Array.Copy(img.R, result.R, img.R.Length);
        Array.Copy(img.G, result.G, img.G.Length);
        Array.Copy(img.B, result.B, img.B.Length);
        for (int i = 0; i < alpha.Length; i++) result.A[i] = alpha[i] * img.A[i];
        return result;
    }

    public static Int32Rect Bounds(float[] mask, int w, int h)
    {
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (mask[y * w + x] < 0.5f) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        }
        return maxX < 0 ? Int32Rect.Empty : new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// How far <paramref name="lit"/> is shifted relative to <paramref name="unlit"/> (image edits tend to
    /// move things a few pixels), found by comparing brightness over the object.
    /// </summary>
    public static (int Dx, int Dy) Align(Img unlit, Img lit, float[] mask, int range)
    {
        var box = Bounds(mask, unlit.W, unlit.H);
        double best = double.MaxValue;
        (int, int) bestShift = (0, 0);
        for (int dy = -range; dy <= range; dy++)
        {
            for (int dx = -range; dx <= range; dx++)
            {
                double cost = 0;
                int n = 0;
                for (int y = box.Y; y < box.Y + box.Height; y += 4)
                {
                    int ly = y + dy;
                    if (ly < 0 || ly >= lit.H) continue;
                    for (int x = box.X; x < box.X + box.Width; x += 4)
                    {
                        int lx = x + dx;
                        int i = y * unlit.W + x;
                        if (lx < 0 || lx >= lit.W || mask[i] < 0.5f) continue;
                        cost += Math.Abs(unlit.Luma(i) - lit.Luma(ly * lit.W + lx));
                        n++;
                    }
                }
                if (n > 0 && cost / n < best)
                {
                    best = cost / n;
                    bestShift = (dx, dy);
                }
            }
        }
        return bestShift;
    }

    /// <summary>
    /// The light a lit photo adds over the unlit one, as grayscale light art (see ThemeAssets.Tint):
    /// alpha is how much brighter it got, gray is 0.5 for colored light and up to 1 where it is white-hot.
    /// The result is in the unlit image's pixel grid. Light far from the object fades out.
    /// </summary>
    public static Img Emission(Img unlit, Img lit, float[] mask, (int Dx, int Dy) shift)
    {
        int w = unlit.W, h = unlit.H;
        var near = Blur(mask, w, h, Math.Max(w, h) * 0.04f);
        float margin = Math.Max(w, h) * 0.05f;
        var result = new Img(w, h);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                int lx = Math.Clamp(x + shift.Dx, 0, lit.W - 1), ly = Math.Clamp(y + shift.Dy, 0, lit.H - 1);
                int j = ly * lit.W + lx;
                float gain = Math.Max(lit.R[j] - unlit.R[i], Math.Max(lit.G[j] - unlit.G[i], lit.B[j] - unlit.B[i]));
                float alpha = SmoothStep(0.02f, 0.32f, gain);

                // Keep the light near the object and away from the photo's edges.
                float reach = Math.Clamp(near[i] * 2.5f, 0, 1);
                float edge = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y)) / margin;
                alpha *= Math.Max(mask[i], reach) * Math.Clamp(edge, 0, 1);

                float minChannel = Math.Min(lit.R[j], Math.Min(lit.G[j], lit.B[j]));
                float hot = SmoothStep(0.72f, 0.95f, minChannel) * mask[i];
                float gray = 0.5f + 0.5f * hot;

                result.R[i] = result.G[i] = result.B[i] = gray;
                result.A[i] = alpha;
            }
        }
        return result;
    }

    /// <summary>Approximate Gaussian blur (three box passes).</summary>
    public static float[] Blur(float[] src, int w, int h, float radius)
    {
        int r = Math.Max(1, (int)MathF.Round(radius));
        var a = (float[])src.Clone();
        var b = new float[a.Length];
        for (int pass = 0; pass < 3; pass++)
        {
            BoxH(a, b, w, h, r);
            BoxV(b, a, w, h, r);
        }
        return a;
    }

    private static float[] Erode(float[] mask, int w, int h)
    {
        var result = new float[mask.Length];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float m = 1;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = Math.Clamp(x + dx, 0, w - 1), yy = Math.Clamp(y + dy, 0, h - 1);
                        m = Math.Min(m, mask[yy * w + xx]);
                    }
                result[y * w + x] = m;
            }
        }
        return result;
    }

    private static void BoxH(float[] src, float[] dst, int w, int h, int r)
    {
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            float sum = 0;
            for (int x = -r; x <= r; x++) sum += src[row + Math.Clamp(x, 0, w - 1)];
            for (int x = 0; x < w; x++)
            {
                dst[row + x] = sum / (2 * r + 1);
                sum += src[row + Math.Min(x + r + 1, w - 1)] - src[row + Math.Max(x - r, 0)];
            }
        }
    }

    private static void BoxV(float[] src, float[] dst, int w, int h, int r)
    {
        for (int x = 0; x < w; x++)
        {
            float sum = 0;
            for (int y = -r; y <= r; y++) sum += src[Math.Clamp(y, 0, h - 1) * w + x];
            for (int y = 0; y < h; y++)
            {
                dst[y * w + x] = sum / (2 * r + 1);
                sum += src[Math.Min(y + r + 1, h - 1) * w + x] - src[Math.Max(y - r, 0) * w + x];
            }
        }
    }

    private static (float, float, float) BorderColor(Img img)
    {
        var rs = new List<float>();
        var gs = new List<float>();
        var bs = new List<float>();
        void Add(int i) { rs.Add(img.R[i]); gs.Add(img.G[i]); bs.Add(img.B[i]); }
        for (int x = 0; x < img.W; x += 2) { Add(x); Add((img.H - 1) * img.W + x); }
        for (int y = 0; y < img.H; y += 2) { Add(y * img.W); Add(y * img.W + img.W - 1); }
        return (Median(rs), Median(gs), Median(bs));
    }

    private static float Median(List<float> values)
    {
        values.Sort();
        return values[values.Count / 2];
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
