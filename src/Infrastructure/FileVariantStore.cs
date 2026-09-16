using System.IO.Abstractions;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class FileVariantStore(IFileSystem fileSystem, IBackupStore backupStore) : IVariantStore
{
    public async Task ApplyAsync(string customPath, Variant variant, VariantChoice choice, CancellationToken ct)
    {
        await backupStore.BackupOnceAsync(customPath, variant.TargetPath, ct).ConfigureAwait(false);

        if (choice.SourcePath is null)
        {
            await backupStore.RestoreAsync(customPath, variant.TargetPath, variant.Preserve, ct).ConfigureAwait(false);
            return;
        }

        var source = fileSystem.Path.Combine(customPath, choice.SourcePath);
        var target = fileSystem.Path.Combine(customPath, variant.TargetPath);

        if (fileSystem.Directory.Exists(source))
        {
            await Task.Run(() => DirectoryCopy.Copy(fileSystem, source, target, variant.Preserve), ct).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(
                () =>
                {
                    fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(target)!);
                    fileSystem.File.Copy(source, target, overwrite: true);
                },
                ct).ConfigureAwait(false);
        }
    }
}
