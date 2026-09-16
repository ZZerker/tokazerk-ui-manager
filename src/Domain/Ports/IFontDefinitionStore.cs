namespace TokaZerkUIConfig.Domain.Ports;

public interface IFontDefinitionStore
{
    Task<FontSettings> ReadAsync(string customPath, CancellationToken ct);

    Task WriteAsync(string customPath, FontSettings settings, CancellationToken ct);
}

public sealed class FontDefinitionsNotFoundException(IReadOnlyList<string> missingNames) : Exception($"Missing font definitions: {string.Join(", ", missingNames)}")
{
    public IReadOnlyList<string> MissingNames { get; } = missingNames;
}
