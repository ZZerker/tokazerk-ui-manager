using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TokaZerkUIConfig.Application;

namespace TokaZerkUIConfig.App.ViewModels;

public partial class MainWindowViewModel(InstallViewModel install, MapsViewModel maps, FontsViewModel fonts, WindowsViewModel windows, UpdatesViewModel updates, DetectInstalls detectInstalls) : ObservableObject
{
    [ObservableProperty]
    private SectionViewModel selected = install;

    [ObservableProperty]
    private string? installPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool hasUnsavedChanges;

    public IReadOnlyList<SectionViewModel> Sections { get; } = [install, maps, fonts, windows, updates];

    public async Task InitializeAsync(CancellationToken ct)
    {
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

        await fonts.LoadAsync(this.InstallPath, ct);
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private void Apply()
    {
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private void Reset()
    {
    }
}
