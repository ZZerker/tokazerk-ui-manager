using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class TokazerkJsonReader(IFileSystem fileSystem) : IInstalledUiReader
{
    private const string MARKER_FILE_NAME = "tokazerk.json";
    private const string TOKAZERK_UI_NAME = "TokaZerkUI";

    public async Task<InstalledUi> ReadAsync(string customPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customPath) || !fileSystem.Directory.Exists(customPath))
        {
            return new InstalledUi(InstalledUiKind.None, null);
        }

        var containsEntries = fileSystem.Directory.EnumerateFileSystemEntries(customPath).Any();
        if (!containsEntries)
        {
            return new InstalledUi(InstalledUiKind.None, null);
        }

        var markerPath = fileSystem.Path.Combine(customPath, MARKER_FILE_NAME);
        if (!fileSystem.File.Exists(markerPath))
        {
            return new InstalledUi(InstalledUiKind.Other, null);
        }

        try
        {
            var json = await fileSystem.File.ReadAllTextAsync(markerPath, ct).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() == TOKAZERK_UI_NAME
                && root.TryGetProperty("version", out var versionElement)
                && versionElement.ValueKind == JsonValueKind.String
                && SemVer.TryParse(versionElement.GetString(), out var version)
                && root.TryGetProperty("built", out var builtElement)
                && builtElement.ValueKind == JsonValueKind.String
                && DateOnly.TryParseExact(
                    builtElement.GetString(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return new InstalledUi(InstalledUiKind.TokaZerk, version);
            }
        }
        catch (JsonException)
        {
        }

        return new InstalledUi(InstalledUiKind.Other, null);
    }
}
