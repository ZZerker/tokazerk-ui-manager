using System.IO.Abstractions;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class JsonSettingsRepository(IFileSystem fileSystem) : ISettingsRepository
{
    public async Task<UiSettings?> LoadAsync(string customPath, CancellationToken ct)
    {
        var path = this.SettingsPath(customPath);
        if (!fileSystem.File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = fileSystem.File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync(stream, SettingsJsonContext.Default.UiSettings, ct)
                .ConfigureAwait(false);

            if (settings is null || settings.Fonts is null || settings.Variants is null)
            {
                return null;
            }

            var normalized = settings.Variants;
            foreach (var variant in VariantTable.All)
            {
                var choiceId = normalized.Get(variant.Kind);
                if (variant.Choices.All(c => c.Id != choiceId))
                {
                    normalized = normalized.With(variant.Kind, VariantChoice.DEFAULT_ID);
                }
            }

            return settings with { Variants = normalized };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(string customPath, UiSettings settings, CancellationToken ct)
    {
        var path = this.SettingsPath(customPath);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);

        var tempPath = path + ".tmp";
        await using (var stream = fileSystem.File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, SettingsJsonContext.Default.UiSettings, ct)
                .ConfigureAwait(false);
        }

        fileSystem.File.Move(tempPath, path, overwrite: true);
    }

    private string SettingsPath(string customPath) => fileSystem.Path.Combine(customPath, ConfigPaths.SETTINGS_FILE);
}
