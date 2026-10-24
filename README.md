# Crumb

A lightweight desktop app for compressing videos to a maximum file size.

Choose a video, enter a limit such as **25 MB**, and save a smaller copy ready to share.

> **Status:** initial project setup. The application has not been implemented yet, and there are no downloadable builds.

## Planned experience

- Open a video or drop it onto the window.
- Enter the maximum output size in MB.
- Compress locally, with progress and cancellation.
- Save an MP4 copy and verify that it fits the chosen limit.
- Keep the original file untouched.

If the requested size would require a substantial quality reduction, Crumb should explain the trade-off before starting. A size limit is not a promise of lossless compression.

## Technical direction

| Component | Choice |
| --- | --- |
| Language | C# |
| Desktop UI | Avalonia |
| Media processing | FFmpeg and ffprobe, run as separate processes |
| Release target | Native AOT, subject to build and compatibility validation |
| First platform | Windows x64 |
| Distribution | Portable ZIP with the required media tools included |
| Initial output | MP4 with H.264 video and AAC audio |

The intended release should work without installing Python, .NET, or FFmpeg separately. The ZIP may contain several files; portable does not mean a single executable.

Encoding speed and resource use will depend mainly on the video, encoder settings, and hardware. Native AOT is a deployment choice for the application; it does not make FFmpeg encode faster.

## How size targeting will work

Crumb will inspect the video duration, reserve space for audio and container overhead, and calculate a video bitrate. Two-pass encoding will be followed by a check of the actual file size. An oversized result must be retried within a fixed limit or reported as a failure, never shown as a successful compression.

The first version will use decimal units: **1 MB = 1,000,000 bytes**. Presets for particular messaging services can be added later; their upload limits should remain editable.

## Project documents

- [Architecture and compression policy](docs/architecture.md)
- [Roadmap and acceptance criteria](docs/roadmap.md)
- [Contributing](CONTRIBUTING.md)
- [Third-party components](docs/third-party.md)

Build instructions will be added alongside the first working application. There is currently no command that builds or runs Crumb.

## License

Crumb's original code and documentation are available under the [MIT license](LICENSE). Third-party software retains its own license; FFmpeg binaries are not included in this repository at this stage.
