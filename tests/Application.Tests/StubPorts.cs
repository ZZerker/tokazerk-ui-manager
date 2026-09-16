using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Application.Tests;

internal sealed class StubFontDefinitionStore : IFontDefinitionStore
{
    public FontSettings Stored { get; set; } = FontSettings.Default;
    public FontSettings? LastWritten { get; private set; }
    public bool ThrowNotFoundOnRead { get; set; }

    public Task<FontSettings> ReadAsync(string customPath, CancellationToken ct) =>
            this.ThrowNotFoundOnRead
            ? throw new FontDefinitionsNotFoundException(["TokaSmall"])
            : Task.FromResult(this.Stored);

    public Task WriteAsync(string customPath, FontSettings settings, CancellationToken ct)
    {
        this.LastWritten = settings;
        this.Stored = settings;
        return Task.CompletedTask;
    }
}

internal sealed class StubVariantStore : IVariantStore
{
    public List<(Variant Variant, VariantChoice Choice)> Applied { get; } = [];

    public Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct)
    {
        this.Applied.Add((variant, choice));
        return Task.CompletedTask;
    }
}

internal sealed class StubSettingsRepository : ISettingsRepository
{
    public UiSettings? Stored { get; set; }
    public Exception? LoadException { get; set; }

    public Task<UiSettings?> LoadAsync(string customPath, CancellationToken ct)
    {
        if (this.LoadException is not null)
        {
            throw this.LoadException;
        }

        return Task.FromResult(this.Stored);
    }

    public Task SaveAsync(string customPath, UiSettings settings, CancellationToken ct)
    {
        this.Stored = settings;
        return Task.CompletedTask;
    }
}

internal sealed class StubInstalledUiReader : IInstalledUiReader
{
    public InstalledUi InstalledUi { get; set; } = new(InstalledUiKind.TokaZerk, new SemVer(1, 0, 12));

    public Task<InstalledUi> ReadAsync(string customPath, CancellationToken ct) => Task.FromResult(this.InstalledUi);
}
