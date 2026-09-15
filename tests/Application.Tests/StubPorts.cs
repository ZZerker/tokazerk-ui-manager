using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application.Tests;

internal sealed class StubFontDefinitionStore : IFontDefinitionStore
{
    public FontSettings Stored { get; set; } = FontSettings.Default;
    public FontSettings? LastWritten { get; private set; }
    public bool ThrowNotFoundOnRead { get; set; }

    public Task<FontSettings> ReadAsync(string customPath, CancellationToken ct) =>
        ThrowNotFoundOnRead
            ? throw new FontDefinitionsNotFoundException(new[] { "TokaSmall" })
            : Task.FromResult(Stored);

    public Task WriteAsync(string customPath, FontSettings settings, CancellationToken ct)
    {
        LastWritten = settings;
        Stored = settings;
        return Task.CompletedTask;
    }
}

internal sealed class StubVariantStore : IVariantStore
{
    public List<(Variant Variant, VariantChoice Choice)> Applied { get; } = new();

    public Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct)
    {
        Applied.Add((variant, choice));
        return Task.CompletedTask;
    }
}

internal sealed class StubSettingsRepository : ISettingsRepository
{
    public UiSettings? Stored { get; set; }

    public Task<UiSettings?> LoadAsync(string customPath, CancellationToken ct) => Task.FromResult(Stored);

    public Task SaveAsync(string customPath, UiSettings settings, CancellationToken ct)
    {
        Stored = settings;
        return Task.CompletedTask;
    }
}

internal sealed class StubUiVersionReader : IUiVersionReader
{
    public SemVer? Version { get; set; }

    public Task<SemVer?> ReadAsync(string customPath, CancellationToken ct) => Task.FromResult(Version);
}
