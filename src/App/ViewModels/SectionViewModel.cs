using CommunityToolkit.Mvvm.ComponentModel;

namespace TokaZerkUIConfig.App.ViewModels;

public abstract partial class SectionViewModel : ObservableObject
{
    public abstract string Title { get; }
}
