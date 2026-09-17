using CommunityToolkit.Mvvm.ComponentModel;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App.ViewModels;

public partial class UpdatesViewModel : SectionViewModel
{
    private bool isLoadingToolConfig;

    public override string Title => "Updates";

    public event EventHandler? UpdateChannelChanged;

    [ObservableProperty]
    private bool includeBetaReleases;

    [ObservableProperty]
    private bool isToolConfigLoaded;

    [ObservableProperty]
    private string? configError;

    partial void OnIncludeBetaReleasesChanged(bool value)
    {
        if (!this.isLoadingToolConfig)
        {
            this.UpdateChannelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void LoadToolConfig(ToolConfig config)
    {
        this.isLoadingToolConfig = true;
        this.IncludeBetaReleases = config.UpdateChannel == UpdateChannel.Beta;
        this.isLoadingToolConfig = false;
    }
}
