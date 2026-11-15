using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MorseKeyboard;

/// <summary>
/// The A–Z tree in the classic metal-plate layout: from the key, dits run right (E I S H) and dahs
/// run left (T M O), branching downward. Circles are dits, bars are dahs.
/// Drawn from a <see cref="Theme"/>; any part with artwork in <see cref="ThemeAssets"/> uses the image instead.
/// </summary>
public sealed class MorseTreeCanvas : Canvas
{
    public const double CanvasWidth = 680;
    public const double CanvasHeight = 760;

    /// <summary>Where root_*.png images are centered.</summary>
    public static readonly Point RootArtCenter = new(333, 111);

    private enum Side { Top, Bottom, Left, Right }

    private enum Look { Idle, Path, Current, Pending, Error, Success }

    private sealed record NodeSpec(string Code, double X, double Y, Side Label);

    /// <summary>A node (or the root) and everything that lights up with it.</summary>
    private sealed class Node
    {
        public Node(string code, SolidColorBrush? labelBrush)
        {
            Code = code;
            LabelBrush = labelBrush;
        }

        public string Code { get; }
        public SolidColorBrush? LabelBrush { get; }

        /// <summary>Elements whose opacity switches the light on and off.</summary>
        public List<UIElement> Lights { get; } = new();

        /// <summary>Switch the lights to a state's colors or images.</summary>
        public List<Action<Look>> Painters { get; } = new();
    }

    /// <summary>The code-drawn light of one state.</summary>
    private sealed class LookStyle
    {
        public LookStyle(LookSpec spec)
        {
            var c = spec.Color;
            Spec = spec;
            Fill = Palette.Solid(Color.FromArgb(spec.FillAlpha, c.R, c.G, c.B));
            Stroke = Palette.Solid(spec.Stroke);
            var glow = new DropShadowEffect { Color = c, BlurRadius = spec.Blur, ShadowDepth = 0, Opacity = 1 };
            glow.Freeze();
            Glow = glow;
        }

        public LookSpec Spec { get; }
        public Brush Fill { get; }
        public Brush Stroke { get; }
        public Effect Glow { get; }
    }

    private static readonly Point RootPoint = new(333, 150);

    // Positions follow the reference chart; rows are 95 px apart, columns hug the line they hang from.
    private static readonly NodeSpec[] Layout =
    {
        new(".",    400, 150, Side.Top),    // E
        new("..",   474, 150, Side.Top),    // I
        new("...",  548, 150, Side.Top),    // S
        new("....", 622, 150, Side.Top),    // H
        new("..-",  474, 245, Side.Left),   // U
        new("..-.", 474, 340, Side.Right),  // F
        new("...-", 548, 245, Side.Right),  // V
        new(".-",   400, 435, Side.Left),   // A
        new(".-.",  474, 435, Side.Bottom), // R
        new(".-..", 548, 435, Side.Right),  // L
        new(".--",  400, 620, Side.Left),   // W
        new(".--.", 474, 620, Side.Right),  // P
        new(".---", 400, 715, Side.Right),  // J
        new("-",    266, 150, Side.Top),    // T
        new("--",   168, 150, Side.Top),    // M
        new("---",   70, 150, Side.Top),    // O
        new("--.",  168, 245, Side.Right),  // G
        new("--.-",  70, 245, Side.Bottom), // Q
        new("--..", 168, 340, Side.Right),  // Z
        new("-.",   266, 435, Side.Right),  // N
        new("-.-",  168, 435, Side.Top),    // K
        new("-.--",  70, 435, Side.Top),    // Y
        new("-.-.", 168, 525, Side.Right),  // C
        new("-..",  266, 620, Side.Right),  // D
        new("-..-", 168, 620, Side.Left),   // X
        new("-...", 266, 715, Side.Left),   // B
    };

    private static readonly Duration FlashDuration = TimeSpan.FromMilliseconds(750);

    private readonly List<Node> _nodes = new();
    private Node _root = new("", null);
    private Dictionary<Look, LookStyle> _styles = new();
    private Theme _theme = Themes.Classic;
    private ThemeAssets? _assets;

    public MorseTreeCanvas()
    {
        Width = CanvasWidth;
        Height = CanvasHeight;
        IsHitTestVisible = false;
        ApplyTheme(Themes.Classic, null);
    }

    /// <summary>Rebuilds every visual for <paramref name="theme"/>; call <see cref="Render"/> afterwards.</summary>
    public void ApplyTheme(Theme theme, ThemeAssets? assets)
    {
        _theme = theme;
        _assets = assets;
        _styles = new Dictionary<Look, LookStyle>
        {
            [Look.Path] = new(theme.Path),
            [Look.Current] = new(theme.Current),
            [Look.Pending] = new(theme.Pending),
            [Look.Error] = new(theme.Error),
            [Look.Success] = new(theme.Success),
        };

        Children.Clear();
        _nodes.Clear();

        // Layers, bottom to top.
        var backdrop = new List<UIElement>();
        var edgeShadows = new List<UIElement>();
        var edges = new List<UIElement>();
        var nodeShadows = new List<UIElement>();
        var spills = new List<UIElement>();
        var edgeLights = new List<UIElement>();
        var roots = new List<UIElement>();
        var nodeBases = new List<UIElement>();
        var decorations = new List<UIElement>();
        var nodeLights = new List<UIElement>();
        var labels = new List<UIElement>();

        if (assets?.Get("backdrop") is { } backdropImage)
            backdrop.Add(PlaceImage(backdropImage, new Point(0, 0), centered: false));
        else if (theme.Backdrop is not null)
            backdrop.AddRange(theme.Backdrop());
        if ((assets?.NameplateArea ?? theme.NameplateArea) is Rect plate && theme.NameplateEngraving is { } plateEngraving)
            backdrop.Add(Nameplate(plate, plateEngraving, theme));

        var positions = Layout.ToDictionary(n => n.Code, n => new Point(n.X, n.Y));
        foreach (var spec in Layout)
        {
            var p = new Point(spec.X, spec.Y);
            string parentCode = spec.Code[..^1];
            Point parent = parentCode.Length == 0 ? RootPoint : positions[parentCode];
            bool horizontal = Math.Abs(parent.X - p.X) >= Math.Abs(parent.Y - p.Y);
            bool dah = spec.Code[^1] == '-';
            string kind = !dah ? "dit" : horizontal ? "dahh" : "dahv";
            double ditRadius = assets?.DitRadius ?? theme.DitRadius;
            Size dahHalf = assets?.DahHalf ?? theme.DahHalf;
            Size half = !dah ? new Size(ditRadius, ditRadius)
                : horizontal ? dahHalf
                : new Size(dahHalf.Height, dahHalf.Width);

            MorseTable.TryDecode(spec.Code, out char letter);
            var node = new Node(spec.Code, new SolidColorBrush(theme.LabelIdle));
            _nodes.Add(node);

            // The edge from the parent to this node.
            bool edgeHorizontal = Math.Abs(parent.Y - p.Y) < 0.5;
            string edgeName = edgeHorizontal ? "edge_h" : "edge_v";
            if (Part(edgeName + "_shadow", "edge_h_shadow", !edgeHorizontal) is { } edgeShadow)
                edgeShadows.Add(EdgeImage(parent, p, edgeShadow, assets!.OffsetOf(edgeName + "_shadow")));
            edges.Add(Part(edgeName, "edge_h", !edgeHorizontal) is { } edgeArt
                ? EdgeImage(parent, p, edgeArt, assets!.OffsetOf(edgeName))
                : MakeLine(parent, p, theme.EdgeBrush, theme.EdgeThickness));

            if (Part(edgeName + "_on", "edge_h_on", !edgeHorizontal) is { } edgeOn)
            {
                var lit = EdgeImage(parent, p, edgeOn, assets!.OffsetOf(edgeName + "_on"));
                var brush = (ImageBrush)lit.Fill;
                node.Painters.Add(look => brush.ImageSource = LitArt(edgeName + "_on", edgeOn, look));
                AddLight(node, edgeLights, lit);
            }
            else
            {
                var line = MakeLine(parent, p, null, Math.Max(3, theme.EdgeThickness + 1));
                node.Painters.Add(look => line.Stroke = _styles[look].Spec.Brush);
                AddLight(node, edgeLights, line);
            }

            // Shadow and light spill on the background.
            if (Part(kind + "_shadow", "dahh_shadow", kind == "dahv") is { } shadow)
                nodeShadows.Add(PlaceImage(shadow, p + assets!.OffsetOf(kind + "_shadow")));
            string spillName = assets?.Get(kind + "_light") is not null ? kind + "_light" : "light";
            if (assets?.Get(spillName) is { } spill)
            {
                var image = PlaceImage(spill, p + assets.OffsetOf(spillName));
                node.Painters.Add(look => image.Source = LitArt(spillName, spill, look));
                AddLight(node, spills, image);
            }

            // The node itself, unlit.
            if (Part(kind + "_off", "dahh_off", kind == "dahv") is { } offArt)
            {
                nodeBases.Add(PlaceImage(offArt, p + assets!.OffsetOf(kind + "_off")));
            }
            else
            {
                var shape = MakeShape(p, dah, half, theme.DahCorner);
                shape.Fill = theme.NodeFill;
                shape.Stroke = theme.NodeStroke;
                shape.StrokeThickness = theme.NodeStrokeThickness;
                shape.Effect = theme.NodeEffect;
                nodeBases.Add(shape);
                if (theme.NodeDecoration is not null)
                    decorations.AddRange(theme.NodeDecoration(p, dah, half));
            }

            // The node's light.
            if (Part(kind + "_on", "dahh_on", kind == "dahv") is { } onArt)
            {
                var image = PlaceImage(onArt, p + assets!.OffsetOf(kind + "_on"));
                node.Painters.Add(look => image.Source = LitArt(kind + "_on", onArt, look));
                AddLight(node, nodeLights, image);
            }
            else
            {
                var glowHalf = new Size(half.Width * theme.GlowScale, half.Height * theme.GlowScale);
                var glow = MakeShape(p, dah, glowHalf, theme.DahCorner * theme.GlowScale);
                glow.StrokeThickness = Math.Max(1.5, theme.NodeStrokeThickness);
                node.Painters.Add(look => PaintShape(glow, _styles[look]));
                AddLight(node, nodeLights, glow);
            }

            labels.Add(MakeLabel(letter, LabelCenter(p, spec.Label, half), node.LabelBrush!, theme));
        }

        BuildRoot(theme, nodeShadows, roots);

        foreach (var layer in new[] { backdrop, edgeShadows, edges, nodeShadows, spills, edgeLights, roots, nodeBases, decorations, nodeLights, labels })
            foreach (var e in layer) Children.Add(e);
    }

    /// <param name="code">Elements entered so far.</param>
    /// <param name="pending">While the key is held: the element releasing now would add.</param>
    /// <param name="invalid">The code no longer matches any character.</param>
    public void Render(string code, char? pending, bool keyDown, bool invalid)
    {
        string? pendingCode = pending is char p && !invalid ? code + p : null;

        foreach (var node in _nodes)
        {
            Look look;
            if (code.StartsWith(node.Code, StringComparison.Ordinal))
                look = invalid ? Look.Error : node.Code.Length == code.Length ? Look.Current : Look.Path;
            else
                look = node.Code == pendingCode ? Look.Pending : Look.Idle;
            Apply(node, look);
        }

        Apply(_root, invalid ? Look.Error : keyDown ? Look.Current : code.Length > 0 ? Look.Path : Look.Idle);
    }

    /// <summary>Briefly flashes the path of a finished letter, green if it decoded, red if not.</summary>
    public void Flash(string code, bool success)
    {
        var look = success ? Look.Success : Look.Error;
        foreach (var node in _nodes.Where(n => code.StartsWith(n.Code, StringComparison.Ordinal)).Append(_root))
        {
            foreach (var paint in node.Painters) paint(look);
            // Local values hold the end state; the animations run on top and then let go.
            foreach (var light in node.Lights)
            {
                light.Opacity = 0;
                light.BeginAnimation(OpacityProperty, Fade());
            }
            if (node.LabelBrush is { } label)
            {
                label.Color = _theme.LabelIdle;
                label.BeginAnimation(SolidColorBrush.ColorProperty,
                    new ColorAnimation(_styles[look].Spec.Color, _theme.LabelIdle, FlashDuration) { FillBehavior = FillBehavior.Stop });
            }
        }
    }

    private void Apply(Node node, Look look)
    {
        if (look == Look.Idle)
        {
            // Leaves any running flash animation alone; it fades to these same values.
            foreach (var light in node.Lights) light.Opacity = 0;
            if (node.LabelBrush is { } idleLabel) idleLabel.Color = _theme.LabelIdle;
            return;
        }

        var spec = _styles[look].Spec;
        foreach (var paint in node.Painters) paint(look);
        foreach (var light in node.Lights)
        {
            light.BeginAnimation(OpacityProperty, null);
            light.Opacity = spec.Opacity;
        }
        if (node.LabelBrush is { } label)
        {
            label.BeginAnimation(SolidColorBrush.ColorProperty, null);
            label.Color = spec.Color;
        }
    }

    private void BuildRoot(Theme theme, List<UIElement> shadows, List<UIElement> roots)
    {
        _root = new Node("", null);

        if (_assets?.Get("root_shadow") is { } shadow)
            shadows.Add(PlaceImage(shadow, RootArtCenter + _assets.OffsetOf("root_shadow")));

        if (_assets?.Get("root_off") is { } offArt)
        {
            roots.Add(PlaceImage(offArt, RootArtCenter + _assets.OffsetOf("root_off")));
        }
        else
        {
            var geometry = Geometry.Parse(theme.RootGeometry);
            geometry.Freeze();
            var rootBase = MakeRootPath(geometry, theme.RootStrokeThickness);
            rootBase.Stroke = theme.RootStroke;
            rootBase.Fill = theme.RootFill;
            rootBase.Effect = theme.RootEffect;
            roots.Add(rootBase);
        }

        if (_assets?.Get("root_on") is { } onArt)
        {
            var image = PlaceImage(onArt, RootArtCenter + _assets.OffsetOf("root_on"));
            _root.Painters.Add(look => image.Source = LitArt("root_on", onArt, look));
            AddLight(_root, roots, image);
        }
        else
        {
            var geometry = Geometry.Parse(theme.RootGeometry);
            geometry.Freeze();
            var glow = MakeRootPath(geometry, Math.Max(3, theme.RootStrokeThickness));
            _root.Painters.Add(look => PaintShape(glow, _styles[look]));
            AddLight(_root, roots, glow);
        }
    }

    private static void AddLight(Node node, List<UIElement> layer, UIElement light)
    {
        light.Opacity = 0;
        node.Lights.Add(light);
        layer.Add(light);
    }

    /// <summary>A part's image; vertical parts fall back to the horizontal art turned 90°.</summary>
    private BitmapSource? Part(string name, string horizontalName, bool vertical) =>
        _assets is null ? null
        : vertical ? _assets.GetVertical(name, horizontalName)
        : _assets.Get(name);

    private BitmapSource LitArt(string name, BitmapSource grayscale, Look look) =>
        _assets!.Lit(name, grayscale, look.ToString().ToLowerInvariant(), _styles[look].Spec.Color);

    private void PaintShape(Shape shape, LookStyle style)
    {
        shape.Fill = shape is Path && !_theme.RootLightFills ? null : style.Fill;
        shape.Stroke = style.Stroke;
        shape.Effect = style.Glow;
    }

    /// <summary>An image at its design size, centered on <paramref name="at"/> (or with its top-left there).</summary>
    private Image PlaceImage(BitmapSource source, Point at, bool centered = true)
    {
        var size = _assets!.SizeOf(source);
        var image = new Image { Source = source, Width = size.Width, Height = size.Height, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        SetLeft(image, centered ? at.X - size.Width / 2 : at.X);
        SetTop(image, centered ? at.Y - size.Height / 2 : at.Y);
        return image;
    }

    /// <summary>
    /// A straight edge filled with a repeating tile: tiles of edge_h run along x, tiles of edge_v along y,
    /// and the tile's other dimension is the edge's thickness.
    /// </summary>
    private Rectangle EdgeImage(Point from, Point to, BitmapSource tile, Vector offset)
    {
        var size = _assets!.SizeOf(tile);
        var brush = new ImageBrush(tile)
        {
            TileMode = TileMode.Tile,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, size.Width, size.Height),
            Stretch = Stretch.Fill,
        };
        var rect = new Rectangle { Fill = brush };
        RenderOptions.SetBitmapScalingMode(rect, BitmapScalingMode.HighQuality);

        bool horizontal = Math.Abs(from.Y - to.Y) < 0.5;
        if (horizontal)
        {
            rect.Width = Math.Abs(to.X - from.X);
            rect.Height = size.Height;
            SetLeft(rect, Math.Min(from.X, to.X) + offset.X);
            SetTop(rect, from.Y - size.Height / 2 + offset.Y);
        }
        else
        {
            rect.Width = size.Width;
            rect.Height = Math.Abs(to.Y - from.Y);
            SetLeft(rect, from.X - size.Width / 2 + offset.X);
            SetTop(rect, Math.Min(from.Y, to.Y) + offset.Y);
        }
        return rect;
    }

    private static DoubleAnimation Fade() => new(1, 0, FlashDuration)
    {
        FillBehavior = FillBehavior.Stop,
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
    };

    private static Shape MakeShape(Point center, bool dah, Size half, double corner)
    {
        Shape shape = dah ? new Rectangle { RadiusX = corner, RadiusY = corner } : new Ellipse();
        shape.Width = half.Width * 2;
        shape.Height = half.Height * 2;
        SetLeft(shape, center.X - half.Width);
        SetTop(shape, center.Y - half.Height);
        return shape;
    }

    private static Line MakeLine(Point from, Point to, Brush? stroke, double thickness) => new()
    {
        X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
        Stroke = stroke,
        StrokeThickness = thickness,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };

    private static Path MakeRootPath(Geometry geometry, double thickness) => new()
    {
        Data = geometry,
        StrokeThickness = thickness,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };

    /// <summary>"Morse Keyboard" and the version, engraved in the middle of a blank nameplate.</summary>
    private static UIElement Nameplate(Rect area, Engraving engraving, Theme theme)
    {
        var version = typeof(MorseTreeCanvas).Assembly.GetName().Version ?? new Version(0, 0, 0);
        var family = theme.NameplateFont ?? new FontFamily("Georgia");
        var fill = Palette.Solid(theme.NameplateFill);

        // The name gets about two thirds of the plate's width (clear of rivets and border), the version a smaller line.
        const string name = "Morse Keyboard";
        var probe = new FormattedText(name, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 10, Brushes.Black, 1.0);
        double size = Math.Min(area.Height * 0.3, 10 * area.Width * 0.6 / probe.Width);

        var lines = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new EngravedText(name, family, size, FontWeights.Bold, FontStyles.Normal, fill, engraving)
                    { HorizontalAlignment = HorizontalAlignment.Center },
                new EngravedText($"ver. {version.Major}.{version.Minor}.{Math.Max(0, version.Build)}", family, size * 0.7,
                        FontWeights.Bold, FontStyles.Normal, fill, engraving with { Depth = engraving.Depth * 0.8 })
                    { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -size * 0.12, 0, 0) },
            },
        };
        var box = new Grid { Width = area.Width, Height = area.Height, Children = { lines } };
        SetLeft(box, area.X);
        SetTop(box, area.Y);
        return box;
    }

    private static UIElement MakeLabel(char letter, Point center, Brush foreground, Theme theme)
    {
        if (theme.LabelEngraving is { } engraving)
        {
            var engraved = new EngravedText(letter.ToString(), theme.LabelFont, theme.LabelSize, theme.LabelWeight,
                theme.LabelStyle, foreground, engraving)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var cell = new Grid { Width = 44, Height = 40, Children = { engraved }, Effect = theme.LabelEffect };
            SetLeft(cell, center.X - 22);
            SetTop(cell, center.Y - 20);
            return cell;
        }

        var text = new TextBlock
        {
            Text = letter.ToString(),
            FontFamily = theme.LabelFont,
            FontSize = theme.LabelSize,
            FontWeight = theme.LabelWeight,
            FontStyle = theme.LabelStyle,
            Foreground = foreground,
            Effect = theme.LabelEffect,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var box = new Grid { Width = 44, Height = 40, Children = { text } };
        SetLeft(box, center.X - 22);
        SetTop(box, center.Y - 20);
        return box;
    }

    private static Point LabelCenter(Point p, Side side, Size half)
    {
        const double gap = 19;
        return side switch
        {
            Side.Top => new Point(p.X, p.Y - half.Height - gap),
            Side.Bottom => new Point(p.X, p.Y + half.Height + gap),
            Side.Left => new Point(p.X - half.Width - gap, p.Y),
            _ => new Point(p.X + half.Width + gap, p.Y),
        };
    }
}
