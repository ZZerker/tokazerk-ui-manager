namespace TokaZerkUIConfig.Domain.Ports;

public interface IUiArchiveStore
{
    Task<string> ArchiveAsync(string customPath, CancellationToken ct);

    string CreateDownloadPath(string customPath);

    void Verify(string zipPath, string customPath);

    void DiscardDownload(string downloadPath);

    Task InstallAsync(string zipPath, string customPath, CancellationToken ct);
}
