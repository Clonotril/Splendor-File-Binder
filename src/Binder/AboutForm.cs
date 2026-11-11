using System.Diagnostics;
using System.Reflection;

namespace SplendorFileBinder;

/// <summary>
/// About dialog displaying application info, developer links, guide, disclaimer, and MIT license.
/// </summary>
public class AboutForm : Form
{
    private const string RepoUrl = "https://github.com/Clonotril/Splendor-File-Binder";
    private const string GuideUrl = "https://github.com/Clonotril/Splendor-File-Binder#readme";
    private const string DeveloperUrl = "https://github.com/Clonotril";
    private const string ReleasesUrl = "https://github.com/Clonotril/Splendor-File-Binder/releases";
    private const string IssuesUrl = "https://github.com/Clonotril/Splendor-File-Binder/issues";
    private const string LicenseUrl = "https://github.com/Clonotril/Splendor-File-Binder/blob/main/LICENSE";

    private const string MitLicenseText =
        "MIT License\r\n\r\n" +
        "Copyright (c) 2026 Ronxk\r\n\r\n" +
        "Permission is hereby granted, free of charge, to any person obtaining a copy " +
        "of this software and associated documentation files (the \"Software\"), to deal " +
        "in the Software without restriction, including without limitation the rights " +
        "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell " +
        "copies of the Software, and to permit persons to whom the Software is " +
        "furnished to do so, subject to the following conditions:\r\n\r\n" +
        "The above copyright notice and this permission notice shall be included in all " +
        "copies or substantial portions of the Software.\r\n\r\n" +
        "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
        "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
        "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE " +
        "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
        "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
        "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE " +
        "SOFTWARE.";

    private readonly PictureBox picLogo;

    public AboutForm()
    {
        Text = "About Splendor File Binder";
        ClientSize = new Size(520, 570);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.BgWindow;
        ForeColor = Theme.TextPrimary;
        Font = Theme.FontBody;

        Theme.ApplyDarkTitleBar(Handle);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        int pad = 18;
        int cw = ClientSize.Width - (pad * 2);

        // ================================================================
        // 1. APP IDENTITY CARD
        // ================================================================
        var cardApp = new FluentCard
        {
            Location = new Point(pad, 16),
            Size = new Size(cw, 86),
            CornerRadius = 8
        };
        Controls.Add(cardApp);

        picLogo = new PictureBox
        {
            Location = new Point(14, 19),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        if (File.Exists(iconPath))
        {
            try { picLogo.Image = Image.FromFile(iconPath); } catch { }
        }
        cardApp.Controls.Add(picLogo);

        var lblTitle = new Label
        {
            Text = "SPLENDOR FILE BINDER",
            Font = Theme.FontHeaderTitle,
            ForeColor = Theme.TextPrimary,
            Location = new Point(74, 14),
            AutoSize = true
        };
        cardApp.Controls.Add(lblTitle);

        string appVersion = Assembly.GetExecutingAssembly().GetName().Version is { } v
            ? $"v{v.Major}.{v.Minor}" : "v1.0";
        var lblVersion = new Label
        {
            Text = $"{appVersion} • FLUENT EDITION • .NET 10 (win-x64)",
            Font = Theme.FontBadge,
            ForeColor = Theme.AccentCyan,
            Location = new Point(75, 41),
            AutoSize = true
        };
        cardApp.Controls.Add(lblVersion);

        var lblTagline = new Label
        {
            Text = "Self-extracting executable packager for Windows",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location = new Point(75, 58),
            AutoSize = true
        };
        cardApp.Controls.Add(lblTagline);

        // ================================================================
        // 2. DEVELOPER & PROJECT LINKS CARD
        // ================================================================
        var cardInfo = new FluentCard
        {
            Location = new Point(pad, cardApp.Bottom + 12),
            Size = new Size(cw, 156),
            CornerRadius = 8
        };
        Controls.Add(cardInfo);

        var lblInfoHeader = new Label
        {
            Text = "DEVELOPER & DOCUMENTATION",
            Font = Theme.FontBadge,
            ForeColor = Theme.TextMuted,
            Location = new Point(14, 12),
            AutoSize = true
        };
        cardInfo.Controls.Add(lblInfoHeader);

        int labelX = 14;
        int valX = 128;

        AddInfoLabelRow(cardInfo, "Developer", "Ronak Pandey (Ronxk)", labelX, valX, 34, Theme.FontBodySemibold, Theme.TextPrimary);
        AddLinkRow(cardInfo, "GitHub", "@Clonotril", DeveloperUrl, labelX, valX, 58);
        AddLinkRow(cardInfo, "Guide (README)", "github.com/Clonotril/Splendor-File-Binder", GuideUrl, labelX, valX, 82);

        // Releases & Issues on one row
        cardInfo.Controls.Add(new Label
        {
            Text = "Project Links",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location = new Point(labelX, 108),
            Size = new Size(106, 18)
        });

        var flowLinks = new FlowLayoutPanel
        {
            Location = new Point(valX - 3, 105),
            Size = new Size(cw - valX - 10, 22),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var lnkRepo = CreateLinkLabel("Repository", RepoUrl, Point.Empty);
        lnkRepo.Margin = new Padding(0, 1, 4, 0);
        flowLinks.Controls.Add(lnkRepo);

        flowLinks.Controls.Add(new Label
        {
            Text = "•",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 2, 4, 0)
        });

        var lnkReleases = CreateLinkLabel("Releases", ReleasesUrl, Point.Empty);
        lnkReleases.Margin = new Padding(0, 1, 4, 0);
        flowLinks.Controls.Add(lnkReleases);

        flowLinks.Controls.Add(new Label
        {
            Text = "•",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 2, 4, 0)
        });

        var lnkIssues = CreateLinkLabel("Report Issue", IssuesUrl, Point.Empty);
        lnkIssues.Margin = new Padding(0, 1, 0, 0);
        flowLinks.Controls.Add(lnkIssues);

        cardInfo.Controls.Add(flowLinks);

        AddInfoLabelRow(cardInfo, "Powered By", ".NET 10 WinForms & Electron rcedit (PE resources)", labelX, valX, 130, Theme.FontSmall, Theme.TextSecondary);

        // ================================================================
        // 3. WARNING / EDUCATIONAL USE DISCLAIMER
        // ================================================================
        var cardWarning = new FluentCard
        {
            Location = new Point(pad, cardInfo.Bottom + 12),
            Size = new Size(cw, 98),
            CornerRadius = 8,
            BorderColor = Color.FromArgb(110, 75, 22)
        };
        Controls.Add(cardWarning);

        var lblWarnTitle = new Label
        {
            Text = "⚠  WARNING — EDUCATIONAL & AUTHORIZED USE ONLY",
            Font = Theme.FontBadge,
            ForeColor = Theme.AccentAmber,
            Location = new Point(14, 10),
            AutoSize = true
        };
        cardWarning.Controls.Add(lblWarnTitle);

        var lblWarnBody = new Label
        {
            Text = "Splendor File Binder is intended strictly for educational purposes, legitimate software packaging, and authorized testing. Do not use this tool for malicious distribution or unauthorized access. The user assumes full responsibility for all generated executables.",
            Font = Theme.FontSmall,
            ForeColor = Color.FromArgb(230, 200, 145),
            Location = new Point(14, 29),
            Size = new Size(cw - 28, 60)
        };
        cardWarning.Controls.Add(lblWarnBody);

        // ================================================================
        // 4. MIT LICENSE CARD
        // ================================================================
        var cardLicense = new FluentCard
        {
            Location = new Point(pad, cardWarning.Bottom + 12),
            Size = new Size(cw, 118),
            CornerRadius = 8
        };
        Controls.Add(cardLicense);

        var lblLicenseTitle = new Label
        {
            Text = "MIT LICENSE  •  Copyright (c) 2026 Ronxk",
            Font = Theme.FontBadge,
            ForeColor = Theme.TextMuted,
            Location = new Point(14, 11),
            AutoSize = true
        };
        cardLicense.Controls.Add(lblLicenseTitle);

        var lnkLicense = CreateLinkLabel("View LICENSE ↗", LicenseUrl, new Point(cw - 14 - 120, 9));
        lnkLicense.Font = Theme.FontSmall;
        lnkLicense.AutoSize = false;
        lnkLicense.Size = new Size(120, 18);
        lnkLicense.TextAlign = ContentAlignment.TopRight;
        cardLicense.Controls.Add(lnkLicense);

        var txtLicense = new TextBox
        {
            Text = MitLicenseText,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextSecondary,
            Font = Theme.FontSmall,
            Location = new Point(14, 30),
            Size = new Size(cw - 28, 76),
            TabStop = false
        };
        cardLicense.Controls.Add(txtLicense);

        // ================================================================
        // 5. ACTION BUTTONS
        // ================================================================
        int btnY = cardLicense.Bottom + 14;

        var btnOpenGuide = new FluentButton
        {
            Text = "📖 Open Guide",
            Location = new Point(pad, btnY),
            Size = new Size(135, 32),
            CornerRadius = 6
        };
        btnOpenGuide.Click += (_, _) => OpenUrl(GuideUrl);
        Controls.Add(btnOpenGuide);

        var btnClose = new FluentButton
        {
            Text = "Close",
            Location = new Point(ClientSize.Width - pad - 100, btnY),
            Size = new Size(100, 32),
            IsPrimary = true,
            CornerRadius = 6
        };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);

        AcceptButton = btnClose;
        CancelButton = btnClose;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            picLogo.Image?.Dispose();
            picLogo.Image = null;
        }
        base.Dispose(disposing);
    }

    private static void AddInfoLabelRow(Control parent, string caption, string value, int labelX, int valX, int y, Font valFont, Color valColor)
    {
        parent.Controls.Add(new Label
        {
            Text = caption,
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location = new Point(labelX, y + 1),
            Size = new Size(106, 18)
        });
        parent.Controls.Add(new Label
        {
            Text = value,
            Font = valFont,
            ForeColor = valColor,
            Location = new Point(valX, y),
            AutoSize = true
        });
    }

    private static void AddLinkRow(Control parent, string caption, string linkText, string url, int labelX, int valX, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = caption,
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location = new Point(labelX, y + 1),
            Size = new Size(106, 18)
        });
        parent.Controls.Add(CreateLinkLabel(linkText, url, new Point(valX, y)));
    }

    private static LinkLabel CreateLinkLabel(string text, string url, Point location)
    {
        var link = new LinkLabel
        {
            Text = text,
            Font = Theme.FontBody,
            LinkColor = Theme.AccentCyan,
            ActiveLinkColor = Color.White,
            VisitedLinkColor = Theme.AccentCyan,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Location = location,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        link.LinkClicked += (_, _) => OpenUrl(url);
        return link;
    }

    private static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open link:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
