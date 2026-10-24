using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Crumb.Core;

namespace Crumb.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private MediaInfo? _info;
    private CancellationTokenSource? _cancellation;
    private string _limitMb = "25";
    private string _outputPath = "";
    private int _resolution;
    private bool _reducedCpu;
    private bool _isBusy;
    private string _status = "Make room for your video.";
    private string _detail = "Choose a video and a size limit to get started.";
    private string _errorDetails = "";
    private bool _indeterminate;
    private double _progress;
    private string? _completedPath;

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; Refresh(); } }
    public bool CanEdit => !IsBusy;
    public bool HasVideo => _info is not null;
    public string FileName => _info is null ? "Drop a video here" : System.IO.Path.GetFileName(_info.Path);
    public string SourceDetails => _info is null ? "or select a file from your computer" :
        $"{FormatSize(_info.SizeBytes)}  ·  {FormatDuration(_info.DurationSeconds)}  ·  {_info.Video.Width} × {_info.Video.Height}  ·  {(_info.Audio is null ? "No audio" : "With audio")}";
    public string LimitMb { get => _limitMb; set { _limitMb = value; _completedPath = null; Refresh(); } }
    public string OutputPath { get => _outputPath; set { _outputPath = value; Refresh(); } }
    public int ResolutionIndex { get => _resolution; set { _resolution = value; Refresh(); } }
    public bool ReducedCpu { get => _reducedCpu; set { _reducedCpu = value; Refresh(); } }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string Detail { get => _detail; private set => Set(ref _detail, value); }
    public string ErrorDetails { get => _errorDetails; private set { Set(ref _errorDetails, value); Changed(nameof(HasErrorDetails)); } }
    public bool HasErrorDetails => !string.IsNullOrWhiteSpace(ErrorDetails);
    public bool IsIndeterminate { get => _indeterminate; private set => Set(ref _indeterminate, value); }
    public double ProgressValue { get => _progress; private set { Set(ref _progress, value); Changed(nameof(ProgressLabel)); } }
    public string ProgressLabel => $"{ProgressValue:0}%";
    public bool HasResult => _completedPath is not null && !IsBusy;
    public string? CompletedPath => _completedPath;
    public string? InputPath => _info?.Path;
    public bool AlreadyFits => _info is not null && TryLimit(out var limit) && _info.SizeBytes <= limit;
    public string CompressionHint
    {
        get
        {
            if (!TryLimit(out var limit)) return "Enter a size from 0.1 to 1,000,000 MB.";
            if (_info is null) return "1 MB = 1,000,000 bytes. The final file will be checked against this limit.";
            if (AlreadyFits) return "This video already fits the limit. You can share the original.";
            try
            {
                var plan = SizeBudget.Calculate(_info, limit, MaxHeight, ReducedCpu);
                return plan.LowQuality
                    ? "This is a tight limit. Expect visible quality loss; a lower resolution may help."
                    : $"Up to {100 * (1 - limit / (double)_info.SizeBytes):0}% smaller. Some quality loss is expected.";
            }
            catch (MediaException ex) { return ex.Message; }
        }
    }
    public bool CanCompress
    {
        get
        {
            if (IsBusy || _info is null || AlreadyFits || !TryLimit(out var limit) || string.IsNullOrWhiteSpace(OutputPath)) return false;
            try { SizeBudget.Calculate(_info, limit, MaxHeight, ReducedCpu); return true; }
            catch (MediaException) { return false; }
        }
    }
    private int MaxHeight => ResolutionIndex switch { 1 => 1080, 2 => 720, 3 => 480, _ => 0 };

    public async Task LoadAsync(string path)
    {
        if (IsBusy) return;
        Begin("Reading your video", "Checking duration, dimensions, and audio.");
        IsIndeterminate = true;
        try
        {
            _info = await new MediaProbe(MediaTools.Resolve()).InspectAsync(path, _cancellation!.Token);
            OutputPath = SuggestOutput(_info.Path);
            Status = "Choose your size limit.";
            Detail = "The original stays untouched. Compression runs on your computer.";
        }
        catch (OperationCanceledException) { Status = "Reading cancelled."; Detail = "Choose a video when you are ready."; }
        catch (Exception ex) { _info = null; ShowError(ex); }
        finally { End(); }
    }

    public async Task CompressAsync()
    {
        if (!CanCompress || _info is null || !TryLimit(out var limit)) return;
        var options = new CompressionOptions(_info.Path, OutputPath, limit, MaxHeight, ReducedCpu);
        Begin("Preparing your video", "You can cancel at any time.");
        try
        {
            var progress = new Progress<CompressionProgress>(p =>
            {
                Status = p.Stage;
                ProgressValue = p.Fraction * 100;
                Detail = p.Attempt > 1 ? $"Adjusting the size · attempt {p.Attempt} of 3" : "Working locally. Your original is safe.";
            });
            var result = await new VideoCompressor(MediaTools.Resolve()).CompressAsync(options, progress, _cancellation!.Token);
            _completedPath = result.OutputPath;
            ProgressValue = 100;
            Status = "Ready to share.";
            Detail = $"{FormatSize(result.SizeBytes)} · within your {LimitMb} MB limit.";
        }
        catch (OperationCanceledException) { Status = "Compression cancelled."; Detail = "No final file was saved. Your original is unchanged."; ProgressValue = 0; }
        catch (Exception ex) { ShowError(ex); ProgressValue = 0; }
        finally { End(); }
    }

    public void Cancel()
    {
        if (!IsBusy) return;
        _cancellation?.Cancel();
        Status = "Cancelling…";
        Detail = "Waiting for the media process to stop and clearing temporary files.";
    }

    public void ShowError(Exception ex)
    {
        Status = "Something needs your attention.";
        Detail = ex is MediaException ? ex.Message : "The file could not be processed. Check the output folder and try again.";
        ErrorDetails = ex is MediaException media ? media.Details ?? "" : ex.Message;
    }

    private void Begin(string status, string detail)
    {
        _cancellation = new CancellationTokenSource();
        _completedPath = null;
        ErrorDetails = "";
        ProgressValue = 0;
        IsBusy = true;
        Status = status;
        Detail = detail;
    }
    private void End()
    {
        _cancellation?.Dispose();
        _cancellation = null;
        IsIndeterminate = false;
        IsBusy = false;
    }
    private bool TryLimit(out long bytes)
    {
        bytes = 0;
        if (!decimal.TryParse(LimitMb.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mb) ||
            mb < .1m || mb > 1_000_000m) return false;
        bytes = (long)decimal.Floor(mb * 1_000_000m);
        return true;
    }
    private void Refresh()
    {
        foreach (var property in new[] { nameof(IsBusy), nameof(CanEdit), nameof(HasVideo), nameof(FileName), nameof(SourceDetails),
            nameof(LimitMb), nameof(OutputPath), nameof(ResolutionIndex), nameof(ReducedCpu), nameof(CanCompress),
            nameof(CompressionHint), nameof(HasResult), nameof(AlreadyFits) }) Changed(property);
    }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; Changed(property);
    }
    private void Changed(string? property) => PropertyChanged?.Invoke(this, new(property));
    public static string FormatSize(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1_000_000_000d:0.##} GB" : $"{bytes / 1_000_000d:0.##} MB";
    private static string FormatDuration(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
    private static string SuggestOutput(string input)
    {
        var stem = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(input)!, System.IO.Path.GetFileNameWithoutExtension(input) + "-crumb");
        var candidate = stem + ".mp4";
        for (var i = 2; File.Exists(candidate); i++) candidate = stem + "-" + i + ".mp4";
        return candidate;
    }
}
