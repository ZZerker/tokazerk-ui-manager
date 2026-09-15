namespace TokaZerkUIConfig.Domain.Ports;

public interface IVariantStore
{
    Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct);
}
