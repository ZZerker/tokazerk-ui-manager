using System.IO.Abstractions;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class FileVariantStore : IVariantStore
{
    private readonly IFileSystem _fileSystem;
    private readonly IBackupStore _backupStore;

    public FileVariantStore(IFileSystem fileSystem, IBackupStore backupStore)
    {
        _fileSystem = fileSystem;
        _backupStore = backupStore;
    }

    public async Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct)
    {
        await _backupStore.BackupOnceAsync(customPath, variant.TargetPath, ct).ConfigureAwait(false);

        if (choice.SourcePath is null)
        {
            await _backupStore.RestoreAsync(customPath, variant.TargetPath, variant.Preserve, ct).ConfigureAwait(false);
            return;
        }

        var source = _fileSystem.Path.Combine(customPath, choice.SourcePath);
        var target = _fileSystem.Path.Combine(customPath, variant.TargetPath);

        if (_fileSystem.Directory.Exists(source))
        {
            await Task.Run(() => DirectoryCopy.Copy(_fileSystem, source, target, variant.Preserve), ct).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(
                () =>
                {
                    _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(target)!);
                    _fileSystem.File.Copy(source, target, overwrite: true);
                },
                ct).ConfigureAwait(false);
        }
    }
}
