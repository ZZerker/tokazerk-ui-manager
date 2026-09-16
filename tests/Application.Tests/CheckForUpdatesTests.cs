using TokaZerkUIConfig.Domain;
using Xunit;

namespace TokaZerkUIConfig.Application.Tests;

public class CheckForUpdatesTests
{
    [Fact]
    public async Task BetaChannelInToolConfigRequestsPreReleasesWhenCustomPathIsNull()
    {
        var releaseSource = new StubReleaseSource();
        var installedUiReader = new StubInstalledUiReader();
        var toolConfigStore = new StubToolConfigStore { Stored = new ToolConfig(UpdateChannel.Beta) };
        var useCase = new CheckForUpdates(releaseSource, installedUiReader, toolConfigStore);

        await useCase.ExecuteAsync(null, new SemVer(1, 0, 0), "win-x64", CancellationToken.None);

        Assert.Equal(new[] { true, true }, releaseSource.IncludePreReleaseRequests);
    }

    [Fact]
    public async Task UiUpdateAvailableComparesReleaseAgainstInstalledVersion()
    {
        var releaseSource = new StubReleaseSource();
        releaseSource.ReleaseByRepo[ReleaseRepositories.UI_REPO] =
            new ReleaseInfo(new SemVer(1, 0, 12), "v1.0.12", "TokaZerkUI-v1.0.12.zip", "https://example.test/ui.zip", "");
        var installedUiReader = new StubInstalledUiReader { InstalledUi = new InstalledUi(InstalledUiKind.TokaZerk, new SemVer(1, 0, 11)) };
        var toolConfigStore = new StubToolConfigStore();
        var useCase = new CheckForUpdates(releaseSource, installedUiReader, toolConfigStore);

        var check = await useCase.ExecuteAsync("custom", new SemVer(1, 0, 0), "win-x64", CancellationToken.None);

        Assert.True(check.UiUpdateAvailable);

        installedUiReader.InstalledUi = new InstalledUi(InstalledUiKind.TokaZerk, new SemVer(1, 0, 12));
        var equalCheck = await useCase.ExecuteAsync("custom", new SemVer(1, 0, 0), "win-x64", CancellationToken.None);

        Assert.False(equalCheck.UiUpdateAvailable);
    }

    [Fact]
    public async Task ToolUpdateAvailableComparesReleaseAgainstToolVersion()
    {
        var releaseSource = new StubReleaseSource();
        releaseSource.ReleaseByRepo[ReleaseRepositories.TOOL_REPO] =
            new ReleaseInfo(new SemVer(2, 0, 0), "v2.0.0", "TokaZerkUIConfig-win-x64.zip", "https://example.test/tool.zip", "");
        var installedUiReader = new StubInstalledUiReader();
        var toolConfigStore = new StubToolConfigStore();
        var useCase = new CheckForUpdates(releaseSource, installedUiReader, toolConfigStore);

        var check = await useCase.ExecuteAsync("custom", new SemVer(1, 0, 0), "win-x64", CancellationToken.None);

        Assert.True(check.ToolUpdateAvailable);
    }
}
