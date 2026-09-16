using System.IO.Abstractions;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class FolderBackupStore(IFileSystem fileSystem) : IBackupStore
{
    public Task BackupOnceAsync(string customPath, string relativePath, CancellationToken ct)
    {
        var source = fileSystem.Path.Combine(customPath, relativePath);
        var backup = this.BackupPath(customPath, relativePath);

        if (fileSystem.Directory.Exists(backup) || fileSystem.File.Exists(backup))
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                if (fileSystem.Directory.Exists(source))
                {
                    DirectoryCopy.Copy(fileSystem, source, backup);
                }
                else if (fileSystem.File.Exists(source))
                {
                    fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(backup)!);
                    fileSystem.File.Copy(source, backup);
                }
            },
            ct);
    }

    public Task<bool> RestoreAsync(string customPath, string relativePath, IReadOnlyList<string> preserve, CancellationToken ct)
    {
        var target = fileSystem.Path.Combine(customPath, relativePath);
        var backup = this.BackupPath(customPath, relativePath);

        if (fileSystem.Directory.Exists(backup))
        {
            return Task.Run(
                () =>
                {
                    DirectoryCopy.Copy(fileSystem, backup, target, preserve);
                    return true;
                },
                ct);
        }

        if (fileSystem.File.Exists(backup))
        {
            return Task.Run(
                () =>
                {
                    fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(target)!);
                    fileSystem.File.Copy(backup, target, overwrite: true);
                    return true;
                },
                ct);
        }

        return Task.FromResult(false);
    }

    private string BackupPath(string customPath, string relativePath) => fileSystem.Path.Combine(customPath, ConfigPaths.BACKUP_DIR, relativePath);
}
