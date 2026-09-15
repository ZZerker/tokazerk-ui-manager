namespace TokaZerkUIConfig.Domain.Ports;

public interface IUiVersionReader
{
    Task<SemVer?> ReadAsync(string customPath, CancellationToken ct);
}
