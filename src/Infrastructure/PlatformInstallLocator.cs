using System.IO.Abstractions;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class PlatformInstallLocator(IFileSystem fileSystem, LocatorEnvironment environment) : IInstallLocator
{
    public Task<IReadOnlyList<UiInstall>> DetectAsync(CancellationToken ct)
    {
        var found = new List<UiInstall>();

        this.AddIfValid(found, this.EdenLauncherCandidate(), InstallSource.EdenLauncher);
        this.AddIfValid(found, this.BlackthornLauncherCandidate(), InstallSource.BlackthornLauncher);

        foreach (var prefix in this.WinePrefixCandidates())
        {
            foreach (var suffix in DaocPrefixSuffixSegments)
            {
                var root = fileSystem.Path.Combine(new[] { prefix }.Concat(suffix).ToArray());
                this.AddIfValid(found, root, InstallSource.PrefixScan);
            }
        }

        if (environment.IsWindows)
        {
            if (!string.IsNullOrEmpty(environment.ProgramFilesX86))
            {
                this.AddIfValid(found, fileSystem.Path.Combine(environment.ProgramFilesX86, "Electronic Arts", "Dark Age of Camelot"), InstallSource.WellKnown);
            }

            if (!string.IsNullOrEmpty(environment.ProgramFiles))
            {
                this.AddIfValid(found, fileSystem.Path.Combine(environment.ProgramFiles, "Electronic Arts", "Dark Age of Camelot"), InstallSource.WellKnown);
            }

            this.AddIfValid(found, @"C:\Spiele\Eden DAoC", InstallSource.WellKnown);
        }

        var deduped = found
            .GroupBy(i => this.Normalize(i.GameRoot))
            .Select(g => g.First())
            .ToList();

        return Task.FromResult<IReadOnlyList<UiInstall>>(deduped);
    }

    public UiInstall? Validate(string path)
    {
        var root = this.ResolveRoot(path);
        return this.BuildInstall(root, InstallSource.Manual);
    }

    private static readonly string[][] DaocPrefixSuffixSegments =
    [
            ["drive_c", "Program Files (x86)", "Electronic Arts", "Dark Age of Camelot"],
            ["drive_c", "Program Files", "Electronic Arts", "Dark Age of Camelot"]
    ];

    private void AddIfValid(List<UiInstall> found, string? root, InstallSource source)
    {
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var install = this.BuildInstall(root, source);
        if (install is not null)
        {
            found.Add(install);
        }
    }

    private UiInstall? BuildInstall(string root, InstallSource source)
    {
        // Normalize a launcher-supplied root (which may mix separators, e.g. "C:/Spiele/Blackthorn DAoC\ui\custom")
        // so the stored GameRoot is comparable and displayable in normal form.
        var fullRoot = fileSystem.Path.GetFullPath(root);
        var camelotExe = fileSystem.Path.Combine(fullRoot, "camelot.exe");
        var uiDir = fileSystem.Path.Combine(fullRoot, "ui");
        if (!fileSystem.File.Exists(camelotExe) || !fileSystem.Directory.Exists(uiDir))
        {
            return null;
        }

        var customPath = fileSystem.Path.Combine(uiDir, "custom");
        // Players clone one game folder to make the other server's client, so the launcher config
        // that pointed at this root outranks the DLLs; those only decide when no launcher did.
        var server = source switch
        {
            InstallSource.EdenLauncher => ServerKind.Eden,
            InstallSource.BlackthornLauncher => ServerKind.Blackthorn,
            _ => this.DetectServer(fullRoot)
        };
        return new UiInstall(fullRoot, customPath, source, server);
    }

    private ServerKind DetectServer(string root)
    {
        // A Blackthorn folder is an Eden client plus the bridge, so the bridge is checked first.
        if (fileSystem.File.Exists(fileSystem.Path.Combine(root, "btui_game_bridge.dll")))
        {
            return ServerKind.Blackthorn;
        }

        if (fileSystem.File.Exists(fileSystem.Path.Combine(root, "eden.dll")))
        {
            return ServerKind.Eden;
        }

        return ServerKind.Unknown;
    }

    private string ResolveRoot(string path)
    {
        var trimmed = fileSystem.Path.TrimEndingDirectorySeparator(path);
        var name = fileSystem.Path.GetFileName(trimmed);
        if (!string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uiDir = fileSystem.Path.GetDirectoryName(trimmed);
        if (string.IsNullOrEmpty(uiDir) || !string.Equals(fileSystem.Path.GetFileName(uiDir), "ui", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return fileSystem.Path.GetDirectoryName(uiDir) ?? trimmed;
    }

    private string? EdenConfigPath() =>
            environment.IsWindows
            ? this.CombineIfNotNull(environment.AppData, "eden-launcher", "config.json")
            : this.CombineIfNotNull(environment.Home, ".config", "eden-launcher", "config.json");

    private string? BlackthornConfigPath() =>
            environment.IsWindows
            ? this.CombineIfNotNull(environment.AppData, "bt-launcher", "config.json")
            : this.CombineIfNotNull(environment.Home, ".config", "bt-launcher", "config.json");

    private string? EdenLauncherCandidate() => this.ReadPathKey(this.EdenConfigPath(), "gameDir");

    private string? BlackthornLauncherCandidate() => this.ReadPathKey(this.BlackthornConfigPath(), "gamePath");

    private IEnumerable<string> EdenPrefixCandidates()
    {
        var configPath = this.EdenConfigPath();

        var winePrefix = this.ReadPathKey(configPath, "winePrefix");
        if (winePrefix is not null)
        {
            yield return winePrefix;
        }

        var protonPrefix = this.ReadPathKey(configPath, "protonPrefix");
        if (protonPrefix is not null)
        {
            yield return protonPrefix;
        }

        var protonSteamPath = this.ReadPathKey(configPath, "protonSteamPath");
        if (protonSteamPath is not null)
        {
            yield return fileSystem.Path.Combine(protonSteamPath, "steamapps", "compatdata");
        }
    }

    private string? BlackthornPrefixCandidate() => this.ReadNestedPathKey(this.BlackthornConfigPath(), "uiOptions", "linuxWinePrefix");

    private string? ReadPathKey(string? configPath, string key)
    {
        if (string.IsNullOrEmpty(configPath) || !fileSystem.File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var stream = fileSystem.File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return document.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string? ReadNestedPathKey(string? configPath, string objectKey, string key)
    {
        if (string.IsNullOrEmpty(configPath) || !fileSystem.File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var stream = fileSystem.File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty(objectKey, out var nested) || nested.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return nested.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private IEnumerable<string> WinePrefixCandidates()
    {
        if (environment.IsWindows)
        {
            yield break;
        }

        var prefixes = new List<string>();
        prefixes.AddRange(this.EdenPrefixCandidates());

        var blackthornPrefix = this.BlackthornPrefixCandidate();
        if (blackthornPrefix is not null)
        {
            prefixes.Add(blackthornPrefix);
        }

        if (!string.IsNullOrEmpty(environment.Home))
        {
            prefixes.Add(fileSystem.Path.Combine(environment.Home, ".wine"));
            prefixes.Add(fileSystem.Path.Combine(environment.Home, ".steam", "steam", "steamapps", "compatdata"));
            prefixes.Add(fileSystem.Path.Combine(environment.Home, ".local", "share", "Steam", "steamapps", "compatdata"));
            prefixes.Add(fileSystem.Path.Combine(environment.Home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam", "steamapps", "compatdata"));
        }

        foreach (var expanded in prefixes
                     .Where(p => fileSystem.Directory.Exists(p))
                     .SelectMany(this.ExpandCompatDataPrefixes)
                     .Distinct())
        {
            yield return expanded;
        }
    }

    private IEnumerable<string> ExpandCompatDataPrefixes(string prefix)
    {
        if (!prefix.Contains("compatdata", StringComparison.OrdinalIgnoreCase))
        {
            yield return prefix;
            yield break;
        }

        string[] appDirs;
        try
        {
            appDirs = fileSystem.Directory.GetDirectories(prefix);
        }
        catch (JsonException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var appDir in appDirs)
        {
            var pfx = fileSystem.Path.Combine(appDir, "pfx");
            if (fileSystem.Directory.Exists(pfx))
            {
                yield return pfx;
            }
        }
    }

    private string? CombineIfNotNull(string? basePath, params string[] parts) =>
        string.IsNullOrEmpty(basePath) ? null : fileSystem.Path.Combine(new[] { basePath }.Concat(parts).ToArray());

    private string Normalize(string path)
    {
        try
        {
            return fileSystem.Path.GetFullPath(path).TrimEnd('\\', '/').ToLowerInvariant();
        }
        catch (Exception)
        {
            return path.TrimEnd('\\', '/').ToLowerInvariant();
        }
    }
}
