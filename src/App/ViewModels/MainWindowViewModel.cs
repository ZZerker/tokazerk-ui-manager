using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TokaZerkUIConfig.App.ViewModels;

public partial class MainWindowViewModel(InstallViewModel install, MapsViewModel maps, FontsViewModel fonts, WindowsViewModel windows, UpdatesViewModel updates) : ObservableObject
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

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private void Apply()
    {
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))]
    private void Reset()
    {
    }
}
