using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TokaZerkUIConfig.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private SectionViewModel selected;

    [ObservableProperty]
    private string? installPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool hasUnsavedChanges;

    public IReadOnlyList<SectionViewModel> Sections { get; }

    public MainWindowViewModel(
        InstallViewModel install,
        MapsViewModel maps,
        FontsViewModel fonts,
        WindowsViewModel windows,
        UpdatesViewModel updates)
    {
        Sections = new SectionViewModel[] { install, maps, fonts, windows, updates };
        selected = install;
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
