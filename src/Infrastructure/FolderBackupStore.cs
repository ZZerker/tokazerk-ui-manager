using System.IO.Abstractions;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class FolderBackupStore : IBackupStore
{
    private readonly IFileSystem _fileSystem;

    public FolderBackupStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task BackupOnceAsync(string customPath, string relativePath, CancellationToken ct)
    {
        var source = _fileSystem.Path.Combine(customPath, relativePath);
        var backup = BackupPath(customPath, relativePath);

        if (_fileSystem.Directory.Exists(backup) || _fileSystem.File.Exists(backup))
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                if (_fileSystem.Directory.Exists(source))
                {
                    DirectoryCopy.Copy(_fileSystem, source, backup);
                }
                else if (_fileSystem.File.Exists(source))
                {
                    _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(backup)!);
                    _fileSystem.File.Copy(source, backup);
                }
            },
            ct);
    }

    public Task<bool> RestoreAsync(string customPath, string relativePath, IReadOnlyList<string> preserve, CancellationToken ct)
    {
        var target = _fileSystem.Path.Combine(customPath, relativePath);
        var backup = BackupPath(customPath, relativePath);

        if (_fileSystem.Directory.Exists(backup))
        {
            return Task.Run(
                () =>
                {
                    DirectoryCopy.Copy(_fileSystem, backup, target, preserve);
                    return true;
                },
                ct);
        }

        if (_fileSystem.File.Exists(backup))
        {
            return Task.Run(
                () =>
                {
                    _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(target)!);
                    _fileSystem.File.Copy(backup, target, overwrite: true);
                    return true;
                },
                ct);
        }

        return Task.FromResult(false);
    }

    private string BackupPath(string customPath, string relativePath) =>
        _fileSystem.Path.Combine(customPath, ConfigPaths.BackupDir, relativePath);
}
