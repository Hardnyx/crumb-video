# Architecture

This document records the implementation and remaining release-validation work for the first preview.

## Desktop application

The application uses C# / .NET 10 with Avalonia 12.1.4 and a small MVVM layer. `Crumb.Desktop` contains the interface and `Crumb.Core` handles media inspection and compression independently. `Crumb.Tests` exercises the core with generated real media and renders the desktop window with Avalonia's headless backend.

Bindings are compiled, and media JSON is parsed without reflection-based serialization. The Windows pipeline publishes Native AOT and verifies the resulting executable. A self-contained fallback is available through an explicit publishing option.

The first window should contain video selection, source details, an editable size limit, an output location, a compress button, progress, and cancellation. Advanced encoder controls can wait. Do not make users configure FFmpeg paths in the released application.

## Media tools and execution

Bundle platform-specific FFmpeg and ffprobe executables with the portable release. Resolve them relative to the application directory, independently of the current working directory. Development builds may use an explicitly configured local tool directory.

Start processes directly with `ProcessStartInfo.ArgumentList` and `UseShellExecute = false`. Pass paths as arguments, never through a shell command. Drain standard output and standard error concurrently to avoid blocked pipes. Bound captured error logs and present a concise error in the UI.

Run one compression job at a time. Process completion is awaited asynchronously, with progress updates throttled to approximately eight per second. The complete video is not loaded into application memory. Normal encoding uses at most eight threads and leaves one logical CPU available when possible; reduced CPU mode halves that cap. These are initial policies, not published performance measurements. GPU encoding remains a later iteration.

## Inspection and size budget

1. Use ffprobe JSON output to read duration and stream information. Reject unreadable media, missing video streams, and unknown or nonpositive durations with a clear message.
2. Convert the user's decimal MB limit into an integer byte limit. Validate numeric bounds before calculating bitrates.
3. Reserve an initial 3% of the limit for container overhead and bitrate variation. This is a starting policy to calibrate with real files, not a guarantee.
4. Allocate audio bitrate only if an audio stream is selected. Start with one video stream and the default audio stream, falling back to the first usable audio stream. Exclude cover art from video selection.
5. Calculate `video_bps = floor(8 * usable_bytes / duration_seconds) - audio_bps`. The audio allowance must account for the selected audio stream's duration if it exceeds the video duration. Reject an unusable budget rather than silently removing audio or truncating content.

For example, 25 MB with a 3% reserve, a 60-second duration, and 96,000 bit/s audio yields approximately 3,137,333 bit/s for video. The actual MP4 must still be measured.

If the input already fits, offer to keep it as-is. Do not re-encode just to enlarge a file toward the limit.

## Encoding and validation

Use H.264 video, AAC audio when present, and an MP4 container for the initial compatibility path. Begin with FFmpeg's two-pass bitrate mode and a unique pass-log location per job. Keep video settings identical between the two passes. Parse machine-readable progress from `-progress`, not localized status text.

Write into a job-specific temporary file in the destination directory. Preserve the source file and require a different output path. Do not silently overwrite an existing destination. Avoid using `-fs` as a size guarantee: success must preserve the intended media duration.

After encoding, check the process exit code, output byte size, readable video stream, and duration within a documented tolerance. Only report success if the file fits the byte limit and passes media validation. When an otherwise valid result is oversized, lower the video bitrate using the observed size and retry at most twice. Rerun both passes when settings change. If it still does not fit, report failure and suggest a smaller resolution or a less restrictive limit.

Keep a validated result temporary until it can be moved to the requested final path without overwriting another file. On cancellation or failure, terminate the active process tree, await exit, and remove the job's own temporary files and pass logs. Never clean unrelated files from a shared directory.

## Distribution

Windows x64 is the first target. The portable ZIP includes the application, media executables, corresponding media sources, notices, and necessary native dependencies. It still needs manual verification on a clean supported Windows machine without developer tools or a separate .NET/FFmpeg installation. A portable folder contains several files.

An optional Inno Setup installer wraps the same payload for per-user shortcuts and uninstall support. Linux and macOS builds require their own dependency and packaging checks; choosing Avalonia alone does not establish support for those platforms.

## References

- [Avalonia Native AOT deployment](https://docs.avaloniaui.net/docs/deployment/native-aot)
- [FFmpeg options, two-pass encoding, and progress](https://ffmpeg.org/ffmpeg.html)
