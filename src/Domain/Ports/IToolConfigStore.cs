namespace TokaZerkUIConfig.Domain.Ports;

public interface IToolConfigStore
{
    Task<ToolConfig> LoadAsync(CancellationToken ct);

    Task SaveAsync(ToolConfig config, CancellationToken ct);
}
