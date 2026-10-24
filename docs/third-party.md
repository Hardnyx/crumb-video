# Third-party components

Crumb's original material is MIT licensed. That license does not replace the licenses of its dependencies or media tools.

## Application dependencies

The desktop application uses Avalonia 12.1.4, its Fluent theme and Inter font package, and their resolved runtime dependencies, including SkiaSharp. The publisher copies available license/notice files and NuGet package metadata from the actual dependency graph into `licenses/nuget`. NuGet package versions are pinned in the project files; test-only packages are not distributed with the app.

## Media tools

The bundled Windows media executables are compiled from:

| Component | Revision | Role |
| --- | --- | --- |
| FFmpeg 9.0.2 | `946fcce07b6dcd0331c8cc609192aeff5e1924f8` | ffmpeg and ffprobe executables |
| x264 | `31e19f92f00c7003fa115047ce50978bc98c3a0d` | H.264 encoding |

FFmpeg is built with GPL components and version 3 enabled. The resulting media tools are distributed under GPLv3, with the applicable texts in `licenses/media`. Crumb launches them as separate processes; its own code remains under MIT.

The exact, unmodified source archives for FFmpeg and x264 are included under `sources`, with the build script, pinned hashes, and reproduction instructions. Build configuration and compiler output are included under `build-logs`. External media libraries are not auto-detected or bundled by the media build.

References:

- [FFmpeg license and legal considerations](https://ffmpeg.org/legal.html)
- [FFmpeg source](https://github.com/FFmpeg/FFmpeg)
- [x264 source mirror](https://github.com/mirror/x264)
- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
