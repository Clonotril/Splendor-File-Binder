using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SplendorFileBinder;

/// <summary>
/// Modern Fluent Dark theme palette & helper drawing utilities.
/// </summary>
public static class Theme
{
    // Palette - Deep slate & obsidian layers
    public static readonly Color BgWindow        = Color.FromArgb(18, 19, 24);
    public static readonly Color BgHeader        = Color.FromArgb(14, 15, 19);
    public static readonly Color BgCard          = Color.FromArgb(24, 26, 33);
    public static readonly Color BgCardHover     = Color.FromArgb(30, 33, 42);
    public static readonly Color BgField         = Color.FromArgb(15, 16, 21);
    public static readonly Color BgFieldHover    = Color.FromArgb(22, 24, 31);
    
    // Borders
    public static readonly Color BorderSubtle    = Color.FromArgb(40, 43, 54);
    public static readonly Color BorderFocus     = Color.FromArgb(0, 162, 237);
    public static readonly Color BorderCard      = Color.FromArgb(34, 37, 46);

    // Text colors
    public static readonly Color TextPrimary     = Color.FromArgb(242, 245, 250);
    public static readonly Color TextSecondary   = Color.FromArgb(160, 168, 184);
    public static readonly Color TextMuted       = Color.FromArgb(110, 118, 134);

    // Accents
    public static readonly Color AccentCyan      = Color.FromArgb(0, 184, 240);
    public static readonly Color AccentPurple    = Color.FromArgb(138, 92, 246);
    public static readonly Color AccentGreen     = Color.FromArgb(16, 185, 129);
    public static readonly Color AccentAmber     = Color.FromArgb(245, 158, 11);
    public static readonly Color AccentRose      = Color.FromArgb(244, 63, 94);

    // Fonts
    public static readonly Font FontHeaderTitle  = new("Segoe UI", 13.5f, FontStyle.Bold);
    public static readonly Font FontSectionTitle = new("Segoe UI Semibold", 10.5f);
    public static readonly Font FontBody         = new("Segoe UI", 9.25f);
    public static readonly Font FontBodySemibold = new("Segoe UI Semibold", 9.25f);
    public static readonly Font FontMono         = new("Consolas", 9f);
    public static readonly Font FontSmall        = new("Segoe UI", 8.25f);
    public static readonly Font FontBadge        = new("Segoe UI Semibold", 8f);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>
    /// Enables Windows 11 / 10 native immersive dark mode title bar.
    /// </summary>
    public static void ApplyDarkTitleBar(IntPtr handle)
    {
        try
        {
            int darkMode = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }
        catch { }
    }

    /// <summary>
    /// Enables double buffering on any control via reflection to eliminate flickering.
    /// </summary>
    public static void EnableDoubleBuffering(Control control)
    {
        try
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        catch { }
    }

    /// <summary>
    /// Creates a graphics path for a rounded rectangle.
    /// </summary>
    public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// A sleek rounded container card with optional subtle border.
/// </summary>
public class FluentCard : Panel
{
    public int CornerRadius { get; set; } = 8;
    public Color BorderColor { get; set; } = Theme.BorderCard;
    public bool ShowBorder { get; set; } = true;

    public FluentCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.BgCard;
        Padding = new Padding(12);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.CreateRoundedPath(rect, CornerRadius);

        using (var brush = new SolidBrush(BackColor))
            e.Graphics.FillPath(brush, path);

        if (ShowBorder)
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawPath(pen, path);
        }
    }
}

/// <summary>
/// Smooth animated button with Fluent rounded pill styling, gradient highlights, and hover effects.
/// </summary>
public class FluentButton : Button
{
    public bool IsPrimary { get; set; } = false;
    public bool IsDanger { get; set; } = false;
    public int CornerRadius { get; set; } = 6;
    public Color CustomColor { get; set; } = Color.Empty;

    private bool isHovered = false;
    private bool isPressed = false;

    public FluentButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.FontBodySemibold;
        Cursor = Cursors.Hand;
        Size = new Size(100, 32);
        UseMnemonic = false; // Prevent & or _ from turning into mnemonics!
    }

    protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { isPressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { isPressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.CreateRoundedPath(rect, CornerRadius);

        Color baseBg;
        Color baseBorder;
        Color textColor;

        if (!Enabled)
        {
            baseBg = Color.FromArgb(22, 23, 28);
            baseBorder = Color.FromArgb(34, 36, 44);
            textColor = Theme.TextMuted;
        }
        else if (IsPrimary)
        {
            baseBg = isPressed ? Color.FromArgb(0, 140, 205) :
                     isHovered ? Color.FromArgb(0, 195, 255) : Color.FromArgb(0, 168, 235);
            baseBorder = baseBg;
            textColor = Color.FromArgb(10, 15, 25);
        }
        else if (IsDanger)
        {
            baseBg = isPressed ? Color.FromArgb(60, 18, 26) :
                     isHovered ? Color.FromArgb(50, 20, 28) : Color.FromArgb(35, 18, 24);
            baseBorder = isHovered ? Theme.AccentRose : Color.FromArgb(75, 30, 40);
            textColor = Color.FromArgb(255, 130, 145);
        }
        else if (CustomColor != Color.Empty)
        {
            baseBg = isPressed ? Color.FromArgb(42, 45, 56) :
                     isHovered ? Color.FromArgb(34, 38, 48) : Theme.BgCard;
            baseBorder = isHovered ? CustomColor : Theme.BorderSubtle;
            textColor = isHovered ? Color.White : Theme.TextPrimary;
        }
        else
        {
            baseBg = isPressed ? Color.FromArgb(38, 41, 52) :
                     isHovered ? Color.FromArgb(32, 35, 44) : Color.FromArgb(24, 26, 33);
            baseBorder = isHovered ? Color.FromArgb(60, 65, 80) : Theme.BorderSubtle;
            textColor = isHovered ? Color.White : Theme.TextPrimary;
        }

        // Fill background
        using (var brush = new SolidBrush(baseBg))
            e.Graphics.FillPath(brush, path);

        // Draw border
        using (var pen = new Pen(baseBorder, 1))
            e.Graphics.DrawPath(pen, path);

        // Draw text with mnemonic disabled so ampersands / symbols render cleanly
        TextRenderer.DrawText(e.Graphics, Text, Font, rect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// A sleek, rounded animated progress bar.
/// </summary>
public class FluentProgressBar : Control
{
    private int value = 0;
    private int maximum = 100;

    public int Maximum
    {
        get => maximum;
        set { maximum = Math.Max(1, value); Invalidate(); }
    }

    public int Value
    {
        get => value;
        set
        {
            this.value = Math.Clamp(value, 0, maximum);
            Invalidate();
        }
    }

    public Color ProgressColor { get; set; } = Theme.AccentCyan;

    public FluentProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Height = 6;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var bgPath = Theme.CreateRoundedPath(bgRect, Height / 2);

        using (var trackBrush = new SolidBrush(Color.FromArgb(24, 26, 33)))
            e.Graphics.FillPath(trackBrush, bgPath);

        if (value > 0)
        {
            int fillW = (int)((float)value / maximum * (Width - 1));
            fillW = Math.Max(Height, fillW);
            var fillRect = new Rectangle(0, 0, fillW, Height - 1);
            using var fillPath = Theme.CreateRoundedPath(fillRect, Height / 2);

            using var gradBrush = new LinearGradientBrush(fillRect, ProgressColor, Theme.AccentPurple, LinearGradientMode.Horizontal);
            e.Graphics.FillPath(gradBrush, fillPath);
        }
    }
}
