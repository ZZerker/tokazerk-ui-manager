using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application;

public sealed class UpdateSelf(IReleaseSource releaseSource, ISelfUpdater selfUpdater)
{
    public async Task<ApplyResult> ExecuteAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken ct)
    {
        try
        {
            var downloadedFile = selfUpdater.DownloadPath;
            await releaseSource.DownloadAsync(release.AssetUrl, downloadedFile, progress, ct).ConfigureAwait(false);

            var applied = await selfUpdater.ApplyAsync(downloadedFile, ct).ConfigureAwait(false);
            if (!applied)
            {
                return ApplyResult.Fail("Could not replace the running executable");
            }

            return ApplyResult.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApplyResult.Fail(ex.Message);
        }
    }
}
