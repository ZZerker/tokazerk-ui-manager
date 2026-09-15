namespace TokaZerkUIConfig.Domain.Ports;

public interface ISettingsRepository
{
    Task<UiSettings?> LoadAsync(string customPath, CancellationToken ct);

    Task SaveAsync(string customPath, UiSettings settings, CancellationToken ct);
}
