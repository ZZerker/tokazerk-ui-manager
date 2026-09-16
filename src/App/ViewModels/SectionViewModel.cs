using CommunityToolkit.Mvvm.ComponentModel;

namespace TokaZerkUIConfig.App.ViewModels;

public abstract partial class SectionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isEnabled = true;

    public abstract string Title { get; }
}
