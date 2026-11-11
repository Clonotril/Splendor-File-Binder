// ============================================================================
// IconInjector.cs - Modifies PE resources (icon + version info) using rcedit
//
// Properly handles .NET single-file executables by:
//   1. Splitting the exe into PE + overlay (the .NET bundle)
//   2. Running rcedit on just the PE portion (icon + version strings)
//   3. Re-appending the overlay with ALL internal offsets fixed
//   4. Fixing the bundle header offset in the PE marker
// ============================================================================

using System.Diagnostics;
using System.Text;

namespace SplendorFileBinder;

/// <summary>
/// Holds version/assembly info to inject into a PE executable.
/// Use <see cref="FromExe"/> to clone info from an existing exe.
/// </summary>
public class VersionInfoData
{
    public string? FileDescription { get; set; }
    public string? ProductName { get; set; }
    public string? CompanyName { get; set; }
    public string? LegalCopyright { get; set; }
    public string? OriginalFilename { get; set; }
    public string? InternalName { get; set; }
    public string? FileVersion { get; set; }
    public string? ProductVersion { get; set; }

    /// <summary>
    /// Reads version info from an existing executable file.
    /// </summary>
    public static VersionInfoData? FromExe(string exePath)
    {
        if (!File.Exists(exePath)) return null;

        var fvi = FileVersionInfo.GetVersionInfo(exePath);
        return new VersionInfoData
        {
            FileDescription = fvi.FileDescription,
            ProductName = fvi.ProductName,
            CompanyName = fvi.CompanyName,
            LegalCopyright = fvi.LegalCopyright,
            OriginalFilename = fvi.OriginalFilename,
            InternalName = fvi.InternalName,
            FileVersion = fvi.FileVersion,
            ProductVersion = fvi.ProductVersion,
        };
    }

    /// <summary>
    /// Returns a short summary string like "Chrome - Google LLC v120.0"
    /// </summary>
    public string ToSummary()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(FileDescription))
            parts.Add(FileDescription);
        if (!string.IsNullOrWhiteSpace(CompanyName))
            parts.Add(CompanyName);
        if (!string.IsNullOrWhiteSpace(ProductVersion))
            parts.Add($"v{ProductVersion}");
        else if (!string.IsNullOrWhiteSpace(FileVersion))
            parts.Add($"v{FileVersion}");

        return parts.Count > 0 ? string.Join(" — ", parts) : "(no version info)";
    }

    /// <summary>
    /// Returns true if any field has a value.
    /// </summary>
    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(FileDescription) ||
        !string.IsNullOrWhiteSpace(ProductName) ||
        !string.IsNullOrWhiteSpace(CompanyName) ||
        !string.IsNullOrWhiteSpace(LegalCopyright) ||
        !string.IsNullOrWhiteSpace(FileVersion) ||
        !string.IsNullOrWhiteSpace(ProductVersion);
}

public static class IconInjector
{
    // .NET bundle marker signature: SHA-256 of ".net core bundle"
    private static readonly byte[] BundleSignature =
    [
        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
        0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
        0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
    ];

    /// <summary>
    /// Injects an icon into an exe (convenience wrapper).
    /// </summary>
    public static void InjectIcon(string exePath, string iconPath)
        => ModifyResources(exePath, iconPath, null);

    /// <summary>
    /// Modifies PE resources: sets icon and/or version info.
    /// </summary>
    public static void ModifyResources(string exePath, string? iconPath, VersionInfoData? versionInfo)
    {
        if (!string.IsNullOrEmpty(iconPath) && !File.Exists(iconPath))
            throw new FileNotFoundException($"Icon file not found: {iconPath}");

        bool hasIcon = !string.IsNullOrEmpty(iconPath) && File.Exists(iconPath);
        bool hasVersionInfo = versionInfo != null && versionInfo.HasAnyValue;
        if (!hasIcon && !hasVersionInfo) return;

        string rceditPath = FindRcedit();

        // Read the original exe
        byte[] original = File.ReadAllBytes(exePath);
        long overlayStart = FindPEOverlayOffset(original);

        if (overlayStart >= original.Length)
        {
            // No overlay — rcedit can modify directly (not a single-file exe)
            RunRcedit(rceditPath, exePath, iconPath, versionInfo);
            return;
        }

        // --- .NET single-file exe: split, modify PE, recombine ---

        // 1. Save overlay bytes (contains the .NET bundle data + manifest)
        byte[] overlay = new byte[original.Length - overlayStart];
        Array.Copy(original, overlayStart, overlay, 0, overlay.Length);

        // 2. Find the bundle marker in the PE
        int sigPos = FindSignature(original, BundleSignature);
        long originalBundleOffset = -1;

        if (sigPos >= 8)
        {
            originalBundleOffset = BitConverter.ToInt64(original, sigPos - 8);
        }

        // 3. Write PE-only to a temp file
        string tempExe = Path.Combine(Path.GetTempPath(), $"stub_pe_{Guid.NewGuid():N}.exe");
        try
        {
            using (var fs = File.Create(tempExe))
                fs.Write(original, 0, (int)overlayStart);

            // 4. Run rcedit on the PE-only file (icon + version info in one call)
            RunRcedit(rceditPath, tempExe, iconPath, versionInfo);

            // 5. Read the modified PE
            byte[] modifiedPe = File.ReadAllBytes(tempExe);
            long delta = modifiedPe.Length - overlayStart;

            // 6. Fix all bundle offsets
            if (originalBundleOffset >= 0 && delta != 0)
            {
                int modifiedSigPos = FindSignature(modifiedPe, BundleSignature);
                if (modifiedSigPos >= 8)
                {
                    long newOffset = originalBundleOffset + delta;
                    byte[] newOffsetBytes = BitConverter.GetBytes(newOffset);
                    Array.Copy(newOffsetBytes, 0, modifiedPe, modifiedSigPos - 8, 8);
                }

                FixBundleManifestOffsets(overlay, originalBundleOffset, overlayStart, delta);
            }

            // 7. Write final: modified PE + fixed overlay
            using (var fs = File.Create(exePath))
            {
                fs.Write(modifiedPe, 0, modifiedPe.Length);
                fs.Write(overlay, 0, overlay.Length);
            }
        }
        finally
        {
            try { File.Delete(tempExe); } catch { }
        }
    }

    // ----------------------------------------------------------------
    // Bundle manifest offset fixing (for .NET single-file exes)
    // ----------------------------------------------------------------

    /// <summary>
    /// Parses the .NET bundle manifest in the overlay and fixes all absolute
    /// file offsets by adding 'delta' to them.
    /// </summary>
    private static void FixBundleManifestOffsets(byte[] overlay, long bundleHeaderFileOffset, long overlayStart, long delta)
    {
        int headerStart = (int)(bundleHeaderFileOffset - overlayStart);

        if (headerStart < 0 || headerStart >= overlay.Length)
            return;

        using var ms = new MemoryStream(overlay);
        ms.Position = headerStart;
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        uint majorVersion = reader.ReadUInt32();
        uint minorVersion = reader.ReadUInt32();
        int numFiles = reader.ReadInt32();

        // Bundle ID (7-bit encoded length-prefixed string)
        reader.ReadString();

        // Bundle v2+ has DepsJson/RuntimeConfig locations and flags
        if (majorVersion >= 2)
        {
            long depsJsonOffsetPos = ms.Position;
            long depsJsonOffset = reader.ReadInt64();
            long depsJsonSize = reader.ReadInt64();

            long runtimeConfigOffsetPos = ms.Position;
            long runtimeConfigOffset = reader.ReadInt64();
            long runtimeConfigSize = reader.ReadInt64();

            ulong flags = reader.ReadUInt64();

            if (depsJsonOffset > 0 && depsJsonSize > 0)
            {
                byte[] newBytes = BitConverter.GetBytes(depsJsonOffset + delta);
                Array.Copy(newBytes, 0, overlay, (int)depsJsonOffsetPos, 8);
            }

            if (runtimeConfigOffset > 0 && runtimeConfigSize > 0)
            {
                byte[] newBytes = BitConverter.GetBytes(runtimeConfigOffset + delta);
                Array.Copy(newBytes, 0, overlay, (int)runtimeConfigOffsetPos, 8);
            }
        }

        for (int i = 0; i < numFiles; i++)
        {
            long entryOffsetPos = ms.Position;
            long fileOffset = reader.ReadInt64();
            long fileSize = reader.ReadInt64();

            if (majorVersion >= 6)
                reader.ReadInt64(); // compressedSize

            reader.ReadByte(); // fileType
            reader.ReadString(); // relativePath

            if (fileOffset > 0)
            {
                byte[] newBytes = BitConverter.GetBytes(fileOffset + delta);
                Array.Copy(newBytes, 0, overlay, (int)entryOffsetPos, 8);
            }
        }
    }

    // ----------------------------------------------------------------
    // rcedit interaction
    // ----------------------------------------------------------------

    /// <summary>
    /// Runs rcedit with all requested modifications in a single call.
    /// </summary>
    private static void RunRcedit(string rceditPath, string exePath, string? iconPath, VersionInfoData? vi)
    {
        string args = BuildRceditArgs(exePath, iconPath, vi, includeNumericVersions: true);

        int exitCode = ExecuteRcedit(rceditPath, args, out string stderr);

        if (exitCode != 0)
        {
            // Retry without numeric version args — they're the most common failure point
            string fallbackArgs = BuildRceditArgs(exePath, iconPath, vi, includeNumericVersions: false);
            if (fallbackArgs != args)
            {
                exitCode = ExecuteRcedit(rceditPath, fallbackArgs, out stderr);
            }

            if (exitCode != 0)
                throw new InvalidOperationException($"rcedit failed: {stderr}");
        }
    }

    private static string BuildRceditArgs(string exePath, string? iconPath, VersionInfoData? vi, bool includeNumericVersions)
    {
        var args = new StringBuilder();
        args.Append($"\"{exePath}\"");

        if (!string.IsNullOrEmpty(iconPath))
            args.Append($" --set-icon \"{iconPath}\"");

        if (vi != null)
        {
            void AddString(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    args.Append($" --set-version-string \"{key}\" \"{EscapeArg(value)}\"");
            }

            AddString("FileDescription", vi.FileDescription);
            AddString("ProductName", vi.ProductName);
            AddString("CompanyName", vi.CompanyName);
            AddString("LegalCopyright", vi.LegalCopyright);
            AddString("OriginalFilename", vi.OriginalFilename);
            AddString("InternalName", vi.InternalName);

            if (!string.IsNullOrWhiteSpace(vi.FileVersion))
            {
                if (includeNumericVersions)
                {
                    string? fv = SanitizeVersion(vi.FileVersion);
                    if (fv != null)
                        args.Append($" --set-file-version \"{EscapeArg(fv)}\"");
                }
                AddString("FileVersion", vi.FileVersion);
            }
            if (!string.IsNullOrWhiteSpace(vi.ProductVersion))
            {
                if (includeNumericVersions)
                {
                    string? pv = SanitizeVersion(vi.ProductVersion);
                    if (pv != null)
                        args.Append($" --set-product-version \"{EscapeArg(pv)}\"");
                }
                AddString("ProductVersion", vi.ProductVersion);
            }
        }

        return args.ToString();
    }

    private static int ExecuteRcedit(string rceditPath, string arguments, out string stderr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = rceditPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start rcedit.");

        stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Escapes quotes inside an rcedit argument value.</summary>
    private static string EscapeArg(string value) => value.Replace("\"", "\\\"");

    /// <summary>
    /// Extracts a numeric version (x.y.z.w) from a version string.
    /// Returns null if no valid version can be parsed.
    /// </summary>
    private static string? SanitizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;

        // Strip leading 'v' or 'V'
        string v = version.TrimStart();
        if (v.StartsWith('v') || v.StartsWith('V'))
            v = v[1..];

        // Take only the numeric prefix (stop at first non-digit, non-dot)
        int end = 0;
        while (end < v.Length && (char.IsDigit(v[end]) || v[end] == '.'))
            end++;
        v = v[..end].Trim('.');

        if (string.IsNullOrEmpty(v)) return null;

        // Try parsing as System.Version
        if (Version.TryParse(v, out var parsed))
        {
            return $"{parsed.Major}.{Math.Max(0, parsed.Minor)}.{Math.Max(0, parsed.Build)}.{Math.Max(0, parsed.Revision)}";
        }

        return null;
    }

    // ----------------------------------------------------------------
    // PE parsing helpers
    // ----------------------------------------------------------------

    private static long FindPEOverlayOffset(byte[] pe)
    {
        if (pe.Length < 0x40 || pe[0] != 'M' || pe[1] != 'Z')
            return pe.Length;

        int peOff = BitConverter.ToInt32(pe, 0x3C);
        if (peOff + 24 > pe.Length || pe[peOff] != 'P' || pe[peOff + 1] != 'E')
            return pe.Length;

        ushort numSections = BitConverter.ToUInt16(pe, peOff + 6);
        ushort optHeaderSize = BitConverter.ToUInt16(pe, peOff + 20);
        int sectionStart = peOff + 24 + optHeaderSize;

        long maxEnd = 0;
        for (int i = 0; i < numSections; i++)
        {
            int secOff = sectionStart + i * 40;
            if (secOff + 40 > pe.Length) break;

            uint rawSize = BitConverter.ToUInt32(pe, secOff + 16);
            uint rawPtr = BitConverter.ToUInt32(pe, secOff + 20);
            long end = rawPtr + rawSize;
            if (end > maxEnd) maxEnd = end;
        }

        return maxEnd > 0 ? maxEnd : pe.Length;
    }

    private static int FindSignature(byte[] data, byte[] signature)
    {
        for (int i = 0; i <= data.Length - signature.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < signature.Length; j++)
            {
                if (data[i + j] != signature[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    /// <summary>
    /// Finds rcedit.exe in the project tools folder or on PATH.
    /// </summary>
    private static string FindRcedit()
    {
        string[] searchPaths =
        [
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "rcedit.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\..\..\tools\rcedit.exe"),
        ];

        string? appDir = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (appDir != null)
        {
            searchPaths =
            [
                .. searchPaths,
                Path.Combine(appDir, "rcedit.exe"),
                Path.Combine(appDir, "tools", "rcedit.exe"),
                Path.Combine(appDir, @"..\..\..\..\..\..\tools\rcedit.exe"),
            ];
        }

        foreach (string path in searchPaths)
        {
            string full = Path.GetFullPath(path);
            if (File.Exists(full)) return full;
        }

        // Try system PATH
        try
        {
            var psi = new ProcessStartInfo("where", "rcedit")
            { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string result = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                string first = result.Split('\n')[0].Trim();
                if (proc.ExitCode == 0 && File.Exists(first)) return first;
            }
        }
        catch { }

        throw new FileNotFoundException(
            "rcedit.exe not found. Place it in the 'tools' folder next to the project.");
    }
}
