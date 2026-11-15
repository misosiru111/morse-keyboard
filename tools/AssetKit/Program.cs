using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
using MorseKeyboard;
using static AssetKit.Kit;

namespace AssetKit;

/// <summary>
/// Writes artwork for the 電信局 theme into Assets/Themes/telegraph/ and an overview sheet into design/.
/// Usage: dotnet run --project tools/AssetKit -- &lt;starter|import&gt; &lt;repo root&gt;
///   starter: code-drawn starter art for every part (overwrites all parts)
///   import:  parts made from the photos in design/raw/telegraph/ (overwrites only those parts)
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";
        if (mode is not ("starter" or "import"))
        {
            Console.Error.WriteLine("Usage: AssetKit <starter|import> <repo root>");
            return 1;
        }
        string root = Path.GetFullPath(args.Length > 1 ? args[1] : Environment.CurrentDirectory);
        if (!File.Exists(Path.Combine(root, "MorseKeyboard.csproj")))
        {
            Console.Error.WriteLine($"Not the repository root: {root}");
            return 1;
        }

        OutputDirectory = Path.Combine(root, "Assets", "Themes", Themes.Telegraph.Id);
        if (mode == "starter")
            Telegraph.Make();
        else
            Importer.Run(Path.Combine(root, "design", "raw", Themes.Telegraph.Id));
        Sheet(Themes.Telegraph, Path.Combine(root, "design", $"{Themes.Telegraph.Id}_parts.png"));

        Console.WriteLine($"Wrote {Made.Count} parts to {OutputDirectory}");
        return 0;
    }

    /// <summary>Every part on a checkerboard; light art is shown as drawn (grayscale) and colored for each state.</summary>
    private static void Sheet(Theme theme, string path)
    {
        const double cell = 170, label = 22, gap = 10;
        var states = new[] { theme.Path, theme.Current, theme.Pending, theme.Success, theme.Error };
        var parts = Made.Where(m => m.Name is not ("background" or "backdrop")).ToList();
        int columns = 1 + states.Length;
        double width = columns * (cell + gap) + gap;
        double height = parts.Count * (cell + label + gap) + gap + 30;

        var canvas = new Canvas { Width = width, Height = height, Background = new SolidColorBrush(Hex("#2B2D33")) };
        var header = new TextBlock
        {
            Text = "左: 描いた画像 / 右: アプリが付ける色（経路・最深・行き先・確定・エラー）",
            Foreground = Brushes.White, FontSize = 14,
        };
        Canvas.SetLeft(header, gap);
        Canvas.SetTop(header, 8);
        canvas.Children.Add(header);

        double y = 30;
        foreach (var (name, image, lit) in parts)
        {
            AddCell(canvas, gap, y, cell, label, name, image);
            if (lit)
            {
                for (int i = 0; i < states.Length; i++)
                    AddCell(canvas, gap + (i + 1) * (cell + gap), y, cell, label, "", ThemeAssets.Tint(image, states[i].Color));
            }
            y += cell + label + gap;
        }

        Save(Rasterize(canvas, width, height, 1), path);
    }

    private static void AddCell(Canvas canvas, double x, double y, double cell, double labelHeight, string name, BitmapSource image)
    {
        var checker = new DrawingBrush
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 16, 16),
            ViewportUnits = BrushMappingMode.Absolute,
            Drawing = new DrawingGroup
            {
                Children =
                {
                    new GeometryDrawing(new SolidColorBrush(Hex("#3A2410")), null, new RectangleGeometry(new Rect(0, 0, 16, 16))),
                    new GeometryDrawing(new SolidColorBrush(Hex("#4A3018")), null, new RectangleGeometry(new Rect(0, 0, 8, 8))),
                    new GeometryDrawing(new SolidColorBrush(Hex("#4A3018")), null, new RectangleGeometry(new Rect(8, 8, 8, 8))),
                },
            },
        };
        var box = new Rectangle { Width = cell, Height = cell, Fill = checker };
        Canvas.SetLeft(box, x);
        Canvas.SetTop(box, y + labelHeight);
        canvas.Children.Add(box);

        var img = new Image { Source = image, Width = cell - 8, Height = cell - 8, Stretch = Stretch.Uniform };
        Canvas.SetLeft(img, x + 4);
        Canvas.SetTop(img, y + labelHeight + 4);
        canvas.Children.Add(img);

        var text = new TextBlock { Text = name, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 13 };
        Canvas.SetLeft(text, x);
        Canvas.SetTop(text, y + 2);
        canvas.Children.Add(text);
    }
}
