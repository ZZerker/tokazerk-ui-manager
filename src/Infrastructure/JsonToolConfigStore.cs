using System.IO.Abstractions;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class JsonToolConfigStore(IFileSystem fileSystem, LocatorEnvironment environment) : IToolConfigStore
{
    private const string APP_DIRECTORY_NAME = "TokaZerkUIConfig";
    private const string CONFIG_FILE_NAME = "config.json";

    public async Task<ToolConfig> LoadAsync(CancellationToken ct)
    {
        var path = this.TryGetConfigPath();
        if (path is null || !fileSystem.File.Exists(path))
        {
            return ToolConfig.Default;
        }

        try
        {
            await using var stream = fileSystem.File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync(stream, ToolConfigJsonContext.Default.ToolConfig, ct)
                .ConfigureAwait(false);
            if (config is null)
            {
                return ToolConfig.Default;
            }

            return Enum.IsDefined(config.UpdateChannel)
                ? config
                : config with { UpdateChannel = UpdateChannel.Stable };
        }
        catch (JsonException)
        {
            return ToolConfig.Default;
        }
    }

    public async Task SaveAsync(ToolConfig config, CancellationToken ct)
    {
        var path = this.TryGetConfigPath()
            ?? throw new InvalidOperationException("Cannot save tool configuration because the required user configuration directory is unavailable.");
        var directory = fileSystem.Path.GetDirectoryName(path)!;
        var tempPath = fileSystem.Path.Combine(directory, $"{CONFIG_FILE_NAME}.{Guid.NewGuid():N}.tmp");

        ct.ThrowIfCancellationRequested();
        fileSystem.Directory.CreateDirectory(directory);

        try
        {
            await using (var stream = fileSystem.File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, config, ToolConfigJsonContext.Default.ToolConfig, ct)
                    .ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
            fileSystem.File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            this.TryDeleteTemporaryFile(tempPath);
        }
    }

    private string? TryGetConfigPath()
    {
        var basePath = environment.IsWindows ? environment.AppData : environment.Home;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return null;
        }

        return environment.IsWindows
            ? fileSystem.Path.Combine(basePath, APP_DIRECTORY_NAME, CONFIG_FILE_NAME)
            : fileSystem.Path.Combine(basePath, ".config", APP_DIRECTORY_NAME, CONFIG_FILE_NAME);
    }

    private void TryDeleteTemporaryFile(string tempPath)
    {
        try
        {
            if (fileSystem.File.Exists(tempPath))
            {
                fileSystem.File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
