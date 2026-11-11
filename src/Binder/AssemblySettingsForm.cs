using System.Drawing.Drawing2D;

namespace SplendorFileBinder;

/// <summary>
/// Holds all the assembly settings chosen by the user.
/// </summary>
public class AssemblySettings
{
    public bool ChangeAssemblyInfo { get; set; }
    public VersionInfoData VersionInfo { get; set; } = new();
    public bool ChangeIcon { get; set; }
    public string? IconPath { get; set; }
}

public class AssemblySettingsForm : Form
{
    // Assembly info controls
    private readonly CheckBox chkChangeAssembly;
    private readonly TextBox txtProductName;
    private readonly TextBox txtDescription;
    private readonly TextBox txtCompanyName;
    private readonly TextBox txtCopyright;
    private readonly TextBox txtTrademarks;
    private readonly TextBox txtOriginalFilename;
    private readonly TextBox txtProductVersion;
    private readonly TextBox txtFileVersion;
    private readonly FluentCard pnlAssemblyFields;

    // Icon controls
    private readonly CheckBox chkChangeIcon;
    private readonly TextBox txtIconPath;
    private readonly FluentButton btnBrowseIcon;
    private readonly FluentButton btnClearIcon;
    private readonly PictureBox picIconPreview;
    private readonly FluentCard pnlIconFields;

    // Action buttons
    private readonly FluentButton btnCloneFromExe;
    private readonly FluentButton btnOk;
    private readonly FluentButton btnCancel;
    private readonly FluentButton btnClearAll;

    /// <summary>The result settings after the dialog is closed with OK.</summary>
    public AssemblySettings Result { get; private set; } = new();

    public AssemblySettingsForm(AssemblySettings? existing = null)
    {
        // Form Setup
        Text = "Assembly & Metadata Settings";
        ClientSize = new Size(540, 585);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.BgWindow;
        ForeColor = Theme.TextPrimary;
        Font = Theme.FontBody;

        Theme.ApplyDarkTitleBar(Handle);

        int pad = 20;
        int cw = ClientSize.Width - (pad * 2);

        // Header Title
        var lblHeader = new Label
        {
            Text = "ASSEMBLY & ICON SETTINGS",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.AccentCyan,
            Location = new Point(pad, 14),
            AutoSize = true
        };
        Controls.Add(lblHeader);

        // ═══════════════════════════════════════════
        // ASSEMBLY INFORMATION SECTION
        // ═══════════════════════════════════════════

        chkChangeAssembly = new CheckBox
        {
            Text = "  Change Assembly Information",
            Font = Theme.FontBodySemibold,
            ForeColor = Theme.AccentGreen,
            Location = new Point(pad, 42),
            AutoSize = true,
            Checked = existing?.ChangeAssemblyInfo ?? false
        };
        chkChangeAssembly.CheckedChanged += (_, _) => ToggleAssemblyFields();
        Controls.Add(chkChangeAssembly);

        btnCloneFromExe = new FluentButton
        {
            Text = "📂 Clone from EXE",
            Location = new Point(ClientSize.Width - pad - 145, 38),
            Size = new Size(145, 28),
            CornerRadius = 6,
            CustomColor = Theme.AccentCyan
        };
        btnCloneFromExe.Click += BtnCloneFromExe_Click;
        Controls.Add(btnCloneFromExe);

        pnlAssemblyFields = new FluentCard
        {
            Location = new Point(pad, 72),
            Size = new Size(cw, 320),
            CornerRadius = 8
        };
        Controls.Add(pnlAssemblyFields);

        int y = 12;
        int lblW = 125;
        int gap = 12;
        int fieldX = lblW + gap;
        int fieldW = cw - fieldX - 24;
        int rowH = 37;

        txtProductName      = AddField(pnlAssemblyFields, "Product Name",      ref y, lblW, fieldX, fieldW, rowH);
        txtDescription      = AddField(pnlAssemblyFields, "Description",       ref y, lblW, fieldX, fieldW, rowH);
        txtCompanyName      = AddField(pnlAssemblyFields, "Company Name",      ref y, lblW, fieldX, fieldW, rowH);
        txtCopyright        = AddField(pnlAssemblyFields, "Copyright",         ref y, lblW, fieldX, fieldW, rowH);
        txtTrademarks       = AddField(pnlAssemblyFields, "Trademarks",        ref y, lblW, fieldX, fieldW, rowH);
        txtOriginalFilename = AddField(pnlAssemblyFields, "Original Filename", ref y, lblW, fieldX, fieldW, rowH);
        txtProductVersion   = AddField(pnlAssemblyFields, "Product Version",   ref y, lblW, fieldX, fieldW, rowH);
        txtFileVersion      = AddField(pnlAssemblyFields, "File Version",      ref y, lblW, fieldX, fieldW, rowH);

        txtProductVersion.PlaceholderText = "e.g. 1.0.0.0";
        txtFileVersion.PlaceholderText    = "e.g. 1.0.0.0";

        // ═══════════════════════════════════════════
        // ASSEMBLY ICON SECTION
        // ═══════════════════════════════════════════

        chkChangeIcon = new CheckBox
        {
            Text = "  Change Assembly Icon (.ico)",
            Font = Theme.FontBodySemibold,
            ForeColor = Theme.AccentAmber,
            Location = new Point(pad, 404),
            AutoSize = true,
            Checked = existing?.ChangeIcon ?? false
        };
        chkChangeIcon.CheckedChanged += (_, _) => ToggleIconFields();
        Controls.Add(chkChangeIcon);

        pnlIconFields = new FluentCard
        {
            Location = new Point(pad, 432),
            Size = new Size(cw, 72),
            CornerRadius = 8
        };
        Controls.Add(pnlIconFields);

        picIconPreview = new PictureBox
        {
            Location = new Point(14, 12),
            Size = new Size(48, 48),
            BackColor = Theme.BgField,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlIconFields.Controls.Add(picIconPreview);

        txtIconPath = new TextBox
        {
            Location = new Point(72, 22),
            Size = new Size(cw - 72 - 14 - 90 - 8 - 42, 26),
            Font = Theme.FontBody,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Select an .ico file...",
            ReadOnly = true
        };
        pnlIconFields.Controls.Add(txtIconPath);

        btnBrowseIcon = new FluentButton
        {
            Text = "Browse...",
            Location = new Point(cw - 14 - 90 - 8 - 42, 21),
            Size = new Size(90, 28),
            CornerRadius = 6
        };
        btnBrowseIcon.Click += BtnBrowseIcon_Click;
        pnlIconFields.Controls.Add(btnBrowseIcon);

        btnClearIcon = new FluentButton
        {
            Text = "✕",
            Location = new Point(cw - 14 - 42, 21),
            Size = new Size(42, 28),
            CornerRadius = 6,
            IsDanger = true
        };
        btnClearIcon.Click += (_, _) =>
        {
            txtIconPath.Text = "";
            picIconPreview.Image = null;
        };
        pnlIconFields.Controls.Add(btnClearIcon);

        // ═══════════════════════════════════════════
        // BOTTOM BUTTONS
        // ═══════════════════════════════════════════

        btnClearAll = new FluentButton
        {
            Text = "Clear All",
            Location = new Point(pad, 524),
            Size = new Size(110, 36),
            IsDanger = true,
            CornerRadius = 6
        };
        btnClearAll.Click += BtnClearAll_Click;
        Controls.Add(btnClearAll);

        btnCancel = new FluentButton
        {
            Text = "Cancel",
            Location = new Point(ClientSize.Width - pad - 100 - 10 - 110, 524),
            Size = new Size(100, 36),
            CornerRadius = 6,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnCancel);

        btnOk = new FluentButton
        {
            Text = "✔ Apply",
            Location = new Point(ClientSize.Width - pad - 110, 524),
            Size = new Size(110, 36),
            IsPrimary = true,
            CornerRadius = 6
        };
        btnOk.Click += BtnOk_Click;
        Controls.Add(btnOk);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (existing != null)
            LoadSettings(existing);

        ToggleAssemblyFields();
        ToggleIconFields();
    }

    private TextBox AddField(Panel parent, string labelText, ref int y,
        int lblW, int fieldX, int fieldW, int rowH)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Location = new Point(14, y + 4),
            Size = new Size(lblW, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(lbl);

        var txt = new TextBox
        {
            Location = new Point(fieldX, y + 2),
            Size = new Size(fieldW, 26),
            Font = Theme.FontBody,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        parent.Controls.Add(txt);

        y += rowH;
        return txt;
    }

    private void ToggleAssemblyFields()
    {
        bool on = chkChangeAssembly.Checked;
        pnlAssemblyFields.Enabled = on;
        btnCloneFromExe.Enabled = on;

        foreach (Control c in pnlAssemblyFields.Controls)
        {
            if (c is TextBox tb)
                tb.BackColor = on ? Theme.BgField : Color.FromArgb(24, 25, 30);
        }
    }

    private void ToggleIconFields()
    {
        bool on = chkChangeIcon.Checked;
        pnlIconFields.Enabled = on;

        foreach (Control c in pnlIconFields.Controls)
        {
            if (c is TextBox tb)
                tb.BackColor = on ? Theme.BgField : Color.FromArgb(24, 25, 30);
        }
    }

    private void BtnCloneFromExe_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select an executable or DLL to clone metadata from",
            Filter = "Executables (*.exe)|*.exe|Libraries (*.dll)|*.dll|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            bool clonedAnything = false;

            // Clone metadata
            var vi = VersionInfoData.FromExe(dlg.FileName);
            if (vi != null && vi.HasAnyValue)
            {
                txtProductName.Text      = vi.ProductName ?? "";
                txtDescription.Text      = vi.FileDescription ?? "";
                txtCompanyName.Text      = vi.CompanyName ?? "";
                txtCopyright.Text        = vi.LegalCopyright ?? "";
                txtTrademarks.Text       = "";
                txtOriginalFilename.Text = vi.OriginalFilename ?? "";
                txtProductVersion.Text   = vi.ProductVersion ?? "";
                txtFileVersion.Text      = vi.FileVersion ?? "";
                chkChangeAssembly.Checked = true;
                clonedAnything = true;
            }

            // Clone icon (full multi-resolution extraction from PE resources)
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "SplendorFileBinder");
                Directory.CreateDirectory(tempDir);
                string icoPath = Path.Combine(tempDir, $"cloned_{Path.GetFileNameWithoutExtension(dlg.FileName)}.ico");

                if (IconInjector.ExtractIconToFile(dlg.FileName, icoPath))
                {
                    txtIconPath.Text = icoPath;
                    try { using var ico = new Icon(icoPath); picIconPreview.Image = ico.ToBitmap(); } catch { }
                    chkChangeIcon.Checked = true;
                    clonedAnything = true;
                }
            }
            catch
            {
                // Icon extraction failed — continue without it
            }

            if (!clonedAnything)
            {
                MessageBox.Show("No version information or icon found in this file.", "Clone Metadata",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading metadata:\n{ex.Message}", "Clone Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnBrowseIcon_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select an icon for the output executable",
            Filter = "Icon Files (*.ico)|*.ico"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        txtIconPath.Text = dlg.FileName;
        try
        {
            using var ico = new Icon(dlg.FileName);
            picIconPreview.Image = ico.ToBitmap();
        }
        catch
        {
            picIconPreview.Image = null;
        }
        chkChangeIcon.Checked = true;
    }

    private void BtnClearAll_Click(object? sender, EventArgs e)
    {
        txtProductName.Text = "";
        txtDescription.Text = "";
        txtCompanyName.Text = "";
        txtCopyright.Text = "";
        txtTrademarks.Text = "";
        txtOriginalFilename.Text = "";
        txtProductVersion.Text = "";
        txtFileVersion.Text = "";
        txtIconPath.Text = "";
        picIconPreview.Image = null;
        chkChangeAssembly.Checked = false;
        chkChangeIcon.Checked = false;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        Result = new AssemblySettings
        {
            ChangeAssemblyInfo = chkChangeAssembly.Checked,
            VersionInfo = new VersionInfoData
            {
                ProductName      = txtProductName.Text.Trim(),
                FileDescription  = txtDescription.Text.Trim(),
                CompanyName      = txtCompanyName.Text.Trim(),
                LegalCopyright   = txtCopyright.Text.Trim(),
                OriginalFilename = txtOriginalFilename.Text.Trim(),
                InternalName     = "",
                ProductVersion   = txtProductVersion.Text.Trim(),
                FileVersion      = txtFileVersion.Text.Trim()
            },
            ChangeIcon = chkChangeIcon.Checked,
            IconPath = chkChangeIcon.Checked && !string.IsNullOrWhiteSpace(txtIconPath.Text)
                ? txtIconPath.Text.Trim()
                : null
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void LoadSettings(AssemblySettings s)
    {
        chkChangeAssembly.Checked = s.ChangeAssemblyInfo;
        txtProductName.Text      = s.VersionInfo.ProductName ?? "";
        txtDescription.Text      = s.VersionInfo.FileDescription ?? "";
        txtCompanyName.Text      = s.VersionInfo.CompanyName ?? "";
        txtCopyright.Text        = s.VersionInfo.LegalCopyright ?? "";
        txtOriginalFilename.Text = s.VersionInfo.OriginalFilename ?? "";
        txtProductVersion.Text   = s.VersionInfo.ProductVersion ?? "";
        txtFileVersion.Text      = s.VersionInfo.FileVersion ?? "";

        chkChangeIcon.Checked = s.ChangeIcon;
        if (!string.IsNullOrEmpty(s.IconPath))
        {
            txtIconPath.Text = s.IconPath;
            try { using var ico = new Icon(s.IconPath); picIconPreview.Image = ico.ToBitmap(); } catch { }
        }
    }
}
