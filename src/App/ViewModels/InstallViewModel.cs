using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TokaZerkUIConfig.Application;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.App.ViewModels;

public sealed partial class InstallViewModel(
    DetectInstalls detectInstalls,
    ValidateInstall validateInstall,
    CheckForUpdates checkForUpdates,
    InstallUi installUi) : SectionViewModel
{
    public override string Title => "Install";

    public ObservableCollection<UiInstall> Installs { get; } = [];

    public event EventHandler? InstallSelected;

    public event EventHandler? Installed;

    // Set by the view; the browse command needs a TopLevel the view model must not know about.
    public Func<Task<string?>>? PickFolder { get; set; }

    // Windows paths are case-insensitive, Linux paths are not.
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private UiInstall? selectedInstall;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string installedUiText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool showInstallOffer;

    [ObservableProperty]
    private bool showZipNotice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool isInstalling;

    [ObservableProperty]
    private double installProgress;

    partial void OnSelectedInstallChanged(UiInstall? value)
    {
        if (value is not null)
        {
            this.InstallSelected?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task DetectAsync(CancellationToken ct)
    {
        this.Installs.Clear();

        try
        {
            var installs = await detectInstalls.ExecuteAsync(ct);
            foreach (var install in installs)
            {
                this.Installs.Add(install);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.Error = ex.Message;
        }

        this.SelectedInstall = this.Installs.Count > 0 ? this.Installs[0] : null;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (this.PickFolder is null)
        {
            return;
        }

        var path = await this.PickFolder();
        if (path is null)
        {
            return;
        }

        var install = validateInstall.Execute(path);
        if (install is null)
        {
            this.Error = "No Dark Age of Camelot install found: the folder needs camelot.exe and a ui folder";
            return;
        }

        this.Error = null;

        var existing = this.Installs.FirstOrDefault(
            i => string.Equals(i.GameRoot, install.GameRoot, PathComparison));
        if (existing is not null)
        {
            this.Installs[this.Installs.IndexOf(existing)] = install;
            this.SelectedInstall = install;
            return;
        }

        this.Installs.Add(install);
        this.SelectedInstall = install;
    }

    private bool CanInstall() => this.ShowInstallOffer && !this.IsInstalling && this.SelectedInstall is not null;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync(CancellationToken ct)
    {
        this.Error = null;
        this.IsInstalling = true;
        try
        {
            var customPath = this.SelectedInstall!.CustomPath;
            var check = await checkForUpdates.ExecuteAsync(customPath, AppVersion.Current, AppVersion.Rid, ct);
            if (check.UiRelease is null)
            {
                this.Error = "No TokaZerk UI release found on GitHub";
                return;
            }

            // Progress<T> marshals to the UI thread's SynchronizationContext, which is what we want here.
            var progress = new Progress<double>(p => this.InstallProgress = p);
            var result = await installUi.ExecuteAsync(customPath, check.UiRelease, progress, ct);
            if (!result.Success)
            {
                this.Error = result.Error;
            }
            else
            {
                this.Installed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.Error = ex.Message;
        }
        finally
        {
            this.IsInstalling = false;
            this.InstallProgress = 0;
        }
    }

    public void Load(CurrentState state)
    {
        if (state.IdentityError is not null)
        {
            this.InstalledUiText = state.IdentityError;
            this.ShowInstallOffer = false;
            this.ShowZipNotice = false;
            return;
        }

        switch (state.InstalledUi.Kind)
        {
            case InstalledUiKind.TokaZerk:
                this.InstalledUiText = $"TokaZerk UI {state.InstalledUi.Version}";
                this.ShowInstallOffer = false;
                this.ShowZipNotice = false;
                break;
            case InstalledUiKind.Other:
                this.InstalledUiText = "Another UI is installed";
                this.ShowInstallOffer = true;
                this.ShowZipNotice = true;
                break;
            case InstalledUiKind.None:
            default:
                this.InstalledUiText = "No custom UI is installed";
                this.ShowInstallOffer = true;
                this.ShowZipNotice = false;
                break;
        }
    }
}
