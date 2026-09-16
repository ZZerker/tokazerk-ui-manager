using System.Diagnostics;
using System.IO.Abstractions;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class ExeSwapSelfUpdater(IFileSystem fileSystem) : ISelfUpdater
{
    public string DownloadPath =>
        (Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unknown")) + ".new";

    public Task<bool> ApplyAsync(string downloadedFile, CancellationToken ct)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unknown");
        var old = exe + ".old";
        var moved1 = false;
        var moved2 = false;

        try
        {
            if (fileSystem.File.Exists(old))
            {
                fileSystem.File.Delete(old);
            }

            // Windows will not let a running exe be overwritten, but it will let it be renamed,
            // so the swap goes through .old and the new file takes the original name.
            fileSystem.File.Move(exe, old);
            moved1 = true;
            fileSystem.File.Move(downloadedFile, exe);
            moved2 = true;

            if (!OperatingSystem.IsWindows())
            {
                // SHORTCUT: IFileSystem 22.0.15 has no SetUnixFileMode wrapper, so this goes through
                // System.IO.File directly instead of the testable abstraction.
                File.SetUnixFileMode(
                    exe,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                    | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            using var process = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false });
            if (process is null)
            {
                this.RollBack(moved1, moved2, exe, old, downloadedFile);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.RollBack(moved1, moved2, exe, old, downloadedFile);
            return Task.FromResult(false);
        }
    }

    private void RollBack(bool moved1, bool moved2, string exe, string old, string downloadedFile)
    {
        if (moved2)
        {
            fileSystem.File.Move(exe, downloadedFile);
        }

        if (moved1)
        {
            fileSystem.File.Move(old, exe);
        }
    }

    public void CleanupOnStart()
    {
        var old = (Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unknown")) + ".old";
        try
        {
            if (fileSystem.File.Exists(old))
            {
                fileSystem.File.Delete(old);
            }
        }
        catch (IOException)
        {
            // The old process may still be shutting down and holding the file.
        }
    }
}
