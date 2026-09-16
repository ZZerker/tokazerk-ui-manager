using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class VariantChoiceRowViewModel(
    string id,
    string label,
    Action<VariantChoiceRowViewModel> selectionChanged) : ObservableObject, IDisposable
{
    public string Id { get; } = id;

    public string Label { get; } = label;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private Bitmap? thumbnail;

    [ObservableProperty]
    private string? error;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            selectionChanged(this);
        }
    }

    public void ReplaceThumbnail(Bitmap? bitmap)
    {
        var previous = this.Thumbnail;
        this.Thumbnail = bitmap;
        previous?.Dispose();
    }

    public void Dispose()
    {
        this.Thumbnail?.Dispose();
        this.Thumbnail = null;
    }
}
