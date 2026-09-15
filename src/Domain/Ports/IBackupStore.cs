namespace TokaZerkUIConfig.Domain.Ports;

public interface IBackupStore
{
    Task BackupOnceAsync(string customPath, string relativePath, CancellationToken ct);

    Task<bool> RestoreAsync(string customPath, string relativePath, IReadOnlyList<string> preserve, CancellationToken ct);
}
