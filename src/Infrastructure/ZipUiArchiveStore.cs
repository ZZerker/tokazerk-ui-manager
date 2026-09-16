using System.Globalization;
using System.IO.Abstractions;
using System.IO.Compression;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class ZipUiArchiveStore(IFileSystem fileSystem, IInstalledUiReader installedUiReader) : IUiArchiveStore
{
    public async Task<string> ArchiveAsync(string customPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customPath))
        {
            throw new ArgumentException("The custom UI path is required", nameof(customPath));
        }

        var normalizedCustomPath = fileSystem.Path.GetFullPath(customPath).TrimEnd('\\', '/');
        if (!fileSystem.Directory.Exists(normalizedCustomPath))
        {
            throw new DirectoryNotFoundException($"Custom UI directory not found: {normalizedCustomPath}");
        }

        this.ValidateArchiveTree(normalizedCustomPath, ct);

        var installedUi = await installedUiReader.ReadAsync(normalizedCustomPath, ct).ConfigureAwait(false);
        var uiPath = fileSystem.Path.GetDirectoryName(normalizedCustomPath)
            ?? throw new ArgumentException("The custom UI path must have a parent directory", nameof(customPath));
        var backupsPath = fileSystem.Path.Combine(uiPath, "backups");
        fileSystem.Directory.CreateDirectory(backupsPath);

        var suffix = installedUi is { Kind: InstalledUiKind.TokaZerk, Version: not null }
            ? installedUi.Version.Value.ToString()
            : DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var baseName = $"custom-{installedUi.Kind.ToString().ToLowerInvariant()}-{suffix}";
        var temporaryPath = fileSystem.Path.Combine(backupsPath, $".{Guid.NewGuid():N}.tmp");

        try
        {
            ct.ThrowIfCancellationRequested();
            await ZipFile.CreateFromDirectoryAsync(
                    normalizedCustomPath,
                    temporaryPath,
                    CompressionLevel.Optimal,
                    includeBaseDirectory: false,
                    ct)
                .ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            using (var archive = ZipFile.OpenRead(temporaryPath))
            {
                if (!archive.Entries.Any(entry => !string.IsNullOrEmpty(entry.Name)))
                {
                    throw new InvalidDataException("The UI archive contains no files");
                }
            }

            ct.ThrowIfCancellationRequested();

            var collision = 1;
            while (true)
            {
                var fileName = collision == 1 ? $"{baseName}.zip" : $"{baseName}-{collision}.zip";
                var archivePath = fileSystem.Path.Combine(backupsPath, fileName);
                if (fileSystem.File.Exists(archivePath) || fileSystem.Directory.Exists(archivePath))
                {
                    collision++;
                    continue;
                }

                try
                {
                    fileSystem.File.Move(temporaryPath, archivePath);
                    return archivePath;
                }
                catch (IOException) when (fileSystem.File.Exists(archivePath) || fileSystem.Directory.Exists(archivePath))
                {
                    collision++;
                }
            }
        }
        catch
        {
            if (fileSystem.File.Exists(temporaryPath))
            {
                fileSystem.File.Delete(temporaryPath);
            }

            throw;
        }
    }

    private const string CUSTOM_PREFIX = "custom/";

    public string CreateDownloadPath(string customPath)
    {
        var uiPath = fileSystem.Path.GetDirectoryName(fileSystem.Path.GetFullPath(customPath).TrimEnd('\\', '/'))
            ?? throw new ArgumentException("The custom UI path must have a parent directory", nameof(customPath));
        var backupsPath = fileSystem.Path.Combine(uiPath, "backups");
        fileSystem.Directory.CreateDirectory(backupsPath);
        return fileSystem.Path.Combine(backupsPath, $".download-{Guid.NewGuid():N}.zip");
    }

    public void Verify(string zipPath, string customPath)
    {
        var normalizedCustomPath = fileSystem.Path.GetFullPath(customPath).TrimEnd('\\', '/');

        using var archive = ZipFile.OpenRead(zipPath);
        var hasAnyEntry = false;
        var hasCustomRoot = true;
        var hasSettingsFile = false;
        foreach (var entry in archive.Entries)
        {
            hasAnyEntry = true;
            if (!entry.FullName.StartsWith(CUSTOM_PREFIX, StringComparison.Ordinal))
            {
                hasCustomRoot = false;
                continue;
            }

            if (entry.FullName == $"{CUSTOM_PREFIX}tokazerk.json")
            {
                hasSettingsFile = true;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var target = fileSystem.Path.GetFullPath(
                fileSystem.Path.Combine(normalizedCustomPath, entry.FullName[CUSTOM_PREFIX.Length..]));
            if (!target.StartsWith(normalizedCustomPath + fileSystem.Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"The UI archive entry is outside the custom directory: {entry.FullName}");
            }
        }

        if (!hasAnyEntry || !hasCustomRoot)
        {
            throw new InvalidDataException("The UI archive does not have a 'custom/' root");
        }

        if (!hasSettingsFile)
        {
            throw new InvalidDataException("The UI archive is missing custom/tokazerk.json");
        }
    }

    public void DiscardDownload(string downloadPath)
    {
        if (fileSystem.File.Exists(downloadPath))
        {
            fileSystem.File.Delete(downloadPath);
        }
    }

    public Task InstallAsync(string zipPath, string customPath, CancellationToken ct)
    {
        this.Verify(zipPath, customPath);

        return Task.Run(
            () =>
            {
                try
                {
                    var normalizedCustomPath = fileSystem.Path.GetFullPath(customPath).TrimEnd('\\', '/');
                    fileSystem.Directory.CreateDirectory(normalizedCustomPath);
                    this.ClearDirectoryContents(normalizedCustomPath);

                    using var archive = ZipFile.OpenRead(zipPath);
                    foreach (var entry in archive.Entries)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        var relativePath = entry.FullName[CUSTOM_PREFIX.Length..];
                        var target = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(normalizedCustomPath, relativePath));
                        if (!target.StartsWith(normalizedCustomPath + fileSystem.Path.DirectorySeparatorChar, StringComparison.Ordinal))
                        {
                            throw new InvalidDataException($"The UI archive entry is outside the custom directory: {entry.FullName}");
                        }

                        var targetDirectory = fileSystem.Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(targetDirectory))
                        {
                            fileSystem.Directory.CreateDirectory(targetDirectory);
                        }

                        entry.ExtractToFile(target, overwrite: true);
                    }
                }
                finally
                {
                    if (fileSystem.File.Exists(zipPath))
                    {
                        fileSystem.File.Delete(zipPath);
                    }
                }
            },
            ct);
    }

    private void ClearDirectoryContents(string normalizedCustomPath)
    {
        foreach (var filePath in fileSystem.Directory.GetFiles(normalizedCustomPath))
        {
            fileSystem.File.Delete(filePath);
        }

        foreach (var directoryPath in fileSystem.Directory.GetDirectories(normalizedCustomPath))
        {
            fileSystem.Directory.Delete(directoryPath, recursive: true);
        }
    }

    private void ValidateArchiveTree(string normalizedCustomPath, CancellationToken ct)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(normalizedCustomPath);

        while (pendingDirectories.TryPop(out var directoryPath))
        {
            ct.ThrowIfCancellationRequested();
            this.ValidateEntry(normalizedCustomPath, directoryPath);

            foreach (var entryPath in fileSystem.Directory.EnumerateFileSystemEntries(directoryPath))
            {
                ct.ThrowIfCancellationRequested();
                var normalizedEntryPath = fileSystem.Path.GetFullPath(entryPath);
                var attributes = this.ValidateEntry(normalizedCustomPath, normalizedEntryPath);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pendingDirectories.Push(normalizedEntryPath);
                }
            }
        }
    }

    private FileAttributes ValidateEntry(string normalizedCustomPath, string entryPath)
    {
        var relativePath = fileSystem.Path.GetRelativePath(normalizedCustomPath, entryPath);
        if (fileSystem.Path.IsPathRooted(relativePath)
            || relativePath == ".."
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || relativePath.StartsWith(@"..\", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The UI archive entry is outside the custom directory: {entryPath}");
        }

        var attributes = fileSystem.File.GetAttributes(entryPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"The UI archive cannot contain a reparse point: {entryPath}");
        }

        return attributes;
    }
}
