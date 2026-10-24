# Roadmap

## Repository setup

- [x] Define the project name and scope.
- [x] Record the desktop stack and portable distribution plan.
- [x] Add the project license and contribution conventions.
- [x] Specify the compression and output validation policy.

## First working version

- [ ] Create and pin the C# / Avalonia solution and dependencies.
- [ ] Select a local video and inspect it with ffprobe.
- [ ] Accept a maximum size in decimal MB and show source details.
- [ ] Encode to MP4 with a calculated bitrate and two passes.
- [ ] Display progress and support cancellation without freezing the UI.
- [ ] Verify the actual size, media streams, and duration before reporting success.
- [ ] Handle impossible limits, invalid inputs, tool failures, and existing output paths.
- [ ] Validate portrait videos, silent videos, and Unicode paths.
- [ ] Add relevant automated checks and a Windows CI build.

Acceptance: compress several real videos below their chosen byte limits, keep the originals intact, and leave no partial final file after failure or cancellation.

## First portable release

- [ ] Validate Windows Native AOT publishing and measure startup, memory, and encoding resource use.
- [ ] Pin and bundle FFmpeg/ffprobe with checksums and the corresponding notices and sources.
- [ ] Package a ZIP and test it on a clean supported Windows machine.
- [ ] Document supported inputs, output behavior, and known limitations.
- [ ] Publish a tested build with release notes and usage screenshots.

Acceptance: download, extract, launch, and compress without installing a runtime or media tools separately.

## Later

- Drag and drop, editable size presets, and an installer.
- Resolution suggestions and a reduced CPU mode.
- Hardware encoding after size and quality validation.
- A sequential batch queue and additional operating systems.
