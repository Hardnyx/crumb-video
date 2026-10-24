namespace Crumb.Core;

public sealed record MediaTools(string Ffmpeg, string Ffprobe)
{
    public static MediaTools Resolve()
    {
        var configured = Environment.GetEnvironmentVariable("CRUMB_FFMPEG_DIR");
        var directory = string.IsNullOrWhiteSpace(configured)
            ? System.IO.Path.Combine(AppContext.BaseDirectory, "tools") : System.IO.Path.GetFullPath(configured);
        var suffix = OperatingSystem.IsWindows() ? ".exe" : "";
        var ffmpeg = System.IO.Path.Combine(directory, "ffmpeg" + suffix);
        var ffprobe = System.IO.Path.Combine(directory, "ffprobe" + suffix);
        if (!File.Exists(ffmpeg) || !File.Exists(ffprobe))
            throw new MediaException("The media tools are missing. Extract the complete Crumb portable ZIP again.",
                "Developers: set CRUMB_FFMPEG_DIR to the directory containing ffmpeg and ffprobe.");
        return new(ffmpeg, ffprobe);
    }
}
