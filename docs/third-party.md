# Third-party components

No third-party binaries are distributed in this repository yet. Crumb's MIT license covers its original material, not the dependencies of a future release.

The planned desktop UI is [Avalonia](https://github.com/AvaloniaUI/Avalonia). Media processing will use [FFmpeg and ffprobe](https://ffmpeg.org/) as separate executables.

For each distributed component, record its exact version, origin, checksum, license, and required notices. Include corresponding source and build information where required by that component's license. Keep these records with the release payload rather than relying on an unpinned download link.

FFmpeg's applicable license depends on its build configuration. A build with GPL components such as libx264 must not be labeled as an LGPL-only build. Before packaging the first release, select a redistributable build, retain its configuration and corresponding source, and include its actual license and notices. Do not distribute a build configured with `--enable-nonfree`.

Reference: [FFmpeg license and legal considerations](https://ffmpeg.org/legal.html).
