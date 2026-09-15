using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using TokaZerkUIConfig.Domain;
using TokaZerkUIConfig.Domain.Ports;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class AssetsXmlFontStore : IFontDefinitionStore
{
    private static readonly IReadOnlyDictionary<string, Regex> DefinitionRegexes =
        Enum.GetValues<FontTier>()
            .SelectMany(FontTierInfo.DefinitionNames)
            .ToDictionary(name => name, BuildDefinitionRegex);

    private readonly IFileSystem _fileSystem;

    public AssetsXmlFontStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<FontSettings> ReadAsync(string customPath, CancellationToken ct)
    {
        var text = await ReadTextAsync(customPath, ct).ConfigureAwait(false);

        var missing = new List<string>();
        var settings = FontSettings.Default;

        foreach (var tier in Enum.GetValues<FontTier>())
        {
            var name = FontTierInfo.DefinitionNames(tier)[0];
            var match = DefinitionRegexes[name].Match(text);
            if (!match.Success)
            {
                missing.Add(name);
                continue;
            }

            settings = settings.With(tier, int.Parse(match.Groups["height"].Value));
        }

        if (missing.Count > 0)
        {
            throw new FontDefinitionsNotFoundException(missing);
        }

        return settings;
    }

    public async Task WriteAsync(string customPath, FontSettings settings, CancellationToken ct)
    {
        var text = await ReadTextAsync(customPath, ct).ConfigureAwait(false);

        var missing = new List<string>();

        foreach (var tier in Enum.GetValues<FontTier>())
        {
            var px = settings.Get(tier);
            foreach (var name in FontTierInfo.DefinitionNames(tier))
            {
                var found = 0;
                text = DefinitionRegexes[name].Replace(
                    text,
                    m =>
                    {
                        found++;
                        return m.Groups["prefix"].Value + px + m.Groups["suffix"].Value;
                    });

                if (found == 0)
                {
                    missing.Add(name);
                }
            }
        }

        if (missing.Count > 0)
        {
            throw new FontDefinitionsNotFoundException(missing);
        }

        var path = AssetsXmlPath(customPath);
        var tempPath = path + ".tmp";
        var bytes = Encoding.Latin1.GetBytes(text);
        await _fileSystem.File.WriteAllBytesAsync(tempPath, bytes, ct).ConfigureAwait(false);
        _fileSystem.File.Move(tempPath, path, overwrite: true);
    }

    private async Task<string> ReadTextAsync(string customPath, CancellationToken ct)
    {
        var path = AssetsXmlPath(customPath);
        var bytes = await _fileSystem.File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        return Encoding.Latin1.GetString(bytes);
    }

    private string AssetsXmlPath(string customPath) => _fileSystem.Path.Combine(customPath, "assets.xml");

    // Regex is built per name (escapedName is not a compile-time constant), so it cannot use
    // [GeneratedRegex]; the pattern captures prefix/height/suffix so writes touch only the digits.
    private static Regex BuildDefinitionRegex(string name)
    {
        var escapedName = Regex.Escape(name);
        return new Regex(
            $@"(?<prefix><TTFFont>\s*<Name>{escapedName}</Name>(?:(?!</TTFFont>).)*?<Height>)(?<height>\d+)(?<suffix></Height>)",
            RegexOptions.Singleline);
    }
}
