using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App.ViewModels;

public partial class MainWindowViewModel(
    InstallViewModel install,
    MapsViewModel maps,
    FontsViewModel fonts,
    WindowsViewModel windows,
    UpdatesViewModel updates,
    DetectInstalls detectInstalls,
    LoadCurrentState loadCurrentState,
    ApplyVariant applyVariant) : ObservableObject
{
    private int selectionRevision;
    private bool selectionEventsSubscribed;

    [ObservableProperty]
    private SectionViewModel selected = install;

    [ObservableProperty]
    private string? installPath;

    [ObservableProperty]
    private string statusText = "No custom UI is installed";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool hasUnsavedChanges;

    public IReadOnlyList<SectionViewModel> Sections { get; } = [install, maps, fonts, windows, updates];

    public async Task InitializeAsync(CancellationToken ct)
    {
        this.SubscribeToSelectionChanges();

        // SHORTCUT: the first detected install is used until the Install section (step 10) lets the user pick one.
        try
        {
            var installs = await detectInstalls.ExecuteAsync(ct);
            this.InstallPath = installs.Count > 0 ? installs[0].CustomPath : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.InstallPath = null;
        }

        await this.RefreshSectionsAsync(ct);
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private async Task ApplyAsync(CancellationToken ct)
    {
        if (this.InstallPath is null || !maps.IsEnabled)
        {
            return;
        }

        var appliedSelectionRevision = this.selectionRevision;
        var mapChoiceId = maps.SelectedChoiceId;
        var targetWindowChoiceId = windows.SelectedTargetWindowId;
        var floatTargetChoiceId = windows.SelectedFloatTargetId;

        var mapResult = await applyVariant.ExecuteAsync(
            this.InstallPath,
            VariantKind.MapSize,
            mapChoiceId,
            ct);
        var targetWindowResult = await applyVariant.ExecuteAsync(
            this.InstallPath,
            VariantKind.TargetWindow,
            targetWindowChoiceId,
            ct);
        var floatTargetResult = await applyVariant.ExecuteAsync(
            this.InstallPath,
            VariantKind.FloatTarget,
            floatTargetChoiceId,
            ct);

        maps.Error = mapResult.Success ? null : mapResult.Error ?? "Could not apply the map size";

        var windowErrors = new[] { targetWindowResult, floatTargetResult }
            .Where(result => !result.Success)
            .Select(result => result.Error ?? "Could not apply the window selection");
        var windowError = string.Join(Environment.NewLine, windowErrors);
        windows.Error = windowError.Length == 0 ? null : windowError;

        this.HasUnsavedChanges = appliedSelectionRevision != this.selectionRevision
            || !mapResult.Success
            || !targetWindowResult.Success
            || !floatTargetResult.Success;
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private async Task ResetAsync(CancellationToken ct)
    {
        var resetSelectionRevision = this.selectionRevision;
        await this.RefreshSectionsAsync(ct);
        if (maps.IsEnabled && resetSelectionRevision == this.selectionRevision)
        {
            this.HasUnsavedChanges = false;
        }
    }

    private void SubscribeToSelectionChanges()
    {
        if (this.selectionEventsSubscribed)
        {
            return;
        }

        maps.SelectionChanged += this.OnSelectionChanged;
        windows.SelectionChanged += this.OnSelectionChanged;
        this.selectionEventsSubscribed = true;
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        this.selectionRevision++;
        this.HasUnsavedChanges = true;
    }

    private async Task RefreshSectionsAsync(CancellationToken ct)
    {
        var customPath = this.InstallPath;
        if (customPath is null)
        {
            this.DisableConfigurationSections("No custom UI is installed");
            return;
        }

        var state = await loadCurrentState.ExecuteAsync(customPath, ct);
        if (state.IdentityError is not null)
        {
            this.DisableConfigurationSections(state.IdentityError);
            return;
        }

        if (state.InstalledUi.Kind == InstalledUiKind.Other)
        {
            this.DisableConfigurationSections("Another UI is installed");
            return;
        }

        if (state.InstalledUi.Kind == InstalledUiKind.None)
        {
            this.DisableConfigurationSections("No custom UI is installed");
            return;
        }

        maps.IsEnabled = true;
        fonts.IsEnabled = true;
        windows.IsEnabled = true;
        this.StatusText = customPath;

        await maps.LoadAsync(customPath, state, ct);
        fonts.Load(customPath, state);
        windows.Load(state);
    }

    private void DisableConfigurationSections(string statusText)
    {
        maps.IsEnabled = false;
        fonts.IsEnabled = false;
        windows.IsEnabled = false;
        this.HasUnsavedChanges = false;
        this.Selected = install;
        this.StatusText = statusText;
    }
}
