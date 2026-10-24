using System.Diagnostics;
using System.Text;

namespace Crumb.Core;

internal sealed record ProcessResult(int ExitCode, string Output, string Error);

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments,
        CancellationToken cancellationToken, Action<string>? onOutput = null, int outputLimit = 2_000_000)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new MediaException("The media tool could not start.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new MediaException("The media tool could not start. Check the portable files and permissions.", ex.Message);
        }

        using var cancellation = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });
        var stdout = ReadAsync(process.StandardOutput, outputLimit, onOutput);
        var stderr = ReadAsync(process.StandardError, 24_000, null);
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        var output = await stdout.ConfigureAwait(false);
        var error = await stderr.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new(process.ExitCode, output, error);
    }

    private static async Task<string> ReadAsync(StreamReader reader, int limit, Action<string>? callback)
    {
        var captured = new StringBuilder();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            // Drain the stream even after reaching the capture limit.
            callback?.Invoke(line);
            if (captured.Length < limit)
                captured.AppendLine(line[..Math.Min(line.Length, limit - captured.Length)]);
        }
        return captured.ToString();
    }
}
