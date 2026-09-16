using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class InstallUi(IReleaseSource releaseSource, IUiArchiveStore archiveStore, IInstalledUiReader installedUiReader, ISettingsRepository settingsRepository)
{
    public async Task<ApplyResult> ExecuteAsync(string customPath, ReleaseInfo release, IProgress<double>? progress, CancellationToken ct)
    {
        var temp = archiveStore.CreateDownloadPath(customPath);
        try
        {
            await releaseSource.DownloadAsync(release.AssetUrl, temp, progress, ct).ConfigureAwait(false);

            archiveStore.Verify(temp, customPath);

            var installed = await installedUiReader.ReadAsync(customPath, ct).ConfigureAwait(false);
            if (installed.Kind != InstalledUiKind.None)
            {
                await archiveStore.ArchiveAsync(customPath, ct).ConfigureAwait(false);
            }

            var existing = await settingsRepository.LoadAsync(customPath, ct).ConfigureAwait(false) ?? UiSettings.Default;

            await archiveStore.InstallAsync(temp, customPath, ct).ConfigureAwait(false);

            await settingsRepository.SaveAsync(customPath, existing, ct).ConfigureAwait(false);

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
        finally
        {
            archiveStore.DiscardDownload(temp);
        }
    }
}
