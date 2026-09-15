using System.IO.Abstractions;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class VersionInfoReader : IUiVersionReader
{
    private readonly IFileSystem _fileSystem;

    public VersionInfoReader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<SemVer?> ReadAsync(string customPath, CancellationToken ct)
    {
        var path = _fileSystem.Path.Combine(customPath, "versioninfo.txt");
        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        try
        {
            var lines = await _fileSystem.File.ReadAllLinesAsync(path, ct).ConfigureAwait(false);
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
