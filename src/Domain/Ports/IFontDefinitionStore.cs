namespace TokaZerkUIConfig.Domain.Ports;

public interface IFontDefinitionStore
{
    Task<FontSettings> ReadAsync(string customPath, CancellationToken ct);

    Task WriteAsync(string customPath, FontSettings settings, CancellationToken ct);
}

public sealed class FontDefinitionsNotFoundException : Exception
{
    public FontDefinitionsNotFoundException(IReadOnlyList<string> missingNames)
        : base($"Missing font definitions: {string.Join(", ", missingNames)}")
    {
        MissingNames = missingNames;
    }

    public IReadOnlyList<string> MissingNames { get; }
}
