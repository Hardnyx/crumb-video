# Building releases

## Development and tests

Use the .NET 10 SDK. `global.json` permits newer .NET 10 feature bands. NuGet dependencies are pinned in the project files.

Set `CRUMB_FFMPEG_DIR` to an existing directory containing ffmpeg and ffprobe with libx264/AAC support, then run:

```text
dotnet build Crumb.sln -c Release
dotnet test tests/Crumb.Tests -c Release
dotnet run --project src/Crumb.Desktop
```

Tests generate their own short clips. They verify silent and portrait videos, Unicode and shell-like file names, byte caps, preservation of input content, output decoding, cancellation of an active process, invalid files, existing destinations, budget math, and UI rendering/bindings. Tests fail if the real tools are missing rather than silently skipping integration coverage.

Set `CRUMB_SCREENSHOT_DIR` to a directory when running tests to save a screenshot of the real Avalonia window rendered in memory. This does not exercise native file dialogs.

## Media tools

The Windows media payload is built on Ubuntu 24.04 with MinGW-w64. Install `build-essential mingw-w64 nasm pkg-config curl xz-utils`, then run:

```bash
bash scripts/build-media.sh "$PWD/artifacts/media"
```

The script downloads exact source commits from the FFmpeg repository and the x264 mirror, verifies pinned SHA-256 values, and builds static Windows executables. It enables libx264 and the internal media components, disables network support and external library auto-detection, and includes the exact source archives, build instructions, configuration log, and license texts in the payload. No unpinned prebuilt FFmpeg package is used.

To update the tools, review upstream changes, update both the commit and archive hash in `scripts/media-versions.env`, and run the full Windows test/build job.

## Windows application

Use Windows x64, the .NET 10 SDK, and the C++/Windows SDK toolchain required by .NET Native AOT. These are developer requirements, not requirements for users of the portable build. From PowerShell 7:

```powershell
./scripts/Publish-Windows.ps1 -MediaDirectory ./artifacts/media
```

The script publishes Native AOT, assembles notices and media tools, runs a compression check through the published `Crumb.exe`, and produces a portable ZIP with a SHA-256 file. It refuses to overwrite an existing package directory. Choose a new output directory or remove your previous generated output before rebuilding.

Use `-SelfContained` to explicitly publish the fallback that bundles the .NET runtime instead of using AOT. It still requires no separate runtime installation by the user. Do not silently substitute the fallback when an AOT build fails.

Use `-Installer` with Inno Setup 6 installed to build a per-user installer from the same payload. The GitHub Actions manual trigger offers an installer option.

## Continuous integration

`.github/workflows/build.yml` runs Linux tests, builds the Windows media payload from source, runs Windows tests against that payload, publishes Native AOT, and checks the resulting executable. Successful builds on `main` publish a preview prerelease with its exact commit as the tag target. Pull requests only build and test; they do not publish prereleases.

The automated native check calls `Crumb.exe --verify-bundle INPUT OUTPUT MAX_BYTES`. This maintenance option uses the same compression core and relative bundled-tool lookup as the GUI. It returns a nonzero exit code on failure.

Before calling a build stable, manually check launch, file dialogs, drag and drop, display scaling, closing during a job, and playback on a clean supported Windows machine. CI and headless rendering do not replace those checks. No startup or memory measurements have been published yet.
