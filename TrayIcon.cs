using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MorseKeyboard;

/// <summary>Notification-area icon: shows ON/OFF and gives access to the window, settings and exit.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly Icon _onIcon = CreateIcon(Color.FromArgb(255, 177, 59));
    private readonly Icon _offIcon = CreateIcon(Color.FromArgb(118, 122, 130));

    public TrayIcon(Action toggle, Action toggleWindow, Action openSettings, Action exit)
    {
        _toggleItem = new ToolStripMenuItem("モールス入力 ON/OFF (Ctrl+Alt+M)", null, (_, _) => toggle());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripMenuItem("ツリーを表示 / 隠す", null, (_, _) => toggleWindow()));
        menu.Items.Add(new ToolStripMenuItem("設定...", null, (_, _) => openSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("終了", null, (_, _) => exit()));

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = _offIcon,
            Text = "Morse Keyboard",
            Visible = true,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) toggleWindow();
        };
    }

    public void SetEnabled(bool on)
    {
        _notifyIcon.Icon = on ? _onIcon : _offIcon;
        _notifyIcon.Text = on ? "Morse Keyboard: ON" : "Morse Keyboard: OFF";
        _toggleItem.Checked = on;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _onIcon.Dispose();
        _offIcon.Dispose();
    }

    /// <summary>A dot and a dash on a dark rounded square.</summary>
    private static Icon CreateIcon(Color accent)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var background = new SolidBrush(Color.FromArgb(40, 42, 48));
            using var foreground = new SolidBrush(accent);
            FillRounded(g, background, new RectangleF(1, 1, 30, 30), 7);
            g.FillEllipse(foreground, 4.5f, 12, 8, 8);
            FillRounded(g, foreground, new RectangleF(15, 13, 13, 6), 3);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private static void FillRounded(Graphics g, Brush brush, RectangleF r, float radius)
    {
        float d = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
