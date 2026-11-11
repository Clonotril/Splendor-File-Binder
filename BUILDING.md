# Splendor File Binder — Build & Release Instructions

> **Copy-paste this entire message to any AI model to build and release the project from scratch.**

---

## Project Overview

**Splendor File Binder** is a .NET 10 Windows application with two sub-projects:

| Project | Path | What it is |
|---------|------|------------|
| **Binder** (GUI) | `src\Binder\Binder.csproj` | WinForms GUI app that binds files into a single EXE |
| **Stub** | `src\Stub\Stub.csproj` | Self-contained stub EXE that gets embedded into bound files |

The project also depends on `tools\rcedit.exe` (already in the repo) for setting icons/metadata.

---

## Prerequisites

- **OS:** Windows (x64)
- **.NET 10 SDK** is required. The machine may only have the .NET runtime installed (not the SDK).

### How to check if .NET 10 SDK is installed:
```powershell
dotnet --list-sdks
```
If it shows **no SDKs** or no `10.x.x` SDK, install it:

```powershell
# Install .NET 10 SDK (no admin needed — installs to user profile)
& ([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
```

After installing, **every `dotnet` command in this session** needs the PATH set first:
```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
```

---

## Build Commands (Release — for GitHub distribution)

Run these from the project root.

### Step 1: Kill any running instances
```powershell
taskkill /F /IM SplendorFileBinder.exe 2>$null
taskkill /F /IM SplendorFileStub.exe 2>$null
```

### Step 2: Clean previous build (optional but recommended)
```powershell
dotnet clean src\Stub\Stub.csproj -c Release
dotnet clean src\Binder\Binder.csproj -c Release
```

### Step 3: Publish the Stub (self-contained, trimmed, single-file)
```powershell
dotnet publish src\Stub\Stub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish\Stub
```

### Step 4: Publish the Binder GUI (self-contained, single-file)
```powershell
dotnet publish src\Binder\Binder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\Binder
```

### Step 5: Copy Stub, rcedit, and app icon next to Binder
```powershell
Copy-Item "publish\Stub\SplendorFileStub.exe" "publish\Binder\SplendorFileStub.exe" -Force
Copy-Item "tools\rcedit.exe" "publish\Binder\rcedit.exe" -Force
Copy-Item "app.ico" "publish\Binder\app.ico" -Force
```

---

## Release Folder

After building, the **`publish\Binder\`** folder contains everything needed:

| File | Purpose |
|------|---------|
| `SplendorFileBinder.exe` | Main GUI app (standalone, no .NET needed) |
| `SplendorFileStub.exe` | Stub embedded into bound output files |
| `rcedit.exe` | Sets icons/metadata on output EXEs |
| `app.ico` | Application icon used by the GUI and About dialog |

> The `.pdb` and `crash.log` files (if present) are for debugging only — exclude them from the release zip.

---

## Create Release Zip

```powershell
Compress-Archive -Path "publish\Binder\*.exe", "publish\Binder\app.ico" -DestinationPath "SplendorFileBinder-v1.0-win-x64.zip" -Force
```

Upload `SplendorFileBinder-v1.0-win-x64.zip` to GitHub Releases.

---

## Quick Copy-Paste (All-in-One Script)

```powershell
# Set PATH (only needed if SDK was installed via dotnet-install script)
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"

# Kill running instances
taskkill /F /IM SplendorFileBinder.exe 2>$null
taskkill /F /IM SplendorFileStub.exe 2>$null

# Clean
dotnet clean src\Stub\Stub.csproj -c Release
dotnet clean src\Binder\Binder.csproj -c Release

# Publish Stub
dotnet publish src\Stub\Stub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish\Stub

# Publish Binder
dotnet publish src\Binder\Binder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\Binder

# Copy supporting files
Copy-Item "publish\Stub\SplendorFileStub.exe" "publish\Binder\SplendorFileStub.exe" -Force
Copy-Item "tools\rcedit.exe" "publish\Binder\rcedit.exe" -Force
Copy-Item "app.ico" "publish\Binder\app.ico" -Force

# Create release zip (exclude .pdb and logs)
Compress-Archive -Path "publish\Binder\*.exe", "publish\Binder\app.ico" -DestinationPath "SplendorFileBinder-v1.0-win-x64.zip" -Force
```

---

## ⚠️ Do NOT use `build.bat`

The included `build.bat` has a `cmd.exe` parsing bug (parentheses in `if` blocks conflict with output paths). Use the PowerShell commands above instead.
