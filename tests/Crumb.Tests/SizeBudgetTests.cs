using Crumb.Core;
using Xunit;

namespace Crumb.Tests;

public sealed class SizeBudgetTests
{
    private static MediaInfo Info(double videoSeconds = 60, double? audioSeconds = 60) =>
        new("source.mp4", 100_000_000, new(0, 1920, 1080, videoSeconds, 30),
            audioSeconds is { } duration ? new(1, duration, 2) : null);

    [Fact]
    public void ReservesOverheadAndAudio()
    {
        var plan = SizeBudget.Calculate(Info(), 25_000_000);
        Assert.Equal(3_137_333, plan.VideoBitrate);
        Assert.Equal(96_000, plan.AudioBitrate);
        Assert.True(plan.EstimatedBytes <= 25_000_000 * .97);
    }
    [Fact]
    public void SilentVideoDoesNotReserveAudio()
    {
        var plan = SizeBudget.Calculate(Info(audioSeconds: null), 25_000_000);
        Assert.Equal(0, plan.AudioBitrate);
        Assert.Equal(3_233_333, plan.VideoBitrate);
    }
    [Fact]
    public void LongAudioUsesItsOwnDuration()
    {
        var plan = SizeBudget.Calculate(Info(60, 120), 25_000_000);
        Assert.Equal(3_041_333, plan.VideoBitrate);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(99_999)]
    [InlineData(1_000_000_000_001)]
    public void InvalidLimitsAreRejected(long bytes) => Assert.Throws<MediaException>(() => SizeBudget.Calculate(Info(), bytes));

    [Fact]
    public void ImpossibleFullLengthBudgetIsRejected() =>
        Assert.Throws<MediaException>(() => SizeBudget.Calculate(Info(3600, 3600), 100_000));
    [Fact]
    public void ReducedCpuLimitsThreads()
    {
        var normal = SizeBudget.Calculate(Info(), 25_000_000);
        var reduced = SizeBudget.Calculate(Info(), 25_000_000, reducedCpu: true);
        Assert.InRange(normal.Threads, 1, 8);
        Assert.InRange(reduced.Threads, 1, normal.Threads);
    }
    [Fact]
    public void LowBitrateWarnsAboutQuality() => Assert.True(SizeBudget.Calculate(Info(), 2_000_000).LowQuality);
}
