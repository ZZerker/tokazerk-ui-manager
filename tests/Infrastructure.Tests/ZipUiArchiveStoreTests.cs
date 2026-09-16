using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using TokaZerkUIConfig.Infrastructure;
using Xunit;

namespace TokaZerkUIConfig.Infrastructure.Tests;

public class ZipUiArchiveStoreTests
{
    [Fact]
    public async Task ArchiveAsyncCreatesCollisionSafeArchiveContainingEveryFileAtRoot()
    {
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"TokaZerkUIConfig-{Guid.NewGuid():N}"));
        try
        {
            var customPath = Path.Combine(tempRoot, "ui", "custom");
            var nestedPath = Path.Combine(customPath, "Options", "TargetWindow");
            Directory.CreateDirectory(nestedPath);
            await File.WriteAllTextAsync(
                Path.Combine(customPath, "tokazerk.json"),
                """{"name":"TokaZerkUI","version":"1.0.12","built":"2026-09-15"}""");
            await File.WriteAllTextAsync(Path.Combine(customPath, "assets.xml"), "assets");
            await File.WriteAllTextAsync(Path.Combine(nestedPath, "custom2_window.xml"), "window");

            var fileSystem = new FileSystem();
            var reader = new TokazerkJsonReader(fileSystem);
            var store = new ZipUiArchiveStore(fileSystem, reader);
            var occupiedArchivePath = Path.Combine(tempRoot, "ui", "backups", "custom-tokazerk-1.0.12.zip");
            Directory.CreateDirectory(occupiedArchivePath);

            var firstArchivePath = await store.ArchiveAsync(customPath, CancellationToken.None);
            var secondArchivePath = await store.ArchiveAsync(customPath, CancellationToken.None);

            Assert.NotEqual(firstArchivePath, secondArchivePath);
            Assert.True(File.Exists(firstArchivePath));
            Assert.True(File.Exists(secondArchivePath));
            Assert.True(Directory.Exists(occupiedArchivePath));
            Assert.Equal("custom-tokazerk-1.0.12-2.zip", Path.GetFileName(firstArchivePath));
            Assert.Equal("custom-tokazerk-1.0.12-3.zip", Path.GetFileName(secondArchivePath));

            var expectedEntries = Directory.GetFiles(customPath, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(customPath, path).Replace('\\', '/'))
                .Order()
                .ToArray();
            using var archive = ZipFile.OpenRead(firstArchivePath);
            var actualEntries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry => entry.FullName)
                .Order()
                .ToArray();

            Assert.Equal(expectedEntries, actualEntries);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ArchiveAsyncRejectsReparsePointBeforeCreatingArchive()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(
            "custom/tokazerk.json",
            new MockFileData(
                """{"name":"TokaZerkUI","version":"1.0.12","built":"2026-09-15"}"""));
        fileSystem.AddFile(
            "custom/link",
            new MockFileData("target")
            {
                Attributes = FileAttributes.ReparsePoint,
            });
        var reader = new TokazerkJsonReader(fileSystem);
        var store = new ZipUiArchiveStore(fileSystem, reader);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => store.ArchiveAsync("custom", CancellationToken.None));

        Assert.Contains("reparse point", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
