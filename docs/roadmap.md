# Roadmap

## First preview

- [x] C# / .NET 10 solution with Avalonia desktop UI and a separate compression core.
- [x] Video selection, drag and drop, and ffprobe inspection.
- [x] Editable decimal MB limit, resolution options, and reduced CPU mode.
- [x] Two-pass MP4 encoding with a size budget, actual-size validation, and bounded retries.
- [x] Progress, cancellation, temporary-file cleanup, and original-file protection.
- [x] Real-media tests for silent/portrait clips, duration, decoding, size caps, and Unicode paths.
- [x] Headless rendering and binding smoke test for the actual window.
- [x] Windows CI, Native AOT publishing, portable ZIP, and optional installer scripts.
- [x] Media build from pinned sources with corresponding source and notices in the payload.

## Before a stable release

- [ ] Confirm successful end-to-end Windows CI and inspect the downloadable package.
- [ ] Manually verify native file dialogs, drag and drop, playback, display scaling, and closing during a job.
- [ ] Test the portable build on a clean supported Windows installation without a separately installed runtime or media tools.
- [ ] Record startup time, idle memory, peak encoding memory, and reduced-CPU behavior on representative hardware.
- [ ] Test a broader library of real files, including variable frame rate and unusual timestamps.
- [ ] Consider code signing and a stable release process.

## Later

- Quality-aware resolution suggestions and additional UI languages.
- Sequential batch queue.
- GPU encoding after separate size and quality validation.
- HDR handling and multiple-track controls.
- Linux and macOS distribution support.
