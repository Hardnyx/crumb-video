using System.Diagnostics;
using System.Security.Cryptography;
using Crumb.Core;
using Xunit;

namespace Crumb.Tests;

public sealed class CompressionIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "crumb-tests-" + Guid.NewGuid().ToString("N"));
    private readonly MediaTools _tools;

    public CompressionIntegrationTests()
    {
        Directory.CreateDirectory(_directory);
        // Integration tests deliberately fail if the real media tools are not configured.
        _tools = MediaTools.Resolve();
    }

    [Theory]
    [InlineData(true, "640x360", 0)]
    [InlineData(false, "360x640", 480)]
    public async Task FullVideoFitsCapPreservesInputAndDecodes(bool audio, string size, int height)
    {
        var input = await MakeVideo(audio, size);
        var original = SHA256.HashData(await File.ReadAllBytesAsync(input, TestContext.Current.CancellationToken));
        var output = Path.Combine(_directory, "ready.mp4");
        var result = await new VideoCompressor(_tools).CompressAsync(new(input, output, 180_000, height, true), cancellationToken: TestContext.Current.CancellationToken);
        Assert.InRange(result.SizeBytes, 1, 180_000);
        var probe = new MediaProbe(_tools);
        var media = await probe.InspectAsync(output, TestContext.Current.CancellationToken);
        Assert.InRange(media.Video.DurationSeconds, 2.9, 3.2);
        Assert.Equal(audio, media.Audio is not null);
        Assert.Equal(size == "360x640", media.Video.Height > media.Video.Width);
        if (height > 0) Assert.True(media.Video.Height <= height);
        Assert.Equal(original, SHA256.HashData(await File.ReadAllBytesAsync(input, TestContext.Current.CancellationToken)));
        Assert.Empty(Directory.GetDirectories(_directory, ".crumb-*"));
        await Execute(_tools.Ffmpeg, ["-v", "error", "-i", output, "-f", "null", "-"]);
    }

    [Fact]
    public async Task ExistingDestinationAndOriginalCannotBeOverwritten()
    {
        var input = await MakeVideo(false, "320x240");
        var output = Path.Combine(_directory, "existing.mp4");
        await File.WriteAllTextAsync(output, "do not overwrite", TestContext.Current.CancellationToken);
        var compressor = new VideoCompressor(_tools);
        await Assert.ThrowsAsync<MediaException>(() => compressor.CompressAsync(new(input, output, 180_000), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("do not overwrite", await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<MediaException>(() => compressor.CompressAsync(new(input, input, 180_000), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationStopsJobAndCleansTemporaryFiles()
    {
        var input = await MakeVideo(true, "640x360");
        var output = Path.Combine(_directory, "cancelled.mp4");
        using var cts = new CancellationTokenSource();
        var progress = new ImmediateProgress(p => { if (p.Stage.Contains("Pass 1")) cts.Cancel(); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new VideoCompressor(_tools).CompressAsync(new(input, output, 180_000), progress, cts.Token));
        Assert.False(File.Exists(output));
        Assert.True(File.Exists(input));
        Assert.Empty(Directory.GetDirectories(_directory, ".crumb-*"));
    }

    [Fact]
    public async Task CancellationKillsAnActiveEncoder()
    {
        var input = await MakeVideo(false, "1280x720", "6");
        var output = Path.Combine(_directory, "active-cancel.mp4");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var observedRunning = false;
        var progress = new ImmediateProgress(p =>
        {
            if (p.Fraction > 0 && p.Stage.Contains("Pass 1"))
            {
                observedRunning = true;
                cts.Cancel();
            }
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new VideoCompressor(_tools).CompressAsync(new(input, output, 300_000), progress, cts.Token));
        Assert.True(observedRunning);
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetDirectories(_directory, ".crumb-*"));
    }

    [Fact]
    public async Task InvalidInputLeavesNoOutput()
    {
        var input = Path.Combine(_directory, "broken.mp4");
        await File.WriteAllTextAsync(input, "not a video", TestContext.Current.CancellationToken);
        var output = Path.Combine(_directory, "output.mp4");
        await Assert.ThrowsAsync<MediaException>(() => new VideoCompressor(_tools).CompressAsync(new(input, output, 180_000), cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetDirectories(_directory, ".crumb-*"));
    }

    private async Task<string> MakeVideo(bool audio, string size, string duration = "3")
    {
        var path = Path.Combine(_directory, "vídeo with spaces ' $ clip.mkv");
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", $"testsrc2=size={size}:rate=24" };
        if (audio) args.AddRange(["-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000"]);
        args.AddRange(["-t", duration, "-c:v", "libx264", "-threads", "2", "-preset", "ultrafast", "-qp", "0"]);
        if (audio) args.AddRange(["-c:a", "pcm_s16le"]);
        args.Add(path);
        await Execute(_tools.Ffmpeg, args);
        return path;
    }

    private static async Task Execute(string executable, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.True(process.ExitCode == 0, await error);
    }
    public void Dispose() { try { Directory.Delete(_directory, true); } catch (IOException) { } }
    private sealed class ImmediateProgress(Action<CompressionProgress> report) : IProgress<CompressionProgress>
    { public void Report(CompressionProgress value) => report(value); }
}
