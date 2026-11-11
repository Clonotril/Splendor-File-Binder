# Splendor File Binder

A Windows file-binding utility that combines multiple files into a single self-extracting executable (.exe).
When the bound executable is launched, it extracts and runs all embedded files automatically.

---

## Features

- **Bind any files** — Combine documents, images, videos, executables, or any file type into a single `.exe`
- **Custom icon** — Set a custom `.ico` for the output executable
- **Assembly info injection** — Set product name, description, company, version, copyright, etc.
- **Clone from EXE** — One-click clone of assembly info from any existing `.exe`
- **Extract location** — Choose where files are extracted: `%TEMP%`, `%APPDATA%`, current directory, or `%PROGRAMDATA%`
- **Windows startup** — Mark eligible files (`.exe`, `.com`, `.scr`, `.bat`, `.cmd`, `.vbs`, `.js`, `.wsf`, `.ps1`, `.lnk`) to be copied to the Windows Startup folder
- **Run as Admin** — Mark individual files (`.exe`, `.com`, `.scr`, `.msi`, `.bat`, `.cmd`, `.vbs`, `.js`, `.wsf`, `.ps1`, `.reg`, `.cpl`) to launch with a UAC elevation prompt
- **Self-contained** — The release build produces standalone executables that require no .NET runtime on the target machine

---

## How It Works

Splendor File Binder uses a **stub + payload** architecture (the same technique used by
self-extracting archives like WinRAR SFX and 7-Zip SFX):

```
┌──────────────────────────────────────────────────────────┐
│  Output .exe Layout                                      │
├──────────────────────────────────────────────────────────┤
│  SplendorFileStub.exe bytes   (the runner program)       │
│  ─────────────────────────────────────────────           │
│  ZIP archive              (all bound files, compressed)  │
│  ─────────────────────────────────────────────           │
│  8 bytes: ZIP offset      (where the ZIP starts)         │
│  8 bytes: magic marker    ("SPLNDBND")                   │
└──────────────────────────────────────────────────────────┘
```

### Binding (creating the exe)
1. The Binder GUI reads the pre-built `SplendorFileStub.exe`
2. If custom icon or assembly info is set, it modifies the stub PE using **rcedit** (with proper .NET single-file overlay handling)
3. It creates a ZIP archive containing all selected files + a `__splendor_manifest.json` with settings
4. It writes: `stub bytes + ZIP bytes + offset + magic` → saves as the output `.exe`

### Running (the bound exe)
1. The stub reads its **own file** from disk
2. It checks the last 8 bytes for the magic marker `"SPLNDBND"`
3. It reads the ZIP offset and extracts files to the configured location
4. It copies any startup-marked files to the Windows Startup folder
5. It opens every extracted file using the system's default application
6. After a short delay, it cleans up temp files (only for temp extractions)

---

## Download

Grab the latest release from the [Releases](../../releases) page — just extract the zip and double-click `SplendorFileBinder.exe`. No installation or .NET runtime needed.

The release contains 3 files:

| File | Description |
|------|-------------|
| `SplendorFileBinder.exe` | The GUI application |
| `SplendorFileStub.exe` | The stub runtime (auto-detected, keep in the same folder) |
| `rcedit.exe` | Resource editor for icon/assembly injection (auto-detected, keep in the same folder) |

---

## Building from Source

### Prerequisites

- **Windows 10/11**
- **.NET 10 SDK** (or later) — [Download](https://dotnet.microsoft.com/download)
- **rcedit.exe** — Place in the `tools/` folder ([Download from GitHub](https://github.com/electron/rcedit/releases))

### Using the build script

```cmd
build.bat              REM Debug build (for development, needs .NET runtime)
build.bat release      REM Release build (standalone EXEs, no runtime needed)
```

The release build publishes everything to `publish\Binder\` — ready to zip and distribute.

### Using the command line

```cmd
REM Publish the Stub (self-contained, trimmed)
dotnet publish src\Stub\Stub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish\Stub

REM Build the Binder GUI (debug, needs .NET runtime)
dotnet build src\Binder\Binder.csproj -c Debug
```

---

## Usage

1. **Launch** `SplendorFileBinder.exe`
2. **Stub Path** — Auto-detected if `SplendorFileStub.exe` is in the same folder. Otherwise, browse for it.
3. **Add Files** — Click `➕ Add` to select files to bind
4. **Startup** — Check the Startup column for eligible files you want added to Windows startup
5. **Run as Admin** — Check the Admin column for eligible files that need elevated privileges
6. **Extract Location** — Choose where files are extracted on the target machine
7. **Assembly Settings** *(optional)* — Click to set custom icon, version info, or clone from an existing exe
8. **Bind** — Click `🔗 BIND`, choose where to save the output `.exe`
9. **Test** — Run the output `.exe` to verify it extracts and opens all bound files

---

## Project Structure

```
Splendor-File-Binder/
├── SplendorFileBinder.slnx        # Solution file
├── build.bat                      # Build script (debug + release)
├── README.md
├── src/
│   ├── Binder/                    # WinForms GUI application
│   │   ├── Binder.csproj
│   │   ├── Program.cs             # Entry point
│   │   ├── MainForm.cs            # GUI layout and event handling
│   │   ├── BinderEngine.cs        # Core binding logic (ZIP + stub assembly)
│   │   ├── IconInjector.cs        # PE resource modification via rcedit
│   │   └── AssemblySettingsForm.cs # Assembly info / icon settings dialog
│   └── Stub/                      # Self-contained runner (embedded in output)
│       ├── Stub.csproj
│       └── Program.cs             # Extraction, startup, and execution logic
└── tools/
    └── rcedit.exe                 # Third-party PE resource editor (not in repo)
```

---

## Technical Details

| Component | Type | Framework | Output |
|-----------|------|-----------|--------|
| Binder GUI | WinForms | .NET 10 (Windows) | `SplendorFileBinder.exe` |
| Stub Runner | Console (self-contained, trimmed) | .NET 10 | `SplendorFileStub.exe` |

### Key Classes

- **`MainForm`** — WinForms UI with file management, drag-and-drop, progress reporting, startup toggles, extract location selector, and stub path auto-detection
- **`BinderEngine`** — Static `Bind()` method that creates the ZIP payload with a JSON manifest and assembles the final output exe
- **`IconInjector`** — Modifies PE resources (icon + version info) using rcedit, with proper handling of .NET single-file executables (splits PE from overlay, modifies, and reassembles with fixed bundle offsets)
- **`AssemblySettingsForm`** — Dialog for editing version info fields, browsing for icons, and cloning assembly info from existing executables
- **`VersionInfoData`** — Data class for version/assembly info with `FromExe()` to clone from any PE file
- **`Stub.Program`** — Reads its own bytes, validates the magic marker, parses the manifest, extracts files, handles startup registration, and opens files with default apps

### Extract Locations

| Option | Path | Auto-cleanup |
|--------|------|-------------|
| Temp *(default)* | `%TEMP%\SplendorFileBinder\<id>` | ✅ Yes |
| AppData | `%APPDATA%\SplendorFileBinder\<id>` | ❌ No |
| Current Directory | `<exe dir>\SplendorFileBinder_<id>` | ❌ No |
| ProgramData | `%PROGRAMDATA%\SplendorFileBinder\<id>` | ❌ No |

---

## License

This project is provided for educational purposes.
