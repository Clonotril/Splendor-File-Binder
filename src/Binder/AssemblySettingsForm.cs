// ============================================================================
// AssemblySettingsForm.cs - Separate dialog for editing assembly/version info
//
// Allows the user to:
//   - Manually type in version info fields (Product Name, Description, etc.)
//   - Clone all fields from an existing exe with one click
//   - Select a custom icon for the output exe
// ============================================================================

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
    private readonly Panel pnlAssemblyFields;

    // Icon controls
    private readonly CheckBox chkChangeIcon;
    private readonly TextBox txtIconPath;
    private readonly Button btnBrowseIcon;
    private readonly Button btnClearIcon;
    private readonly PictureBox picIconPreview;
    private readonly Panel pnlIconFields;

    // Action buttons
    private readonly Button btnCloneFromExe;
    private readonly Button btnOk;
    private readonly Button btnCancel;
    private readonly Button btnClearAll;

    // Colors
    private static readonly Color BgDark = Color.FromArgb(24, 24, 28);
    private static readonly Color BgPanel = Color.FromArgb(36, 38, 44);
    private static readonly Color BgField = Color.FromArgb(50, 52, 60);
    private static readonly Color TextWhite = Color.FromArgb(230, 230, 235);
    private static readonly Color TextLabel = Color.FromArgb(160, 165, 175);
    private static readonly Color TextPlaceholder = Color.FromArgb(90, 95, 105);
    private static readonly Color AccentBlue = Color.FromArgb(0, 160, 240);
    private static readonly Color AccentGreen = Color.FromArgb(0, 200, 130);
    private static readonly Color AccentOrange = Color.FromArgb(255, 180, 50);

    /// <summary>The result settings after the dialog is closed with OK.</summary>
    public AssemblySettings Result { get; private set; } = new();

    public AssemblySettingsForm(AssemblySettings? existing = null)
    {
        // ---- Form Setup ----
        Text = "Assembly Settings";
        ClientSize = new Size(520, 570);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = BgDark;
        ForeColor = TextWhite;
        Font = new Font("Segoe UI", 9.5f);

        int pad = 24;        // side padding
        int cw = 520 - pad * 2; // content width

        // ═══════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════
        var lblHeader = new Label
        {
            Text = "ASSEMBLY SETTINGS",
            Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = AccentBlue,
            Location = new Point(pad, 16),
            AutoSize = true
        };
        Controls.Add(lblHeader);

        // ═══════════════════════════════════════════
        // ASSEMBLY INFORMATION SECTION
        // ═══════════════════════════════════════════

        // Checkbox + Clone button row
        chkChangeAssembly = new CheckBox
        {
            Text = "  Change Assembly Information",
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = AccentGreen,
            Location = new Point(pad, 52),
            AutoSize = true,
            Checked = existing?.ChangeAssemblyInfo ?? false
        };
        chkChangeAssembly.CheckedChanged += (_, _) => ToggleAssemblyFields();
        Controls.Add(chkChangeAssembly);

        btnCloneFromExe = new Button
        {
            Text = "📂 Clone from EXE",
            Location = new Point(520 - pad - 145, 48),
            Size = new Size(145, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = BgPanel,
            ForeColor = Color.FromArgb(120, 190, 255),
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btnCloneFromExe.FlatAppearance.BorderColor = Color.FromArgb(60, 70, 90);
        btnCloneFromExe.Click += BtnCloneFromExe_Click;
        Controls.Add(btnCloneFromExe);

        // Fields panel with rounded feel (darker bg)
        pnlAssemblyFields = new Panel
        {
            Location = new Point(pad, 84),
            Size = new Size(cw, 310),
            BackColor = BgPanel
        };
        Controls.Add(pnlAssemblyFields);

        // Create all assembly fields — labels LEFT, textboxes RIGHT with good gap
        int y = 14;
        int lblW = 130;
        int gap = 12;
        int fieldX = lblW + gap;
        int fieldW = cw - fieldX - 16;
        int rowH = 36;

        txtProductName     = AddField(pnlAssemblyFields, "Product Name",      ref y, lblW, fieldX, fieldW, rowH);
        txtDescription     = AddField(pnlAssemblyFields, "Description",       ref y, lblW, fieldX, fieldW, rowH);
        txtCompanyName     = AddField(pnlAssemblyFields, "Company Name",      ref y, lblW, fieldX, fieldW, rowH);
        txtCopyright       = AddField(pnlAssemblyFields, "Copyright",         ref y, lblW, fieldX, fieldW, rowH);
        txtTrademarks      = AddField(pnlAssemblyFields, "Trademarks",        ref y, lblW, fieldX, fieldW, rowH);
        txtOriginalFilename= AddField(pnlAssemblyFields, "Original Filename", ref y, lblW, fieldX, fieldW, rowH);
        txtProductVersion  = AddField(pnlAssemblyFields, "Product Version",   ref y, lblW, fieldX, fieldW, rowH);
        txtFileVersion     = AddField(pnlAssemblyFields, "File Version",      ref y, lblW, fieldX, fieldW, rowH);

        txtProductVersion.PlaceholderText = "e.g.  1.0.0.0";
        txtFileVersion.PlaceholderText    = "e.g.  1.0.0.0";

        // ═══════════════════════════════════════════
        // ASSEMBLY ICON SECTION
        // ═══════════════════════════════════════════

        chkChangeIcon = new CheckBox
        {
            Text = "  Change Assembly Icon",
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = AccentOrange,
            Location = new Point(pad, 406),
            AutoSize = true,
            Checked = existing?.ChangeIcon ?? false
        };
        chkChangeIcon.CheckedChanged += (_, _) => ToggleIconFields();
        Controls.Add(chkChangeIcon);

        pnlIconFields = new Panel
        {
            Location = new Point(pad, 436),
            Size = new Size(cw, 68),
            BackColor = BgPanel
        };
        Controls.Add(pnlIconFields);

        // Icon preview
        picIconPreview = new PictureBox
        {
            Location = new Point(14, 10),
            Size = new Size(48, 48),
            BackColor = BgField,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlIconFields.Controls.Add(picIconPreview);

        txtIconPath = new TextBox
        {
            Location = new Point(74, 18),
            Size = new Size(cw - 74 - 16 - 90 - 8 - 44, 28),
            Font = new Font("Segoe UI", 9f),
            BackColor = BgField,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Select a .ico file...",
            ReadOnly = true
        };
        pnlIconFields.Controls.Add(txtIconPath);

        btnBrowseIcon = new Button
        {
            Text = "Browse...",
            Location = new Point(cw - 16 - 90 - 8 - 44, 16),
            Size = new Size(86, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = BgField,
            ForeColor = TextWhite,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btnBrowseIcon.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);
        btnBrowseIcon.Click += BtnBrowseIcon_Click;
        pnlIconFields.Controls.Add(btnBrowseIcon);

        btnClearIcon = new Button
        {
            Text = "✕",
            Location = new Point(cw - 16 - 44, 16),
            Size = new Size(40, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = BgField,
            ForeColor = Color.FromArgb(255, 100, 100),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClearIcon.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);
        btnClearIcon.Click += (_, _) =>
        {
            txtIconPath.Text = "";
            picIconPreview.Image = null;
        };
        pnlIconFields.Controls.Add(btnClearIcon);

        // ═══════════════════════════════════════════
        // BOTTOM BUTTONS
        // ═══════════════════════════════════════════

        // Separator line
        var line = new Panel
        {
            BackColor = Color.FromArgb(50, 55, 65),
            Location = new Point(pad, 516),
            Size = new Size(cw, 1)
        };
        Controls.Add(line);

        btnClearAll = new Button
        {
            Text = "🗑  Clear All",
            Location = new Point(pad, 528),
            Size = new Size(115, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 30, 30),
            ForeColor = Color.FromArgb(255, 120, 120),
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        btnClearAll.FlatAppearance.BorderColor = Color.FromArgb(120, 50, 50);
        btnClearAll.Click += BtnClearAll_Click;
        Controls.Add(btnClearAll);

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(520 - pad - 100 - 10 - 110, 528),
            Size = new Size(100, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 52, 60),
            ForeColor = TextWhite,
            Font = new Font("Segoe UI", 10f),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);
        Controls.Add(btnCancel);

        btnOk = new Button
        {
            Text = "✔  Apply",
            Location = new Point(520 - pad - 110, 528),
            Size = new Size(110, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10.5f),
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderColor = AccentBlue;
        btnOk.Click += BtnOk_Click;
        Controls.Add(btnOk);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // ── Load existing values ──
        if (existing != null)
            LoadSettings(existing);

        ToggleAssemblyFields();
        ToggleIconFields();
    }

    // ----------------------------------------------------------------
    // Field creation helper — label on left, textbox on right
    // ----------------------------------------------------------------

    private TextBox AddField(Panel parent, string labelText, ref int y,
        int lblW, int fieldX, int fieldW, int rowH)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TextLabel,
            Location = new Point(14, y + 5),
            Size = new Size(lblW, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(lbl);

        var txt = new TextBox
        {
            Location = new Point(fieldX, y + 2),
            Size = new Size(fieldW, 28),
            Font = new Font("Segoe UI", 9.5f),
            BackColor = BgField,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle
        };
        parent.Controls.Add(txt);

        y += rowH;
        return txt;
    }

    // ----------------------------------------------------------------
    // Enable / disable sections
    // ----------------------------------------------------------------

    private void ToggleAssemblyFields()
    {
        bool on = chkChangeAssembly.Checked;
        pnlAssemblyFields.Enabled = on;
        btnCloneFromExe.Enabled = on;

        // Dim the panel when disabled
        foreach (Control c in pnlAssemblyFields.Controls)
        {
            if (c is TextBox tb)
                tb.BackColor = on ? BgField : Color.FromArgb(32, 34, 38);
        }
    }

    private void ToggleIconFields()
    {
        bool on = chkChangeIcon.Checked;
        pnlIconFields.Enabled = on;

        foreach (Control c in pnlIconFields.Controls)
        {
            if (c is TextBox tb)
                tb.BackColor = on ? BgField : Color.FromArgb(32, 34, 38);
        }
    }

    // ----------------------------------------------------------------
    // Button handlers
    // ----------------------------------------------------------------

    private void BtnCloneFromExe_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select an exe/dll to clone assembly info from",
            Filter = "Executables (*.exe)|*.exe|Libraries (*.dll)|*.dll|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var vi = VersionInfoData.FromExe(dlg.FileName);
            if (vi == null || !vi.HasAnyValue)
            {
                MessageBox.Show("No version info found in this file.", "Clone",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtProductName.Text      = vi.ProductName ?? "";
            txtDescription.Text      = vi.FileDescription ?? "";
            txtCompanyName.Text      = vi.CompanyName ?? "";
            txtCopyright.Text        = vi.LegalCopyright ?? "";
            txtTrademarks.Text       = "";
            txtOriginalFilename.Text = vi.OriginalFilename ?? "";
            txtProductVersion.Text   = vi.ProductVersion ?? "";
            txtFileVersion.Text      = vi.FileVersion ?? "";

            chkChangeAssembly.Checked = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading file:\n{ex.Message}", "Clone Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnBrowseIcon_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select an icon for the output exe",
            Filter = "Icon Files (*.ico)|*.ico"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        txtIconPath.Text = dlg.FileName;
        try
        {
            picIconPreview.Image = new Icon(dlg.FileName).ToBitmap();
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

    // ----------------------------------------------------------------
    // Load existing settings
    // ----------------------------------------------------------------

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
            try { picIconPreview.Image = new Icon(s.IconPath).ToBitmap(); } catch { }
        }

    }
}
