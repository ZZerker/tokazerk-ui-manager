namespace TokaZerkUIConfig.Domain.Ports;

public interface IUiArchiveStore
{
    Task<string> ArchiveAsync(string customPath, CancellationToken ct);
}
