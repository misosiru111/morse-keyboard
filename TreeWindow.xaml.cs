using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static MorseKeyboard.NativeMethods;

namespace MorseKeyboard;

/// <summary>Always-on-top overlay that never takes focus: the tree, the digit chips and the status strip.</summary>
public partial class TreeWindow : Window
{
    private const int MaxHistory = 26;

    private readonly AppSettings _settings;
    private readonly StringBuilder _history = new();
    private Theme _theme = Themes.Classic;

    // Tape style: ink for the printing, and the candidate's glow in the glass window.
    private Brush _ink = Brushes.Black;
    private Brush _faintInk = Brushes.Gray;
    private Brush _errorInk = Brushes.DarkRed;
    private readonly DropShadowEffect _candidateGlow = new() { ShadowDepth = 0, BlurRadius = 26, Opacity = 1 };

    // The last finished letter stays in the status strip until the next press.
    private string _lastCode = "";
    private char? _lastLetter;

    private Point? _dragOrigin;
    private double _dragLeft;
    private double _dragTop;

    public TreeWindow(AppSettings settings, ThemeAssets? assets)
    {
        _settings = settings;
        InitializeComponent();
        LoadBounds();
        ApplyTheme(Themes.Get(settings.ThemeId), assets);
        ApplySettings();
    }

    public event Action? ToggleRequested;
    public event Action? SettingsRequested;
    public event Action? HideRequested;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // No activation on click (focus stays in the app being typed into) and no Alt+Tab entry.
        var hwnd = new WindowInteropHelper(this).Handle;
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));
    }

    public void Render(MorseDecoder decoder)
    {
        string code = decoder.Code;
        char? pending = decoder.IsKeyDown ? (decoder.IsDahPending ? '-' : '.') : null;
        bool invalid = !MorseTable.IsValidPrefix(code);

        Tree.Render(code, pending, decoder.IsKeyDown, invalid);
        Digits.Render(code, pending, invalid);

        if (code.Length == 0 && pending is null)
        {
            ShowLast();
            return;
        }

        DrawSymbols(code, pending, Ink(invalid ? _theme.Error : _theme.Path));
        string probe = code + pending;
        if (MorseTable.TryDecode(probe, out char candidate))
            SetCandidate(candidate.ToString(), pending is null ? _theme.Path : _theme.Pending);
        else
            SetCandidate(invalid ? "?" : "", _theme.Error);
    }

    private bool OnTape => _theme.StripStyle == StripStyle.Tape;

    /// <summary>Symbols are printed in ink on the tape, and shown in the state's color on a panel.</summary>
    private Brush Ink(LookSpec look) => !OnTape ? look.Brush : look == _theme.Error ? _errorInk : _ink;

    private void SetCandidate(string text, LookSpec look)
    {
        CandidateText.Text = text;
        CandidateText.Foreground = look.Brush;
        if (OnTape) _candidateGlow.Color = look.Color;
        CandidateText.Effect = OnTape && text.Length > 0 ? _candidateGlow : null;
    }

    public void ShowDecoded(char letter, string code, char output)
    {
        _lastCode = code;
        _lastLetter = letter;
        Tree.Flash(code, success: true);
        Digits.Flash(letter);
        AppendHistory(output);
    }

    public void ShowInvalid(string code)
    {
        _lastCode = code;
        _lastLetter = null;
        Tree.Flash(code, success: false);
    }

    /// <summary>Adds a typed character to the history line (null just redraws it).</summary>
    public void AppendHistory(char? c)
    {
        if (c is char ch)
        {
            _history.Append(ch);
            if (_history.Length > MaxHistory)
                _history.Remove(0, _history.Length - MaxHistory);
        }

        HistoryText.Inlines.Clear();
        if (_history.Length > 1)
            HistoryText.Inlines.Add(new Run(_history.ToString(0, _history.Length - 1)));
        if (_history.Length > 0)
            HistoryText.Inlines.Add(new Run(_history[^1].ToString()) { Foreground = OnTape ? _ink : _theme.HistoryBright });
        HistoryText.Inlines.Add(new Run("▏") { Foreground = OnTape ? _faintInk : _theme.Path.Brush });
    }

    public void SetEnabled(bool on)
    {
        StatusText.Text = on ? "ON" : "OFF";
        StatusDot.Fill = on ? _theme.Success.Brush : Palette.OffBrush;
        ContentRoot.Opacity = on ? 1.0 : 0.4;
    }

    public void ApplySettings()
    {
        Opacity = _settings.WindowOpacity;
        SpeedText.Text = $"{_settings.Wpm} WPM · 電鍵 {_settings.KeyerKey.DisplayName()}";
        PracticeBadge.Visibility = _settings.SendToApp ? Visibility.Collapsed : Visibility.Visible;
    }

    public string ThemeId => _theme.Id;

    /// <summary>Switches every visual to <paramref name="theme"/>; the caller re-renders the decoder state.</summary>
    public void ApplyTheme(Theme theme, ThemeAssets? assets)
    {
        _theme = theme;

        FrameBorder.Background = assets?.Get("background") is { } background
            ? new ImageBrush(background) { Stretch = Stretch.Fill } // painted edges stay visible; wood hides the stretch
            : theme.FrameBackground();
        FrameBorder.BorderBrush = theme.FrameBorder;
        FrameBorder.BorderThickness = new Thickness(theme.FrameBorderThickness);
        FrameBorder.CornerRadius = new CornerRadius(theme.CornerRadius);
        FrameOverlay.Content = assets?.Get("screw") is { } screw
            ? CornerImages(screw, assets)
            : theme.FrameOverlay?.Invoke();

        HeaderLeft.Margin = new Thickness(theme.HeaderInset, 0, 0, 0);
        HeaderRight.Margin = new Thickness(0, 0, theme.HeaderInset - 4, 0);
        ToggleButton.Background = theme.PillBackground;
        ToggleButton.BorderBrush = theme.PillBorder;
        ToggleButton.Foreground = theme.PillText;
        SpeedText.Foreground = theme.HeaderText;
        SettingsButton.Foreground = theme.HeaderText;
        HideButton.Foreground = theme.HeaderText;

        Strip.BorderBrush = theme.StripBorder;
        Strip.CornerRadius = new CornerRadius(theme.StripCorner);
        HistoryText.FontFamily = theme.HistoryFont;
        CandidateText.FontFamily = theme.CandidateFont;
        _ink = Palette.Solid(theme.TapeInk);
        _faintInk = Palette.Solid(Color.FromArgb(0x70, theme.TapeInk.R, theme.TapeInk.G, theme.TapeInk.B));
        _errorInk = Palette.Solid(theme.TapeErrorInk);
        if (theme.StripStyle == StripStyle.Tape)
        {
            Strip.Background = Brushes.Transparent;
            Strip.BorderThickness = new Thickness(0);
            Strip.Padding = new Thickness(0);
            Tape.Background = assets?.Get("tape") is { } tape ? new ImageBrush(tape) { Stretch = Stretch.Fill } : DrawnTape();
            Tape.Padding = new Thickness(26, 16, 20, 16);
            Tape.Margin = new Thickness(0, 0, 14, 0);
            Tape.Effect = new DropShadowEffect { Color = Colors.Black, Direction = 315, ShadowDepth = 4, BlurRadius = 9, Opacity = 0.6 };
            WindowColumn.Width = new GridLength(124);
            WindowArt.Content = assets?.Get("window") is { } window
                ? new Image { Source = window, Stretch = Stretch.Uniform, Effect = Tape.Effect }
                : DrawnWindow();
            HistoryText.Foreground = Palette.Solid(Color.FromArgb(0xC8, theme.TapeInk.R, theme.TapeInk.G, theme.TapeInk.B));
            HistoryText.FontWeight = FontWeights.Bold;
        }
        else
        {
            Strip.Background = theme.StripBackground;
            Strip.BorderThickness = new Thickness(1);
            Strip.Padding = new Thickness(20, 14, 20, 14);
            Tape.Background = null;
            Tape.Padding = new Thickness(0);
            Tape.Margin = new Thickness(0);
            Tape.Effect = null;
            WindowColumn.Width = new GridLength(110);
            WindowArt.Content = null;
            HistoryText.Foreground = theme.HistoryText;
            HistoryText.FontWeight = FontWeights.Normal;
        }

        Tree.ApplyTheme(theme, assets);
        Digits.ApplyTheme(theme, assets);
        AppendHistory(null);
        SetEnabled(StatusText.Text != "OFF");
    }

    /// <summary>Cream paper tape drawn in code, used until tape.png artwork exists.</summary>
    private static Brush DrawnTape() => new LinearGradientBrush(new GradientStopCollection
    {
        new(Color.FromRgb(0xF2, 0xE8, 0xCC), 0),
        new(Color.FromRgb(0xE8, 0xDA, 0xB4), 0.6),
        new(Color.FromRgb(0xD6, 0xC4, 0x98), 1),
    }, 90);

    /// <summary>A brass-framed dark glass window drawn in code, used until window.png artwork exists.</summary>
    private static UIElement DrawnWindow() => new Border
    {
        Width = 112,
        Height = 112,
        CornerRadius = new CornerRadius(16),
        BorderThickness = new Thickness(7),
        BorderBrush = new LinearGradientBrush(Color.FromRgb(0xF4, 0xDB, 0x98), Color.FromRgb(0x7A, 0x56, 0x1C), 45),
        Background = new RadialGradientBrush(Color.FromRgb(0x2E, 0x1E, 0x0E), Color.FromRgb(0x08, 0x05, 0x02))
            { GradientOrigin = new Point(0.4, 0.35) },
        Effect = new DropShadowEffect { Color = Colors.Black, Direction = 315, ShadowDepth = 4, BlurRadius = 9, Opacity = 0.6 },
    };

    /// <summary>One image in each corner of the frame, drawn at its design size (window pixels).</summary>
    private static UIElement CornerImages(BitmapSource image, ThemeAssets assets)
    {
        var size = assets.SizeOf(image);
        var grid = new Grid { IsHitTestVisible = false };
        foreach (var (h, v) in new[]
        {
            (HorizontalAlignment.Left, VerticalAlignment.Top), (HorizontalAlignment.Right, VerticalAlignment.Top),
            (HorizontalAlignment.Left, VerticalAlignment.Bottom), (HorizontalAlignment.Right, VerticalAlignment.Bottom),
        })
        {
            var corner = new Image
            {
                Source = image,
                Width = size.Width,
                Height = size.Height,
                HorizontalAlignment = h,
                VerticalAlignment = v,
                Margin = new Thickness(5),
            };
            RenderOptions.SetBitmapScalingMode(corner, BitmapScalingMode.HighQuality);
            grid.Children.Add(corner);
        }
        return grid;
    }

    public void StoreBounds()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
    }

    private void LoadBounds()
    {
        if (_settings.WindowWidth is double w && _settings.WindowHeight is double h)
        {
            Width = Math.Max(MinWidth, w);
            Height = Math.Max(MinHeight, h);
        }

        if (_settings.WindowLeft is double left && _settings.WindowTop is double top && IsOnScreen(left, top))
        {
            Left = left;
            Top = top;
        }
        else
        {
            var work = SystemParameters.WorkArea;
            Left = work.Right - Width - 24;
            Top = work.Top + 24;
        }
    }

    private static bool IsOnScreen(double left, double top) =>
        left >= SystemParameters.VirtualScreenLeft - 40 &&
        top >= SystemParameters.VirtualScreenTop - 10 &&
        left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80 &&
        top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60;

    private void ShowLast()
    {
        if (_lastCode.Length == 0)
        {
            CodeSymbols.Children.Clear();
            CandidateText.Text = "";
            return;
        }
        bool ok = _lastLetter is not null;
        var look = ok ? _theme.Success : _theme.Error;
        DrawSymbols(_lastCode, null, Ink(look));
        SetCandidate(ok ? _lastLetter.ToString()! : "?", look);
    }

    private void DrawSymbols(string code, char? pending, Brush color)
    {
        CodeSymbols.Children.Clear();
        foreach (char element in code)
            CodeSymbols.Children.Add(Symbol(element, color));
        if (pending is char p)
            CodeSymbols.Children.Add(Symbol(p, OnTape ? _faintInk : _theme.Pending.Brush));
    }

    private static Shape Symbol(char element, Brush fill)
    {
        Shape shape = element == '.'
            ? new Ellipse { Width = 20, Height = 20 }
            : new Rectangle { Width = 50, Height = 20, RadiusX = 10, RadiusY = 10 };
        shape.Fill = fill;
        shape.Margin = new Thickness(0, 0, 10, 0);
        shape.VerticalAlignment = VerticalAlignment.Center;
        return shape;
    }

    // Manual dragging: the system move loop does not track live for WS_EX_NOACTIVATE windows.

    private void Frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragOrigin = CursorInDips();
        _dragLeft = Left;
        _dragTop = Top;
        FrameBorder.CaptureMouse();
        e.Handled = true;
    }

    private void Frame_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragOrigin is not Point origin) return;
        var p = CursorInDips();
        Left = _dragLeft + p.X - origin.X;
        Top = _dragTop + p.Y - origin.Y;
    }

    private void Frame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragOrigin is null) return;
        FrameBorder.ReleaseMouseCapture();
    }

    private void Frame_LostMouseCapture(object sender, MouseEventArgs e) => _dragOrigin = null;

    private Point CursorInDips()
    {
        GetCursorPos(out var pt);
        var fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        return fromDevice.Transform(new Point(pt.X, pt.Y));
    }

    private void Toggle_Click(object sender, RoutedEventArgs e) => ToggleRequested?.Invoke();

    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void Hide_Click(object sender, RoutedEventArgs e) => HideRequested?.Invoke();
}
