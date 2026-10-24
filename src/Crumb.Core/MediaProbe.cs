using System.Globalization;
using System.Text.Json;

namespace Crumb.Core;

public sealed class MediaProbe(MediaTools tools)
{
    public async Task<MediaInfo> InspectAsync(string path, CancellationToken cancellationToken = default)
    {
        path = System.IO.Path.GetFullPath(path);
        if (!File.Exists(path)) throw new MediaException("The selected video no longer exists.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        ProcessResult result;
        try
        {
            result = await ProcessRunner.RunAsync(tools.Ffprobe,
                ["-v", "error", "-show_format", "-show_streams", "-of", "json", path], timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MediaException("Reading this video's information took too long.");
        }
        if (result.ExitCode != 0) throw new MediaException("This file could not be read as a video.", result.Error);
        try { return Parse(path, new FileInfo(path).Length, result.Output); }
        catch (JsonException ex) { throw new MediaException("The media tool returned invalid video information.", ex.Message); }
    }

    public static MediaInfo Parse(string path, long sizeBytes, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("streams", out var streams) || streams.ValueKind != JsonValueKind.Array)
            throw new MediaException("No video stream was found.");
        var format = root.TryGetProperty("format", out var f) ? f : default;
        var fallbackDuration = Number(format, "duration");
        var videos = streams.EnumerateArray().Where(s => Text(s, "codec_type") == "video" && !Disposition(s, "attached_pic")).ToArray();
        if (videos.Length == 0) throw new MediaException("No video stream was found. Audio files and cover images are not supported.");
        var v = videos.FirstOrDefault(s => Disposition(s, "default"));
        if (v.ValueKind == JsonValueKind.Undefined) v = videos[0];
        if (Text(v, "color_transfer") is "smpte2084" or "arib-std-b67")
            throw new MediaException("HDR video is not supported in this version. Export an SDR copy first to preserve its appearance.");
        var videoDuration = Duration(v, fallbackDuration);
        var width = (int)Number(v, "width");
        var height = (int)Number(v, "height");
        if (!double.IsFinite(videoDuration) || videoDuration <= 0 || width < 2 || height < 2)
            throw new MediaException("This video has no usable duration or dimensions.");
        var rotation = 0d;
        if (v.TryGetProperty("side_data_list", out var sides))
            foreach (var side in sides.EnumerateArray())
                if (side.TryGetProperty("rotation", out _)) rotation = Number(side, "rotation");
        if (Math.Abs(rotation % 180) > 45 && Math.Abs(rotation % 180) < 135) (width, height) = (height, width);
        var frameRate = Ratio(Text(v, "avg_frame_rate"));
        if (!double.IsFinite(frameRate) || frameRate < 0) frameRate = 0;
        var video = new VideoStream((int)Number(v, "index"), width, height, videoDuration, frameRate);

        var audios = streams.EnumerateArray().Where(s => Text(s, "codec_type") == "audio").ToArray();
        AudioStream? audio = null;
        if (audios.Length > 0)
        {
            var a = audios.FirstOrDefault(s => Disposition(s, "default"));
            if (a.ValueKind == JsonValueKind.Undefined) a = audios[0];
            var duration = Duration(a, Math.Max(fallbackDuration, videoDuration));
            audio = new((int)Number(a, "index"), Math.Max(duration, 0), (int)Number(a, "channels"));
        }
        return new(path, sizeBytes, video, audio);
    }

    private static bool Disposition(JsonElement stream, string name) =>
        stream.TryGetProperty("disposition", out var d) && Number(d, name) == 1;

    private static double Duration(JsonElement stream, double fallback)
    {
        var duration = Number(stream, "duration");
        if (duration > 0 && double.IsFinite(duration)) return duration;
        if (stream.TryGetProperty("tags", out var tags))
        {
            // Matroska often writes nanoseconds (9 fractional digits), beyond TimeSpan's parser precision.
            var parts = Text(tags, "DURATION").Split(':');
            if (parts.Length == 3 && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var hours) &&
                double.TryParse(parts[1], CultureInfo.InvariantCulture, out var minutes) &&
                double.TryParse(parts[2], CultureInfo.InvariantCulture, out var seconds))
            {
                var tagged = hours * 3600 + minutes * 60 + seconds;
                if (tagged > 0 && double.IsFinite(tagged)) return tagged;
            }
        }
        return double.IsFinite(fallback) ? Math.Max(fallback, 0) : 0;
    }

    private static double Number(JsonElement element, string name) =>
        double.TryParse(Text(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0;
    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.ToString() : "";
    private static double Ratio(string text)
    {
        var parts = text.Split('/');
        return parts.Length == 2 && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var a) &&
            double.TryParse(parts[1], CultureInfo.InvariantCulture, out var b) && b > 0 ? a / b : 0;
    }
}
