using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TokaZerkUIConfig.App.ViewModels;

namespace TokaZerkUIConfig.App.Views;

public partial class InstallView : UserControl
{
    public InstallView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (_, _) =>
        {
            if (this.DataContext is InstallViewModel vm)
            {
                vm.PickFolder = this.PickFolderAsync;
            }
        };
    }

    private async Task<string?> PickFolderAsync()
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null)
        {
            return null;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select the Dark Age of Camelot folder",
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
