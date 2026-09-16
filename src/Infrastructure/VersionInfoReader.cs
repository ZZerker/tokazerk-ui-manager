using System.IO.Abstractions;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class VersionInfoReader(IFileSystem fileSystem) : IUiVersionReader
{
    public async Task<SemVer?> ReadAsync(string customPath, CancellationToken ct)
    {
        var path = fileSystem.Path.Combine(customPath, "versioninfo.txt");
        if (!fileSystem.File.Exists(path))
        {
            return null;
        }

        try
        {
            var lines = await fileSystem.File.ReadAllLinesAsync(path, ct).ConfigureAwait(false);
            return lines.Length == 0 ? null : SemVer.FindInText(lines[0]);
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
}
