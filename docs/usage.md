# Using Crumb

## Start

Extract the complete portable ZIP and open `Crumb.exe`. Keep its `tools` and other included folders together. The installer is an alternative that adds shortcuts and uninstall support; it does not require administrator privileges.

1. Click **Choose video** or drag a video onto the window.
2. Enter the maximum size in **MB**, or choose a quick preset.
3. Keep **Original** resolution or select a smaller height. Crumb never deliberately upscales the video.
4. Check the destination. The suggested name ends in `-crumb.mp4` and avoids existing files.
5. Click **Compress**. After it finishes, use **Open output** to view the file.

All processing happens on your computer. There are no uploads, accounts, or network services in the application.

## Size and quality

One MB means 1,000,000 bytes. Choose a size slightly below a service's limit when its unit definition is uncertain. Presets are editable numbers, not live promises about a particular service's upload allowance.

A smaller limit requires less information per second. A long video may need lower resolution or may not fit a very small limit at all. Crumb warns when the calculated bitrate is low. It may make up to three encoding attempts to fit the cap; progress restarts when it retries.

Two passes take longer than a single encode. **Limit CPU use** uses fewer encoding threads. It can help other programs remain responsive but usually takes longer. GPU encoding is not included in this version.

## Files and cancellation

The original is never overwritten. Choose a new destination if the name already exists. After cancellation or failure, Crumb removes its job's temporary files and does not leave a partial file at the requested final path. Closing the window while working cancels the job and waits for cleanup.

An operating-system crash or forced termination may leave a `.crumb-*` temporary directory in the destination folder. Close Crumb before deleting such a leftover directory.

## Current limits

- Output is MP4 with 8-bit H.264 video and AAC audio, when audio is present.
- One video stream and one default audio stream are retained. Audio is mono or stereo. Other audio tracks, subtitles, attachments, and chapters are omitted.
- The output's duration is checked against the source; a file cannot satisfy the cap by silently truncating the video.
- HDR inputs are rejected to avoid producing an incorrectly converted image. Export an SDR copy first.
- Media without a usable duration, damaged inputs, or unsupported codecs are rejected.
- Only one job runs at a time. Batch queues and GPU encoding are not implemented.
- Windows x64 is the first distribution target. Preview builds are unsigned and are not yet verified on a clean Windows installation.

If the media tools are missing, extract the whole portable ZIP again. **Error details** provides technical output for a bug report. Review it for personal file paths before sharing.
