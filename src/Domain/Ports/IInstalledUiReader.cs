namespace TokaZerkUIConfig.Domain.Ports;

public interface IInstalledUiReader
{
    Task<InstalledUi> ReadAsync(string customPath, CancellationToken ct);
}
