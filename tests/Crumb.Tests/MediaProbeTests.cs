using Crumb.Core;
using Xunit;

namespace Crumb.Tests;

public sealed class MediaProbeTests
{
    [Fact]
    public void ExcludesCoverArtAndSelectsDefaultAudio()
    {
        var info = MediaProbe.Parse("clip.mkv", 5_000_000, """
            {"format":{"duration":"8.5"},"streams":[
              {"index":0,"codec_type":"video","width":500,"height":500,"disposition":{"attached_pic":1}},
              {"index":1,"codec_type":"video","width":1280,"height":720,"avg_frame_rate":"30000/1001"},
              {"index":2,"codec_type":"audio","channels":2},
              {"index":3,"codec_type":"audio","channels":1,"disposition":{"default":1}}
            ]}
            """);
        Assert.Equal(1, info.Video.Index);
        Assert.Equal(3, info.Audio!.Index);
        Assert.Equal(8.5, info.Video.DurationSeconds);
        Assert.InRange(info.Video.FramesPerSecond, 29.96, 29.98);
    }
    [Fact]
    public void ReadsTaggedDurationAndRotation()
    {
        var info = MediaProbe.Parse("portrait.mov", 1, """
            {"streams":[{"index":0,"codec_type":"video","width":1280,"height":720,
              "tags":{"DURATION":"00:00:05.500000000"},"side_data_list":[{"rotation":90}]}]}
            """);
        Assert.Equal(720, info.Video.Width);
        Assert.Equal(1280, info.Video.Height);
        Assert.Equal(5.5, info.DurationSeconds);
    }
    [Theory]
    [InlineData("{\"streams\":[]}")]
    [InlineData("{\"streams\":[{\"codec_type\":\"audio\"}]}")]
    [InlineData("{\"streams\":[{\"codec_type\":\"video\",\"width\":640,\"height\":360}]}")]
    public void UnusableMediaIsRejected(string json) => Assert.Throws<MediaException>(() => MediaProbe.Parse("invalid", 1, json));
}
