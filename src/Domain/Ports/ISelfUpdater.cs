namespace TokaZerkUIConfig.Domain.Ports;

public interface ISelfUpdater
{
    Task<bool> ApplyAsync(string downloadedFile, CancellationToken ct);

    void CleanupOnStart();
}
