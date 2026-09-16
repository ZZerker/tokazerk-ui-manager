using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class CheckForUpdates(IReleaseSource releaseSource, IInstalledUiReader installedUiReader, ISettingsRepository settingsRepository)
{
    public async Task<UpdateCheck> ExecuteAsync(string? customPath, SemVer toolVersion, string rid, CancellationToken ct)
    {
        var channel = customPath is null
            ? UpdateChannel.Stable
            : (await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default).UpdateChannel;
        var includePreReleases = channel == UpdateChannel.Beta;

        SemVer? installedUiVersion = null;
        if (customPath is not null)
        {
            var installedUi = await installedUiReader.ReadAsync(customPath, ct).ConfigureAwait(false);
            if (installedUi.Kind == InstalledUiKind.TokaZerk)
            {
                installedUiVersion = installedUi.Version;
            }
        }

        var uiRelease = await releaseSource.GetLatestAsync(
                ReleaseRepositories.UI_OWNER,
                ReleaseRepositories.UI_REPO,
                includePreReleases,
                ReleaseRepositories.IsUiAsset,
                ct)
            .ConfigureAwait(false);

        var toolRelease = await releaseSource.GetLatestAsync(
                ReleaseRepositories.TOOL_OWNER,
                ReleaseRepositories.TOOL_REPO,
                includePreReleases,
                name => ReleaseRepositories.IsToolAsset(name, rid),
                ct)
            .ConfigureAwait(false);

        return new UpdateCheck(uiRelease, installedUiVersion, toolRelease, toolVersion);
    }
}
