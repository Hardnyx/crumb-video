namespace Crumb.Core;

public sealed record VideoStream(int Index, int Width, int Height, double DurationSeconds, double FramesPerSecond);
public sealed record AudioStream(int Index, double DurationSeconds, int Channels);

public sealed record MediaInfo(string Path, long SizeBytes, VideoStream Video, AudioStream? Audio)
{
    public double DurationSeconds => Math.Max(Video.DurationSeconds, Audio?.DurationSeconds ?? 0);
}

public sealed record CompressionOptions(string InputPath, string OutputPath, long MaxBytes,
    int MaxHeight = 0, bool ReducedCpu = false);

public sealed record CompressionPlan(long VideoBitrate, int AudioBitrate, int MaxHeight, int Threads,
    double EstimatedBytes, bool LowQuality);

public sealed record CompressionProgress(string Stage, double Fraction, int Attempt = 1);
public sealed record CompressionResult(string OutputPath, long SizeBytes, int Attempts);

public sealed class MediaException(string message, string? details = null) : Exception(message)
{
    public string? Details { get; } = details;
}
