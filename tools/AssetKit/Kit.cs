using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace AssetKit;

/// <summary>Drawing and file helpers shared by the theme generators. All sizes are in design units.</summary>
internal static class Kit
{
    public const double PixelsPerUnit = 3;

    public static string OutputDirectory { get; set; } = "";

    /// <summary>Everything written so far, for the overview sheet.</summary>
    public static List<(string Name, BitmapSource Image, bool Lit)> Made { get; } = new();

    /// <summary>Renders the elements on a transparent canvas of the given design size and saves name.png.</summary>
    public static BitmapSource Part(string name, double width, double height, bool lit, params UIElement[] elements)
    {
        var canvas = new Canvas { Width = width, Height = height };
        foreach (var e in elements) canvas.Children.Add(e);
        var bitmap = Rasterize(canvas, width, height, PixelsPerUnit);
        Save(bitmap, System.IO.Path.Combine(OutputDirectory, name + ".png"));
        Made.Add((name, bitmap, lit));
        return bitmap;
    }

    public static BitmapSource Rasterize(FrameworkElement element, double width, double height, double scale)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Round(width * scale), (int)Math.Round(height * scale),
            96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(element);
        bitmap.Freeze();
        return bitmap;
    }

    public static void Save(BitmapSource bitmap, string path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    // Shapes, positioned by their center.

    public static Ellipse Circle(double cx, double cy, double r, Brush? fill, Brush? stroke = null, double strokeWidth = 0) =>
        Oval(cx, cy, r * 2, r * 2, fill, stroke, strokeWidth);

    public static Ellipse Oval(double cx, double cy, double w, double h, Brush? fill, Brush? stroke = null, double strokeWidth = 0)
    {
        var e = new Ellipse { Width = w, Height = h, Fill = fill, Stroke = stroke, StrokeThickness = strokeWidth };
        Canvas.SetLeft(e, cx - w / 2);
        Canvas.SetTop(e, cy - h / 2);
        return e;
    }

    public static Rectangle Box(double cx, double cy, double w, double h, double radius, Brush? fill, Brush? stroke = null, double strokeWidth = 0)
    {
        var r = new Rectangle { Width = w, Height = h, RadiusX = radius, RadiusY = radius, Fill = fill, Stroke = stroke, StrokeThickness = strokeWidth };
        Canvas.SetLeft(r, cx - w / 2);
        Canvas.SetTop(r, cy - h / 2);
        return r;
    }

    public static Path Stroke(string data, Brush brush, double width, double dx = 0, double dy = 0)
    {
        var p = new Path
        {
            Data = Geometry.Parse(data),
            Stroke = brush,
            StrokeThickness = width,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        Canvas.SetLeft(p, dx);
        Canvas.SetTop(p, dy);
        return p;
    }

    public static T Blur<T>(T element, double radius) where T : UIElement
    {
        element.Effect = new BlurEffect { Radius = radius * PixelsPerUnit, KernelType = KernelType.Gaussian };
        return element;
    }

    public static T Rotate<T>(T element, double degrees) where T : UIElement
    {
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = new RotateTransform(degrees);
        return element;
    }

    // Brushes.

    public static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public static SolidColorBrush Solid(string hex) => new(Hex(hex));

    /// <summary>Grayscale light art: 0.5 becomes the state color, 1 white-hot (see ThemeAssets.Tint).</summary>
    public static Color Gray(double level, double alpha = 1)
    {
        byte l = (byte)Math.Clamp((int)Math.Round(level * 255), 0, 255);
        return Color.FromArgb((byte)Math.Clamp((int)Math.Round(alpha * 255), 0, 255), l, l, l);
    }

    public static GradientStopCollection Stops(params (Color Color, double Offset)[] stops)
    {
        var collection = new GradientStopCollection();
        foreach (var (c, o) in stops) collection.Add(new GradientStop(c, o));
        return collection;
    }

    /// <summary>
    /// Lit from the top-left, across the shape's short side: a horizontal bar is bright on top,
    /// a vertical one on the left, a circle along the diagonal.
    /// </summary>
    public static LinearGradientBrush FromTopLeft(double w, double h, params (Color, double)[] stops)
    {
        var brush = new LinearGradientBrush(Stops(stops)) { MappingMode = BrushMappingMode.Absolute };
        if (w >= h)
        {
            brush.StartPoint = new Point(w / 2 - 0.35 * h, 0);
            brush.EndPoint = new Point(w / 2 + 0.35 * h, h);
        }
        else
        {
            brush.StartPoint = new Point(0, h / 2 - 0.35 * w);
            brush.EndPoint = new Point(w, h / 2 + 0.35 * w);
        }
        return brush;
    }

    /// <summary>A gradient across the short side of a w x h box (top to bottom, or left to right).</summary>
    public static LinearGradientBrush Across(double w, double h, params (Color, double)[] stops) =>
        w >= h ? new LinearGradientBrush(Stops(stops), new Point(0, 0), new Point(0, 1))
               : new LinearGradientBrush(Stops(stops), new Point(0, 0), new Point(1, 0));

    /// <summary>A gradient along the long side of a w x h box.</summary>
    public static LinearGradientBrush Along(double w, double h, params (Color, double)[] stops) =>
        w >= h ? new LinearGradientBrush(Stops(stops), new Point(0, 0), new Point(1, 0))
               : new LinearGradientBrush(Stops(stops), new Point(0, 0), new Point(0, 1));

    public static RadialGradientBrush Radial(Point origin, params (Color, double)[] stops) =>
        new(Stops(stops)) { GradientOrigin = origin, Center = origin };

    public static RadialGradientBrush Radial(params (Color, double)[] stops) => new(Stops(stops));
}
