using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MorseKeyboard;

/// <summary>
/// How text cut into a surface catches a light from the top-left: the cut's top-left inner wall is in
/// shadow, its bottom-right inner wall and the outer lip below-right of the cut catch the light.
/// Sizes are in the same units as the font size.
/// </summary>
/// <param name="Depth">Width of the shadowed and lit inner walls.</param>
/// <param name="InnerShadow">The top-left inner wall.</param>
/// <param name="InnerLight">The bottom-right inner wall.</param>
/// <param name="Lip">The surface just outside the cut, below-right of it.</param>
/// <param name="Sheen">Optional overlay on the filled cut (e.g. the shine of gilt).</param>
public sealed record Engraving(double Depth, Color InnerShadow, Color InnerLight, Color Lip, Brush? Sheen = null);

/// <summary>
/// One line of text (or any shape) drawn as if engraved and filled. The fill brush is kept by reference,
/// so animating its color (lighting a letter) needs no re-render.
/// </summary>
public sealed class EngravedText : FrameworkElement
{
    private readonly Geometry _letters;
    private readonly Geometry _lip;
    private readonly Geometry _innerShadow;
    private readonly Geometry _innerLight;
    private readonly Brush _fill;
    private readonly Brush? _sheen;
    private readonly Brush _lipBrush;
    private readonly Brush _innerShadowBrush;
    private readonly Brush _innerLightBrush;
    private readonly Size _size;

    public EngravedText(string text, FontFamily family, double size, FontWeight weight, FontStyle style, Brush fill, Engraving engraving)
        : this(Format(text, family, size, weight, style), fill, engraving)
    {
    }

    /// <summary>An engraved shape (e.g. a dot or a dash), laid out by its bounds from the origin.</summary>
    public EngravedText(Geometry shape, Brush fill, Engraving engraving)
        : this((shape, new Size(shape.Bounds.Right, shape.Bounds.Bottom)), fill, engraving)
    {
    }

    private EngravedText((Geometry Shape, Size Size) outline, Brush fill, Engraving engraving)
    {
        _fill = fill;
        _sheen = engraving.Sheen;
        _lipBrush = Palette.Solid(engraving.Lip);
        _innerShadowBrush = Palette.Solid(engraving.InnerShadow);
        _innerLightBrush = Palette.Solid(engraving.InnerLight);
        _size = new Size(outline.Size.Width + engraving.Depth, outline.Size.Height + engraving.Depth);

        _letters = outline.Shape.Clone();
        _letters.Freeze();
        _lip = Combine(Shifted(_letters, engraving.Depth * 0.7), _letters);
        _innerShadow = Combine(_letters, Shifted(_letters, engraving.Depth));
        _innerLight = Combine(_letters, Shifted(_letters, -engraving.Depth * 0.6));
    }

    protected override Size MeasureOverride(Size availableSize) => _size;

    private static (Geometry, Size) Format(string text, FontFamily family, double size, FontWeight weight, FontStyle style)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family, style, weight, FontStretches.Normal), size, Brushes.Black, 1.0);
        return (formatted.BuildGeometry(new Point(0, 0)), new Size(formatted.WidthIncludingTrailingWhitespace, formatted.Height));
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawGeometry(_lipBrush, null, _lip);
        dc.DrawGeometry(_fill, null, _letters);
        if (_sheen is not null) dc.DrawGeometry(_sheen, null, _letters);
        dc.DrawGeometry(_innerLightBrush, null, _innerLight);
        dc.DrawGeometry(_innerShadowBrush, null, _innerShadow);
    }

    private static Geometry Shifted(Geometry geometry, double offset)
    {
        var copy = geometry.Clone();
        copy.Transform = new TranslateTransform(offset, offset);
        copy.Freeze();
        return copy;
    }

    /// <summary>The part of <paramref name="a"/> not covered by <paramref name="b"/>.</summary>
    private static Geometry Combine(Geometry a, Geometry b)
    {
        var result = Geometry.Combine(a, b, GeometryCombineMode.Exclude, null);
        result.Freeze();
        return result;
    }
}
