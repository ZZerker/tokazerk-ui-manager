using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class FontsViewModel : SectionViewModel
{
    private const int RENDER_DEBOUNCE_MS = 150;

    private readonly IUiPreviewRenderer previewRenderer;
    private bool loading;
    private string? loadError;
    private string? tierOrderMessage;
    private string? renderError;
    private CancellationTokenSource? renderCts;

    public FontsViewModel(IUiPreviewRenderer previewRenderer)
    {
        this.previewRenderer = previewRenderer;
        this.IsEnabled = false;

        this.Tiers =
        [
            new FontTierRowViewModel(FontTier.Small, "Small", this.OnRowChanged),
            new FontTierRowViewModel(FontTier.Medium, "Medium", this.OnRowChanged),
            new FontTierRowViewModel(FontTier.Large, "Large", this.OnRowChanged),
            new FontTierRowViewModel(FontTier.XLarge, "X-Large", this.OnRowChanged),
            new FontTierRowViewModel(FontTier.ChatSmall, "Chat small", this.OnRowChanged),
            new FontTierRowViewModel(FontTier.ChatLarge, "Chat large", this.OnRowChanged)
        ];
    }

    public override string Title => "Fonts";

    public IReadOnlyList<FontTierRowViewModel> Tiers { get; }

    public IReadOnlyList<PreviewWindow> Windows => PreviewWindows.All;

    [ObservableProperty]
    private PreviewWindow selectedWindow = PreviewWindows.All[0];

    partial void OnSelectedWindowChanged(PreviewWindow value) => this.RequestRender();

    [ObservableProperty]
    private Bitmap? preview;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private bool isRendering;

    [ObservableProperty]
    private string? customPath;

    public FontSettings Current
    {
        get
        {
            var settings = FontSettings.Default;
            foreach (var row in this.Tiers)
            {
                settings = settings.With(row.Tier, row.Px);
            }

            return settings;
        }
    }

    public void Load(string customPath, CurrentState state)
    {
        this.CustomPath = customPath;
        var fonts = state.FontsInXml ?? state.Settings.Fonts;

        this.loading = true;
        foreach (var row in this.Tiers)
        {
            row.SetPx(fonts.Get(row.Tier));
        }

        this.loading = false;

        string?[] loadErrors = [state.SettingsError, state.FontError];
        var loadError = string.Join(Environment.NewLine, loadErrors.Where(error => error is not null));
        this.loadError = loadError.Length == 0 ? null : loadError;
        this.Validate();
        this.RequestRender();
    }

    private void OnRowChanged()
    {
        if (this.loading)
        {
            return;
        }

        this.Validate();
        this.RequestRender();
    }

    private void Validate()
    {
        var warnings = this.Current.Validate();

        foreach (var row in this.Tiers)
        {
            row.Warning = null;
            row.IsError = false;
        }

        this.tierOrderMessage = null;

        foreach (var warning in warnings)
        {
            if (warning.Tier is null)
            {
                this.tierOrderMessage = warning.Message;
                continue;
            }

            var row = this.Tiers.FirstOrDefault(r => r.Tier == warning.Tier);
            if (row is null)
            {
                continue;
            }

            // An error wins over a MayClip warning already set on the same row.
            if (row.Warning is null || (warning.IsError && !row.IsError))
            {
                row.Warning = warning.Message;
                row.IsError = warning.IsError;
            }
        }

        this.RefreshError();
    }

    private void RefreshError()
    {
        string?[] messages = [this.loadError, this.tierOrderMessage, this.renderError];
        var text = string.Join(Environment.NewLine, messages.Where(m => m is not null));
        this.Error = text.Length == 0 ? null : text;
    }

    private void RequestRender()
    {
        this.renderCts?.Cancel();
        this.renderCts?.Dispose();
        var cts = new CancellationTokenSource();
        this.renderCts = cts;
        _ = this.RenderAsync(cts.Token);
    }

    private async Task RenderAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RENDER_DEBOUNCE_MS, ct);

            if (this.CustomPath is null)
            {
                return;
            }

            this.IsRendering = true;

            var image = await this.previewRenderer.RenderAsync(this.CustomPath, this.SelectedWindow.WindowId, this.Current, ct);

            // The renderer finishes a render already under way even after cancellation, so discard a stale result here.
            if (ct.IsCancellationRequested)
            {
                return;
            }

            using var stream = new MemoryStream(image.PngBytes);
            var bitmap = new Bitmap(stream);
            var old = this.Preview;
            this.Preview = bitmap;
            old?.Dispose();

            this.renderError = null;
            this.RefreshError();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                this.renderError = ex.Message;
                this.RefreshError();
            }
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                this.IsRendering = false;
            }
        }
    }
}
