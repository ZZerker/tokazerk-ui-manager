namespace TokaZerkUIConfig.Domain.Ports;

public interface IMapThumbnailSource
{
    Task<byte[]> LoadPngAsync(string ddsPath, CancellationToken ct);
}
