using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class InstallUiTests
{
    private static readonly ReleaseInfo Release = new(new SemVer(1, 0, 13), "v1.0.13", "TokaZerkUI-v1.0.13.zip", "https://example.test/asset.zip", "");

    [Fact]
    public async Task HappyPathDownloadsVerifiesArchivesThenInstallsAndSavesSettings()
    {
        var releaseSource = new StubReleaseSource();
        var archiveStore = new StubUiArchiveStore();
        var installedUiReader = new StubInstalledUiReader { InstalledUi = new InstalledUi(InstalledUiKind.Other, null) };
        var settingsRepository = new StubSettingsRepository { Stored = UiSettings.Default };
        var previewRenderer = new StubPreviewRenderer();
        var useCase = new InstallUi(releaseSource, archiveStore, installedUiReader, settingsRepository, previewRenderer);

        var result = await useCase.ExecuteAsync("custom", Release, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["Download"], releaseSource.Calls);
        Assert.Equal(["Verify", "Archive", "Install", "DiscardDownload"], archiveStore.Calls);
        Assert.Equal(UiSettings.Default, settingsRepository.Stored);
        Assert.Equal(1, previewRenderer.InvalidateCalls);
    }

    [Fact]
    public async Task VerifyFailureFailsWithoutArchivingOrInstalling()
    {
        var releaseSource = new StubReleaseSource();
        var archiveStore = new StubUiArchiveStore { ThrowOnVerify = true };
        var installedUiReader = new StubInstalledUiReader { InstalledUi = new InstalledUi(InstalledUiKind.Other, null) };
        var settingsRepository = new StubSettingsRepository();
        var previewRenderer = new StubPreviewRenderer();
        var useCase = new InstallUi(releaseSource, archiveStore, installedUiReader, settingsRepository, previewRenderer);

        var result = await useCase.ExecuteAsync("custom", Release, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(["Verify", "DiscardDownload"], archiveStore.Calls);
        Assert.Equal(0, previewRenderer.InvalidateCalls);
    }

    [Fact]
    public async Task EmptyCustomFolderSkipsArchiving()
    {
        var releaseSource = new StubReleaseSource();
        var archiveStore = new StubUiArchiveStore();
        var installedUiReader = new StubInstalledUiReader { InstalledUi = new InstalledUi(InstalledUiKind.None, null) };
        var settingsRepository = new StubSettingsRepository();
        var previewRenderer = new StubPreviewRenderer();
        var useCase = new InstallUi(releaseSource, archiveStore, installedUiReader, settingsRepository, previewRenderer);

        var result = await useCase.ExecuteAsync("custom", Release, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["Verify", "Install", "DiscardDownload"], archiveStore.Calls);
    }
}
