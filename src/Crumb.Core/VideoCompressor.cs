using System.Diagnostics;
using System.Globalization;

namespace Crumb.Core;

public sealed class VideoCompressor(MediaTools tools)
{
    public async Task<CompressionResult> CompressAsync(CompressionOptions options,
        IProgress<CompressionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var input = Path.GetFullPath(options.InputPath);
        var output = Path.GetFullPath(options.OutputPath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(input, output, comparison)) throw new MediaException("Choose a different output file. The original must stay untouched.");
        if (File.Exists(output) || Directory.Exists(output)) throw new MediaException("The output already exists. Choose a new file name.");
        if (!string.Equals(Path.GetExtension(output), ".mp4", StringComparison.OrdinalIgnoreCase))
            throw new MediaException("Choose an output file ending in .mp4.");
        var destination = Path.GetDirectoryName(output)!;
        if (!Directory.Exists(destination)) throw new MediaException("The output folder does not exist.");
        progress?.Report(new("Reading video", 0));
        var probe = new MediaProbe(tools);
        var info = await probe.InspectAsync(input, cancellationToken).ConfigureAwait(false);
        var plan = SizeBudget.Calculate(info, options.MaxBytes, options.MaxHeight, options.ReducedCpu);
        var inputStamp = File.GetLastWriteTimeUtc(input);
        var working = Path.Combine(destination, ".crumb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);
        var temporary = Path.Combine(working, "video.mp4");
        var passLog = Path.Combine(working, "pass");
        try
        {
            var bitrate = plan.VideoBitrate;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                for (var pass = 1; pass <= 2; pass++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var stage = pass == 1 ? "Analyzing video · pass 1 of 2" : "Compressing · pass 2 of 2";
                    progress?.Report(new(stage, (pass - 1) * .49, attempt));
                    var clock = Stopwatch.StartNew();
                    var capturedPass = pass;
                    var result = await ProcessRunner.RunAsync(tools.Ffmpeg,
                        Arguments(input, temporary, info, plan, bitrate, pass, passLog), cancellationToken, line =>
                        {
                            if (!line.StartsWith("out_time_us=", StringComparison.Ordinal) || clock.ElapsedMilliseconds < 120) return;
                            if (!long.TryParse(line.AsSpan("out_time_us=".Length), CultureInfo.InvariantCulture, out var microseconds)) return;
                            clock.Restart();
                            var duration = capturedPass == 1 ? info.Video.DurationSeconds : info.DurationSeconds;
                            var fraction = Math.Clamp(microseconds / 1_000_000d / duration, 0, 1);
                            progress?.Report(new(stage, (capturedPass - 1) * .49 + fraction * .49, attempt));
                        }).ConfigureAwait(false);
                    if (result.ExitCode != 0)
                        throw new MediaException("Compression failed. The video may be damaged or use unsupported settings.", result.Error);
                }

                progress?.Report(new("Checking size and duration", .99, attempt));
                var encoded = await probe.InspectAsync(temporary, cancellationToken).ConfigureAwait(false);
                ValidateMedia(info, encoded);
                if (encoded.SizeBytes <= options.MaxBytes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (new FileInfo(input).Length != info.SizeBytes || File.GetLastWriteTimeUtc(input) != inputStamp)
                        throw new MediaException("The original video changed during compression. Please select it again.");
                    File.Move(temporary, output, overwrite: false);
                    progress?.Report(new("Ready to share", 1, attempt));
                    return new(output, encoded.SizeBytes, attempt);
                }

                // Hold the measured non-video allowance fixed rather than scaling away the audio budget.
                var estimatedVideoBytes = bitrate * info.Video.DurationSeconds / 8;
                var otherBytes = Math.Max(0, encoded.SizeBytes - estimatedVideoBytes);
                var remaining = options.MaxBytes * .96 - otherBytes;
                var nextRate = (long)Math.Floor(remaining * 8 / info.Video.DurationSeconds);
                bitrate = Math.Min(nextRate, (long)(bitrate * .94));
                if (bitrate < SizeBudget.MinimumVideoBitrate) break;
            }
            throw new MediaException("The video could not fit this size after three attempts. Increase the limit or choose a lower resolution.");
        }
        finally
        {
            // Only this job's directory is ever removed. A validated final file is outside it.
            try { Directory.Delete(working, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static IEnumerable<string> Arguments(string input, string output, MediaInfo info,
        CompressionPlan plan, long bitrate, int pass, string passLog)
    {
        var threads = plan.Threads;
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
            "-threads", threads.ToString(CultureInfo.InvariantCulture), "-filter_threads", threads.ToString(CultureInfo.InvariantCulture),
            "-i", input, "-map", "0:" + info.Video.Index.ToString(CultureInfo.InvariantCulture),
            "-c:v", "libx264", "-preset", "fast", "-pix_fmt", "yuv420p", "-b:v", bitrate.ToString(CultureInfo.InvariantCulture),
            "-threads", threads.ToString(CultureInfo.InvariantCulture), "-pass", pass.ToString(CultureInfo.InvariantCulture), "-passlogfile", passLog,
            "-vf", plan.MaxHeight == 0 ? "scale=trunc(iw/2)*2:trunc(ih/2)*2" :
                $"scale=-2:min({plan.MaxHeight.ToString(CultureInfo.InvariantCulture)}\\,trunc(ih/2)*2)",
            "-map_metadata", "-1", "-map_chapters", "-1", "-sn", "-dn", "-progress", "pipe:1", "-nostats" };
        if (pass == 1) args.AddRange(["-an", "-f", "null", "-"]);
        else
        {
            if (info.Audio is { } audio)
                args.AddRange(["-map", "0:" + audio.Index.ToString(CultureInfo.InvariantCulture),
                    "-c:a", "aac", "-b:a", plan.AudioBitrate.ToString(CultureInfo.InvariantCulture), "-ac", audio.Channels == 1 ? "1" : "2"]);
            else args.Add("-an");
            args.AddRange(["-movflags", "+faststart", "-f", "mp4", output]);
        }
        return args;
    }

    private static void ValidateMedia(MediaInfo source, MediaInfo output)
    {
        var tolerance = Math.Max(.3, 3 / Math.Max(source.Video.FramesPerSecond, 10));
        if (Math.Abs(source.Video.DurationSeconds - output.Video.DurationSeconds) > tolerance)
            throw new MediaException("The output video duration did not match the original. No final file was saved.");
        if (source.Audio is { } audio && (output.Audio is not { } encodedAudio ||
            Math.Abs(audio.DurationSeconds - encodedAudio.DurationSeconds) > Math.Max(.5, tolerance)))
            throw new MediaException("The output audio was incomplete. No final file was saved.");
        if (output.SizeBytes <= 0) throw new MediaException("The media tool produced an empty file.");
    }
}
