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
    public static readonly Color BgWindow        = Color.FromArgb(16, 17, 22);
    public static readonly Color BgHeader        = Color.FromArgb(12, 13, 17);
    public static readonly Color BgCard          = Color.FromArgb(22, 24, 30);
    public static readonly Color BgCardHover     = Color.FromArgb(28, 31, 39);
    public static readonly Color BgField         = Color.FromArgb(13, 14, 18);
    public static readonly Color BgFieldHover    = Color.FromArgb(20, 22, 28);
    
    // Borders
    public static readonly Color BorderSubtle    = Color.FromArgb(38, 41, 52);
    public static readonly Color BorderHover     = Color.FromArgb(58, 64, 80);
    public static readonly Color BorderFocus     = Color.FromArgb(0, 164, 240);
    public static readonly Color BorderCard      = Color.FromArgb(32, 35, 44);

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
/// A modern Fluent rounded TextBox with custom borders, smooth hover/focus transitions,
/// inner padding, and placeholder text support.
/// </summary>
public class FluentTextBox : Control
{
    private readonly TextBox innerTextBox;
    private bool isHovered = false;
    private bool isFocused = false;
    private int cornerRadius = 6;

    public int CornerRadius
    {
        get => cornerRadius;
        set { cornerRadius = value; Invalidate(); }
    }

    [System.ComponentModel.Browsable(true)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => innerTextBox.Text ?? "";
        set => innerTextBox.Text = value ?? "";
    }

    public string PlaceholderText
    {
        get => innerTextBox.PlaceholderText;
        set => innerTextBox.PlaceholderText = value;
    }

    public bool ReadOnly
    {
        get => innerTextBox.ReadOnly;
        set => innerTextBox.ReadOnly = value;
    }

    public new Font Font
    {
        get => innerTextBox.Font;
        set { base.Font = value; innerTextBox.Font = value; PositionInnerTextBox(); }
    }

    public TextBox InnerTextBox => innerTextBox;

    public FluentTextBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

        innerTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            Font = Theme.FontBody
        };

        innerTextBox.MouseEnter += (_, _) => { isHovered = true; Invalidate(); };
        innerTextBox.MouseLeave += (_, _) => { isHovered = false; Invalidate(); };
        innerTextBox.GotFocus += (_, _) => { isFocused = true; Invalidate(); };
        innerTextBox.LostFocus += (_, _) => { isFocused = false; Invalidate(); };
        innerTextBox.TextChanged += (_, e) => OnTextChanged(e);

        Controls.Add(innerTextBox);

        Size = new Size(200, 32);
        BackColor = Theme.BgField;
        PositionInnerTextBox();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionInnerTextBox();
    }

    protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

    private void PositionInnerTextBox()
    {
        if (innerTextBox == null) return;
        int padH = 10;
        int txtH = innerTextBox.PreferredHeight;
        int padV = Math.Max(2, (Height - txtH) / 2);
        innerTextBox.Location = new Point(padH, padV);
        innerTextBox.Width = Math.Max(10, Width - (padH * 2));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.CreateRoundedPath(rect, cornerRadius);

        Color bg = !Enabled ? Color.FromArgb(20, 21, 26) :
                   isFocused ? Theme.BgField :
                   isHovered ? Theme.BgFieldHover : Theme.BgField;

        innerTextBox.BackColor = bg;

        using (var brush = new SolidBrush(bg))
            e.Graphics.FillPath(brush, path);

        Color border;
        int borderWidth = 1;
        if (!Enabled)
        {
            border = Color.FromArgb(30, 32, 40);
        }
        else if (isFocused)
        {
            border = Theme.BorderFocus;
            borderWidth = 1;
        }
        else if (isHovered)
        {
            border = Theme.BorderHover;
        }
        else
        {
            border = Theme.BorderSubtle;
        }

        using (var pen = new Pen(border, borderWidth))
            e.Graphics.DrawPath(pen, path);
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
        UseMnemonic = false;
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
            baseBg = Color.FromArgb(20, 21, 26);
            baseBorder = Color.FromArgb(32, 34, 42);
            textColor = Theme.TextMuted;
        }
        else if (IsPrimary)
        {
            baseBg = isPressed ? Color.FromArgb(0, 135, 200) :
                     isHovered ? Color.FromArgb(0, 190, 255) : Color.FromArgb(0, 164, 235);
            baseBorder = isHovered ? Color.FromArgb(100, 215, 255) : baseBg;
            textColor = Color.FromArgb(8, 12, 20);
        }
        else if (IsDanger)
        {
            baseBg = isPressed ? Color.FromArgb(50, 16, 24) :
                     isHovered ? Color.FromArgb(42, 18, 26) : Color.FromArgb(28, 16, 22);
            baseBorder = isHovered ? Theme.AccentRose : Color.FromArgb(68, 28, 38);
            textColor = isHovered ? Color.FromArgb(255, 160, 175) : Color.FromArgb(240, 120, 135);
        }
        else if (CustomColor != Color.Empty)
        {
            baseBg = isPressed ? Color.FromArgb(38, 42, 54) :
                     isHovered ? Color.FromArgb(32, 36, 48) : Color.FromArgb(24, 27, 36);
            baseBorder = isHovered ? CustomColor : Theme.BorderSubtle;
            textColor = isHovered ? Color.White : Theme.TextPrimary;
        }
        else
        {
            baseBg = isPressed ? Color.FromArgb(34, 38, 48) :
                     isHovered ? Color.FromArgb(30, 33, 42) : Color.FromArgb(22, 24, 32);
            baseBorder = isHovered ? Color.FromArgb(64, 70, 88) : Theme.BorderSubtle;
            textColor = isHovered ? Color.White : Theme.TextPrimary;
        }

        // Fill background
        using (var brush = new SolidBrush(baseBg))
            e.Graphics.FillPath(brush, path);

        // Draw border
        using (var pen = new Pen(baseBorder, 1))
            e.Graphics.DrawPath(pen, path);

        // Draw text
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

        using (var trackBrush = new SolidBrush(Color.FromArgb(22, 24, 30)))
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
