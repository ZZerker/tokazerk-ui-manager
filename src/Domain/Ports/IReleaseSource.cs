namespace TokaZerkUIConfig.Domain.Ports;

public interface IReleaseSource
{
    Task<ReleaseInfo?> GetLatestAsync(string owner, string repo, bool includePreReleases, Func<string, bool> assetFilter, CancellationToken ct);

    Task DownloadAsync(string url, string destinationFile, IProgress<double>? progress, CancellationToken ct);
}
