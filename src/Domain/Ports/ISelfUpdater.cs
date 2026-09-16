namespace TokaZerkUIConfig.Domain.Ports;

public interface ISelfUpdater
{
    string DownloadPath { get; }

    Task<bool> ApplyAsync(string downloadedFile, CancellationToken ct);

    void CleanupOnStart();
}
