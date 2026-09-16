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

internal sealed class StubReleaseSource : IReleaseSource
{
    public Dictionary<string, ReleaseInfo?> ReleaseByRepo { get; } = [];
    public List<(string Url, string DestinationFile)> Downloads { get; } = [];
    public List<string> Calls { get; } = [];
    public bool IncludePreReleasesRequested { get; private set; }

    public Task<ReleaseInfo?> GetLatestAsync(string owner, string repo, bool includePreReleases, Func<string, bool> assetFilter, CancellationToken ct)
    {
        this.IncludePreReleasesRequested = includePreReleases;
        return Task.FromResult(this.ReleaseByRepo.GetValueOrDefault(repo));
    }

    public Task DownloadAsync(string url, string destinationFile, IProgress<double>? progress, CancellationToken ct)
    {
        this.Downloads.Add((url, destinationFile));
        this.Calls.Add("Download");
        return Task.CompletedTask;
    }
}

internal sealed class StubUiArchiveStore : IUiArchiveStore
{
    public List<string> Calls { get; } = [];
    public bool ThrowOnVerify { get; set; }

    public Task<string> ArchiveAsync(string customPath, CancellationToken ct)
    {
        this.Calls.Add("Archive");
        return Task.FromResult("archive.zip");
    }

    public string CreateDownloadPath(string customPath) => "download.zip";

    public void Verify(string zipPath, string customPath)
    {
        this.Calls.Add("Verify");
        if (this.ThrowOnVerify)
        {
            throw new InvalidDataException("Invalid archive");
        }
    }

    public void DiscardDownload(string downloadPath)
    {
        this.Calls.Add("DiscardDownload");
    }

    public Task InstallAsync(string zipPath, string customPath, CancellationToken ct)
    {
        this.Calls.Add("Install");
        return Task.CompletedTask;
    }
}

internal sealed class StubBackupStore : IBackupStore
{
    public bool Cleared { get; private set; }

    public Task BackupOnceAsync(string customPath, string relativePath, CancellationToken ct) => Task.CompletedTask;

    public Task<bool> RestoreAsync(string customPath, string relativePath, IReadOnlyList<string> preserve, CancellationToken ct) => Task.FromResult(false);

    public Task ClearAsync(string customPath, CancellationToken ct)
    {
        this.Cleared = true;
        return Task.CompletedTask;
    }
}
