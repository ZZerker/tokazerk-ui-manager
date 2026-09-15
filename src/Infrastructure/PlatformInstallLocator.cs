using System.IO.Abstractions;
using System.Text.Json;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class PlatformInstallLocator : IInstallLocator
{
    private readonly IFileSystem _fileSystem;
    private readonly LocatorEnvironment _environment;

    public PlatformInstallLocator(IFileSystem fileSystem, LocatorEnvironment environment)
    {
        _fileSystem = fileSystem;
        _environment = environment;
    }

    public Task<IReadOnlyList<UiInstall>> DetectAsync(CancellationToken ct)
    {
        var found = new List<UiInstall>();

        AddIfValid(found, EdenLauncherCandidate(), InstallSource.EdenLauncher);
        AddIfValid(found, BlackthornLauncherCandidate(), InstallSource.BlackthornLauncher);

        foreach (var prefix in WinePrefixCandidates())
        {
            foreach (var suffix in DaocPrefixSuffixSegments)
            {
                var root = _fileSystem.Path.Combine(new[] { prefix }.Concat(suffix).ToArray());
                AddIfValid(found, root, InstallSource.PrefixScan);
            }
        }

        if (_environment.IsWindows)
        {
            if (!string.IsNullOrEmpty(_environment.ProgramFilesX86))
            {
                AddIfValid(found, _fileSystem.Path.Combine(_environment.ProgramFilesX86, "Electronic Arts", "Dark Age of Camelot"), InstallSource.WellKnown);
            }

            if (!string.IsNullOrEmpty(_environment.ProgramFiles))
            {
                AddIfValid(found, _fileSystem.Path.Combine(_environment.ProgramFiles, "Electronic Arts", "Dark Age of Camelot"), InstallSource.WellKnown);
            }

            AddIfValid(found, @"C:\Spiele\Eden DAoC", InstallSource.WellKnown);
        }

        var deduped = found
            .GroupBy(i => Normalize(i.GameRoot))
            .Select(g => g.First())
            .ToList();

        return Task.FromResult<IReadOnlyList<UiInstall>>(deduped);
    }

    public UiInstall? Validate(string path)
    {
        var root = ResolveRoot(path);
        return BuildInstall(root, InstallSource.Manual);
    }

    private static readonly string[][] DaocPrefixSuffixSegments =
    {
        new[] { "drive_c", "Program Files (x86)", "Electronic Arts", "Dark Age of Camelot" },
        new[] { "drive_c", "Program Files", "Electronic Arts", "Dark Age of Camelot" },
    };

    private void AddIfValid(List<UiInstall> found, string? root, InstallSource source)
    {
        if (string.IsNullOrEmpty(root))
        {
            return;
        }

        var install = BuildInstall(root, source);
        if (install is not null)
        {
            found.Add(install);
        }
    }

    private UiInstall? BuildInstall(string root, InstallSource source)
    {
        var camelotExe = _fileSystem.Path.Combine(root, "camelot.exe");
        var uiDir = _fileSystem.Path.Combine(root, "ui");
        if (!_fileSystem.File.Exists(camelotExe) || !_fileSystem.Directory.Exists(uiDir))
        {
            return null;
        }

        var customPath = _fileSystem.Path.Combine(uiDir, "custom");
        var server = DetectServer(root);
        return new UiInstall(root, customPath, source, server);
    }

    private ServerKind DetectServer(string root)
    {
        if (_fileSystem.File.Exists(_fileSystem.Path.Combine(root, "eden.dll")))
        {
            return ServerKind.Eden;
        }

        if (_fileSystem.File.Exists(_fileSystem.Path.Combine(root, "btui_game_bridge.dll")))
        {
            return ServerKind.Blackthorn;
        }

        return ServerKind.Unknown;
    }

    private string ResolveRoot(string path)
    {
        var trimmed = _fileSystem.Path.TrimEndingDirectorySeparator(path);
        var name = _fileSystem.Path.GetFileName(trimmed);
        if (!string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uiDir = _fileSystem.Path.GetDirectoryName(trimmed);
        if (string.IsNullOrEmpty(uiDir) || !string.Equals(_fileSystem.Path.GetFileName(uiDir), "ui", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return _fileSystem.Path.GetDirectoryName(uiDir) ?? trimmed;
    }

    private string? EdenConfigPath() =>
        _environment.IsWindows
            ? CombineIfNotNull(_environment.AppData, "eden-launcher", "config.json")
            : CombineIfNotNull(_environment.Home, ".config", "eden-launcher", "config.json");

    private string? BlackthornConfigPath() =>
        _environment.IsWindows
            ? CombineIfNotNull(_environment.AppData, "bt-launcher", "config.json")
            : CombineIfNotNull(_environment.Home, ".config", "bt-launcher", "config.json");

    private string? EdenLauncherCandidate() => ReadPathKey(EdenConfigPath(), "gameDir");

    private string? BlackthornLauncherCandidate() => ReadPathKey(BlackthornConfigPath(), "gamePath");

    private IEnumerable<string> EdenPrefixCandidates()
    {
        var configPath = EdenConfigPath();

        var winePrefix = ReadPathKey(configPath, "winePrefix");
        if (winePrefix is not null)
        {
            yield return winePrefix;
        }

        var protonPrefix = ReadPathKey(configPath, "protonPrefix");
        if (protonPrefix is not null)
        {
            yield return protonPrefix;
        }

        var protonSteamPath = ReadPathKey(configPath, "protonSteamPath");
        if (protonSteamPath is not null)
        {
            yield return _fileSystem.Path.Combine(protonSteamPath, "steamapps", "compatdata");
        }
    }

    private string? BlackthornPrefixCandidate() =>
        ReadNestedPathKey(BlackthornConfigPath(), "uiOptions", "linuxWinePrefix");

    private string? ReadPathKey(string? configPath, string key)
    {
        if (string.IsNullOrEmpty(configPath) || !_fileSystem.File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(configPath);
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
        if (string.IsNullOrEmpty(configPath) || !_fileSystem.File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(configPath);
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
        if (_environment.IsWindows)
        {
            yield break;
        }

        var prefixes = new List<string>();
        prefixes.AddRange(EdenPrefixCandidates());

        var blackthornPrefix = BlackthornPrefixCandidate();
        if (blackthornPrefix is not null)
        {
            prefixes.Add(blackthornPrefix);
        }

        if (!string.IsNullOrEmpty(_environment.Home))
        {
            prefixes.Add(_fileSystem.Path.Combine(_environment.Home, ".wine"));
            prefixes.Add(_fileSystem.Path.Combine(_environment.Home, ".steam", "steam", "steamapps", "compatdata"));
            prefixes.Add(_fileSystem.Path.Combine(_environment.Home, ".local", "share", "Steam", "steamapps", "compatdata"));
            prefixes.Add(_fileSystem.Path.Combine(_environment.Home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam", "steamapps", "compatdata"));
        }

        foreach (var expanded in prefixes
                     .Where(p => _fileSystem.Directory.Exists(p))
                     .SelectMany(ExpandCompatDataPrefixes)
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
            appDirs = _fileSystem.Directory.GetDirectories(prefix);
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
            var pfx = _fileSystem.Path.Combine(appDir, "pfx");
            if (_fileSystem.Directory.Exists(pfx))
            {
                yield return pfx;
            }
        }
    }

    private string? CombineIfNotNull(string? basePath, params string[] parts) =>
        string.IsNullOrEmpty(basePath) ? null : _fileSystem.Path.Combine(new[] { basePath }.Concat(parts).ToArray());

    private string Normalize(string path)
    {
        try
        {
            return _fileSystem.Path.GetFullPath(path).TrimEnd('\\', '/').ToLowerInvariant();
        }
        catch (Exception)
        {
            return path.TrimEnd('\\', '/').ToLowerInvariant();
        }
    }
}
