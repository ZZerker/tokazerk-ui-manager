using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class MapsViewModel : SectionViewModel, IDisposable
{
    private const string THUMBNAIL_FILE_NAME = "r001.dds";

    private readonly IMapThumbnailSource thumbnailSource;
    private readonly Variant mapVariant;
    private bool isLoadingSelection;

    public MapsViewModel(IMapThumbnailSource thumbnailSource)
    {
        this.thumbnailSource = thumbnailSource;
        this.IsEnabled = false;
        this.mapVariant = VariantTable.Get(VariantKind.MapSize);
        this.Choices = this.mapVariant.Choices
            .Select(choice => new VariantChoiceRowViewModel(choice.Id, choice.Label, this.OnChoiceSelected))
            .ToArray();
    }

    public override string Title => "Maps";

    public IReadOnlyList<VariantChoiceRowViewModel> Choices { get; }

    public event EventHandler? SelectionChanged;

    [ObservableProperty]
    private string selectedChoiceId = VariantChoice.DEFAULT_ID;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private bool isLoading;

    public async Task LoadAsync(string customPath, CurrentState state, CancellationToken ct)
    {
        this.SelectChoice(state.Settings.Variants.Get(VariantKind.MapSize));
        this.Error = state.SettingsError;

        this.IsLoading = true;
        try
        {
            await Task.WhenAll(this.Choices.Select(choice => this.LoadThumbnailAsync(customPath, choice, ct)));
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    public void Dispose()
    {
        foreach (var choice in this.Choices)
        {
            choice.Dispose();
        }
    }

    private async Task LoadThumbnailAsync(string customPath, VariantChoiceRowViewModel row, CancellationToken ct)
    {
        row.Error = null;

        var choice = this.mapVariant.Choices.Single(item => item.Id == row.Id);
        var sourceDirectory = choice.SourcePath ?? this.mapVariant.TargetPath;
        var ddsPath = Path.Combine(customPath, sourceDirectory, THUMBNAIL_FILE_NAME);

        try
        {
            var pngBytes = await this.thumbnailSource.LoadPngAsync(ddsPath, ct);
            using var stream = new MemoryStream(pngBytes, writable: false);
            var bitmap = new Bitmap(stream);
            row.ReplaceThumbnail(bitmap);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            row.ReplaceThumbnail(null);
            row.Error = ex.Message;
        }
    }

    private void OnChoiceSelected(VariantChoiceRowViewModel selected)
    {
        var changed = this.SelectedChoiceId != selected.Id;
        this.SelectedChoiceId = selected.Id;
        foreach (var choice in this.Choices)
        {
            choice.IsSelected = ReferenceEquals(choice, selected);
        }

        if (changed && !this.isLoadingSelection)
        {
            this.SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SelectChoice(string choiceId)
    {
        var selected = this.Choices.FirstOrDefault(choice => choice.Id == choiceId)
            ?? this.Choices.Single(choice => choice.Id == VariantChoice.DEFAULT_ID);

        this.isLoadingSelection = true;
        try
        {
            selected.IsSelected = true;
        }
        finally
        {
            this.isLoadingSelection = false;
        }
    }

}
