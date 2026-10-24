using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Crumb.Desktop.ViewModels;

namespace Crumb.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model = new();
    private bool _closeWhenIdle;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _model;
        DropZone.AddHandler(DragDrop.DragOverEvent, DragOver);
        DropZone.AddHandler(DragDrop.DropEvent, DropVideo);
        Closing += (_, e) =>
        {
            if (!_model.IsBusy) return;
            e.Cancel = true;
            _closeWhenIdle = true;
            _model.Cancel();
        };
        _model.PropertyChanged += (_, e) =>
        {
            if (_closeWhenIdle && e.PropertyName == nameof(MainViewModel.IsBusy) && !_model.IsBusy)
                Dispatcher.UIThread.Post(Close);
        };
    }

    private async void ChooseVideo(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new()
            {
                Title = "Choose a video", AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Videos")
                    { Patterns = ["*.mp4", "*.mkv", "*.mov", "*.webm", "*.avi", "*.m4v", "*.wmv", "*.mpeg", "*.mpg", "*.mts", "*.m2ts"] }, FilePickerFileTypes.All]
            });
            if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) await _model.LoadAsync(path);
        }
        catch (Exception ex) { _model.ShowError(ex); }
    }

    private async void ChooseOutput(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Save a new MP4 copy", DefaultExtension = "mp4",
                SuggestedFileName = string.IsNullOrWhiteSpace(_model.OutputPath) ? "video-crumb.mp4" : System.IO.Path.GetFileName(_model.OutputPath),
                FileTypeChoices = [new FilePickerFileType("MP4 video") { Patterns = ["*.mp4"] }]
            });
            if (file?.TryGetLocalPath() is { } path) _model.OutputPath = path;
        }
        catch (Exception ex) { _model.ShowError(ex); }
    }

    private void ChoosePreset(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string limit }) _model.LimitMb = limit;
    }
    private async void CompressVideo(object? sender, RoutedEventArgs e) => await _model.CompressAsync();
    private void CancelJob(object? sender, RoutedEventArgs e) => _model.Cancel();
    private async void OpenResult(object? sender, RoutedEventArgs e) => await OpenFile(_model.CompletedPath);
    private async void OpenOriginal(object? sender, RoutedEventArgs e) => await OpenFile(_model.InputPath);
    private async Task OpenFile(string? path)
    {
        if (path is null) return;
        try
        {
            var file = await StorageProvider.TryGetFileFromPathAsync(path);
            if (file is not null) await Launcher.LaunchFileAsync(file);
        }
        catch (Exception ex) { _model.ShowError(ex); }
    }
    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = !_model.IsBusy && e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void DropVideo(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (_model.IsBusy) return;
        var files = e.DataTransfer.TryGetFiles();
        if (files?.FirstOrDefault()?.TryGetLocalPath() is { } path) await _model.LoadAsync(path);
    }
}
