using System.IO.Abstractions;

namespace TokaZerkUIConfig.Infrastructure;

internal static class DirectoryCopy
{
    public static void Copy(IFileSystem fs, string source, string target, IReadOnlyList<string>? preserve = null)
    {
        fs.Directory.CreateDirectory(target);

        foreach (var sourceFile in fs.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = fs.Path.GetRelativePath(source, sourceFile);

            if (preserve is { Count: > 0 })
            {
                var topLevel = relative.Split(fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar)[0];
                if (preserve.Contains(topLevel, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var targetFile = fs.Path.Combine(target, relative);
            fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(targetFile)!);
            fs.File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }
}
