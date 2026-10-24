namespace Crumb.Core;

public static class SizeBudget
{
    public const long MinimumBytes = 100_000;
    public const long MaximumBytes = 1_000_000_000_000;
    public const long MinimumVideoBitrate = 32_000;

    public static CompressionPlan Calculate(MediaInfo info, long maxBytes, int maxHeight = 0, bool reducedCpu = false)
    {
        if (maxBytes < MinimumBytes || maxBytes > MaximumBytes)
            throw new MediaException("Choose a size between 0.1 MB and 1,000,000 MB.");
        if (!double.IsFinite(info.DurationSeconds) || info.Video.DurationSeconds <= 0)
            throw new MediaException("A valid video duration is required.");
        if (maxHeight != 0 && maxHeight is not (1080 or 720 or 480))
            throw new MediaException("Choose Original, 1080p, 720p, or 480p.");
        var usableBits = maxBytes * 8d * .97;
        var audioRate = info.Audio is null ? 0 : 96_000;
        var audioDuration = info.Audio?.DurationSeconds ?? 0;
        if (audioRate > 0 && (usableBits - audioRate * audioDuration) / info.Video.DurationSeconds < 80_000)
            audioRate = 48_000;
        var videoRate = (long)Math.Floor((usableBits - audioRate * audioDuration) / info.Video.DurationSeconds);
        if (videoRate < MinimumVideoBitrate)
            throw new MediaException("This size is too small for the full video and audio. Increase the limit or shorten the clip first.");
        // Avoid extremely large rates for short clips; fitting the cap is the objective.
        videoRate = Math.Min(videoRate, 100_000_000);
        var scale = maxHeight > 0 ? Math.Min(1d, maxHeight / (double)info.Video.Height) : 1d;
        var pixelsPerSecond = info.Video.Width * scale * info.Video.Height * scale * Math.Max(24, info.Video.FramesPerSecond);
        var normalThreads = Math.Clamp(Environment.ProcessorCount - 1, 1, 8);
        return new(videoRate, audioRate, maxHeight, reducedCpu ? Math.Max(1, normalThreads / 2) : normalThreads,
            (videoRate * info.Video.DurationSeconds + audioRate * audioDuration) / 8, videoRate / pixelsPerSecond < .045);
    }
}
