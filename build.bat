@echo off
REM ============================================================================
REM build.bat - Builds both Splendor File Binder projects
REM
REM Usage:  build.bat          (Debug build - for development, needs .NET runtime)
REM         build.bat release  (Release build - standalone EXEs, no runtime needed)
REM ============================================================================

set CONFIG=Debug
if /I "%1"=="release" set CONFIG=Release

echo.
echo  ====================================
echo   Building Splendor File Binder (%CONFIG%)
echo  ====================================
echo.

REM Kill any running instance first so the build can overwrite the exe
taskkill /F /IM SplendorFileBinder.exe >nul 2>&1

REM Publish the Stub as a self-contained single-file exe
echo [1/2] Publishing SplendorFileStub (self-contained single-file)...
dotnet publish src\Stub\Stub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish\Stub
if errorlevel 1 (
    echo ERROR: Stub publish failed!
    pause
    exit /b 1
)

echo.
if /I "%CONFIG%"=="Release" (
    REM Release: Publish the Binder GUI as a self-contained single-file exe
    echo [2/2] Publishing SplendorFileBinder GUI (self-contained single-file)...
    dotnet publish src\Binder\Binder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\Binder
) else (
    REM Debug: Just build normally (faster, needs .NET runtime)
    echo [2/2] Building SplendorFileBinder GUI...
    dotnet build src\Binder\Binder.csproj -c Debug
)
if errorlevel 1 (
    echo ERROR: Binder build/publish failed!
    pause
    exit /b 1
)

if /I "%CONFIG%"=="Release" (
    REM Copy the Stub and rcedit alongside the Binder so it auto-detects them
    echo.
    echo Copying SplendorFileStub.exe and rcedit.exe next to SplendorFileBinder.exe...
    copy /Y publish\Stub\SplendorFileStub.exe publish\Binder\SplendorFileStub.exe >nul
    copy /Y tools\rcedit.exe publish\Binder\rcedit.exe >nul
)

echo.
echo  ====================================
echo   Build complete!
echo  ====================================
echo.
echo  Stub (standalone):  publish\Stub\SplendorFileStub.exe

if /I "%CONFIG%"=="Release" (
    echo  Binder (standalone): publish\Binder\SplendorFileBinder.exe
    echo.
    echo  Both EXEs are fully standalone - no .NET runtime needed.
    echo  Zip the publish folder for GitHub release distribution.
) else (
    echo  Binder GUI:         src\Binder\bin\Debug\net10.0-windows\SplendorFileBinder.exe
    echo.
    echo  To run the Binder:
    echo    dotnet run --project src\Binder\Binder.csproj
)
echo.
pause
